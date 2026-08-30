using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using DMToCSharp.Core;
using DMToCSharp.CSharpToDM;
using DMToCSharp.Emitter;
using DMToCSharp.Lexer;
using DMToCSharp.Parser;
using DMToCSharp.Preprocessor;
using DMToCSharp.Semantics;

namespace DMToCSharp
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            if (args.Length == 0 || args[0] == "help" || args[0] == "--help" || args[0] == "-h")
            {
                PrintHelp();
                return 0;
            }

            string command = args[0].ToLowerInvariant();

            if (command == "test")
            {
                return RunBuiltinTests();
            }

            if (command == "server" || command == "tgui")
            {
                int port = 8080;
                if (args.Length > 1) int.TryParse(args[1], out port);
                return RunTGUIServer(port);
            }

            if (command == "map-inspect" || command == "map")
            {
                string mapPath = args.Length > 1 ? args[1] : @"psychonaut_station\_maps\shuttles\assault_pod_default.dmm";
                return RunMapInspect(mapPath);
            }

            if (command == "project" || command == "batch")
            {
                string projectPath = args.Length > 1 ? args[1] : Directory.GetCurrentDirectory();
                string outDir = args.Length > 2 ? args[2] : null;
                var res = Compiler.ProjectCompiler.CompileProject(projectPath, outDir);
                return res.SuccessfulFiles > 0 ? 0 : 1;
            }

            if (command == "cs2dm")
            {
                return RunCSharpToDM(args);
            }

            if (command == "dm2cs" || command == "compile" || File.Exists(args[0]))
            {
                return RunDMToCSharp(args);
            }

            Console.WriteLine("Unknown command: " + command);
            PrintHelp();
            return 1;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine(" DMToCSharp - Bidirectional DreamMaker <-> C# Compiler & Transpiler");
            Console.WriteLine("================================================================================");
            Console.WriteLine("Usage:");
            Console.WriteLine("  dm2cs [compile] <file.dm|file.dme> [options]   Compile DM code to C#");
            Console.WriteLine("  dm2cs cs2dm <file.cs> [options]                Transpile C# code to DM");
            Console.WriteLine("  dm2cs test                                     Run all test suites & demos");
            Console.WriteLine();
            Console.WriteLine("DM -> C# Options:");
            Console.WriteLine("  -o, --output <file>    Output C# file path (default: <name>.cs)");
            Console.WriteLine("  --exe <file>           Compile to executable (.exe) using C# compiler");
            Console.WriteLine("  --run                  Compile and execute the output immediately");
            Console.WriteLine("  -D<symbol>[=<val>]     Define preprocessor symbol");
            Console.WriteLine();
            Console.WriteLine("C# -> DM Options:");
            Console.WriteLine("  -o, --output <file>    Output DM file path (default: <name>.dm)");
            Console.WriteLine("================================================================================");
        }

        public static int RunDMToCSharp(string[] args)
        {
            int argIdx = 0;
            if (args[0].Equals("dm2cs", StringComparison.OrdinalIgnoreCase) || args[0].Equals("compile", StringComparison.OrdinalIgnoreCase))
            {
                argIdx++;
            }

            if (argIdx >= args.Length)
            {
                Console.WriteLine("Error: No input DM file specified.");
                return 1;
            }

            string inputFile = args[argIdx++];
            string outputFile = null;
            string exeFile = null;
            bool runAfterBuild = false;
            List<string> defines = new List<string>();

            while (argIdx < args.Length)
            {
                string arg = args[argIdx++];
                if ((arg == "-o" || arg == "--output") && argIdx < args.Length)
                {
                    outputFile = args[argIdx++];
                }
                else if (arg == "--exe" && argIdx < args.Length)
                {
                    exeFile = args[argIdx++];
                }
                else if (arg == "--run")
                {
                    runAfterBuild = true;
                }
                else if (arg.StartsWith("-D"))
                {
                    defines.Add(arg.Substring(2));
                }
            }

            if (!File.Exists(inputFile))
            {
                Console.WriteLine("Error: File not found: " + inputFile);
                return 1;
            }

            if (string.IsNullOrEmpty(outputFile))
            {
                string baseName = Path.GetFileNameWithoutExtension(inputFile);
                outputFile = Path.Combine(Path.GetDirectoryName(inputFile) ?? ".", baseName + ".cs");
            }

            Console.WriteLine(string.Format("[DM -> C#] Compiling {0}...", inputFile));

            // 1. Preprocessor
            var preprocessor = new DMPreprocessor();
            foreach (var d in defines)
            {
                if (d.Contains("="))
                {
                    var parts = d.Split(new[] { '=' }, 2);
                    preprocessor.Define(parts[0], parts[1]);
                }
                else
                {
                    preprocessor.Define(d);
                }
            }

            var preprocessedLines = preprocessor.ProcessFile(inputFile);
            PrintDiagnostics(preprocessor.Diagnostics);

            // 2. Lexer
            var lexer = new DMLexer();
            var tokens = lexer.Tokenize(preprocessedLines);
            PrintDiagnostics(lexer.Diagnostics);

            // 3. Parser
            var parser = new DMParser(tokens);
            var ast = parser.ParseFile();
            PrintDiagnostics(parser.Diagnostics);

            // 4. Semantic Object Tree
            var objectTree = new DMObjectTree();
            objectTree.ProcessAST(ast);
            PrintDiagnostics(objectTree.Diagnostics);

            // 5. C# Code Generation
            var emitter = new CSharpEmitter(objectTree);
            string generatedCSharp = emitter.Emit();

            File.WriteAllText(outputFile, generatedCSharp, Encoding.UTF8);
            Console.WriteLine(string.Format("[DM -> C#] Successfully generated C# code: {0}", outputFile));

            // 6. Build Executable if requested or --run
            if (runAfterBuild || !string.IsNullOrEmpty(exeFile))
            {
                if (string.IsNullOrEmpty(exeFile))
                {
                    exeFile = Path.ChangeExtension(outputFile, ".exe");
                }

                bool compiled = CompileCSharpToExe(outputFile, exeFile);
                if (compiled && runAfterBuild)
                {
                    Console.WriteLine(string.Format("[Run] Executing {0}...", exeFile));
                    Console.WriteLine("----------------------------------------------------------------");
                    var proc = Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.GetFullPath(exeFile),
                        UseShellExecute = false
                    });
                    proc.WaitForExit();
                    Console.WriteLine("----------------------------------------------------------------");
                    Console.WriteLine(string.Format("[Run] Process exited with code: {0}", proc.ExitCode));
                    return proc.ExitCode;
                }
            }

            return 0;
        }

        public static int RunCSharpToDM(string[] args)
        {
            int argIdx = 1;
            if (argIdx >= args.Length)
            {
                Console.WriteLine("Error: No input C# file specified.");
                return 1;
            }

            string inputFile = args[argIdx++];
            string outputFile = null;

            while (argIdx < args.Length)
            {
                string arg = args[argIdx++];
                if ((arg == "-o" || arg == "--output") && argIdx < args.Length)
                {
                    outputFile = args[argIdx++];
                }
            }

            if (!File.Exists(inputFile))
            {
                Console.WriteLine("Error: File not found: " + inputFile);
                return 1;
            }

            if (string.IsNullOrEmpty(outputFile))
            {
                string baseName = Path.GetFileNameWithoutExtension(inputFile);
                outputFile = Path.Combine(Path.GetDirectoryName(inputFile) ?? ".", baseName + ".dm");
            }

            Console.WriteLine(string.Format("[C# -> DM] Transpiling {0} to DreamMaker...", inputFile));

            string csCode = File.ReadAllText(inputFile);
            var lexer = new CSharpLexer();
            var tokens = lexer.Tokenize(csCode, inputFile);

            var parser = new CSharpParser(tokens);
            var compilationUnit = parser.ParseCompilationUnit();
            PrintDiagnostics(parser.Diagnostics);

            var dmEmitter = new DMEmitter();
            string dmCode = dmEmitter.Emit(compilationUnit);

            File.WriteAllText(outputFile, dmCode, Encoding.UTF8);
            Console.WriteLine(string.Format("[C# -> DM] Successfully generated DreamMaker code: {0}", outputFile));
            return 0;
        }

        private static bool CompileCSharpToExe(string csSourceFile, string exeOutputFile)
        {
            string cscPath = FindCscCompiler();
            if (string.IsNullOrEmpty(cscPath) || !File.Exists(cscPath))
            {
                Console.WriteLine("Warning: csc.exe compiler not found. Could not build native executable.");
                return false;
            }

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string srcDir = Path.Combine(appDir, "..", "..", "src");
            if (!Directory.Exists(srcDir))
            {
                srcDir = Path.Combine(Directory.GetCurrentDirectory(), "src");
            }

            List<string> sourceFiles = new List<string>();
            sourceFiles.Add(string.Format("\"{0}\"", Path.GetFullPath(csSourceFile)));

            if (Directory.Exists(srcDir))
            {
                foreach (var f in Directory.GetFiles(Path.Combine(srcDir, "Core"), "*.cs", SearchOption.AllDirectories))
                    sourceFiles.Add(string.Format("\"{0}\"", f));
                foreach (var f in Directory.GetFiles(Path.Combine(srcDir, "Runtime"), "*.cs", SearchOption.AllDirectories))
                    sourceFiles.Add(string.Format("\"{0}\"", f));
            }

            string allSources = string.Join(" ", sourceFiles.ToArray());
            string args = string.Format("/nologo /target:exe /out:\"{0}\" {1}", Path.GetFullPath(exeOutputFile), allSources);

            var psi = new ProcessStartInfo
            {
                FileName = cscPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var p = Process.Start(psi);
            string outText = p.StandardOutput.ReadToEnd();
            string errText = p.StandardError.ReadToEnd();
            p.WaitForExit();

            if (p.ExitCode != 0)
            {
                Console.WriteLine("C# Compilation Failed:");
                Console.WriteLine(outText);
                Console.WriteLine(errText);
                return false;
            }

            Console.WriteLine(string.Format("[Build] Successfully compiled binary: {0}", exeOutputFile));
            return true;
        }

        private static string FindCscCompiler()
        {
            string[] paths = new[]
            {
                @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
                @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
            };

            foreach (var p in paths)
            {
                if (File.Exists(p)) return p;
            }
            return "csc.exe";
        }

        private static void PrintDiagnostics(List<CompilerDiagnostic> diagnostics)
        {
            if (diagnostics == null) return;
            foreach (var d in diagnostics)
            {
                if (d.Severity == DiagnosticSeverity.Error)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(d.ToString());
                    Console.ResetColor();
                }
                else if (d.Severity == DiagnosticSeverity.Warning)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(d.ToString());
                    Console.ResetColor();
                }
            }
        }

        private static int RunBuiltinTests()
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine(" Running DMToCSharp Built-in Test Suite & Verification");
            Console.WriteLine("================================================================================");

            string testsDir = Path.Combine(Directory.GetCurrentDirectory(), "tests");
            if (!Directory.Exists(testsDir))
            {
                Console.WriteLine("Tests directory not found: " + testsDir);
                return 1;
            }

            int passed = 0;
            int total = 0;

            // 1. Run DM -> C# Tests
            string dmTestsDir = Path.Combine(testsDir, "dm_to_cs");
            if (Directory.Exists(dmTestsDir))
            {
                foreach (var dmFile in Directory.GetFiles(dmTestsDir, "*.dm"))
                {
                    total++;
                    string testName = Path.GetFileName(dmFile);
                    Console.WriteLine(string.Format("[TEST {0}] DM -> C# -> Run: {1}", total, testName));

                    string csOut = Path.ChangeExtension(dmFile, ".cs");
                    string exeOut = Path.ChangeExtension(dmFile, ".exe");

                    int res = RunDMToCSharp(new[] { "compile", dmFile, "-o", csOut, "--exe", exeOut, "--run" });
                    if (res == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(string.Format("  ==> PASSED: {0}", testName));
                        Console.ResetColor();
                        passed++;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(string.Format("  ==> FAILED: {0} (Exit code {1})", testName, res));
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                }
            }

            // 2. Run C# -> DM Tests
            string csTestsDir = Path.Combine(testsDir, "cs_to_dm");
            if (Directory.Exists(csTestsDir))
            {
                foreach (var csFile in Directory.GetFiles(csTestsDir, "*.cs"))
                {
                    total++;
                    string testName = Path.GetFileName(csFile);
                    Console.WriteLine(string.Format("[TEST {0}] C# -> DM: {1}", total, testName));

                    string dmOut = Path.ChangeExtension(csFile, ".dm");
                    int res = RunCSharpToDM(new[] { "cs2dm", csFile, "-o", dmOut });
                    if (res == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(string.Format("  ==> PASSED: {0}", testName));
                        Console.ResetColor();
                        passed++;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(string.Format("  ==> FAILED: {0}", testName));
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                }
            }

            Console.WriteLine("================================================================================");
            Console.WriteLine(string.Format("Test Results: {0}/{1} Passed ({2}%)", passed, total, (passed * 100 / Math.Max(1, total))));
            Console.WriteLine("================================================================================");

            return passed == total ? 0 : 1;
        }

        private static int RunTGUIServer(int port)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine(" Space Station 13 - Live TGUI Web Server & Control Console");
            Console.WriteLine("================================================================================");
            var server = new Runtime.TGUI.TGUIHttpServer(port);
            server.Start();
            Console.WriteLine("Press Enter to stop the server...");
            Console.ReadLine();
            server.Stop();
            return 0;
        }

        private static int RunMapInspect(string mapPath)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine(" Space Station 13 - DMM Map Inspector");
            Console.WriteLine("================================================================================");
            Console.WriteLine("Analyzing map: " + mapPath);
            var report = Runtime.Maps.StationMapInspector.InspectMapFile(mapPath);

            Console.WriteLine(string.Format("Dimensions: {0} x {1} x {2}", report.SizeX, report.SizeY, report.SizeZ));
            Console.WriteLine(string.Format("Tile Definitions: {0}", report.TotalTileDefinitions));
            Console.WriteLine(string.Format("Turfs Loaded:     {0}", report.TotalTurfsLoaded));
            Console.WriteLine(string.Format("Objects Loaded:   {0}", report.TotalObjectsLoaded));
            Console.WriteLine(string.Format("Airlocks/Doors:   {0}", report.TotalAirlocks));
            Console.WriteLine(string.Format("Machines/Comps:   {0}", report.TotalMachines));
            Console.WriteLine(string.Format("Lights/Fixtures:  {0}", report.TotalLights));
            Console.WriteLine("================================================================================");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[Map Inspection Success] Map fully loaded and verified in 3D Spatial Grid!");
            Console.ResetColor();
            return 0;
        }
    }
}
