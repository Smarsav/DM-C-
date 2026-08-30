using System;
using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.CSharpToDM
{
    public class CSharpParser
    {
        private readonly List<CSToken> _tokens;
        private int _index;
        private readonly List<CompilerDiagnostic> _diagnostics = new List<CompilerDiagnostic>();

        public List<CompilerDiagnostic> Diagnostics
        {
            get { return _diagnostics; }
        }

        public CSharpParser(List<CSToken> tokens)
        {
            _tokens = tokens ?? new List<CSToken>();
            _index = 0;
        }

        private CSToken Current
        {
            get { return _index < _tokens.Count ? _tokens[_index] : _tokens[_tokens.Count - 1]; }
        }

        private CSToken Peek(int offset = 1)
        {
            return (_index + offset) < _tokens.Count ? _tokens[_index + offset] : _tokens[_tokens.Count - 1];
        }

        private bool IsAtEnd
        {
            get { return Current.Type == CSTokenType.EOF; }
        }

        private CSToken Advance()
        {
            CSToken tok = Current;
            if (!IsAtEnd) _index++;
            return tok;
        }

        private bool Match(CSTokenType type)
        {
            if (Current.Type == type)
            {
                Advance();
                return true;
            }
            return false;
        }

        private CSToken Consume(CSTokenType type, string msg)
        {
            if (Current.Type == type) return Advance();
            _diagnostics.Add(CompilerDiagnostic.Error(Current.Location, string.Format("{0} (got '{1}')", msg, Current.Text)));
            return Current;
        }

        public CSCompilationUnit ParseCompilationUnit()
        {
            var unit = new CSCompilationUnit { Location = Current.Location };

            while (!IsAtEnd)
            {
                if (Match(CSTokenType.Using))
                {
                    string ns = ParseQualifiedName();
                    Consume(CSTokenType.Semicolon, "Expected ';' after using directive");
                    unit.Usings.Add(ns);
                    continue;
                }

                if (Match(CSTokenType.Namespace))
                {
                    string ns = ParseQualifiedName();
                    Consume(CSTokenType.LeftBrace, "Expected '{' after namespace");
                    while (!IsAtEnd && Current.Type != CSTokenType.RightBrace)
                    {
                        var cls = ParseClassDeclaration();
                        if (cls != null) unit.Classes.Add(cls);
                    }
                    Consume(CSTokenType.RightBrace, "Expected '}' to close namespace");
                    continue;
                }

                var topCls = ParseClassDeclaration();
                if (topCls != null)
                {
                    unit.Classes.Add(topCls);
                }
                else
                {
                    Advance();
                }
            }

            return unit;
        }

        private string ParseQualifiedName()
        {
            string name = Current.Text;
            Advance();
            while (Match(CSTokenType.Dot))
            {
                name += "." + Current.Text;
                Advance();
            }
            return name;
        }

        private CSClassDeclaration ParseClassDeclaration()
        {
            Location loc = Current.Location;
            bool isStatic = false;

            while (Current.Type == CSTokenType.Public || Current.Type == CSTokenType.Private ||
                   Current.Type == CSTokenType.Protected || Current.Type == CSTokenType.Internal ||
                   Current.Type == CSTokenType.Static || Current.Type == CSTokenType.Abstract ||
                   Current.Type == CSTokenType.Sealed)
            {
                if (Current.Type == CSTokenType.Static) isStatic = true;
                Advance();
            }

            if (!Match(CSTokenType.Class) && !Match(CSTokenType.Struct))
            {
                return null;
            }

            string className = Current.Text;
            Advance();

            string baseClass = null;
            if (Match(CSTokenType.Colon))
            {
                baseClass = ParseQualifiedName();
            }

            Consume(CSTokenType.LeftBrace, "Expected '{' after class header");

            var cls = new CSClassDeclaration
            {
                Location = loc,
                Name = className,
                BaseClass = baseClass,
                IsStatic = isStatic
            };

            while (!IsAtEnd && Current.Type != CSTokenType.RightBrace)
            {
                var member = ParseMemberDeclaration();
                if (member != null)
                {
                    cls.Members.Add(member);
                }
                else
                {
                    Advance();
                }
            }

            Consume(CSTokenType.RightBrace, "Expected '}' to close class");
            return cls;
        }

        private CSMemberDeclaration ParseMemberDeclaration()
        {
            Location loc = Current.Location;
            bool isStatic = false;
            bool isOverride = false;
            bool isVirtual = false;

            while (Current.Type == CSTokenType.Public || Current.Type == CSTokenType.Private ||
                   Current.Type == CSTokenType.Protected || Current.Type == CSTokenType.Internal ||
                   Current.Type == CSTokenType.Static || Current.Type == CSTokenType.Override ||
                   Current.Type == CSTokenType.Virtual)
            {
                if (Current.Type == CSTokenType.Static) isStatic = true;
                if (Current.Type == CSTokenType.Override) isOverride = true;
                if (Current.Type == CSTokenType.Virtual) isVirtual = true;
                Advance();
            }

            if (Current.Type == CSTokenType.RightBrace || IsAtEnd)
                return null;

            if (Current.Type == CSTokenType.Identifier && Peek().Type == CSTokenType.LeftParen)
            {
                string ctorName = Current.Text;
                Advance();
                var ctor = new CSMethodDeclaration
                {
                    Location = loc,
                    Name = ctorName,
                    TypeName = "void",
                    IsStatic = isStatic
                };
                ParseMethodParameters(ctor);
                if (Current.Type == CSTokenType.LeftBrace)
                {
                    ctor.Body = ParseBlockStatement();
                }
                return ctor;
            }

            string typeName = Current.Text;
            Advance();

            string memberName = Current.Text;
            Advance();

            if (Current.Type == CSTokenType.LeftParen)
            {
                var method = new CSMethodDeclaration
                {
                    Location = loc,
                    Name = memberName,
                    TypeName = typeName,
                    IsStatic = isStatic,
                    IsOverride = isOverride,
                    IsVirtual = isVirtual
                };
                ParseMethodParameters(method);

                if (Match(CSTokenType.Arrow))
                {
                    CSExpression expr = ParseExpression();
                    Consume(CSTokenType.Semicolon, "Expected ';' after expression body");
                    var block = new CSBlockStatement { Location = loc };
                    block.Statements.Add(new CSReturnStatement { Location = loc, Value = expr });
                    method.Body = block;
                }
                else if (Current.Type == CSTokenType.LeftBrace)
                {
                    method.Body = ParseBlockStatement();
                }
                else
                {
                    Match(CSTokenType.Semicolon);
                }
                return method;
            }

            if (Match(CSTokenType.Arrow))
            {
                CSExpression expr = ParseExpression();
                Consume(CSTokenType.Semicolon, "Expected ';' after property arrow");
                return new CSPropertyDeclaration
                {
                    Location = loc,
                    Name = memberName,
                    TypeName = typeName,
                    IsStatic = isStatic,
                    Initializer = expr
                };
            }

            if (Match(CSTokenType.LeftBrace))
            {
                while (!IsAtEnd && Current.Type != CSTokenType.RightBrace) Advance();
                Consume(CSTokenType.RightBrace, "Expected '}' in property");

                CSExpression propInit = null;
                if (Match(CSTokenType.Assign))
                {
                    propInit = ParseExpression();
                    Consume(CSTokenType.Semicolon, "Expected ';' after property initializer");
                }

                return new CSPropertyDeclaration
                {
                    Location = loc,
                    Name = memberName,
                    TypeName = typeName,
                    IsStatic = isStatic,
                    Initializer = propInit
                };
            }

            CSExpression fieldInit = null;
            if (Match(CSTokenType.Assign))
            {
                fieldInit = ParseExpression();
            }
            Consume(CSTokenType.Semicolon, "Expected ';' after field declaration");

            return new CSFieldDeclaration
            {
                Location = loc,
                Name = memberName,
                TypeName = typeName,
                IsStatic = isStatic,
                Initializer = fieldInit
            };
        }

        private void ParseMethodParameters(CSMethodDeclaration method)
        {
            Consume(CSTokenType.LeftParen, "Expected '(' in method parameters");
            while (!IsAtEnd && Current.Type != CSTokenType.RightParen)
            {
                Location paramLoc = Current.Location;
                string pType = Current.Text;
                Advance();
                string pName = Current.Text;
                Advance();

                CSExpression pDefault = null;
                if (Match(CSTokenType.Assign))
                {
                    pDefault = ParseExpression();
                }

                method.Parameters.Add(new CSParameter
                {
                    Location = paramLoc,
                    TypeName = pType,
                    Name = pName,
                    DefaultValue = pDefault
                });

                if (!Match(CSTokenType.Comma)) break;
            }
            Consume(CSTokenType.RightParen, "Expected ')' after method parameters");
        }

        private CSBlockStatement ParseBlockStatement()
        {
            Location loc = Current.Location;
            Consume(CSTokenType.LeftBrace, "Expected '{' at start of block");
            var block = new CSBlockStatement { Location = loc };

            while (!IsAtEnd && Current.Type != CSTokenType.RightBrace)
            {
                var stmt = ParseStatement();
                if (stmt != null)
                {
                    block.Statements.Add(stmt);
                }
                else
                {
                    Advance();
                }
            }

            Consume(CSTokenType.RightBrace, "Expected '}' at end of block");
            return block;
        }

        private CSStatement ParseStatement()
        {
            Location loc = Current.Location;

            if (Current.Type == CSTokenType.LeftBrace)
            {
                return ParseBlockStatement();
            }

            if (Match(CSTokenType.If))
            {
                Consume(CSTokenType.LeftParen, "Expected '(' after 'if'");
                CSExpression cond = ParseExpression();
                Consume(CSTokenType.RightParen, "Expected ')' after if condition");
                CSStatement thenStmt = ParseStatement();
                CSStatement elseStmt = null;
                if (Match(CSTokenType.Else))
                {
                    elseStmt = ParseStatement();
                }
                return new CSIfStatement { Location = loc, Condition = cond, ThenBranch = thenStmt, ElseBranch = elseStmt };
            }

            if (Match(CSTokenType.While))
            {
                Consume(CSTokenType.LeftParen, "Expected '(' after 'while'");
                CSExpression cond = ParseExpression();
                Consume(CSTokenType.RightParen, "Expected ')' after while condition");
                CSStatement body = ParseStatement();
                return new CSWhileStatement { Location = loc, Condition = cond, Body = body };
            }

            if (Match(CSTokenType.For))
            {
                Consume(CSTokenType.LeftParen, "Expected '(' after 'for'");
                CSStatement init = null;
                if (Current.Type != CSTokenType.Semicolon) init = ParseStatement();
                else Consume(CSTokenType.Semicolon, "Expected ';'");

                CSExpression cond = null;
                if (Current.Type != CSTokenType.Semicolon) cond = ParseExpression();
                Consume(CSTokenType.Semicolon, "Expected ';'");

                CSExpression incr = null;
                if (Current.Type != CSTokenType.RightParen) incr = ParseExpression();
                Consume(CSTokenType.RightParen, "Expected ')'");

                CSStatement body = ParseStatement();
                return new CSForStatement { Location = loc, Initializer = init, Condition = cond, Increment = incr, Body = body };
            }

            if (Match(CSTokenType.ForEach))
            {
                Consume(CSTokenType.LeftParen, "Expected '(' after 'foreach'");
                string varType = Current.Text; Advance();
                string varName = Current.Text; Advance();
                Consume(CSTokenType.In, "Expected 'in' in foreach");
                CSExpression col = ParseExpression();
                Consume(CSTokenType.RightParen, "Expected ')' after foreach collection");
                CSStatement body = ParseStatement();
                return new CSForEachStatement { Location = loc, TypeName = varType, VarName = varName, Collection = col, Body = body };
            }

            if (Match(CSTokenType.Switch))
            {
                Consume(CSTokenType.LeftParen, "Expected '(' after 'switch'");
                CSExpression swVal = ParseExpression();
                Consume(CSTokenType.RightParen, "Expected ')' after switch value");
                Consume(CSTokenType.LeftBrace, "Expected '{' in switch");

                var sw = new CSSwitchStatement { Location = loc, Value = swVal };
                while (!IsAtEnd && Current.Type != CSTokenType.RightBrace)
                {
                    if (Match(CSTokenType.Case))
                    {
                        var clause = new CSCaseClause { Location = Current.Location };
                        clause.Values.Add(ParseExpression());
                        Consume(CSTokenType.Colon, "Expected ':' after case value");
                        var body = new CSBlockStatement { Location = Current.Location };
                        while (!IsAtEnd && Current.Type != CSTokenType.Case && Current.Type != CSTokenType.Default && Current.Type != CSTokenType.RightBrace)
                        {
                            body.Statements.Add(ParseStatement());
                        }
                        clause.Body = body;
                        sw.Cases.Add(clause);
                    }
                    else if (Match(CSTokenType.Default))
                    {
                        Consume(CSTokenType.Colon, "Expected ':' after default");
                        var clause = new CSCaseClause { Location = Current.Location, IsDefault = true };
                        var body = new CSBlockStatement { Location = Current.Location };
                        while (!IsAtEnd && Current.Type != CSTokenType.Case && Current.Type != CSTokenType.Default && Current.Type != CSTokenType.RightBrace)
                        {
                            body.Statements.Add(ParseStatement());
                        }
                        clause.Body = body;
                        sw.Cases.Add(clause);
                    }
                    else
                    {
                        Advance();
                    }
                }
                Consume(CSTokenType.RightBrace, "Expected '}' in switch");
                return sw;
            }

            if (Match(CSTokenType.Return))
            {
                CSExpression retVal = null;
                if (Current.Type != CSTokenType.Semicolon) retVal = ParseExpression();
                Consume(CSTokenType.Semicolon, "Expected ';' after return");
                return new CSReturnStatement { Location = loc, Value = retVal };
            }

            if (Match(CSTokenType.Break))
            {
                Consume(CSTokenType.Semicolon, "Expected ';' after break");
                return new CSBreakStatement { Location = loc };
            }

            if (Match(CSTokenType.Continue))
            {
                Consume(CSTokenType.Semicolon, "Expected ';' after continue");
                return new CSContinueStatement { Location = loc };
            }

            if (Match(CSTokenType.Try))
            {
                CSStatement tryBlock = ParseBlockStatement();
                Consume(CSTokenType.Catch, "Expected 'catch' after try");
                string exVar = null;
                if (Match(CSTokenType.LeftParen))
                {
                    string exType = Current.Text; Advance();
                    if (Current.Type == CSTokenType.Identifier) { exVar = Current.Text; Advance(); }
                    Consume(CSTokenType.RightParen, "Expected ')' in catch header");
                }
                CSStatement catchBlock = ParseBlockStatement();
                return new CSTryCatchStatement { Location = loc, TryBlock = tryBlock, ExceptionVar = exVar, CatchBlock = catchBlock };
            }

            if ((Current.Type == CSTokenType.Var || Current.Type == CSTokenType.Int || Current.Type == CSTokenType.Double ||
                 Current.Type == CSTokenType.StringKeyword || Current.Type == CSTokenType.Bool || Current.Type == CSTokenType.Identifier) &&
                 Peek().Type == CSTokenType.Identifier && Peek(2).Type == CSTokenType.Assign)
            {
                string varType = Current.Text; Advance();
                string varName = Current.Text; Advance();
                Consume(CSTokenType.Assign, "Expected '=' in var declaration");
                CSExpression initExpr = ParseExpression();
                Consume(CSTokenType.Semicolon, "Expected ';' after var declaration");
                return new CSVarDeclarationStatement { Location = loc, TypeName = varType, Name = varName, Initializer = initExpr };
            }

            CSExpression expr = ParseExpression();
            Consume(CSTokenType.Semicolon, "Expected ';' after expression");
            return new CSExpressionStatement { Location = loc, Expression = expr };
        }

        // ================= EXPRESSION PARSER =================

        public CSExpression ParseExpression()
        {
            return ParseAssignment();
        }

        private CSExpression ParseAssignment()
        {
            CSExpression expr = ParseTernary();

            if (Current.Type == CSTokenType.Assign || Current.Type == CSTokenType.AddAssign ||
                Current.Type == CSTokenType.SubtractAssign || Current.Type == CSTokenType.MultiplyAssign ||
                Current.Type == CSTokenType.DivideAssign || Current.Type == CSTokenType.ModuloAssign ||
                Current.Type == CSTokenType.BitwiseAndAssign || Current.Type == CSTokenType.BitwiseOrAssign ||
                Current.Type == CSTokenType.BitwiseXorAssign || Current.Type == CSTokenType.ShiftLeftAssign ||
                Current.Type == CSTokenType.ShiftRightAssign)
            {
                string op = Current.Text;
                Advance();
                CSExpression right = ParseAssignment();
                return new CSAssignmentExpression { Location = expr.Location, Operator = op, Target = expr, Value = right };
            }

            return expr;
        }

        private CSExpression ParseTernary()
        {
            CSExpression expr = ParseLogicalOr();
            if (Match(CSTokenType.Question))
            {
                CSExpression t = ParseExpression();
                Consume(CSTokenType.Colon, "Expected ':' in ternary");
                CSExpression f = ParseExpression();
                return new CSTernaryExpression { Location = expr.Location, Condition = expr, TrueExpr = t, FalseExpr = f };
            }
            return expr;
        }

        private CSExpression ParseLogicalOr()
        {
            CSExpression expr = ParseLogicalAnd();
            while (Match(CSTokenType.LogicalOr))
            {
                CSExpression right = ParseLogicalAnd();
                expr = new CSBinaryExpression { Location = expr.Location, Operator = "||", Left = expr, Right = right };
            }
            return expr;
        }

        private CSExpression ParseLogicalAnd()
        {
            CSExpression expr = ParseBitwiseOr();
            while (Match(CSTokenType.LogicalAnd))
            {
                CSExpression right = ParseBitwiseOr();
                expr = new CSBinaryExpression { Location = expr.Location, Operator = "&&", Left = expr, Right = right };
            }
            return expr;
        }

        private CSExpression ParseBitwiseOr()
        {
            CSExpression expr = ParseBitwiseXor();
            while (Match(CSTokenType.BitwiseOr))
            {
                CSExpression right = ParseBitwiseXor();
                expr = new CSBinaryExpression { Location = expr.Location, Operator = "|", Left = expr, Right = right };
            }
            return expr;
        }

        private CSExpression ParseBitwiseXor()
        {
            CSExpression expr = ParseBitwiseAnd();
            while (Match(CSTokenType.BitwiseXor))
            {
                CSExpression right = ParseBitwiseAnd();
                expr = new CSBinaryExpression { Location = expr.Location, Operator = "^", Left = expr, Right = right };
            }
            return expr;
        }

        private CSExpression ParseBitwiseAnd()
        {
            CSExpression expr = ParseEquality();
            while (Match(CSTokenType.BitwiseAnd))
            {
                CSExpression right = ParseEquality();
                expr = new CSBinaryExpression { Location = expr.Location, Operator = "&", Left = expr, Right = right };
            }
            return expr;
        }

        private CSExpression ParseEquality()
        {
            CSExpression expr = ParseRelational();
            while (Current.Type == CSTokenType.Equal || Current.Type == CSTokenType.NotEqual)
            {
                string op = Current.Text; Advance();
                CSExpression right = ParseRelational();
                expr = new CSBinaryExpression { Location = expr.Location, Operator = op, Left = expr, Right = right };
            }
            return expr;
        }

        private CSExpression ParseRelational()
        {
            CSExpression expr = ParseShift();
            while (Current.Type == CSTokenType.Less || Current.Type == CSTokenType.LessEqual ||
                   Current.Type == CSTokenType.Greater || Current.Type == CSTokenType.GreaterEqual)
            {
                string op = Current.Text; Advance();
                CSExpression right = ParseShift();
                expr = new CSBinaryExpression { Location = expr.Location, Operator = op, Left = expr, Right = right };
            }
            return expr;
        }

        private CSExpression ParseShift()
        {
            CSExpression expr = ParseAdditive();
            while (Current.Type == CSTokenType.ShiftLeft || Current.Type == CSTokenType.ShiftRight)
            {
                string op = Current.Text; Advance();
                CSExpression right = ParseAdditive();
                expr = new CSBinaryExpression { Location = expr.Location, Operator = op, Left = expr, Right = right };
            }
            return expr;
        }

        private CSExpression ParseAdditive()
        {
            CSExpression expr = ParseMultiplicative();
            while (Current.Type == CSTokenType.Plus || Current.Type == CSTokenType.Minus)
            {
                string op = Current.Text; Advance();
                CSExpression right = ParseMultiplicative();
                expr = new CSBinaryExpression { Location = expr.Location, Operator = op, Left = expr, Right = right };
            }
            return expr;
        }

        private CSExpression ParseMultiplicative()
        {
            CSExpression expr = ParseUnary();
            while (Current.Type == CSTokenType.Multiply || Current.Type == CSTokenType.Divide || Current.Type == CSTokenType.Modulo)
            {
                string op = Current.Text; Advance();
                CSExpression right = ParseUnary();
                expr = new CSBinaryExpression { Location = expr.Location, Operator = op, Left = expr, Right = right };
            }
            return expr;
        }

        private CSExpression ParseUnary()
        {
            if (Current.Type == CSTokenType.LogicalNot || Current.Type == CSTokenType.BitwiseNot ||
                Current.Type == CSTokenType.Minus || Current.Type == CSTokenType.Increment || Current.Type == CSTokenType.Decrement)
            {
                string op = Current.Text; Advance();
                CSExpression operand = ParseUnary();
                return new CSUnaryExpression { Location = operand.Location, Operator = op, Operand = operand, IsPostfix = false };
            }
            return ParsePostfix();
        }

        private CSExpression ParsePostfix()
        {
            CSExpression expr = ParsePrimary();

            while (true)
            {
                if (Match(CSTokenType.Dot))
                {
                    string member = Current.Text; Advance();
                    expr = new CSMemberAccessExpression { Location = expr.Location, Target = expr, Member = member };
                }
                else if (Match(CSTokenType.LeftParen))
                {
                    List<CSExpression> args = new List<CSExpression>();
                    while (!IsAtEnd && Current.Type != CSTokenType.RightParen)
                    {
                        args.Add(ParseExpression());
                        if (!Match(CSTokenType.Comma)) break;
                    }
                    Consume(CSTokenType.RightParen, "Expected ')' after invocation args");

                    if (expr is CSMemberAccessExpression)
                    {
                        CSMemberAccessExpression mem = (CSMemberAccessExpression)expr;
                        var inv = new CSInvocationExpression { Location = expr.Location, Target = mem.Target, MethodName = mem.Member };
                        inv.Arguments.AddRange(args);
                        expr = inv;
                    }
                    else if (expr is CSIdentifierExpression)
                    {
                        CSIdentifierExpression id = (CSIdentifierExpression)expr;
                        var inv = new CSInvocationExpression { Location = expr.Location, Target = null, MethodName = id.Identifier };
                        inv.Arguments.AddRange(args);
                        expr = inv;
                    }
                }
                else if (Match(CSTokenType.LeftBracket))
                {
                    CSExpression index = ParseExpression();
                    Consume(CSTokenType.RightBracket, "Expected ']' after index");
                    expr = new CSElementAccessExpression { Location = expr.Location, Target = expr, Argument = index };
                }
                else if (Match(CSTokenType.Increment))
                {
                    expr = new CSUnaryExpression { Location = expr.Location, Operator = "++", Operand = expr, IsPostfix = true };
                }
                else if (Match(CSTokenType.Decrement))
                {
                    expr = new CSUnaryExpression { Location = expr.Location, Operator = "--", Operand = expr, IsPostfix = true };
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        private CSExpression ParsePrimary()
        {
            Location loc = Current.Location;

            if (Match(CSTokenType.Null)) return new CSLiteralExpression { Location = loc, Value = null, RawText = "null" };
            if (Match(CSTokenType.True)) return new CSLiteralExpression { Location = loc, Value = true, RawText = "true" };
            if (Match(CSTokenType.False)) return new CSLiteralExpression { Location = loc, Value = false, RawText = "false" };

            if (Current.Type == CSTokenType.Number)
            {
                var tok = Advance();
                return new CSLiteralExpression { Location = loc, Value = tok.Value, RawText = tok.Text };
            }

            if (Current.Type == CSTokenType.String)
            {
                var tok = Advance();
                return new CSLiteralExpression { Location = loc, Value = tok.Value, RawText = string.Format("\"{0}\"", tok.Text) };
            }

            if (Current.Type == CSTokenType.InterpolatedString)
            {
                var tok = Advance();
                return ParseInterpolatedString(loc, tok.Text);
            }

            if (Match(CSTokenType.This)) return new CSIdentifierExpression { Location = loc, Identifier = "this" };
            if (Match(CSTokenType.Base)) return new CSIdentifierExpression { Location = loc, Identifier = "base" };

            if (Match(CSTokenType.New))
            {
                string newType = Current.Text; Advance();
                while (Match(CSTokenType.Dot)) { newType += "." + Current.Text; Advance(); }

                List<CSExpression> newArgs = new List<CSExpression>();
                if (Match(CSTokenType.LeftParen))
                {
                    while (!IsAtEnd && Current.Type != CSTokenType.RightParen)
                    {
                        newArgs.Add(ParseExpression());
                        if (!Match(CSTokenType.Comma)) break;
                    }
                    Consume(CSTokenType.RightParen, "Expected ')' after new constructor args");
                }
                var creation = new CSObjectCreationExpression { Location = loc, TypeName = newType };
                creation.Arguments.AddRange(newArgs);
                return creation;
            }

            if (Match(CSTokenType.LeftParen))
            {
                CSExpression inner = ParseExpression();
                Consume(CSTokenType.RightParen, "Expected ')'");
                return inner;
            }

            if (Current.Type == CSTokenType.Identifier)
            {
                string id = Current.Text; Advance();
                return new CSIdentifierExpression { Location = loc, Identifier = id };
            }

            var fallback = Advance();
            return new CSLiteralExpression { Location = loc, Value = null, RawText = fallback.Text };
        }

        private CSInterpolatedStringExpression ParseInterpolatedString(Location loc, string text)
        {
            var expr = new CSInterpolatedStringExpression { Location = loc };
            int i = 0;
            int len = text.Length;

            while (i < len)
            {
                int open = text.IndexOf('{', i);
                if (open == -1)
                {
                    expr.Parts.Add(text.Substring(i));
                    break;
                }

                if (open > i)
                {
                    expr.Parts.Add(text.Substring(i, open - i));
                }

                int close = text.IndexOf('}', open);
                if (close == -1)
                {
                    expr.Parts.Add(text.Substring(open));
                    break;
                }

                string inner = text.Substring(open + 1, close - open - 1);
                i = close + 1;

                try
                {
                    var subLexer = new CSharpLexer();
                    var subTokens = subLexer.Tokenize(inner);
                    var subParser = new CSharpParser(subTokens);
                    CSExpression subExpr = subParser.ParseExpression();
                    expr.Parts.Add(subExpr);
                }
                catch
                {
                    expr.Parts.Add(new CSIdentifierExpression { Location = loc, Identifier = inner });
                }
            }

            return expr;
        }
    }
}
