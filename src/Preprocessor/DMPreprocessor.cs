using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DMToCSharp.Core;

namespace DMToCSharp.Preprocessor
{
    public class PreprocessedSourceLine
    {
        public string SourceFile { get; private set; }
        public int SourceLineNumber { get; private set; }
        public string Content { get; private set; }

        public PreprocessedSourceLine(string sourceFile, int sourceLineNumber, string content)
        {
            SourceFile = sourceFile;
            SourceLineNumber = sourceLineNumber;
            Content = content;
        }
    }

    public class DMPreprocessor
    {
        private readonly Dictionary<string, DMMacro> _macros = new Dictionary<string, DMMacro>(StringComparer.Ordinal);
        private readonly HashSet<string> _includedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _includePaths = new List<string>();
        private readonly List<CompilerDiagnostic> _diagnostics = new List<CompilerDiagnostic>();

        public List<CompilerDiagnostic> Diagnostics
        {
            get { return _diagnostics; }
        }

        public Dictionary<string, DMMacro> Macros
        {
            get { return _macros; }
        }

        public DMPreprocessor()
        {
            Define("DM_VERSION", "515");
            Define("DM_BUILD", "1600");
            Define("SPACECARP", "1");
        }

        public void AddIncludePath(string path)
        {
            if (!string.IsNullOrEmpty(path) && !_includePaths.Contains(path))
            {
                _includePaths.Add(path);
            }
        }

        public void Define(string name, string body = "1")
        {
            _macros[name] = new DMMacro(name, body);
        }

        public void Undefine(string name)
        {
            _macros.Remove(name);
        }

        public bool IsDefined(string name)
        {
            return _macros.ContainsKey(name);
        }

        public List<PreprocessedSourceLine> ProcessFile(string filePath)
        {
            string fullPath = Path.GetFullPath(filePath);
            string baseDir = Path.GetDirectoryName(fullPath);
            AddIncludePath(baseDir);

            List<PreprocessedSourceLine> output = new List<PreprocessedSourceLine>();
            ProcessFileInternal(fullPath, output, new Stack<string>());
            return output;
        }

        public List<PreprocessedSourceLine> ProcessSource(string sourceName, string sourceText)
        {
            List<PreprocessedSourceLine> output = new List<PreprocessedSourceLine>();
            ProcessSourceInternal(sourceName, sourceText, Path.GetDirectoryName(Path.GetFullPath(sourceName)) ?? ".", output, new Stack<string>());
            return output;
        }

        private void ProcessFileInternal(string filePath, List<PreprocessedSourceLine> output, Stack<string> includeStack)
        {
            string normalized = Path.GetFullPath(filePath);
            if (includeStack.Contains(normalized))
            {
                _diagnostics.Add(CompilerDiagnostic.Warning(new Location(filePath, 1, 1), "Recursive #include ignored for: " + filePath));
                return;
            }

            if (_includedFiles.Contains(normalized))
            {
                return;
            }

            if (!File.Exists(normalized))
            {
                _diagnostics.Add(CompilerDiagnostic.Error(new Location(filePath, 1, 1), "Include file not found: " + filePath));
                return;
            }

            _includedFiles.Add(normalized);
            includeStack.Push(normalized);

            string content = File.ReadAllText(normalized);
            string baseDir = Path.GetDirectoryName(normalized);
            ProcessSourceInternal(normalized, content, baseDir, output, includeStack);

            includeStack.Pop();
        }

        private void ProcessSourceInternal(string sourceName, string sourceText, string baseDir, List<PreprocessedSourceLine> output, Stack<string> includeStack)
        {
            string[] rawLines = sourceText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            var condStack = new Stack<ConditionalState>();
            condStack.Push(new ConditionalState { IsActive = true, HasBranchTaken = true });

            bool inBlockComment = false;

            for (int i = 0; i < rawLines.Length; i++)
            {
                int currentLineNum = i + 1;
                string line = rawLines[i];

                while (line.EndsWith("\\") && i + 1 < rawLines.Length)
                {
                    line = line.Substring(0, line.Length - 1) + " " + rawLines[++i];
                }

                string processedLine = StripComments(line, ref inBlockComment);
                string trimmed = processedLine.Trim();

                if (trimmed.StartsWith("#"))
                {
                    HandleDirective(trimmed, sourceName, currentLineNum, baseDir, output, includeStack, condStack);
                    continue;
                }

                if (!condStack.Peek().IsActive)
                {
                    continue;
                }

                string expandedLine = ExpandMacrosInLine(processedLine, sourceName, currentLineNum);
                output.Add(new PreprocessedSourceLine(sourceName, currentLineNum, expandedLine));
            }
        }

