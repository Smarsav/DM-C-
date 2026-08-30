using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DMToCSharp.Preprocessor
{
    public class DMMacro
    {
        public string Name { get; private set; }
        public List<string> Parameters { get; private set; }
        public string Body { get; private set; }
        public bool IsFunctionLike
        {
            get { return Parameters != null; }
        }

        public DMMacro(string name, string body)
        {
            Name = name;
            Parameters = null;
            Body = body ?? string.Empty;
        }

        public DMMacro(string name, List<string> parameters, string body)
        {
            Name = name;
            Parameters = parameters ?? new List<string>();
            Body = body ?? string.Empty;
        }

        public string Expand(List<string> arguments = null)
        {
            if (!IsFunctionLike)
                return Body;

            arguments = arguments ?? new List<string>();
            string result = Body;

            for (int i = 0; i < Parameters.Count; i++)
            {
                string param = Parameters[i];
                string arg = i < arguments.Count ? arguments[i].Trim() : "";

                // Stringification: #param
                string stringifyPattern = @"#\s*" + Regex.Escape(param) + @"\b";
                result = Regex.Replace(result, stringifyPattern, "\"" + EscapeString(arg) + "\"");

                // Token pasting: param ## other or other ## param
                string pastePatternLeft = Regex.Escape(param) + @"\s*##\s*";
                result = Regex.Replace(result, pastePatternLeft, arg);

                string pastePatternRight = @"\s*##\s*" + Regex.Escape(param);
                result = Regex.Replace(result, pastePatternRight, arg);

                // Normal replacement
                string wordPattern = @"\b" + Regex.Escape(param) + @"\b";
                result = Regex.Replace(result, wordPattern, arg);
            }

            result = Regex.Replace(result, @"\s*##\s*", "");
            return result;
        }

        private static string EscapeString(string str)
        {
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
