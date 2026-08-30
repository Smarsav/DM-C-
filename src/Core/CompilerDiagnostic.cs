using System;

namespace DMToCSharp.Core
{
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    public class CompilerDiagnostic
    {
        public DiagnosticSeverity Severity { get; private set; }
        public Location Location { get; private set; }
        public string Message { get; private set; }

        public CompilerDiagnostic(DiagnosticSeverity severity, Location location, string message)
        {
            Severity = severity;
            Location = location;
            Message = message;
        }

        public static CompilerDiagnostic Error(Location location, string message)
        {
            return new CompilerDiagnostic(DiagnosticSeverity.Error, location, message);
        }

        public static CompilerDiagnostic Warning(Location location, string message)
        {
            return new CompilerDiagnostic(DiagnosticSeverity.Warning, location, message);
        }

        public static CompilerDiagnostic Info(Location location, string message)
        {
            return new CompilerDiagnostic(DiagnosticSeverity.Info, location, message);
        }

        public override string ToString()
        {
            string sev = Severity.ToString().ToUpper();
            return string.Format("{0} [{1}]: {2}", Location, sev, Message);
        }
    }
}
