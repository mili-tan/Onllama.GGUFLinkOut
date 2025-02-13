using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using McMaster.Extensions.CommandLineUtils;

namespace Onllama.GGUFLinkOut
{
    internal class Program
    {
        public static string GGUFSPath  = Path.Combine(".", "OllamaGGUFs");
        public static string ModelPath = string.Empty;
        static void Main(string[] args)
        {
            var isZh = Thread.CurrentThread.CurrentCulture.Name.Contains("zh");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                Directory.Exists("/usr/share/ollama/.ollama/models") &&
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OLLAMA_MODELS")))
                ModelPath = "/usr/share/ollama/.ollama/models";

            ModelPath = Environment.GetEnvironmentVariable("OLLAMA_MODELS") ??
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ollama",
                            "models");

            var cmd = new CommandLineApplication
            {
                Name = "Onllama.GGUFsLinkOut",
                Description = $"Onllama.GGUFsLinkOut - {(isZh ? " 将 Ollama GGUF 模型文件软链接出，以便其他应用使用。" : "Create out symbolic links for the GGUF Models in Ollama Blobs.")}" +
                              Environment.NewLine +
                              $"Copyright (c) {DateTime.Now.Year} Milkey Tan. Code released under the MIT License"
            };
            cmd.HelpOption("-?|-h|--help|-he");

            var modelPathOption = cmd.Option<string>("-m|--model <path>",
                isZh ? "Ollama 模型文件路径。" : "Set ollama model path",
                CommandOptionType.SingleValue);
            var ggufsPathOption = cmd.Option<string>("-g|--ggufs <path>",
                isZh ? "GGUF 文件链接输出路径。" : "Set GGUFs link output path",
                CommandOptionType.SingleValue);

            cmd.OnExecute(() =>
            {
                if (modelPathOption.HasValue()) ModelPath = modelPathOption.ParsedValue;
                if (ggufsPathOption.HasValue()) GGUFSPath = ggufsPathOption.ParsedValue;
                if (!Directory.Exists(GGUFSPath)) Directory.CreateDirectory(GGUFSPath);

                var manifestsPaths = TraverseFolder(Path.Combine(ModelPath, "manifests"));
                foreach (var item in manifestsPaths)
                {
                    var jobj = JsonNode.Parse(File.ReadAllText(item));
                    var digest = jobj?["layers"]?.AsArray()
                        .First(x => x != null &&
                                    x["mediaType"]!.ToString().Equals("application/vnd.ollama.image.model"))?
                        ["digest"].ToString().Replace(":", "-");

                    if (!File.Exists(Path.Combine(ModelPath, "blobs", digest))) continue;
                    var name = string.Join("-", item.Split('/', '\\').TakeLast(3)).TrimStartString("library-");
                    Console.WriteLine(name + ":" + digest);
                    try
                    {
                        File.CreateSymbolicLink(Path.Combine(GGUFSPath, $"{name}.gguf"),
                            Path.Combine(ModelPath, "blobs", digest));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            });
        }

        static List<string> TraverseFolder(string folderPath)
        {
            var filePaths = new List<string>();

            try
            {
                var files = Directory.GetFiles(folderPath);
                filePaths.AddRange(files);

                var subFolders = Directory.GetDirectories(folderPath);

                foreach (string subFolder in subFolders) 
                    filePaths.AddRange(TraverseFolder(subFolder));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{folderPath}: {ex.Message}");
            }

            return filePaths;
        }
    }

    public static class StringExtensions
    {
        public static string TrimStartString(this string str, string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return str;
            return str.StartsWith(prefix) ? str.Substring(prefix.Length) : str;
        }
    }
}
