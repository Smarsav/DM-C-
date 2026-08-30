using System;
using System.Collections.Generic;
using System.Text;

namespace DMToCSharp.Core
{
    public struct DreamPath : IEquatable<DreamPath>, IComparable<DreamPath>
    {
        public static readonly DreamPath Root = new DreamPath("/");
        public static readonly DreamPath Datum = new DreamPath("/datum");
        public static readonly DreamPath Atom = new DreamPath("/atom");
        public static readonly DreamPath Movable = new DreamPath("/atom/movable");
        public static readonly DreamPath Obj = new DreamPath("/obj");
        public static readonly DreamPath Mob = new DreamPath("/mob");
        public static readonly DreamPath Turf = new DreamPath("/turf");
        public static readonly DreamPath Area = new DreamPath("/area");
        public static readonly DreamPath World = new DreamPath("/world");
        public static readonly DreamPath Client = new DreamPath("/client");
        public static readonly DreamPath List = new DreamPath("/list");
        public static readonly DreamPath Sound = new DreamPath("/sound");
        public static readonly DreamPath Image = new DreamPath("/image");
        public static readonly DreamPath Icon = new DreamPath("/icon");
        public static readonly DreamPath Matrix = new DreamPath("/matrix");
        public static readonly DreamPath Regex = new DreamPath("/regex");
        public static readonly DreamPath Savefile = new DreamPath("/savefile");
        public static readonly DreamPath Exception = new DreamPath("/exception");

        private readonly string[] _elements;
        private readonly string _raw;
        private readonly bool _isAbsolute;

        public bool IsAbsolute
        {
            get { return _isAbsolute; }
        }

        public string[] Elements
        {
            get { return _elements ?? new string[0]; }
        }

        public int ElementCount
        {
            get { return _elements != null ? _elements.Length : 0; }
        }

        public string LastElement
        {
            get
            {
                if (_elements == null || _elements.Length == 0) return string.Empty;
                return _elements[_elements.Length - 1];
            }
        }

        public DreamPath Parent
        {
            get
            {
                if (_elements == null || _elements.Length <= 1)
                {
                    return _isAbsolute ? Root : new DreamPath("");
                }
                string[] parentElements = new string[_elements.Length - 1];
                Array.Copy(_elements, parentElements, parentElements.Length);
                return new DreamPath(_isAbsolute, parentElements);
            }
        }

        public string PathString
        {
            get
            {
                if (_elements == null || _elements.Length == 0)
                {
                    return _isAbsolute ? "/" : "";
                }
                StringBuilder sb = new StringBuilder();
                if (_isAbsolute) sb.Append("/");
                sb.Append(string.Join("/", _elements));
                return sb.ToString();
            }
        }

        public DreamPath(string rawPath)
        {
            if (string.IsNullOrEmpty(rawPath))
            {
                _raw = "";
                _isAbsolute = false;
                _elements = new string[0];
                return;
            }

            _raw = rawPath.Trim();
            _isAbsolute = _raw.StartsWith("/");

            string[] rawParts = _raw.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> parts = new List<string>();
            foreach (var part in rawParts)
            {
                string p = part.Trim();
                if (!string.IsNullOrEmpty(p))
                {
                    parts.Add(p);
                }
            }
            _elements = parts.ToArray();
        }

        public DreamPath(bool isAbsolute, string[] elements)
        {
            _isAbsolute = isAbsolute;
            _elements = elements ?? new string[0];
            _raw = null;
        }

        public DreamPath AddToPath(string subPath)
        {
            return Combine(this, new DreamPath(subPath));
        }

        public DreamPath Combine(DreamPath other)
        {
            return Combine(this, other);
        }

        public static DreamPath Combine(params DreamPath[] paths)
        {
            if (paths == null || paths.Length == 0) return Root;
            DreamPath res = paths[0];
            for (int i = 1; i < paths.Length; i++)
            {
                res = Combine(res, paths[i]);
            }
            return res;
        }

        public static DreamPath Combine(DreamPath first, DreamPath second)
        {
            if (second.IsAbsolute)
                return second;

            List<string> combined = new List<string>(first.Elements);
            combined.AddRange(second.Elements);
            return new DreamPath(first.IsAbsolute, combined.ToArray());
        }

        public bool IsDescendantOf(DreamPath ancestor)
        {
            if (ancestor.PathString == "/") return true;
            if (ElementCount < ancestor.ElementCount) return false;

            for (int i = 0; i < ancestor.ElementCount; i++)
            {
                if (!string.Equals(Elements[i], ancestor.Elements[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        public string ToCSharpClassName()
        {
            if (Elements.Length == 0) return "DMObject";

            StringBuilder sb = new StringBuilder("DM_");
            for (int i = 0; i < Elements.Length; i++)
            {
                if (i > 0) sb.Append("_");
                string elem = Elements[i];
                sb.Append(SanitizeIdentifier(elem));
            }
            return sb.ToString();
        }

        public static string SanitizeIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return "_";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }
            string res = sb.ToString();
            if (char.IsDigit(res[0]))
                res = "_" + res;

            if (IsCSharpKeyword(res))
                res = "@" + res;

            return res;
        }

        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
            "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
            "virtual", "void", "volatile", "while"
        };

        public static bool IsCSharpKeyword(string id)
        {
            return CSharpKeywords.Contains(id);
        }

        public override string ToString()
        {
            return PathString;
        }

        public bool Equals(DreamPath other)
        {
            return string.Equals(PathString, other.PathString, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is DreamPath && Equals((DreamPath)obj);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(PathString);
        }

        public int CompareTo(DreamPath other)
        {
            return string.Compare(PathString, other.PathString, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator ==(DreamPath left, DreamPath right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DreamPath left, DreamPath right)
        {
            return !left.Equals(right);
        }
    }
}
