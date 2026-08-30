using System;
using System.Collections.Generic;
using System.Text;

namespace DMToCSharp.CSharpToDM
{
    public class DMEmitter
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private int _indent = 0;

        private void Indent() { _indent++; }
        private void Unindent() { if (_indent > 0) _indent--; }

        private void WriteLine(string line = "")
        {
            if (string.IsNullOrEmpty(line))
            {
                _sb.AppendLine();
                return;
            }
            _sb.Append(new string('\t', _indent));
            _sb.AppendLine(line);
        }

        public string Emit(CSCompilationUnit unit)
        {
            _sb.Clear();

            WriteLine("// ==========================================================================");
            WriteLine("// Generated from C# by DMToCSharp (C# to DreamMaker Transpiler)");
            WriteLine("// ==========================================================================");
            WriteLine();

            foreach (var cls in unit.Classes)
            {
                if (cls.Name == "GlobalVars")
                {
                    EmitGlobalVars(cls);
                    continue;
                }

                if (cls.Name == "GlobalProcs")
                {
                    EmitGlobalProcs(cls);
                    continue;
                }

                if (cls.Name == "Program")
                {
                    continue;
                }

                EmitClass(cls);
            }

            return _sb.ToString();
        }

        private void EmitGlobalVars(CSClassDeclaration cls)
        {
            WriteLine("// Global Variables");
            foreach (var member in cls.Members)
            {
                if (member is CSFieldDeclaration)
                {
                    CSFieldDeclaration field = (CSFieldDeclaration)member;
                    string init = field.Initializer != null ? " = " + EmitExpression(field.Initializer) : "";
                    WriteLine(string.Format("/var/global/{0}{1}", field.Name, init));
                }
            }
            WriteLine();
        }

        private void EmitGlobalProcs(CSClassDeclaration cls)
        {
            WriteLine("// Global Procedures");
            foreach (var member in cls.Members)
            {
                if (member is CSMethodDeclaration)
                {
                    CSMethodDeclaration method = (CSMethodDeclaration)member;
                    List<string> paramList = new List<string>();
                    foreach (var p in method.Parameters)
                    {
                        string pStr = p.Name;
                        if (p.DefaultValue != null) pStr += " = " + EmitExpression(p.DefaultValue);
                        paramList.Add(pStr);
                    }
                    string paramsJoined = string.Join(", ", paramList.ToArray());
                    WriteLine(string.Format("/proc/{0}({1})", method.Name, paramsJoined));

                    if (method.Body != null)
                    {
                        Indent();
                        foreach (var stmt in method.Body.Statements)
                        {
                            EmitStatement(stmt);
                        }
                        Unindent();
                    }
                    WriteLine();
                }
            }
        }

        private void EmitClass(CSClassDeclaration cls)
        {
            string dmPath = ConvertClassNameToDMPath(cls.Name);
            WriteLine(dmPath);
            Indent();

            foreach (var member in cls.Members)
            {
                if (member is CSFieldDeclaration)
                {
                    CSFieldDeclaration field = (CSFieldDeclaration)member;
                    string init = field.Initializer != null ? " = " + EmitExpression(field.Initializer) : "";
                    if (IsStandardVar(field.Name))
                    {
                        WriteLine(string.Format("{0}{1}", field.Name, init));
                    }
                    else
                    {
                        WriteLine(string.Format("var/{0}{1}", field.Name, init));
                    }
                }
            }

            foreach (var member in cls.Members)
            {
                if (member is CSMethodDeclaration)
                {
                    CSMethodDeclaration method = (CSMethodDeclaration)member;
                    string procName = method.Name == cls.Name ? "New" : method.Name;

                    List<string> paramList = new List<string>();
                    foreach (var p in method.Parameters)
                    {
                        string pStr = p.Name;
                        if (p.DefaultValue != null) pStr += " = " + EmitExpression(p.DefaultValue);
                        paramList.Add(pStr);
                    }
                    string paramsJoined = string.Join(", ", paramList.ToArray());

                    WriteLine(string.Format("proc/{0}({1})", procName, paramsJoined));
                    if (method.Body != null)
                    {
                        Indent();
                        foreach (var stmt in method.Body.Statements)
                        {
                            EmitStatement(stmt);
                        }
                        Unindent();
                    }
                    WriteLine();
                }
            }

            Unindent();
            WriteLine();
        }

        private bool IsStandardVar(string name)
        {
            return name == "name" || name == "desc" || name == "icon" || name == "icon_state" ||
                   name == "density" || name == "opacity" || name == "x" || name == "y" || name == "z";
        }

        private string ConvertClassNameToDMPath(string className)
        {
            if (className.StartsWith("DM_"))
            {
                return "/" + className.Substring(3).Replace('_', '/');
            }
            if (className.StartsWith("DM"))
            {
                return "/" + className.Substring(2).ToLowerInvariant();
            }
            return "/" + className.ToLowerInvariant();
        }

