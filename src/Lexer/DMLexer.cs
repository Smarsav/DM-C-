using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DMToCSharp.Core;
using DMToCSharp.Preprocessor;

namespace DMToCSharp.Lexer
{
    public class DMLexer
    {
        private static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>(StringComparer.OrdinalIgnoreCase)
        {
            { "var", TokenType.Var },
            { "proc", TokenType.Proc },
            { "verb", TokenType.Verb },
            { "set", TokenType.Set },
            { "as", TokenType.As },
            { "in", TokenType.In },
            { "to", TokenType.To },
            { "step", TokenType.Step },
            { "if", TokenType.If },
            { "else", TokenType.Else },
            { "while", TokenType.While },
            { "do", TokenType.Do },
            { "for", TokenType.For },
            { "switch", TokenType.Switch },
            { "return", TokenType.Return },
            { "break", TokenType.Break },
            { "continue", TokenType.Continue },
            { "spawn", TokenType.Spawn },
            { "try", TokenType.Try },
            { "catch", TokenType.Catch },
            { "throw", TokenType.Throw },
            { "new", TokenType.New },
            { "del", TokenType.Del },
            { "goto", TokenType.Goto },
            { "global", TokenType.Global },
            { "const", TokenType.Const },
            { "static", TokenType.Static },
            { "tmp", TokenType.Tmp },
            { "null", TokenType.Null },
            { "true", TokenType.True },
            { "false", TokenType.False },
            { "usr", TokenType.Usr },
            { "src", TokenType.Src },
            { "args", TokenType.Args },
            { "world", TokenType.World },
            { "locate", TokenType.Locate },
            { "input", TokenType.Input },
            { "initial", TokenType.Initial },
            { "istype", TokenType.IsType }
        };

        private readonly List<CompilerDiagnostic> _diagnostics = new List<CompilerDiagnostic>();
        public List<CompilerDiagnostic> Diagnostics
        {
            get { return _diagnostics; }
        }

        public List<Token> Tokenize(List<PreprocessedSourceLine> lines)
        {
            List<Token> tokens = new List<Token>();
            Stack<int> indentStack = new Stack<int>();
            indentStack.Push(0);
            int bracketDepth = 0;
            StringBuilder verbatimSb = null;
            Location verbatimLoc = default(Location);

            string currentFile = "unknown";
            int lastLineNum = 1;

            foreach (var lineObj in lines)
            {
                currentFile = lineObj.SourceFile;
                lastLineNum = lineObj.SourceLineNumber;
                string rawLine = lineObj.Content;

                if (verbatimSb != null)
                {
                    int endIdx = rawLine.IndexOf("\"}");
                    if (endIdx != -1)
                    {
                        verbatimSb.Append(rawLine.Substring(0, endIdx));
                        tokens.Add(new Token(TokenType.VerbatimString, verbatimSb.ToString(), verbatimLoc, verbatimSb.ToString()));
                        verbatimSb = null;
                        rawLine = rawLine.Substring(endIdx + 2);
                    }
                    else
                    {
                        verbatimSb.AppendLine(rawLine);
                        continue;
                    }
                }

                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                int indent = CountIndentation(rawLine);
                string trimmed = rawLine.TrimStart();

                if (trimmed.Length == 0)
                    continue;

                if (bracketDepth == 0)
                {
                    int currentIndent = indentStack.Peek();
                    if (indent > currentIndent)
                    {
                        indentStack.Push(indent);
                        tokens.Add(new Token(TokenType.Indent, "\t", new Location(currentFile, lastLineNum, 1)));
                    }
                    else if (indent < currentIndent)
                    {
                        while (indentStack.Count > 1 && indent < indentStack.Peek())
                        {
                            indentStack.Pop();
                            tokens.Add(new Token(TokenType.Dedent, string.Empty, new Location(currentFile, lastLineNum, 1)));
                        }
                    }
                }

                TokenizeLine(rawLine, currentFile, lastLineNum, tokens, ref bracketDepth, ref verbatimSb, ref verbatimLoc);

                if (bracketDepth == 0 && tokens.Count > 0 && tokens[tokens.Count - 1].Type != TokenType.Newline && tokens[tokens.Count - 1].Type != TokenType.Indent && tokens[tokens.Count - 1].Type != TokenType.Dedent)
                {
                    tokens.Add(new Token(TokenType.Newline, "\n", new Location(currentFile, lastLineNum, rawLine.Length + 1)));
                }
            }

            while (indentStack.Count > 1)
            {
                indentStack.Pop();
                tokens.Add(new Token(TokenType.Dedent, string.Empty, new Location(currentFile, lastLineNum, 1)));
            }

            tokens.Add(new Token(TokenType.EOF, string.Empty, new Location(currentFile, lastLineNum, 1)));
            return tokens;
        }

