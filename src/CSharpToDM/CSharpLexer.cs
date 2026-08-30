using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DMToCSharp.Core;

namespace DMToCSharp.CSharpToDM
{
    public enum CSTokenType
    {
        EOF,
        Identifier,
        Number,
        String,
        InterpolatedString,
        Char,

        Using,
        Namespace,
        Class,
        Struct,
        Interface,
        Public,
        Private,
        Protected,
        Internal,
        Static,
        Virtual,
        Override,
        Abstract,
        Sealed,
        Void,
        Int,
        Double,
        Float,
        StringKeyword,
        Bool,
        Var,
        If,
        Else,
        While,
        Do,
        For,
        ForEach,
        In,
        Switch,
        Case,
        Default,
        Return,
        Break,
        Continue,
        Try,
        Catch,
        Finally,
        Throw,
        New,
        This,
        Base,
        True,
        False,
        Null,
        Get,
        Set,

        LeftParen,
        RightParen,
        LeftBrace,
        RightBrace,
        LeftBracket,
        RightBracket,
        Comma,
        Semicolon,
        Colon,
        Dot,
        Question,
        Arrow,

        Plus,
        Minus,
        Multiply,
        Divide,
        Modulo,
        Increment,
        Decrement,

        Equal,
        NotEqual,
        Less,
        LessEqual,
        Greater,
        GreaterEqual,

        LogicalAnd,
        LogicalOr,
        LogicalNot,

        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        BitwiseNot,
        ShiftLeft,
        ShiftRight,

        Assign,
        AddAssign,
        SubtractAssign,
        MultiplyAssign,
        DivideAssign,
        ModuloAssign,
        BitwiseAndAssign,
        BitwiseOrAssign,
        BitwiseXorAssign,
        ShiftLeftAssign,
        ShiftRightAssign
    }

    public class CSToken
    {
        public CSTokenType Type { get; private set; }
        public string Text { get; private set; }
        public object Value { get; private set; }
        public Location Location { get; private set; }