        private void HandleDirective(string trimmed, string sourceName, int lineNum, string baseDir, List<PreprocessedSourceLine> output, Stack<string> includeStack, Stack<ConditionalState> condStack)
        {
            Location loc = new Location(sourceName, lineNum, 1);
            string directive = trimmed.Substring(1).TrimStart();

            if (directive.StartsWith("ifdef", StringComparison.OrdinalIgnoreCase))
            {
                string symbol = directive.Substring(5).Trim();
                bool parentActive = condStack.Peek().IsActive;
                bool isDef = parentActive && IsDefined(symbol);
                condStack.Push(new ConditionalState { IsActive = isDef, HasBranchTaken = isDef });
            }
            else if (directive.StartsWith("ifndef", StringComparison.OrdinalIgnoreCase))
            {
                string symbol = directive.Substring(6).Trim();
                bool parentActive = condStack.Peek().IsActive;
                bool notDef = parentActive && !IsDefined(symbol);
                condStack.Push(new ConditionalState { IsActive = notDef, HasBranchTaken = notDef });
            }
            else if (directive.StartsWith("if", StringComparison.OrdinalIgnoreCase) && !directive.StartsWith("ifdef", StringComparison.OrdinalIgnoreCase))
            {
                string expr = directive.Substring(2).Trim();
                bool parentActive = condStack.Peek().IsActive;
                bool eval = parentActive && EvaluateExpression(expr);
                condStack.Push(new ConditionalState { IsActive = eval, HasBranchTaken = eval });
            }
            else if (directive.StartsWith("elif", StringComparison.OrdinalIgnoreCase))
            {
                if (condStack.Count <= 1)
                {
                    _diagnostics.Add(CompilerDiagnostic.Error(loc, "#elif without matching #if"));
                    return;
                }
                var curr = condStack.Pop();
                bool parentActive = condStack.Peek().IsActive;
                string expr = directive.Substring(4).Trim();
                if (parentActive && !curr.HasBranchTaken && EvaluateExpression(expr))
                {
                    condStack.Push(new ConditionalState { IsActive = true, HasBranchTaken = true });
                }
                else
                {
                    condStack.Push(new ConditionalState { IsActive = false, HasBranchTaken = curr.HasBranchTaken });
                }
            }
            else if (directive.StartsWith("else", StringComparison.OrdinalIgnoreCase))
            {
                if (condStack.Count <= 1)
                {
                    _diagnostics.Add(CompilerDiagnostic.Error(loc, "#else without matching #if"));
                    return;
                }
                var curr = condStack.Pop();
                bool parentActive = condStack.Peek().IsActive;
                bool active = parentActive && !curr.HasBranchTaken;
                condStack.Push(new ConditionalState { IsActive = active, HasBranchTaken = true });
            }
            else if (directive.StartsWith("endif", StringComparison.OrdinalIgnoreCase))
            {
                if (condStack.Count <= 1)
                {
                    _diagnostics.Add(CompilerDiagnostic.Error(loc, "#endif without matching #if"));
                    return;
                }
                condStack.Pop();
            }
            else
            {
                if (!condStack.Peek().IsActive)
                    return;

                if (directive.StartsWith("define", StringComparison.OrdinalIgnoreCase))
                {
                    ParseDefine(directive.Substring(6).Trim(), loc);
                }
                else if (directive.StartsWith("undef", StringComparison.OrdinalIgnoreCase))
                {
                    string symbol = directive.Substring(5).Trim();
                    Undefine(symbol);
                }
                else if (directive.StartsWith("include", StringComparison.OrdinalIgnoreCase))
                {
                    string includeArg = directive.Substring(7).Trim();
                    ParseInclude(includeArg, baseDir, loc, output, includeStack);
                }
                else if (directive.StartsWith("warn", StringComparison.OrdinalIgnoreCase) || directive.StartsWith("warning", StringComparison.OrdinalIgnoreCase))
                {
                    string msg = directive.Substring(directive.IndexOf(' ') + 1).Trim();
                    _diagnostics.Add(CompilerDiagnostic.Warning(loc, msg));
                }
                else if (directive.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                {
                    string msg = directive.Substring(5).Trim();
                    _diagnostics.Add(CompilerDiagnostic.Error(loc, msg));
                }
            }
        }

        private void ParseDefine(string rest, Location loc)
        {
            if (string.IsNullOrWhiteSpace(rest)) return;

            Match matchFunc = Regex.Match(rest, @"^([a-zA-Z_][a-zA-Z0-9_]*)\(([^)]*)\)\s*(.*)$");
            if (matchFunc.Success)
            {
                string name = matchFunc.Groups[1].Value;
                string rawParams = matchFunc.Groups[2].Value;
                string body = matchFunc.Groups[3].Value;

                List<string> parameters = new List<string>();
                if (!string.IsNullOrWhiteSpace(rawParams))
                {
                    foreach (var p in rawParams.Split(','))
                    {
                        parameters.Add(p.Trim());
                    }
                }
                _macros[name] = new DMMacro(name, parameters, body);
                return;
            }

            Match matchObj = Regex.Match(rest, @"^([a-zA-Z_][a-zA-Z0-9_]*)\s*(.*)$");
            if (matchObj.Success)
            {
                string name = matchObj.Groups[1].Value;
                string body = matchObj.Groups[2].Value;
                _macros[name] = new DMMacro(name, body);
            }
        }

        private void ParseInclude(string includeArg, string baseDir, Location loc, List<PreprocessedSourceLine> output, Stack<string> includeStack)
        {
            string path = includeArg.Trim('\"', '<', '>', ' ', '\t');
            string resolved = ResolveIncludePath(path, baseDir);
            if (resolved != null && File.Exists(resolved))
            {
                ProcessFileInternal(resolved, output, includeStack);
            }
            else
            {
                _diagnostics.Add(CompilerDiagnostic.Error(loc, "Cannot find include file: " + path));
            }
        }

        private string ResolveIncludePath(string relativePath, string baseDir)
        {
            string candidate = Path.Combine(baseDir, relativePath);
            if (File.Exists(candidate)) return candidate;

            foreach (var inc in _includePaths)
            {
                candidate = Path.Combine(inc, relativePath);
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        private bool EvaluateExpression(string expr)
        {
            expr = expr.Trim();
            if (string.IsNullOrEmpty(expr)) return false;

            expr = Regex.Replace(expr, @"defined\s*\(\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*\)", m => IsDefined(m.Groups[1].Value) ? "1" : "0");
            expr = Regex.Replace(expr, @"defined\s+([a-zA-Z_][a-zA-Z0-9_]*)", m => IsDefined(m.Groups[1].Value) ? "1" : "0");

            expr = Regex.Replace(expr, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b", m =>
            {
                string key = m.Groups[1].Value;
                if (key == "true") return "1";
                if (key == "false") return "0";
                DMMacro macro;
                if (_macros.TryGetValue(key, out macro))
                {
                    return string.IsNullOrWhiteSpace(macro.Body) ? "1" : macro.Body;
                }
                return "0";
            });

            try
            {
                return SimpleMathEval(expr) != 0;
            }
            catch
            {
                return false;
            }
        }

        private double SimpleMathEval(string expr)
        {
            using (var dt = new System.Data.DataTable())
            {
                string cleaned = expr.Replace("&&", " AND ").Replace("||", " OR ").Replace("!", " NOT ").Replace("!=", "<>");
                var result = dt.Compute(cleaned, "");
                if (result is bool) return ((bool)result) ? 1 : 0;
                return Convert.ToDouble(result);
            }
        }

        private string ExpandMacrosInLine(string line, string sourceFile, int lineNum)
        {
            if (string.IsNullOrEmpty(line)) return line;

            line = line.Replace("__FILE__", "\"" + sourceFile.Replace("\\", "\\\\") + "\"");
            line = line.Replace("__LINE__", lineNum.ToString());

            StringBuilder sb = new StringBuilder();
            int len = line.Length;
            int i = 0;

            while (i < len)
            {
                char c = line[i];

                if (c == '\"')
                {
                    sb.Append(c);
                    i++;
                    while (i < len)
                    {
                        char sc = line[i];
                        sb.Append(sc);
                        if (sc == '\\' && i + 1 < len)
                        {
                            sb.Append(line[++i]);
                        }
                        else if (sc == '\"')
                        {
                            break;
                        }
                        i++;
                    }
                    i++;
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < len && (char.IsLetterOrDigit(line[i]) || line[i] == '_'))
                    {
                        i++;
                    }
                    string ident = line.Substring(start, i - start);

                    DMMacro macro;
                    if (_macros.TryGetValue(ident, out macro))
                    {
                        if (macro.IsFunctionLike)
                        {
                            int save = i;
                            while (i < len && char.IsWhiteSpace(line[i])) i++;
                            if (i < len && line[i] == '(')
                            {
                                i++; // skip '('
                                int parenDepth = 1;
                                StringBuilder argBuilder = new StringBuilder();
                                List<string> args = new List<string>();

                                while (i < len && parenDepth > 0)
                                {
                                    char ac = line[i];
                                    if (ac == '(') parenDepth++;
                                    else if (ac == ')') parenDepth--;

                                    if (parenDepth == 0)
                                    {
                                        args.Add(argBuilder.ToString());
                                        i++; // skip ')'
                                        break;
                                    }
                                    else if (parenDepth == 1 && ac == ',')
                                    {
                                        args.Add(argBuilder.ToString());
                                        argBuilder.Clear();
                                        i++;
                                        continue;
                                    }
                                    argBuilder.Append(ac);
                                    i++;
                                }
                                sb.Append(macro.Expand(args));
                            }
                            else
                            {
                                i = save;
                                sb.Append(ident);
                            }
                        }
                        else
                        {
                            sb.Append(macro.Expand());
                        }
                    }
                    else
                    {
                        sb.Append(ident);
                    }
                    continue;
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }

        private string StripComments(string line, ref bool inBlockComment)
        {
            StringBuilder sb = new StringBuilder();
            int len = line.Length;
            int i = 0;

            while (i < len)
            {
                if (inBlockComment)
                {
                    if (i + 1 < len && line[i] == '*' && line[i + 1] == '/')
                    {
                        inBlockComment = false;
                        i += 2;
                    }
                    else
                    {
                        i++;
                    }
                    continue;
                }

                char c = line[i];

                if (c == '\"')
                {
                    sb.Append(c);
                    i++;
                    while (i < len)
                    {
                        char sc = line[i];
                        sb.Append(sc);
                        if (sc == '\\' && i + 1 < len)
                        {
                            sb.Append(line[++i]);
                        }
                        else if (sc == '\"')
                        {
                            break;
                        }
                        i++;
                    }
                    i++;
                    continue;
                }

                if (c == '{' && i + 1 < len && line[i + 1] == '\"')
                {
                    sb.Append("{\"");
                    i += 2;
                    while (i < len)
                    {
                        if (line[i] == '\"' && i + 1 < len && line[i + 1] == '}')
                        {
                            sb.Append("\"}");
                            i += 2;
                            break;
                        }
                        sb.Append(line[i]);
                        i++;
                    }
                    continue;
                }

                if (c == '/' && i + 1 < len && line[i + 1] == '/')
                {
                    break;
                }

                if (c == '/' && i + 1 < len && line[i + 1] == '*')
                {
                    inBlockComment = true;
                    i += 2;
                    continue;
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }

        private class ConditionalState
        {
            public bool IsActive { get; set; }
            public bool HasBranchTaken { get; set; }
        }
    }
}
