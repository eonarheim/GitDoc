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
            _tokenReplacements.Add("{date}", DateTime.Now.ToString("d/M/yy 'at' H:mm tt"));
            
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

                    rawText = _anchorInline.Replace(rawText, delegate(Match match)
                        {
                            var group = match.Groups[3]; // href
                            var href = group.Value;
                            Uri relativeUri;

                            // Try and create a relative URI (so absolute will be ignored)
                            if (Uri.TryCreate(href, UriKind.Relative, out relativeUri))
                            {
                                // See if the HREF is pointing to a Markdown file
                                if (Path.GetExtension(relativeUri.ToString()) == ".md")
                                {
                                    var newPath = Path.GetFileNameWithoutExtension(relativeUri.ToString()) + ".html";

                                    return String.Format("[{0}]({1})", match.Groups[2], newPath);
                                }
                            }

                            return match.ToString();
                        }
                    );

                    // Token replacement
                    foreach (var tr in _tokenReplacements)
                    {
                        rawText = rawText.Replace(tr.Key, tr.Value);
                    }

                    var contents = await gitClient.Markdown(rawText);
                    var subFolder = Path.GetDirectoryName(file.Replace(baseDir, "")) ?? "";
                    var outputName = Path.Combine(outputDir, subFolder, Path.GetFileNameWithoutExtension(file) + ".html");
                    
                    contents = template.Replace("{body}", contents);

                    // TODO it would be nice to find the first instance of an <h1> and use its value here
                    contents = contents.Replace("{title}", Path.GetFileNameWithoutExtension(file));

                    File.WriteAllText(outputName, contents);

                    Console.WriteLine("Processed file: {0}", file);
                }
                catch (Exception ex)
                {
                    throw new Exception("Couldn't process file: " + file, ex);                    
                }
            }
        }


        #region "Borrowed" From MarkdownSharp

        #region Copyright and license

        /*

Copyright (c) 2009 - 2010 Jeff Atwood

http://www.opensource.org/licenses/mit-license.php
  
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

Copyright (c) 2003-2004 John Gruber
<http://daringfireball.net/>   
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright
  notice, this list of conditions and the following disclaimer in the
  documentation and/or other materials provided with the distribution.

* Neither the name "Markdown" nor the names of its contributors may
  be used to endorse or promote products derived from this software
  without specific prior written permission.

This software is provided by the copyright holders and contributors "as
is" and any express or implied warranties, including, but not limited
to, the implied warranties of merchantability and fitness for a
particular purpose are disclaimed. In no event shall the copyright owner
or contributors be liable for any direct, indirect, incidental, special,
exemplary, or consequential damages (including, but not limited to,
procurement of substitute goods or services; loss of use, data, or
profits; or business interruption) however caused and on any theory of
liability, whether in contract, strict liability, or tort (including
negligence or otherwise) arising in any way out of the use of this
software, even if advised of the possibility of such damage.
*/

        #endregion

        /// <summary>
        /// maximum nested depth of [] and () supported by the transform; implementation detail
        /// </summary>
        private const int _nestDepth = 6;

        private static Regex _anchorInline = new Regex(string.Format(@"
                (                           # wrap whole match in $1
                    \[
                        ({0})               # link text = $2
                    \]
                    \(                      # literal paren
                        [ ]*
                        ({1})               # href = $3
                        [ ]*
                        (                   # $4
                        (['""])           # quote char = $5
                        (.*?)               # title = $6
                        \5                  # matching quote
                        [ ]*                # ignore any spaces between closing quote and )
                        )?                  # title is optional
                    \)
                )", GetNestedBracketsPattern(), GetNestedParensPattern()),
                  RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static string _nestedBracketsPattern;

        /// <summary>
        /// Reusable pattern to match balanced [brackets]. See Friedl's 
        /// "Mastering Regular Expressions", 2nd Ed., pp. 328-331.
        /// </summary>
        private static string GetNestedBracketsPattern()
        {
            // in other words [this] and [this[also]] and [this[also[too]]]
            // up to _nestDepth
            if (_nestedBracketsPattern == null)
                _nestedBracketsPattern =
                    RepeatString(@"
                    (?>              # Atomic matching
                       [^\[\]]+      # Anything other than brackets
                     |
                       \[
                           ", _nestDepth) + RepeatString(
                    @" \]
                    )*"
                    , _nestDepth);
            return _nestedBracketsPattern;
        }

        private static string _nestedParensPattern;

        /// <summary>
        /// Reusable pattern to match balanced (parens). See Friedl's 
        /// "Mastering Regular Expressions", 2nd Ed., pp. 328-331.
        /// </summary>
        private static string GetNestedParensPattern()
        {
            // in other words (this) and (this(also)) and (this(also(too)))
            // up to _nestDepth
            if (_nestedParensPattern == null)
                _nestedParensPattern =
                    RepeatString(@"
                    (?>              # Atomic matching
                       [^()\s]+      # Anything other than parens or whitespace
                     |
                       \(
                           ", _nestDepth) + RepeatString(
                    @" \)
                    )*"
                    , _nestDepth);
            return _nestedParensPattern;
        }

        /// <summary>
        /// this is to emulate what's evailable in PHP
        /// </summary>
        private static string RepeatString(string text, int count)
        {
            var sb = new StringBuilder(text.Length * count);
            for (int i = 0; i < count; i++)
                sb.Append(text);
            return sb.ToString();
        }
        #endregion
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
