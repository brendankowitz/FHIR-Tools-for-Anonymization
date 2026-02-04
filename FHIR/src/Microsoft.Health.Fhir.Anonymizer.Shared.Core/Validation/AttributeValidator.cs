using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Hl7.Fhir.Model;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Validation
{
    public class AttributeValidator
    {
        private static readonly Type? s_validationType;
        private static readonly MethodInfo? s_validateMethod;

        static AttributeValidator()
        {
            // Try to find DotNetAttributeValidation type using reflection
            // This type exists in R4/STU3 but not in R5
            var validationAssembly = typeof(Resource).Assembly;
            s_validationType = validationAssembly.GetType("Hl7.Fhir.Validation.DotNetAttributeValidation");
            
            if (s_validationType != null)
            {
                s_validateMethod = s_validationType.GetMethod(
                    "TryValidate",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(object), typeof(ICollection<ValidationResult>), typeof(bool) },
                    null);
            }
        }

        public IEnumerable<ValidationResult> Validate(Resource resource)
        {
            var result = new List<ValidationResult>();
            
            // Use reflection to call DotNetAttributeValidation.TryValidate if available
            // For R5, this method doesn't exist, so we skip attribute validation
            if (s_validateMethod != null)
            {
                s_validateMethod.Invoke(null, new object[] { resource, result, true });
            }
            
            return result;
        }
    }
}
