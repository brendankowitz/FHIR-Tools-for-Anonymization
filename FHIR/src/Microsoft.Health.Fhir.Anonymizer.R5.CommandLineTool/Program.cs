using Microsoft.Health.Fhir.Anonymizer.R5.Core;

namespace Microsoft.Health.Fhir.Anonymizer.R5.CommandLineTool
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return AnonymizerToolRunner.Main(args, Constants.SupportedVersion);
        }
    }
}
