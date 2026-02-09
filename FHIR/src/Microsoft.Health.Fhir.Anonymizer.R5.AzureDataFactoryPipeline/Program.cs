using Microsoft.Health.Fhir.Anonymizer.R5.Core;

namespace Microsoft.Health.Fhir.Anonymizer.R5.AzureDataFactoryPipeline
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return AzureDataFactoryPipelineRunner.Main(args, Constants.SupportedVersion);
        }
    }
}
