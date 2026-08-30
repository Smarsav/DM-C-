using System;
using DMToCSharp.Core;

namespace DMToCSharp.Lexer
{
    public class Token
    {
        public TokenType Type { get; set; }
        public string Text { get; set; }
        public object Value { get; set; }
        public Location Location { get; set; }

        public Token(TokenType type, string text, Location location, object value = null)
        {
            Type = type;
            Text = text ?? string.Empty;
            Location = location;
            Value = value;
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}] \"{2}\"", Location, Type, Text);
        }
    }
}
