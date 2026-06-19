using Microsoft.Extensions.Configuration;

namespace Brunsnik.SimpleWorker.Processing
{
    public class ProcessorContext
    {
        private const string RootDirKey = "ROOT_DIR";

        public string InputDirectory { get; init; }

        public string OutputDirectory { get; init; }

        public string ProcessedDirectory { get; init; }

        public ProcessorContext(IConfiguration configuration)
        {
            var rootDirectory = configuration[RootDirKey] ?? configuration["USERPROFILE"];
            InputDirectory = configuration["Conversion:InputFolder"]?.Replace($"{{{RootDirKey}}}", rootDirectory) ?? string.Empty;
            OutputDirectory = configuration["Conversion:OutputFolder"]?.Replace($"{{{RootDirKey}}}", rootDirectory) ?? string.Empty;
            ProcessedDirectory = configuration["Conversion:ProcessedFolder"]?.Replace($"{{{RootDirKey}}}", rootDirectory) ?? string.Empty;
        }
    }
}
