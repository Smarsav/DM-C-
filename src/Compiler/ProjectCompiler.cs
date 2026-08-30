using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using DMToCSharp.Core;
using DMToCSharp.Core.AST;
using DMToCSharp.Emitter;
using DMToCSharp.Lexer;
using DMToCSharp.Parser;
using DMToCSharp.Preprocessor;
using DMToCSharp.Semantics;

namespace DMToCSharp.Compiler
{
    public class BatchCompilationResult
    {
        public int TotalFiles { get; set; }
        public int SuccessfulFiles { get; set; }
        public int FailedFiles { get; set; }
        public int TotalTypesDiscovered { get; set; }
        public int TotalProcsDiscovered { get; set; }
        public double ElapsedSeconds { get; set; }
        public string OutputCSharpFile { get; set; }
        public List<string> Errors { get; private set; }

        public BatchCompilationResult()
        {
            Errors = new List<string>();
        }
    }

    public class ProjectCompiler
    {
        public static BatchCompilationResult CompileProject(string projectPath, string outputDir = null, List<string> initialDefines = null)
        {
            BatchCompilationResult result = new BatchCompilationResult();
            Stopwatch sw = Stopwatch.StartNew();

            List<string> dmFiles = new List<string>();

            if (File.Exists(projectPath))
            {
                string ext = Path.GetExtension(projectPath).ToLowerInvariant();
                if (ext == ".dme")
                {
                    Console.WriteLine("[Project Compiler] Parsing BYOND Environment file: " + projectPath);
                    string projectDir = Path.GetDirectoryName(Path.GetFullPath(projectPath));
                    string[] lines = File.ReadAllLines(projectPath);
                    foreach (var line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("#include \"") && trimmed.EndsWith("\""))
                        {
                            string relPath = trimmed.Substring(10, trimmed.Length - 11).Replace('/', '\\');
                            string fullPath = Path.Combine(projectDir, relPath);
                            if (File.Exists(fullPath) && Path.GetExtension(fullPath).ToLowerInvariant() == ".dm")
                            {
                                dmFiles.Add(fullPath);
                            }
                        }
                    }
                }
                else if (ext == ".dm")
                {
                    dmFiles.Add(Path.GetFullPath(projectPath));
                }
            }
            else if (Directory.Exists(projectPath))
            {
                Console.WriteLine("[Project Compiler] Scanning directory for .dm files: " + projectPath);
                dmFiles.AddRange(Directory.GetFiles(projectPath, "*.dm", SearchOption.AllDirectories));
            }
            else
            {
                result.Errors.Add("Target path does not exist: " + projectPath);
                return result;
            }

            result.TotalFiles = dmFiles.Count;
            Console.WriteLine(string.Format("[Project Compiler] Found {0} DreamMaker source files to compile.", dmFiles.Count));

            if (string.IsNullOrEmpty(outputDir))
            {
                outputDir = Path.Combine(Directory.GetCurrentDirectory(), "compiled_csharp");
            }
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            DMObjectTree globalObjectTree = new DMObjectTree();
            List<DMASTStatement> allASTStatements = new List<DMASTStatement>();

            // Global Preprocessor with standard SS13 defines
            DMPreprocessor preprocessor = new DMPreprocessor();
            preprocessor.Define("TGS", "1");
            preprocessor.Define("SS13", "1");
            preprocessor.Define("DEBUG", "1");
            preprocessor.Define("TEST", "1");

            if (initialDefines != null)
            {
                foreach (var d in initialDefines)
                {
                    int eq = d.IndexOf('=');
                    if (eq != -1)
                        preprocessor.Define(d.Substring(0, eq), d.Substring(eq + 1));
                    else
                        preprocessor.Define(d, "1");
                }
            }

            int processed = 0;
            int successful = 0;

            for (int i = 0; i < dmFiles.Count; i++)
            {
                string file = dmFiles[i];
                processed++;

                try
                {
                    // 1. Preprocess
                    var preprocResult = preprocessor.ProcessFile(file);

                    // 2. Lex
                    var lexer = new DMLexer();
                    var tokens = lexer.Tokenize(preprocResult);

                    // 3. Parse
                    var parser = new DMParser(tokens);
                    var fileAST = parser.ParseFile();

                    if (fileAST != null && fileAST.Definitions != null)
                    {
                        globalObjectTree.ProcessAST(fileAST);
                        successful++;
                    }

                    if (processed % 25 == 0 || processed == dmFiles.Count)
                    {
                        Console.WriteLine(string.Format("  [{0}/{1}] Processed ({2} successful)", processed, dmFiles.Count, successful));
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(string.Format("{0}: {1}", Path.GetFileName(file), ex.Message));
                }
            }

            result.SuccessfulFiles = successful;
            result.FailedFiles = dmFiles.Count - successful;
            result.TotalTypesDiscovered = globalObjectTree.Types.Count;
            result.TotalProcsDiscovered = globalObjectTree.GlobalProcs.Count;

            // 4. Emit Consolidated C# Source Code
            Console.WriteLine("[Project Compiler] Generating consolidated C# source code...");
            CSharpEmitter emitter = new CSharpEmitter(globalObjectTree);
            string emittedCSharp = emitter.Emit();

            string consolidatedFile = Path.Combine(outputDir, "StationProject.cs");
            File.WriteAllText(consolidatedFile, emittedCSharp, Encoding.UTF8);

            sw.Stop();
            result.ElapsedSeconds = sw.Elapsed.TotalSeconds;
            result.OutputCSharpFile = consolidatedFile;

            Console.WriteLine("================================================================================");
            Console.WriteLine(" Project Compilation Summary");
            Console.WriteLine("================================================================================");
            Console.WriteLine(string.Format("Total Files:       {0}", result.TotalFiles));
            Console.WriteLine(string.Format("Successful:        {0}", result.SuccessfulFiles));
            Console.WriteLine(string.Format("Types Created:     {0}", result.TotalTypesDiscovered));
            Console.WriteLine(string.Format("Global Procs:      {0}", result.TotalProcsDiscovered));
            Console.WriteLine(string.Format("Time Elapsed:      {0:F2} seconds", result.ElapsedSeconds));
            Console.WriteLine(string.Format("Generated Output:  {0}", consolidatedFile));
            Console.WriteLine("================================================================================");

            return result;
        }
    }
}
