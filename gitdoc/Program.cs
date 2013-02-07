using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitDoc
{
    class Program
    {
        private static GitDocArgs _options;

        private static readonly IDictionary<string, string> _tokenReplacements = new Dictionary<string, string>(); 

        static void Main(string[] args)
        {
            // Create options obj
            _options = Args.Configuration.Configure<GitDocArgs>().CreateAndBind(args);

            // run in current directory
            var currentDir = Environment.CurrentDirectory;

            // get all markdown files
            var markdownFiles = Directory.EnumerateFiles(
                currentDir, "*.md", SearchOption.AllDirectories);

            ConfigureTokens();

            // Process markdown files
            ParseMarkdownFiles(markdownFiles.ToArray()).Wait();
        }

        private static void ConfigureTokens()
        {
            // {date}
            _tokenReplacements.Add("{date}", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            
            // {author}
            _tokenReplacements.Add("{author}", Environment.UserName);
        }

        private async static Task ParseMarkdownFiles(string[] files)
        {
            var template = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template.html"));

            // Normalize paths
            var baseDir = Path.GetFullPath(_options.BaseDir) + "\\";
            var outputDir = Path.GetFullPath(Path.Combine(baseDir, _options.OutputDir));
            
            // Template
            if (String.IsNullOrWhiteSpace(template))
                throw new Exception("Couldn't find template");           

            // Get relatively-rooted list of directory paths
            var fileDirectories = (from file in files
                                   let truncatedPath = file.Replace(baseDir, "")
                                   select Path.GetDirectoryName(truncatedPath)).Distinct();

            // Make sure the target directories exist
            foreach (var dir in fileDirectories)
            {
                var path = Path.GetFullPath(Path.Combine(outputDir, dir));

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            // Process to output dir
            var gitClient = new GitClient();

            foreach (var file in files)
            {
                try
                {
                    var rawText = File.ReadAllText(file);

                    // Token replacement
                    foreach (var tr in _tokenReplacements)
                    {
                        rawText = rawText.Replace(tr.Key, tr.Value);
                    }

                    var contents = await gitClient.Markdown(rawText);
                    var subFolder = Path.GetDirectoryName(file.Replace(baseDir, "")) ?? "";
                    var outputName = Path.Combine(outputDir, subFolder, Path.GetFileNameWithoutExtension(file) + ".html");

                    contents = template.Replace("{body}", contents);                    

                    File.WriteAllText(outputName, contents);

                    Console.WriteLine("Processed file: {0}", file);
                }
                catch (Exception ex)
                {
                    throw new Exception("Couldn't process file: " + file, ex);                    
                }
            }
        }
    }

    internal class GitDocArgs
    {
        public GitDocArgs()
        {
            OutputDir = ".\\html";
            BaseDir = Environment.CurrentDirectory;
        }

        public string OutputDir { get; set; }
        public string BaseDir { get; set; }
    }
}