        private void EmitStatement(CSStatement stmt)
        {
            if (stmt is CSBlockStatement)
            {
                CSBlockStatement block = (CSBlockStatement)stmt;
                Indent();
                foreach (var s in block.Statements) EmitStatement(s);
                Unindent();
                return;
            }

            if (stmt is CSVarDeclarationStatement)
            {
                CSVarDeclarationStatement varDecl = (CSVarDeclarationStatement)stmt;
                string init = varDecl.Initializer != null ? " = " + EmitExpression(varDecl.Initializer) : "";
                WriteLine(string.Format("var/{0}{1}", varDecl.Name, init));
                return;
            }

            if (stmt is CSIfStatement)
            {
                CSIfStatement ifStmt = (CSIfStatement)stmt;
                WriteLine(string.Format("if({0})", EmitExpression(ifStmt.Condition)));
                Indent();
                EmitStatement(ifStmt.ThenBranch);
                Unindent();
                if (ifStmt.ElseBranch != null)
                {
                    WriteLine("else");
                    Indent();
                    EmitStatement(ifStmt.ElseBranch);
                    Unindent();
                }
                return;
            }

            if (stmt is CSWhileStatement)
            {
                CSWhileStatement whileStmt = (CSWhileStatement)stmt;
                WriteLine(string.Format("while({0})", EmitExpression(whileStmt.Condition)));
                Indent();
                EmitStatement(whileStmt.Body);
                Unindent();
                return;
            }

            if (stmt is CSForStatement)
            {
                CSForStatement forStmt = (CSForStatement)stmt;
                WriteLine(string.Format("for({0}; {1}; {2})", (forStmt.Initializer != null ? EmitStatementInline(forStmt.Initializer) : ""), EmitExpression(forStmt.Condition), EmitExpression(forStmt.Increment)));
                Indent();
                EmitStatement(forStmt.Body);
                Unindent();
                return;
            }

            if (stmt is CSForEachStatement)
            {
                CSForEachStatement forEachStmt = (CSForEachStatement)stmt;
                WriteLine(string.Format("for(var/{0} in {1})", forEachStmt.VarName, EmitExpression(forEachStmt.Collection)));
                Indent();
                EmitStatement(forEachStmt.Body);
                Unindent();
                return;
            }

            if (stmt is CSSwitchStatement)
            {
                CSSwitchStatement switchStmt = (CSSwitchStatement)stmt;
                WriteLine(string.Format("switch({0})", EmitExpression(switchStmt.Value)));
                Indent();
                foreach (var c in switchStmt.Cases)
                {
                    if (c.IsDefault)
                    {
                        WriteLine("else");
                    }
                    else
                    {
                        List<string> vals = new List<string>();
                        foreach (var v in c.Values) vals.Add(EmitExpression(v));
                        WriteLine(string.Format("if({0})", string.Join(", ", vals.ToArray())));
                    }
                    Indent();
                    EmitStatement(c.Body);
                    Unindent();
                }
                Unindent();
                return;
            }

            if (stmt is CSReturnStatement)
            {
                CSReturnStatement retStmt = (CSReturnStatement)stmt;
                string val = retStmt.Value != null ? " " + EmitExpression(retStmt.Value) : "";
                if (val.Trim() == "DMValue.Null" || val.Trim() == "null") val = "";
                WriteLine(string.Format("return{0}", val));
                return;
            }

            if (stmt is CSBreakStatement)
            {
                WriteLine("break");
                return;
            }

            if (stmt is CSContinueStatement)
            {
                WriteLine("continue");
                return;
            }

            if (stmt is CSTryCatchStatement)
            {
                CSTryCatchStatement tryStmt = (CSTryCatchStatement)stmt;
                WriteLine("try");
                Indent();
                EmitStatement(tryStmt.TryBlock);
                Unindent();
                string exVar = !string.IsNullOrEmpty(tryStmt.ExceptionVar) ? string.Format("(var/{0})", tryStmt.ExceptionVar) : "";
                WriteLine(string.Format("catch{0}", exVar));
                Indent();
                EmitStatement(tryStmt.CatchBlock);
                Unindent();
                return;
            }

            if (stmt is CSExpressionStatement)
            {
                CSExpressionStatement exprStmt = (CSExpressionStatement)stmt;
                string exprStr = EmitExpression(exprStmt.Expression);
                if (!string.IsNullOrEmpty(exprStr))
                {
                    WriteLine(exprStr);
                }
                return;
            }
        }

        private string EmitStatementInline(CSStatement stmt)
        {
            if (stmt is CSVarDeclarationStatement)
            {
                CSVarDeclarationStatement v = (CSVarDeclarationStatement)stmt;
                string init = v.Initializer != null ? " = " + EmitExpression(v.Initializer) : "";
                return string.Format("var/{0}{1}", v.Name, init);
            }
            if (stmt is CSExpressionStatement)
            {
                CSExpressionStatement e = (CSExpressionStatement)stmt;
                return EmitExpression(e.Expression);
            }
            return "";
        }

