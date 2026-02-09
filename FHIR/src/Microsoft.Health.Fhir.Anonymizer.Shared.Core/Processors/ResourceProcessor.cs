// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Utility;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class ResourceProcessor
    {
        private static readonly IStructureDefinitionSummaryProvider s_provider = ModelInfoProvider.Instance;
        private readonly string _metaNodeName = "meta";
        private readonly Dictionary<AnonymizerMethod, IAnonymizerProcessor> _processors;
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<ResourceProcessor>();
        private Dictionary<string, List<ITypedElement>> _typeToNodeLookUp;
        private Dictionary<string, List<ITypedElement>> _nameToNodeLookUp;

        public ResourceProcessor(Dictionary<AnonymizerMethod, IAnonymizerProcessor> processors)
        {
            _processors = processors;
        }

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            EnsureArg.IsNotNull(node, nameof(node));

            if (context == null)
            {
                context = new ProcessContext();
            }

            var result = new ProcessResult();
            CreateNodeLookup(node);

            foreach (var rule in context.Rules)
            {
                var ruleContext = new ProcessContext { VisitedNodes = context.VisitedNodes, Rules = new List<AnonymizationFhirPathRule>() };
                var ruleResult = new ProcessResult();
                var method = Enum.Parse<AnonymizerMethod>(rule.Method, true);
                var matchNodes = GetMatchNodes(rule, node);

                foreach (var matchNode in matchNodes)
                {
                    if (!(matchNode is ElementNode matchElementNode))
                    {
                        var errorMessage = $"Failed to process rule {rule.Path}: matched node is not an ElementNode.";
                        _logger.LogWarning(errorMessage);
                        result.AddException(new AnonymizerOperationException(errorMessage));
                        continue;
                    }

                    if (method == AnonymizerMethod.Keep)
                    {
                        ruleContext.VisitedNodes.Add(matchElementNode);
                    }

                    ruleResult = AnonymizeNode(matchElementNode, method, rule, ruleContext, settings);
                    result.Update(ruleResult);
                }

                LogProcessResult(node, rule, ruleResult);
            }

            if (!context.Rules.Any(r => r.Method?.Equals("keep", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                TraverseNodeToRemoveMeta(node);
            }

            return result;
        }

        private void CreateNodeLookup(ElementNode node)
        {
            _typeToNodeLookUp = new Dictionary<string, List<ITypedElement>>();
            _nameToNodeLookUp = new Dictionary<string, List<ITypedElement>>();
            var nodes = node.Descendants().ToList();
            nodes.Add(node);

            foreach (var entry in nodes)
            {
                if (!string.IsNullOrEmpty(entry.InstanceType))
                {
                    if (!_typeToNodeLookUp.ContainsKey(entry.InstanceType))
                    {
                        _typeToNodeLookUp[entry.InstanceType] = new List<ITypedElement>();
                    }

                    _typeToNodeLookUp[entry.InstanceType].Add(entry);
                }

                if (!string.IsNullOrEmpty(entry.Name))
                {
                    if (!_nameToNodeLookUp.ContainsKey(entry.Name))
                    {
                        _nameToNodeLookUp[entry.Name] = new List<ITypedElement>();
                    }

                    _nameToNodeLookUp[entry.Name].Add(entry);
                }
            }
        }

        private ProcessResult AnonymizeNode(ElementNode node, AnonymizerMethod method, AnonymizationFhirPathRule rule, ProcessContext context, Dictionary<string, object> settings)
        {
            var processor = _processors[method];
            var processResult = processor.Process(node, context, settings);
            var resourceId = node.GetResourceId();

            foreach (var processRecord in processResult.ProcessRecords)
            {
                processRecord.ResourceId = resourceId;
            }

            return processResult;
        }

        private void TraverseNodeToRemoveMeta(ElementNode node)
        {
            if (node == null)
            {
                return;
            }

            if (node.Name.Equals(_metaNodeName, StringComparison.InvariantCultureIgnoreCase))
            {
                node.Remove(s_provider, node);
                return;
            }

            foreach (var child in node.Children().ToList())
            {
                TraverseNodeToRemoveMeta(child as ElementNode);
            }
        }

        private List<ITypedElement> GetMatchNodes(AnonymizationFhirPathRule rule, ElementNode node)
        {
            var pathCompiled = new Regex(@"^(?<resourceType>[A-Z][a-zA-Z]+)\.(?<expression>.+)", RegexOptions.Compiled);
            var typeCompiled = new Regex(@"^(?<type>[A-Z][a-zA-Z]+)::(?<expression>.+)", RegexOptions.Compiled);
            var nameCompiled = new Regex(@"^(?<name>[a-z][a-zA-Z]+)::(?<expression>.+)", RegexOptions.Compiled);

            var pathMatch = pathCompiled.Match(rule.Path);
            var typeMatch = typeCompiled.Match(rule.Path);
            var nameMatch = nameCompiled.Match(rule.Path);

            if (pathMatch.Success)
            {
                string resourceType = pathMatch.Groups["resourceType"].Value;
                string expression = pathMatch.Groups["expression"].Value;
                if (node.InstanceType.Equals(resourceType))
                {
                    return node.Select(expression, FhirPathExtensions.Nav).Cast<ITypedElement>().ToList();
                }
            }
            else if (typeMatch.Success)
            {
                string typeString = typeMatch.Groups["type"].Value;
                if (_typeToNodeLookUp.ContainsKey(typeString))
                {
                    var typeMatchNodes = _typeToNodeLookUp[typeString];
                    string expression = typeMatch.Groups["expression"].Value;
                    var result = new List<ITypedElement>();
                    foreach (var typeMatchNode in typeMatchNodes)
                    {
                        result.AddRange(typeMatchNode.Select(expression, FhirPathExtensions.Nav).Cast<ITypedElement>().ToList());
                    }

                    return result;
                }
            }
            else if (nameMatch.Success)
            {
                string nameString = nameMatch.Groups["name"].Value;
                if (_nameToNodeLookUp.ContainsKey(nameString))
                {
                    var nameMatchNodes = _nameToNodeLookUp[nameString];
                    string expression = nameMatch.Groups["expression"].Value;
                    var result = new List<ITypedElement>();
                    foreach (var nameMatchNode in nameMatchNodes)
                    {
                        result.AddRange(nameMatchNode.Select(expression, FhirPathExtensions.Nav).Cast<ITypedElement>().ToList());
                    }

                    return result;
                }
            }
            else
            {
                return node.Select(rule.Path, FhirPathExtensions.Nav).Cast<ITypedElement>().ToList();
            }

            return new List<ITypedElement>();
        }

        private void TraverseNode(ElementNode node)
        {
            if (node == null)
            {
                return;
            }

            foreach (var child in node.Children().ToList())
            {
                TraverseNode(child as ElementNode);
            }
        }

        private void LogProcessResult(ITypedElement node, AnonymizationFhirPathRule rule, ProcessResult resultOnRule)
        {
            if (resultOnRule == null || resultOnRule.ProcessRecords.Count == 0)
            {
                return;
            }

            foreach (var record in resultOnRule.ProcessRecords)
            {
                string resourceId = node.GetResourceId();
                _logger.LogInformation($"Anonymize {resourceId} at {rule.Path} : {record}");
            }
        }

        public void AddProcessor(AnonymizerMethod method, IAnonymizerProcessor processor)
        {
            EnsureArg.IsNotNull(method, nameof(method));
            EnsureArg.IsNotNull(processor, nameof(processor));

            if (!_processors.ContainsKey(method))
            {
                _processors[method] = processor;
            }
        }
    }
}
