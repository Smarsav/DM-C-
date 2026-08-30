using System;

namespace DMToCSharp.Core
{
    public struct Location : IEquatable<Location>
    {
        public static readonly Location Unknown = new Location("unknown", 0, 0);

        public string SourceFile { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }

        public Location(string sourceFile, int line, int column) : this()
        {
            SourceFile = sourceFile ?? "unknown";
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}:{2}", SourceFile, Line, Column);
        }

        public bool Equals(Location other)
        {
            return SourceFile == other.SourceFile && Line == other.Line && Column == other.Column;
        }

        public override bool Equals(object obj)
        {
            return obj is Location && Equals((Location)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (SourceFile != null ? SourceFile.GetHashCode() : 0);
                hash = (hash * 397) ^ Line;
                hash = (hash * 397) ^ Column;
                return hash;
            }
        }

        public static bool operator ==(Location left, Location right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Location left, Location right)
        {
            return !left.Equals(right);
        }
    }
}
