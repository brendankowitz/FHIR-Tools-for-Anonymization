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
                var ruleContext = new ProcessContext { VisitedNodes = context.VisitedNodes, Rules = new List<AnonymizerFhirPathRule>() };
                var ruleResult = new ProcessResult();
                var method = Enum.Parse<AnonymizerMethod>(rule.Method, true);
                var matchNodes = GetMatchNodes(rule, node);

                foreach (var matchNode in matchNodes)
                {
                    if (context.VisitedNodes.Contains(matchNode))
                    {
                        continue;
                    }

                    context.VisitedNodes.Add(matchNode);
                    ruleResult.Update(ProcessNodeRecursive((ElementNode) matchNode.ToElement(), _processors[method], ruleContext, rule.RuleSettings));
                }

                LogProcessResult(node, rule, ruleResult);
                result.Update(ruleResult);
            }

            // Add security tags based on operations performed
            AddSecurityTag(node, result);

            return result;
        }

        public void AddSecurityTag(ElementNode node, ProcessResult result)
        {
            if (result == null || result.ProcessRecords.Count == 0)
            {
                return;
            }

            var metaNode = (ElementNode)node.GetMeta();
            var meta = metaNode?.ToPoco<Meta>() ?? new Meta();

            if (result.IsRedacted && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.REDACT.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.REDACT);
            }

            if (result.IsAbstracted && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.ABSTRED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.ABSTRED);
            }

            if (result.IsCryptoHashed && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.CRYTOHASH.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.CRYTOHASH);
            }

            if (result.IsEncrypted && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.MASKED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.MASKED);
            }

            if (result.IsPerturbed && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.PERTURBED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.PERTURBED);
            }

            if (result.IsSubstituted && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.SUBSTITUTED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.SUBSTITUTED);
            }

            if (result.IsGeneralized && !meta.Security.Any(x =>
                string.Equals(x.Code, SecurityLabels.GENERALIZED.Code, StringComparison.InvariantCultureIgnoreCase)))
            {
                meta.Security.Add(SecurityLabels.GENERALIZED);
            }

            var newMetaNode = ElementNode.FromElement(meta.ToTypedElement());
            if (metaNode == null)
            {
                node.Add(s_provider, newMetaNode, _metaNodeName);
            }
            else
            {
                metaNode.Replace(s_provider, newMetaNode);
            }
        }

        private List<ITypedElement> GetMatchNodes(AnonymizerFhirPathRule rule, ElementNode node)
        {
            var pathCompiled = new Regex(@"^(?<resourceType>[A-Z][a-zA-Z]+)\.(?<expression>.+)", RegexOptions.Compiled);
            var typeCompiled = new Regex(@"^(?<type>[A-Z][a-zA-Z]+)::(?<expression>.+)", RegexOptions.Compiled);
            var nameCompiled = new Regex(@"^(?<name>[a-z][a-zA-Z]+)::(?<expression>.+)", RegexOptions.Compiled);

            var pathMatch = pathCompiled.Match(rule.Path);
            var typeMatch = typeCompiled.Match(rule.Path);
            var nameMatch = nameCompiled.Match(rule.Path);

            if (pathMatch.Success)
            {
                var resourceType = pathMatch.Groups["resourceType"].Value;
                if (!node.InstanceType.Equals(resourceType, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new List<ITypedElement>();
                }

                var expression = pathMatch.Groups["expression"].Value;
                return node.Select(expression).Cast<ITypedElement>().ToList();
            }

            if (typeMatch.Success)
            {
                return GetMatchNodesFromLookUp(_typeToNodeLookUp, typeMatch.Groups["type"].Value, typeMatch.Groups["expression"].Value);
            }

            if (nameMatch.Success)
            {
                return GetMatchNodesFromLookUp(_nameToNodeLookUp, nameMatch.Groups["name"].Value, nameMatch.Groups["expression"].Value);
            }

            return new List<ITypedElement>();
        }

        private List<ITypedElement> GetMatchNodesFromLookUp(
            Dictionary<string, List<ITypedElement>> lookUp, string key, string expression)
        {
            if (string.IsNullOrEmpty(expression))
            {
                return lookUp.ContainsKey(key) ? lookUp[key] : new List<ITypedElement>();
            }

            var nodes = lookUp.ContainsKey(key) ? lookUp[key] : new List<ITypedElement>();
            var matchedNodes = new List<ITypedElement>();
            foreach (var node in nodes)
            {
                matchedNodes.AddRange(node.Select(expression).Cast<ITypedElement>());
            }

            return matchedNodes;
        }

        private void CreateNodeLookup(ITypedElement node)
        {
            _typeToNodeLookUp = new Dictionary<string, List<ITypedElement>>();
            _nameToNodeLookUp = new Dictionary<string, List<ITypedElement>>();
            TraverseNode(node);
        }

        private void TraverseNode(ITypedElement node)
        {
            if (node == null)
            {
                return;
            }

            var typeName = node.InstanceType;
            if (!_typeToNodeLookUp.ContainsKey(typeName))
            {
                _typeToNodeLookUp[typeName] = new List<ITypedElement>();
            }

            _typeToNodeLookUp[typeName].Add(node);

            var nodeName = node.Name;
            if (!string.IsNullOrEmpty(nodeName))
            {
                if (!_nameToNodeLookUp.ContainsKey(nodeName))
                {
                    _nameToNodeLookUp[nodeName] = new List<ITypedElement>();
                }

                _nameToNodeLookUp[nodeName].Add(node);
            }

            foreach (var child in node.Children())
            {
                TraverseNode(child);
            }
        }

        private void LogProcessResult(ITypedElement node, AnonymizerFhirPathRule rule, ProcessResult resultOnRule)
        {
            if (resultOnRule == null || resultOnRule.ProcessRecords.Count == 0)
            {
                return;
            }

            var resourceId = node.GetResourceId();
            var processRecordMap = resultOnRule.GetProcessRecordMap();
            
            foreach (var entry in processRecordMap)
            {
                var operation = entry.Key;
                var matchedNodes = entry.Value;
                
                if (matchedNodes != null && matchedNodes.Any())
                {
                    foreach (var matchNode in matchedNodes)
                    {
                        _logger.LogDebug($"[{resourceId}]: Rule '{rule.Path}' matches '{matchNode.Location}' and perform operation '{operation}'");
                    }
                }
            }
        }

        public ProcessResult ProcessNodeRecursive(ElementNode node, IAnonymizerProcessor processor, ProcessContext context, Dictionary<string, object> settings)
        {
            EnsureArg.IsNotNull(node, nameof(node));
            EnsureArg.IsNotNull(processor, nameof(processor));

            var result = new ProcessResult();
            result.Update(processor.Process(node, context, settings));

            foreach (var child in node.Children().Cast<ElementNode>().ToList())
            {
                result.Update(ProcessNodeRecursive((ElementNode)child, processor, context, settings));
            }

            return result;
        }
    }
}