        public CSToken(CSTokenType type, string text, Location location, object value = null)
        {
            Type = type;
            Text = text ?? "";
            Location = location;
            Value = value;
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}] {2}", Location, Type, Text);
        }
    }

    public class CSharpLexer
    {
        private static readonly Dictionary<string, CSTokenType> Keywords = new Dictionary<string, CSTokenType>(StringComparer.Ordinal)
        {
            { "using", CSTokenType.Using },
            { "namespace", CSTokenType.Namespace },
            { "class", CSTokenType.Class },
            { "struct", CSTokenType.Struct },
            { "interface", CSTokenType.Interface },
            { "public", CSTokenType.Public },
            { "private", CSTokenType.Private },
            { "protected", CSTokenType.Protected },
            { "internal", CSTokenType.Internal },
            { "static", CSTokenType.Static },
            { "virtual", CSTokenType.Virtual },
            { "override", CSTokenType.Override },
            { "abstract", CSTokenType.Abstract },
            { "sealed", CSTokenType.Sealed },
            { "void", CSTokenType.Void },
            { "int", CSTokenType.Int },
            { "double", CSTokenType.Double },
            { "float", CSTokenType.Float },
            { "string", CSTokenType.StringKeyword },
            { "bool", CSTokenType.Bool },
            { "var", CSTokenType.Var },
            { "if", CSTokenType.If },
            { "else", CSTokenType.Else },
            { "while", CSTokenType.While },
            { "do", CSTokenType.Do },
            { "for", CSTokenType.For },
            { "foreach", CSTokenType.ForEach },
            { "in", CSTokenType.In },
            { "switch", CSTokenType.Switch },
            { "case", CSTokenType.Case },
            { "default", CSTokenType.Default },
            { "return", CSTokenType.Return },
            { "break", CSTokenType.Break },
            { "continue", CSTokenType.Continue },
            { "try", CSTokenType.Try },
            { "catch", CSTokenType.Catch },
            { "finally", CSTokenType.Finally },
            { "throw", CSTokenType.Throw },
            { "new", CSTokenType.New },
            { "this", CSTokenType.This },
            { "base", CSTokenType.Base },
            { "true", CSTokenType.True },
            { "false", CSTokenType.False },
            { "null", CSTokenType.Null },
            { "get", CSTokenType.Get },
            { "set", CSTokenType.Set }
        };

        public List<CSToken> Tokenize(string source, string sourceFile = "source.cs")
        {
            List<CSToken> tokens = new List<CSToken>();
            int i = 0;
            int len = source.Length;
            int line = 1;
            int col = 1;

            while (i < len)
            {
                char c = source[i];

                if (c == '\n')
                {
                    line++;
                    col = 1;
                    i++;
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    col++;
                    i++;
                    continue;
                }

                Location loc = new Location(sourceFile, line, col);

                if (c == '/' && i + 1 < len && source[i + 1] == '/')
                {
                    while (i < len && source[i] != '\n') i++;
                    continue;
                }

                if (c == '/' && i + 1 < len && source[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < len && !(source[i] == '*' && source[i + 1] == '/'))
                    {
                        if (source[i] == '\n') { line++; col = 1; }
                        i++;
                    }
                    i += 2;
                    continue;
                }

                if (c == '$' && i + 1 < len && source[i + 1] == '\"')
                {
                    i += 2;
                    StringBuilder sb = new StringBuilder();
                    while (i < len)
                    {
                        if (source[i] == '\\' && i + 1 < len)
                        {
                            sb.Append(source[i]);
                            sb.Append(source[i + 1]);
                            i += 2;
                        }
                        else if (source[i] == '\"')
                        {
                            i++;
                            break;
                        }
                        else
                        {
                            sb.Append(source[i]);
                            i++;
                        }
                    }
                    tokens.Add(new CSToken(CSTokenType.InterpolatedString, sb.ToString(), loc, sb.ToString()));
                    continue;
                }

                if (c == '\"')
                {
                    i++;
                    StringBuilder sb = new StringBuilder();
                    while (i < len)
                    {
                        if (source[i] == '\\' && i + 1 < len)
                        {
                            char next = source[i + 1];
                            if (next == 'n') sb.Append('\n');
                            else if (next == 't') sb.Append('\t');
                            else if (next == '\"') sb.Append('\"');
                            else if (next == '\\') sb.Append('\\');
                            else sb.Append(next);
                            i += 2;
                        }
                        else if (source[i] == '\"')
                        {
                            i++;
                            break;
                        }
                        else
                        {
                            sb.Append(source[i]);
                            i++;
                        }
                    }
                    tokens.Add(new CSToken(CSTokenType.String, sb.ToString(), loc, sb.ToString()));
                    continue;
                }

                if (char.IsDigit(c))
                {
                    int start = i;
                    bool hasDot = false;
                    while (i < len && (char.IsDigit(source[i]) || source[i] == '.' || source[i] == 'f' || source[i] == 'd' || source[i] == 'L'))
                    {
                        if (source[i] == '.')
                        {
                            if (hasDot) break;
                            hasDot = true;
                        }
                        i++;
                    }
                    string numStr = source.Substring(start, i - start).TrimEnd('f', 'F', 'd', 'D', 'L', 'l');
                    double numVal;
                    if (!double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out numVal))
                    {
                        numVal = 0;
                    }
                    tokens.Add(new CSToken(CSTokenType.Number, numStr, loc, numVal));
                    continue;
                }

                if (char.IsLetter(c) || c == '_' || c == '@')
                {
                    int start = i;
                    if (c == '@') i++;
                    while (i < len && (char.IsLetterOrDigit(source[i]) || source[i] == '_'))
                    {
                        i++;
                    }
                    string ident = source.Substring(start, i - start);
                    string rawIdent = ident.TrimStart('@');

                    CSTokenType kwType;
                    if (Keywords.TryGetValue(rawIdent, out kwType))
                    {
                        tokens.Add(new CSToken(kwType, ident, loc));
                    }
                    else
                    {
                        tokens.Add(new CSToken(CSTokenType.Identifier, rawIdent, loc));
                    }
                    continue;
                }

                switch (c)
                {
                    case '(': tokens.Add(new CSToken(CSTokenType.LeftParen, "(", loc)); i++; break;
                    case ')': tokens.Add(new CSToken(CSTokenType.RightParen, ")", loc)); i++; break;
                    case '{': tokens.Add(new CSToken(CSTokenType.LeftBrace, "{", loc)); i++; break;
                    case '}': tokens.Add(new CSToken(CSTokenType.RightBrace, "}", loc)); i++; break;
                    case '[': tokens.Add(new CSToken(CSTokenType.LeftBracket, "[", loc)); i++; break;
                    case ']': tokens.Add(new CSToken(CSTokenType.RightBracket, "]", loc)); i++; break;
                    case ',': tokens.Add(new CSToken(CSTokenType.Comma, ",", loc)); i++; break;
                    case ';': tokens.Add(new CSToken(CSTokenType.Semicolon, ";", loc)); i++; break;
                    case ':': tokens.Add(new CSToken(CSTokenType.Colon, ":", loc)); i++; break;
                    case '?': tokens.Add(new CSToken(CSTokenType.Question, "?", loc)); i++; break;
                    case '.': tokens.Add(new CSToken(CSTokenType.Dot, ".", loc)); i++; break;

                    case '+':
                        if (i + 1 < len && source[i + 1] == '+') { tokens.Add(new CSToken(CSTokenType.Increment, "++", loc)); i += 2; }
                        else if (i + 1 < len && source[i + 1] == '=') { tokens.Add(new CSToken(CSTokenType.AddAssign, "+=", loc)); i += 2; }
                        else { tokens.Add(new CSToken(CSTokenType.Plus, "+", loc)); i++; }
                        break;
                    case '-':
                        if (i + 1 < len && source[i + 1] == '-') { tokens.Add(new CSToken(CSTokenType.Decrement, "--", loc)); i += 2; }
                        else if (i + 1 < len && source[i + 1] == '=') { tokens.Add(new CSToken(CSTokenType.SubtractAssign, "-=", loc)); i += 2; }
                        else { tokens.Add(new CSToken(CSTokenType.Minus, "-", loc)); i++; }
                        break;
                    case '*':
                        if (i + 1 < len && source[i + 1] == '=') { tokens.Add(new CSToken(CSTokenType.MultiplyAssign, "*=", loc)); i += 2; }
                        else { tokens.Add(new CSToken(CSTokenType.Multiply, "*", loc)); i++; }
                        break;
                    case '/':
                        if (i + 1 < len && source[i + 1] == '=') { tokens.Add(new CSToken(CSTokenType.DivideAssign, "/=", loc)); i += 2; }
                        else { tokens.Add(new CSToken(CSTokenType.Divide, "/", loc)); i++; }
                        break;
                    case '%':
                        if (i + 1 < len && source[i + 1] == '=') { tokens.Add(new CSToken(CSTokenType.ModuloAssign, "%=", loc)); i += 2; }
                        else { tokens.Add(new CSToken(CSTokenType.Modulo, "%", loc)); i++; }
                        break;
                    case '=':
                        if (i + 1 < len && source[i + 1] == '=') { tokens.Add(new CSToken(CSTokenType.Equal, "==", loc)); i += 2; }
                        else if (i + 1 < len && source[i + 1] == '>') { tokens.Add(new CSToken(CSTokenType.Arrow, "=>", loc)); i += 2; }
                        else { tokens.Add(new CSToken(CSTokenType.Assign, "=", loc)); i++; }
                        break;
                    case '!':
                        if (i + 1 < len && source[i + 1] == '=') { tokens.Add(new CSToken(CSTokenType.NotEqual, "!=", loc)); i += 2; }
                        else { tokens.Add(new CSToken(CSTokenType.LogicalNot, "!", loc)); i++; }
                        break;
                    case '<':
                        if (i + 1 < len && source[i + 1] == '=') { tokens.Add(new CSToken(CSTokenType.LessEqual, "<=", loc)); i += 2; }
                        else if (i + 1 < len && source[i + 1] == '<')
                        {
                            if (i + 2 < len && source[i + 2] == '=') { tokens.Add(new CSToken(CSTokenType.ShiftLeftAssign, "<<=", loc)); i += 3; }
                            else { tokens.Add(new CSToken(CSTokenType.ShiftLeft, "<<", loc)); i += 2; }
                        }
                        else { tokens.Add(new CSToken(CSTokenType.Less, "<", loc)); i++; }
                        break;
                    case '>':
                        if (i + 1 < len && source[i + 1] == '=') { tokens.Add(new CSToken(CSTokenType.GreaterEqual, ">=", loc)); i += 2; }
                        else if (i + 1 < len && source[i + 1] == '>')
                        {
                            if (i + 2 < len && source[i + 2] == '=') { tokens.Add(new CSToken(CSTokenType.ShiftRightAssign, ">>=", loc)); i += 3; }
                            else { tokens.Add(new CSToken(CSTokenType.ShiftRight, ">>", loc)); i += 2; }
                        }
                        else { tokens.Add(new CSToken(CSTokenType.Greater, ">", loc)); i++; }
                        break;
                    case '&':
                        if (i + 1 < len && source[i + 1] == '&') { tokens.Add(new CSToken(CSTokenType.LogicalAnd, "&&", loc)); i += 2; }
                        else if (i + 1 < len && source[i + 1] == '=') { tokens.Add(new CSToken(CSTokenType.BitwiseAndAssign, "&=", loc)); i += 2; }
                        else { tokens.Add(new CSToken(CSTokenType.BitwiseAnd, "&", loc)); i++; }
                        break;
                    case '|':
                        if (i + 1 < len && source[i + 1] == '|') { tokens.Add(new CSToken(CSTokenType.LogicalOr, "||", loc)); i += 2; }
                        else if (i + 1 < len && source[i + 1] == '=') { tokens.Add(new CSToken(CSTokenType.BitwiseOrAssign, "|=", loc)); i += 2; }
                        else { tokens.Add(new CSToken(CSTokenType.BitwiseOr, "|", loc)); i++; }
                        break;
                    case '^':
                        if (i + 1 < len && source[i + 1] == '=') { tokens.Add(new CSToken(CSTokenType.BitwiseXorAssign, "^=", loc)); i += 2; }
                        else { tokens.Add(new CSToken(CSTokenType.BitwiseXor, "^", loc)); i++; }
                        break;
                    case '~':
                        tokens.Add(new CSToken(CSTokenType.BitwiseNot, "~", loc));
                        i++;
                        break;
                    default:
                        i++;
                        break;
                }
            }

            tokens.Add(new CSToken(CSTokenType.EOF, "", new Location(sourceFile, line, col)));
            return tokens;
        }
    }
}
