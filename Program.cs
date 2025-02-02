using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace Onllama.GGUFLinkOut
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var modelPath = Environment.GetEnvironmentVariable("OLLAMA_MODELS") ??
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ollama",
                                "models");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                Directory.Exists("/usr/share/ollama/.ollama/models") &&
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OLLAMA_MODELS")))
                modelPath = "/usr/share/ollama/.ollama/models";

            if (!Directory.Exists("OllamaGGUFs")) Directory.CreateDirectory("OllamaGGUFs");

            var manifestsPaths = TraverseFolder(Path.Combine(modelPath, "manifests"));
            foreach (var item in manifestsPaths)
            {
                var jobj = JsonNode.Parse(File.ReadAllText(item));
                var digest = jobj?["layers"]?.AsArray()
                    .First(x => x != null && x["mediaType"]!.ToString().Equals("application/vnd.ollama.image.model"))?
                    ["digest"].ToString().Replace(":", "-");

                if (!File.Exists(Path.Combine(modelPath, "blobs", digest))) continue;
                var name = string.Join("-", item.Split('/', '\\').TakeLast(3)).TrimStartString("library-");
                Console.WriteLine(name + ":" + digest);
                try
                {
                    File.CreateSymbolicLink($"./OllamaGGUFs/{name}.gguf", Path.Combine(modelPath, "blobs", digest));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
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