        private int CountIndentation(string line)
        {
            int count = 0;
            foreach (char c in line)
            {
                if (c == '\t') count += 4;
                else if (c == ' ') count += 1;
                else break;
            }
            return count;
        }

        private void TokenizeLine(string line, string file, int lineNum, List<Token> tokens, ref int bracketDepth, ref StringBuilder verbatimSb, ref Location verbatimLoc)
        {
            int i = 0;
            int len = line.Length;

            while (i < len)
            {
                char c = line[i];

                if (c == ' ' || c == '\t' || c == '\r')
                {
                    i++;
                    continue;
                }

                int col = i + 1;
                Location loc = new Location(file, lineNum, col);

                if (c == '{' && i + 1 < len && line[i + 1] == '\"')
                {
                    i += 2;
                    int endIdx = line.IndexOf("\"}", i);
                    if (endIdx != -1)
                    {
                        string strContent = line.Substring(i, endIdx - i);
                        tokens.Add(new Token(TokenType.VerbatimString, strContent, loc, strContent));
                        i = endIdx + 2;
                        continue;
                    }
                    else
                    {
                        verbatimLoc = loc;
                        verbatimSb = new StringBuilder();
                        verbatimSb.AppendLine(line.Substring(i));
                        break;
                    }
                }

                if (c == '\"')
                {
                    i++;
                    StringBuilder sb = new StringBuilder();
                    while (i < len)
                    {
                        char sc = line[i];
                        if (sc == '\\' && i + 1 < len)
                        {
                            char next = line[i + 1];
                            if (next == 'n') { sb.Append('\n'); i += 2; }
                            else if (next == 't') { sb.Append('\t'); i += 2; }
                            else if (next == '\"') { sb.Append('\"'); i += 2; }
                            else if (next == '\\') { sb.Append('\\'); i += 2; }
                            else if (next == '[') { sb.Append('['); i += 2; }
                            else if (next == ']') { sb.Append(']'); i += 2; }
                            else { sb.Append(next); i += 2; }
                        }
                        else if (sc == '\"')
                        {
                            i++;
                            break;
                        }
                        else
                        {
                            sb.Append(sc);
                            i++;
                        }
                    }
                    tokens.Add(new Token(TokenType.String, sb.ToString(), loc, sb.ToString()));
                    continue;
                }

                if (c == '\'')
                {
                    i++;
                    StringBuilder sb = new StringBuilder();
                    while (i < len)
                    {
                        char rc = line[i];
                        if (rc == '\\' && i + 1 < len)
                        {
                            sb.Append(line[i + 1]);
                            i += 2;
                        }
                        else if (rc == '\'')
                        {
                            i++;
                            break;
                        }
                        else
                        {
                            sb.Append(rc);
                            i++;
                        }
                    }
                    tokens.Add(new Token(TokenType.Resource, sb.ToString(), loc, sb.ToString()));
                    continue;
                }

                if (c == '/' && i + 1 < len && (char.IsLetter(line[i + 1]) || line[i + 1] == '_' || line[i + 1] == '/'))
                {
                    bool isPath = false;
                    if (tokens.Count == 0)
                    {
                        isPath = true;
                    }
                    else
                    {
                        TokenType prev = tokens[tokens.Count - 1].Type;
                        if (prev == TokenType.Newline || prev == TokenType.Indent || prev == TokenType.Dedent ||
                            prev == TokenType.Var || prev == TokenType.Proc || prev == TokenType.Verb ||
                            prev == TokenType.New || prev == TokenType.Assign || prev == TokenType.In ||
                            prev == TokenType.Comma || prev == TokenType.LeftParen || prev == TokenType.LeftBracket ||
                            prev == TokenType.Colon || prev == TokenType.Semicolon || prev == TokenType.IsType)
                        {
                            isPath = true;
                        }
                    }

                    if (isPath)
                    {
                        int start = i;
                        while (i < len && (char.IsLetterOrDigit(line[i]) || line[i] == '_' || line[i] == '/'))
                        {
                            i++;
                        }
                        string pathStr = line.Substring(start, i - start);
                        tokens.Add(new Token(TokenType.Path, pathStr, loc, new DreamPath(pathStr)));
                        continue;
                    }
                }

                if (char.IsDigit(c) || (c == '.' && i + 1 < len && char.IsDigit(line[i + 1])))
                {
                    int start = i;
                    if (c == '0' && i + 1 < len && (line[i + 1] == 'x' || line[i + 1] == 'X'))
                    {
                        i += 2;
                        while (i < len && IsHexDigit(line[i])) i++;
                        string hexStr = line.Substring(start, i - start);
                        long hexVal = Convert.ToInt64(hexStr, 16);
                        tokens.Add(new Token(TokenType.Number, hexStr, loc, (double)hexVal));
                        continue;
                    }

                    bool hasDot = (c == '.');
                    i++;
                    while (i < len)
                    {
                        if (line[i] == '.')
                        {
                            if (hasDot || (i + 1 < len && line[i + 1] == '.'))
                                break;
                            hasDot = true;
                            i++;
                        }
                        else if (char.IsDigit(line[i]))
                        {
                            i++;
                        }
                        else if ((line[i] == 'e' || line[i] == 'E') && i + 1 < len)
                        {
                            i++;
                            if (line[i] == '+' || line[i] == '-') i++;
                            while (i < len && char.IsDigit(line[i])) i++;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }

                    string numStr = line.Substring(start, i - start);
                    double numVal;
                    if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out numVal))
                    {
                        tokens.Add(new Token(TokenType.Number, numStr, loc, numVal));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Error, numStr, loc));
                    }
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

