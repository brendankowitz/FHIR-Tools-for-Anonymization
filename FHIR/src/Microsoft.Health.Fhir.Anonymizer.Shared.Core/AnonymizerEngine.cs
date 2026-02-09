using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.FhirPath;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Microsoft.Health.Fhir.Anonymizer.Core.Extensions;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Microsoft.Health.Fhir.Anonymizer.Core.Validation;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core
{
    public class AnonymizerEngine
    {
        private readonly AnonymizerConfigurationManager _configurationManager;
        private readonly Dictionary<string, IAnonymizerProcessor> _processors;
        private readonly AnonymizationFhirPathRule[] _rules;
        private readonly IStructureDefinitionSummaryProvider _provider = new PocoStructureDefinitionSummaryProvider();
        private readonly ResourceValidator _validator = new ResourceValidator();
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerEngine>();
        private readonly IAnonymizerProcessorFactory _customProcessorFactory;

        public static void InitializeFhirPathExtensionSymbols()
        {
            FhirPathCompiler.DefaultSymbolTable.AddExtensionSymbols();
        }

        public AnonymizerEngine(string configFilePath, IAnonymizerProcessorFactory customProcessorFactory = null) : this(AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath), customProcessorFactory)
        {
            
        }

        public AnonymizerEngine(AnonymizerConfigurationManager configurationManager, IAnonymizerProcessorFactory customProcessorFactory = null)
        {
            _configurationManager = configurationManager;
            _processors = new Dictionary<string, IAnonymizerProcessor>();

            _customProcessorFactory = customProcessorFactory;

            InitializeProcessors(_configurationManager);

            _rules = _configurationManager.FhirPathRules;
            _logger.LogDebug("AnonymizerEngine initialized successfully");
        }

        public static AnonymizerEngine CreateWithFileContext(string configFilePath, string fileName, string inputFolderName, IAnonymizerProcessorFactory customProcessorFactory = null)
        {
            var configurationManager = AnonymizerConfigurationManager.CreateFromConfigurationFile(configFilePath);
            var dateShiftScope = configurationManager.GetParameterConfiguration().DateShiftScope;
            var dateShiftKeyPrefix = dateShiftScope switch
            {
                DateShiftScope.File => Path.GetFileName(fileName),
                DateShiftScope.Folder => Path.GetFileName(inputFolderName.TrimEnd('\\', '/')),
                _ => string.Empty
            };

            configurationManager.SetDateShiftKeyPrefix(dateShiftKeyPrefix);
            return new AnonymizerEngine(configurationManager, customProcessorFactory);
        }

        public ITypedElement AnonymizeElement(ITypedElement element, AnonymizerSettings settings = null)
        {
            EnsureArg.IsNotNull(element, nameof(element));
            try
            {
                ElementNode resourceNode = ParseTypedElementToElementNode(element);
                return resourceNode.Anonymize(_rules, _processors);
            }
            catch (AnonymizerProcessingException)
            {
                if(_configurationManager.Configuration.processingErrors == ProcessingErrorsOption.Skip)
                {
                    // Return empty resource.
                    return new EmptyElement(element.InstanceType);
                }

                throw;
            }
            catch (CryptographicException cryptoEx)
            {
                // SECURITY: Allow cryptographic exceptions to propagate with original type
                // These indicate serious security failures that must not be masked
                _logger.LogError(cryptoEx, 
                    "[SECURITY] Cryptographic operation failed during anonymization. " +
                    "This may indicate corrupted keys, tampering, or implementation errors.");
                throw;
            }
            catch (InvalidOperationException invalidOpEx) when (invalidOpEx.Message.Contains("budget"))
            {
                // SECURITY: Allow privacy budget violations to propagate with specific type
                // Masking these could lead to privacy breaches
                _logger.LogError(invalidOpEx,
                    "[SECURITY] Privacy budget violation during anonymization. " +
                    "Operation denied to prevent epsilon exhaustion.");
                throw;
            }
            catch (Exception ex)
            {
                // Log all other exceptions but allow them to propagate
                _logger.LogError(ex, "Exception occurred during element anonymization");
                throw;
            }
        }

        public Resource AnonymizeResource(Resource resource, AnonymizerSettings settings = null)
        {
            EnsureArg.IsNotNull(resource, nameof(resource));

            try
            {
                ValidateInput(settings, resource);
                var anonymizedResource = AnonymizeElement(resource.ToTypedElement()).ToPoco<Resource>();
                ValidateOutput(settings, anonymizedResource);
               
                return anonymizedResource;
            }
            catch (Exception ex)
            {
                HandleAnonymizationException(ex, "resource anonymization");
                throw; // Never reached, but required for compiler
            }
        }

        public string AnonymizeJson(string json, AnonymizerSettings settings = null)
        {
            EnsureArg.IsNotNullOrWhiteSpace(json, nameof(json));

            try
            {
                var element = ParseJsonToTypedElement(json);
                if (settings != null && settings.ValidateInput)
                {
                    ValidateInput(element);
                }

                var anonymizedElement = AnonymizeElement(element);

                if (settings != null && settings.ValidateOutput)
                {
                    ValidateOutput(anonymizedElement);
                }

                return anonymizedElement.ToJson();
            }
            catch (Exception ex)
            {
                HandleAnonymizationException(ex, "JSON anonymization");
                throw; // Never reached, but required for compiler
            }
        }

        /// <summary>
        /// Centralized exception handling for anonymization operations.
        /// Preserves security-relevant exceptions (cryptographic, privacy budget, validation)
        /// with their original types and context. Wraps other exceptions for clarity.
        /// </summary>
        /// <param name="ex">The exception to handle</param>
        /// <param name="operationContext">Description of the operation for logging (e.g., "JSON anonymization")</param>
        private void HandleAnonymizationException(Exception ex, string operationContext)
        {
            // SECURITY: Allow input validation exceptions to propagate unchanged
            // These indicate data quality issues, not security failures
            if (ex is InvalidInputException)
            {
                throw ex;
            }

            // SECURITY: Allow cryptographic exceptions to propagate with original type
            // These indicate serious security failures (corrupted keys, tampering, implementation errors)
            if (ex is CryptographicException)
            {
                _logger.LogError(ex, $"[SECURITY] Cryptographic operation failed during {operationContext}. " +
                                     "This may indicate corrupted keys, tampering, or implementation errors.");
                throw ex;
            }

            // SECURITY: Allow privacy budget violations to propagate with specific type
            // Masking these could lead to privacy breaches by hiding epsilon exhaustion
            if (ex is InvalidOperationException invalidOpEx && invalidOpEx.Message.Contains("budget"))
            {
                _logger.LogError(ex, $"[SECURITY] Privacy budget violation during {operationContext}. " +
                                     "Operation denied to prevent epsilon exhaustion.");
                throw ex;
            }

            // SECURITY: Allow ArgumentException for configuration/parameter validation to propagate
            // These often indicate security-relevant misconfigurations (weak keys, invalid privacy parameters)
            if (ex is ArgumentException)
            {
                _logger.LogError(ex, $"[SECURITY] Invalid argument during {operationContext}. " +
                                     "This may indicate security-relevant configuration issues.");
                throw ex;
            }

            // Application-specific processing exceptions should propagate with their type preserved
            if (ex is AnonymizerProcessingException)
            {
                _logger.LogError(ex, $"Processing error during {operationContext}");
                throw ex;
            }

            // Wrap all other exceptions to provide context while preserving the inner exception
            _logger.LogError(ex, $"Unexpected exception during {operationContext}");
            throw new AnonymizerOperationException($"Anonymization failed during {operationContext}", ex);
        }

        private void InitializeProcessors(AnonymizerConfigurationManager configurationManager)
        {
            _processors[AnonymizerMethod.DateShift.ToString().ToUpperInvariant()] = DateShiftProcessor.Create(configurationManager);
            _processors[AnonymizerMethod.Redact.ToString().ToUpperInvariant()] = RedactProcessor.Create(configurationManager);
            _processors[AnonymizerMethod.CryptoHash.ToString().ToUpperInvariant()] = new CryptoHashProcessor(configurationManager.GetParameterConfiguration().CryptoHashKey);
            _processors[AnonymizerMethod.Encrypt.ToString().ToUpperInvariant()] = new EncryptProcessor(configurationManager.GetParameterConfiguration().EncryptKey);
            _processors[AnonymizerMethod.Substitute.ToString().ToUpperInvariant()] = new SubstituteProcessor();
            _processors[AnonymizerMethod.Perturb.ToString().ToUpperInvariant()] = new PerturbProcessor();
            _processors[AnonymizerMethod.Keep.ToString().ToUpperInvariant()] = new KeepProcessor();
            _processors[AnonymizerMethod.Generalize.ToString().ToUpperInvariant()] = new GeneralizeProcessor();
            _processors[AnonymizerMethod.KAnonymity.ToString().ToUpperInvariant()] = new KAnonymityProcessor();
            _processors[AnonymizerMethod.DifferentialPrivacy.ToString().ToUpperInvariant()] = new DifferentialPrivacyProcessor();
            if (_customProcessorFactory != null)
            {
                InitializeCustomProcessors(configurationManager);
            }
        }

        private void InitializeCustomProcessors(AnonymizerConfigurationManager configurationManager)
        {
            var processors = _customProcessorFactory.GetType().GetField("_customProcessors", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_customProcessorFactory) as Dictionary<string, Type>;
            foreach(var processor in processors)
            {
                _processors[processor.Key.ToUpperInvariant()] = _customProcessorFactory.CreateProcessor(processor.Key, configurationManager.GetParameterConfiguration().CustomSettings);
            }
        }

        private ITypedElement ParseJsonToTypedElement(string json)
        {
            try
            {
                return FhirJsonNode.Parse(json).ToTypedElement(_provider);
            }
            catch (Exception ex)
            {
                throw new InvalidInputException($"The input FHIR resource JSON is invalid.", ex);
            }
        }

        private static ElementNode ParseTypedElementToElementNode(ITypedElement element)
        {
            try
            {
                return ElementNode.FromElement(element);
            }
            catch (Exception ex)
            {
                throw new InvalidInputException("The input FHIR resource is invalid", ex);
            }
        }

        private void ValidateInput(AnonymizerSettings settings, Resource resource)
        {
            if (settings != null && settings.ValidateInput)
            {
                ValidateInput(resource.ToTypedElement());
            }
        }

        private void ValidateInput(ITypedElement element)
        {
            _logger.LogDebug("Validating input resource...");

            _validator.Validate(element);
        }

        private void ValidateOutput(AnonymizerSettings settings, Resource resource)
        {
            if (settings != null && settings.ValidateOutput)
            {
                ValidateOutput(resource.ToTypedElement());
            }
        }

        private void ValidateOutput(ITypedElement element)
        {
            _logger.LogDebug("Validating anonymized resource...");

            _validator.Validate(element);
        }
    }
}