        private string EmitExpression(CSExpression expr)
        {
            if (expr == null) return "";

            if (expr is CSLiteralExpression)
            {
                CSLiteralExpression lit = (CSLiteralExpression)expr;
                if (lit.RawText == "null" || lit.Value == null) return "null";
                if (lit.RawText == "true") return "1";
                if (lit.RawText == "false") return "0";
                return lit.RawText;
            }

            if (expr is CSIdentifierExpression)
            {
                CSIdentifierExpression id = (CSIdentifierExpression)expr;
                if (id.Identifier == "this") return "src";
                if (id.Identifier == "base") return "..";
                if (id.Identifier == "DMValue.Null") return "null";
                return id.Identifier;
            }

            if (expr is CSInterpolatedStringExpression)
            {
                CSInterpolatedStringExpression interp = (CSInterpolatedStringExpression)expr;
                StringBuilder sb = new StringBuilder("\"");
                foreach (var part in interp.Parts)
                {
                    if (part is string)
                    {
                        sb.Append((string)part);
                    }
                    else if (part is CSExpression)
                    {
                        sb.Append("[" + EmitExpression((CSExpression)part) + "]");
                    }
                }
                sb.Append("\"");
                return sb.ToString();
            }

            if (expr is CSUnaryExpression)
            {
                CSUnaryExpression un = (CSUnaryExpression)expr;
                string op = un.Operator;
                string sub = EmitExpression(un.Operand);
                return un.IsPostfix ? string.Format("{0}{1}", sub, op) : string.Format("{0}{1}", op, sub);
            }

            if (expr is CSBinaryExpression)
            {
                CSBinaryExpression bin = (CSBinaryExpression)expr;
                string left = EmitExpression(bin.Left);
                string right = EmitExpression(bin.Right);
                return string.Format("{0} {1} {2}", left, bin.Operator, right);
            }

            if (expr is CSAssignmentExpression)
            {
                CSAssignmentExpression assign = (CSAssignmentExpression)expr;
                string target = EmitExpression(assign.Target);
                string val = EmitExpression(assign.Value);
                return string.Format("{0} {1} {2}", target, assign.Operator, val);
            }

            if (expr is CSMemberAccessExpression)
            {
                CSMemberAccessExpression mem = (CSMemberAccessExpression)expr;
                string target = EmitExpression(mem.Target);
                if (target == "src" || target == "this") return mem.Member;
                if (target == "DMWorld.Instance" || target == "world") return string.Format("world.{0}", mem.Member);
                return string.Format("{0}.{1}", target, mem.Member);
            }

            if (expr is CSElementAccessExpression)
            {
                CSElementAccessExpression elem = (CSElementAccessExpression)expr;
                string target = EmitExpression(elem.Target);
                string index = EmitExpression(elem.Argument);
                return string.Format("{0}[{1}]", target, index);
            }

            if (expr is CSInvocationExpression)
            {
                CSInvocationExpression inv = (CSInvocationExpression)expr;
                List<string> args = new List<string>();
                foreach (var a in inv.Arguments) args.Add(EmitExpression(a));
                string argList = string.Join(", ", args.ToArray());

                if (inv.MethodName == "WriteLine" || inv.MethodName == "Output")
                {
                    return string.Format("world << {0}", argList);
                }

                if (inv.Target is CSIdentifierExpression)
                {
                    CSIdentifierExpression targetId = (CSIdentifierExpression)inv.Target;
                    if (targetId.Identifier == "base")
                    {
                        return string.Format("..({0})", argList);
                    }
                    if (targetId.Identifier == "Math" || targetId.Identifier == "DMBuiltins")
                    {
                        return string.Format("{0}({1})", inv.MethodName.ToLowerInvariant(), argList);
                    }
                }

                if (inv.Target != null)
                {
                    string target = EmitExpression(inv.Target);
                    return string.Format("{0}.{1}({2})", target, inv.MethodName, argList);
                }

                return string.Format("{0}({1})", inv.MethodName, argList);
            }

            if (expr is CSObjectCreationExpression)
            {
                CSObjectCreationExpression creation = (CSObjectCreationExpression)expr;
                string dmType = ConvertClassNameToDMPath(creation.TypeName);
                List<string> args = new List<string>();
                foreach (var a in creation.Arguments) args.Add(EmitExpression(a));
                string argList = string.Join(", ", args.ToArray());
                return string.Format("new {0}({1})", dmType, argList);
            }

            if (expr is CSTernaryExpression)
            {
                CSTernaryExpression tern = (CSTernaryExpression)expr;
                return string.Format("({0} ? {1} : {2})", EmitExpression(tern.Condition), EmitExpression(tern.TrueExpr), EmitExpression(tern.FalseExpr));
            }

            return expr.ToString();
        }
    }
}