                    TokenType kwType;
                    if (Keywords.TryGetValue(ident, out kwType))
                    {
                        tokens.Add(new Token(kwType, ident, loc));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Identifier, ident, loc, ident));
                    }
                    continue;
                }

                switch (c)
                {
                    case '(':
                        bracketDepth++;
                        tokens.Add(new Token(TokenType.LeftParen, "(", loc));
                        i++;
                        break;
                    case ')':
                        if (bracketDepth > 0) bracketDepth--;
                        tokens.Add(new Token(TokenType.RightParen, ")", loc));
                        i++;
                        break;
                    case '[':
                        bracketDepth++;
                        tokens.Add(new Token(TokenType.LeftBracket, "[", loc));
                        i++;
                        break;
                    case ']':
                        if (bracketDepth > 0) bracketDepth--;
                        tokens.Add(new Token(TokenType.RightBracket, "]", loc));
                        i++;
                        break;
                    case '{':
                        bracketDepth++;
                        tokens.Add(new Token(TokenType.LeftBrace, "{", loc));
                        i++;
                        break;
                    case '}':
                        if (bracketDepth > 0) bracketDepth--;
                        tokens.Add(new Token(TokenType.RightBrace, "}", loc));
                        i++;
                        break;
                    case ',':
                        tokens.Add(new Token(TokenType.Comma, ",", loc));
                        i++;
                        break;
                    case ';':
                        tokens.Add(new Token(TokenType.Semicolon, ";", loc));
                        i++;
                        break;
                    case ':':
                        tokens.Add(new Token(TokenType.Colon, ":", loc));
                        i++;
                        break;
                    case '?':
                        tokens.Add(new Token(TokenType.Question, "?", loc));
                        i++;
                        break;
                    case '~':
                        if (i + 1 < len && line[i + 1] == '=')
                        {
                            tokens.Add(new Token(TokenType.Equivalent, "~=", loc));
                            i += 2;
                        }
                        else if (i + 1 < len && line[i + 1] == '!')
                        {
                            tokens.Add(new Token(TokenType.NotEquivalent, "~!", loc));
                            i += 2;
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.Tilde, "~", loc));
                            i++;
                        }
                        break;
                    case '.':
                        if (i + 2 < len && line[i + 1] == '.' && line[i + 2] == '.')
                        {
                            tokens.Add(new Token(TokenType.DotDotDot, "...", loc));
                            i += 3;
                        }
                        else if (i + 1 < len && line[i + 1] == '.')
                        {
                            tokens.Add(new Token(TokenType.DotDot, "..", loc));
                            i += 2;
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.Dot, ".", loc));
                            i++;
                        }
                        break;
                    case '+':
                        if (i + 1 < len && line[i + 1] == '+') { tokens.Add(new Token(TokenType.Increment, "++", loc)); i += 2; }
                        else if (i + 1 < len && line[i + 1] == '=') { tokens.Add(new Token(TokenType.AddAssign, "+=", loc)); i += 2; }
                        else { tokens.Add(new Token(TokenType.Plus, "+", loc)); i++; }
                        break;
                    case '-':
                        if (i + 1 < len && line[i + 1] == '-') { tokens.Add(new Token(TokenType.Decrement, "--", loc)); i += 2; }
                        else if (i + 1 < len && line[i + 1] == '=') { tokens.Add(new Token(TokenType.SubtractAssign, "-=", loc)); i += 2; }
                        else { tokens.Add(new Token(TokenType.Minus, "-", loc)); i++; }
                        break;
                    case '*':
                        if (i + 1 < len && line[i + 1] == '*') { tokens.Add(new Token(TokenType.Power, "**", loc)); i += 2; }
                        else if (i + 1 < len && line[i + 1] == '=') { tokens.Add(new Token(TokenType.MultiplyAssign, "*=", loc)); i += 2; }
                        else { tokens.Add(new Token(TokenType.Multiply, "*", loc)); i++; }
                        break;
                    case '/':
                        if (i + 1 < len && line[i + 1] == '=') { tokens.Add(new Token(TokenType.DivideAssign, "/=", loc)); i += 2; }
                        else { tokens.Add(new Token(TokenType.Divide, "/", loc)); i++; }
                        break;
                    case '%':
                        if (i + 1 < len && line[i + 1] == '=') { tokens.Add(new Token(TokenType.ModuloAssign, "%=", loc)); i += 2; }
                        else { tokens.Add(new Token(TokenType.Modulo, "%", loc)); i++; }
                        break;
                    case '=':
                        if (i + 1 < len && line[i + 1] == '=') { tokens.Add(new Token(TokenType.Equal, "==", loc)); i += 2; }
                        else { tokens.Add(new Token(TokenType.Assign, "=", loc)); i++; }
                        break;
                    case '!':
                        if (i + 1 < len && line[i + 1] == '=') { tokens.Add(new Token(TokenType.NotEqual, "!=", loc)); i += 2; }
                        else { tokens.Add(new Token(TokenType.Bang, "!", loc)); i++; }
                        break;
                    case '<':
                        if (i + 1 < len && line[i + 1] == '<')
                        {
                            if (i + 2 < len && line[i + 2] == '=') { tokens.Add(new Token(TokenType.ShiftLeftAssign, "<<=", loc)); i += 3; }
                            else { tokens.Add(new Token(TokenType.ShiftLeft, "<<", loc)); i += 2; }
                        }
                        else if (i + 1 < len && line[i + 1] == '=') { tokens.Add(new Token(TokenType.LessEqual, "<=", loc)); i += 2; }
                        else { tokens.Add(new Token(TokenType.Less, "<", loc)); i++; }
                        break;
                    case '>':
                        if (i + 1 < len && line[i + 1] == '>')
                        {
                            if (i + 2 < len && line[i + 2] == '=') { tokens.Add(new Token(TokenType.ShiftRightAssign, ">>=", loc)); i += 3; }
                            else { tokens.Add(new Token(TokenType.ShiftRight, ">>", loc)); i += 2; }
                        }
                        else if (i + 1 < len && line[i + 1] == '=') { tokens.Add(new Token(TokenType.GreaterEqual, ">=", loc)); i += 2; }
                        else { tokens.Add(new Token(TokenType.Greater, ">", loc)); i++; }
                        break;
                    case '&':
                        if (i + 1 < len && line[i + 1] == '&') { tokens.Add(new Token(TokenType.LogicalAnd, "&&", loc)); i += 2; }
                        else if (i + 1 < len && line[i + 1] == '=') { tokens.Add(new Token(TokenType.BitwiseAndAssign, "&=", loc)); i += 2; }
                        else { tokens.Add(new Token(TokenType.BitwiseAnd, "&", loc)); i++; }
                        break;
                    case '|':
                        if (i + 1 < len && line[i + 1] == '|') { tokens.Add(new Token(TokenType.LogicalOr, "||", loc)); i += 2; }
                        else if (i + 1 < len && line[i + 1] == '=') { tokens.Add(new Token(TokenType.BitwiseOrAssign, "|=", loc)); i += 2; }
                        else { tokens.Add(new Token(TokenType.BitwiseOr, "|", loc)); i++; }
                        break;
                    case '^':
                        if (i + 1 < len && line[i + 1] == '=') { tokens.Add(new Token(TokenType.BitwiseXorAssign, "^=", loc)); i += 2; }
                        else { tokens.Add(new Token(TokenType.BitwiseXor, "^", loc)); i++; }
                        break;
                    default:
                        tokens.Add(new Token(TokenType.Error, c.ToString(), loc));
                        i++;
                        break;
                }
            }
        }

        private bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }
    }
}
