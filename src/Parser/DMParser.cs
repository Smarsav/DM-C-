using System;
using System.Collections.Generic;
using DMToCSharp.Core;
using DMToCSharp.Core.AST;
using DMToCSharp.Lexer;
using DMToCSharp.Preprocessor;

namespace DMToCSharp.Parser
{
    public class DMParser
    {
        private readonly List<Token> _tokens;
        private int _index;
        private readonly List<CompilerDiagnostic> _diagnostics = new List<CompilerDiagnostic>();

        public List<CompilerDiagnostic> Diagnostics
        {
            get { return _diagnostics; }
        }

        public DMParser(List<Token> tokens)
        {
            _tokens = tokens ?? new List<Token>();
            _index = 0;
        }

        private Token Current
        {
            get { return _index < _tokens.Count ? _tokens[_index] : _tokens[_tokens.Count - 1]; }
        }

        private Token Peek(int offset = 1)
        {
            return (_index + offset) < _tokens.Count ? _tokens[_index + offset] : _tokens[_tokens.Count - 1];
        }

        private bool IsAtEnd
        {
            get { return Current.Type == TokenType.EOF; }
        }

        private Token Advance()
        {
            Token token = Current;
            if (!IsAtEnd) _index++;
            return token;
        }

        private bool Match(TokenType type)
        {
            if (Current.Type == type)
            {
                Advance();
                return true;
            }
            return false;
        }

        private Token Consume(TokenType type, string message)
        {
            if (Current.Type == type)
            {
                return Advance();
            }
            _diagnostics.Add(CompilerDiagnostic.Error(Current.Location, string.Format("{0} (got '{1}' of type {2})", message, Current.Text, Current.Type)));
            return Current;
        }

        private void SkipNewlinesAndSemicolons()
        {
            while (Current.Type == TokenType.Newline || Current.Type == TokenType.Semicolon)
            {
                Advance();
            }
        }

        public DMASTFile ParseFile()
        {
            Location startLoc = Current.Location;
            List<DMASTDefinition> definitions = new List<DMASTDefinition>();

            SkipNewlinesAndSemicolons();

            while (!IsAtEnd)
            {
                try
                {
                    var defs = ParseTopLevelDefinition(DreamPath.Root);
                    if (defs != null && defs.Count > 0)
                    {
                        definitions.AddRange(defs);
                    }
                }
                catch (Exception ex)
                {
                    _diagnostics.Add(CompilerDiagnostic.Error(Current.Location, "Parse error: " + ex.Message));
                    Synchronize();
                }

                SkipNewlinesAndSemicolons();
            }

            return new DMASTFile(startLoc, definitions);
        }

        private void Synchronize()
        {
            Advance();
            while (!IsAtEnd)
            {
                if (Current.Type == TokenType.Newline || Current.Type == TokenType.Dedent)
                {
                    Advance();
                    return;
                }
                Advance();
            }
        }

        private List<DMASTDefinition> ParseTopLevelDefinition(DreamPath currentScope)
        {
            SkipNewlinesAndSemicolons();
            if (IsAtEnd) return null;

            Location loc = Current.Location;
            List<DMASTDefinition> result = new List<DMASTDefinition>();

            if (Current.Type == TokenType.Path)
            {
                DreamPath path = (DreamPath)Current.Value;
                Advance();

                DreamPath fullPath = currentScope == DreamPath.Root ? path : DreamPath.Combine(currentScope, path);
                return ParseDefinitionForPath(loc, fullPath);
            }

            if (Current.Type == TokenType.Var)
            {
                Advance();
                return ParseVarDeclarationAfterVarKeyword(loc, currentScope);
            }

            if (Current.Type == TokenType.Proc || Current.Type == TokenType.Verb)
            {
                bool isVerb = Current.Type == TokenType.Verb;
                Advance();
                return ParseProcDeclarationAfterProcKeyword(loc, currentScope, isVerb);
            }

            if (Current.Type == TokenType.Identifier)
            {
                string ident = Current.Text;
                Advance();

                if (Current.Type == TokenType.Assign)
                {
                    Advance();
                    DMASTExpression expr = ParseExpression();
                    SkipNewlinesAndSemicolons();
                    var varDef = new DMASTVarDefinition(loc, currentScope, ident, DreamPath.Root, expr);
                    result.Add(varDef);
                    return result;
                }

                if (Current.Type == TokenType.LeftParen)
                {
                    return ParseProcAfterIdentifier(loc, currentScope, ident, false);
                }

                DreamPath subPath = currentScope.AddToPath(ident);
                return ParseDefinitionForPath(loc, subPath);
            }

            Advance();
            return result;
        }

        private List<DMASTDefinition> ParseDefinitionForPath(Location loc, DreamPath fullPath)
        {
            List<DMASTDefinition> result = new List<DMASTDefinition>();

            string[] elems = fullPath.Elements;
            int procIdx = -1;
            int verbIdx = -1;
            int varIdx = -1;

            for (int i = 0; i < elems.Length; i++)
            {
                if (string.Equals(elems[i], "proc", StringComparison.OrdinalIgnoreCase)) procIdx = i;
                else if (string.Equals(elems[i], "verb", StringComparison.OrdinalIgnoreCase)) verbIdx = i;
                else if (string.Equals(elems[i], "var", StringComparison.OrdinalIgnoreCase)) varIdx = i;
            }

            if (procIdx >= 0 || verbIdx >= 0)
            {
                bool isVerb = verbIdx >= 0;
                int splitIdx = isVerb ? verbIdx : procIdx;

                string[] objElems = new string[splitIdx];
                Array.Copy(elems, objElems, splitIdx);
                DreamPath objPath = new DreamPath(true, objElems);

                string procName = (splitIdx + 1 < elems.Length) ? elems[splitIdx + 1] : "";
                if (string.IsNullOrEmpty(procName) && Current.Type == TokenType.Identifier)
                {
                    procName = Current.Text;
                    Advance();
                }

                return ParseProcBody(loc, objPath, procName, isVerb);
            }

            if (varIdx >= 0)
            {
                string[] objElems = new string[varIdx];
                Array.Copy(elems, objElems, varIdx);
                DreamPath objPath = new DreamPath(true, objElems);

                bool isGlobal = false;
                bool isConst = false;
                bool isStatic = false;
                int startVarName = varIdx + 1;

                while (startVarName < elems.Length)
                {
                    string m = elems[startVarName];
                    if (string.Equals(m, "global", StringComparison.OrdinalIgnoreCase)) { isGlobal = true; startVarName++; }
                    else if (string.Equals(m, "const", StringComparison.OrdinalIgnoreCase)) { isConst = true; startVarName++; }
                    else if (string.Equals(m, "static", StringComparison.OrdinalIgnoreCase)) { isStatic = true; startVarName++; }
                    else break;
                }

                string varName = (startVarName < elems.Length) ? elems[startVarName] : "";
                if (string.IsNullOrEmpty(varName) && Current.Type == TokenType.Identifier)
                {
                    varName = Current.Text;
                    Advance();
                }

                DMASTExpression initExpr = null;
                if (Match(TokenType.Assign))
                {
                    initExpr = ParseExpression();
                }
                SkipNewlinesAndSemicolons();

                var vdef = new DMASTVarDefinition(loc, objPath, varName, DreamPath.Root, initExpr, isGlobal, isConst, isStatic);
                result.Add(vdef);
                return result;
            }

            if (Match(TokenType.Assign))
            {
                DMASTExpression initExpr = ParseExpression();
                SkipNewlinesAndSemicolons();

                string varName = fullPath.LastElement;
                DreamPath objPath = fullPath.Parent;
                var vdef = new DMASTVarDefinition(loc, objPath, varName, DreamPath.Root, initExpr);
                result.Add(vdef);
                return result;
            }

            if (Current.Type == TokenType.LeftParen)
            {
                string procName = fullPath.LastElement;
                DreamPath objPath = fullPath.Parent;
                return ParseProcBody(loc, objPath, procName, false);
            }

            var objDef = new DMASTObjectDefinition(loc, fullPath);
            result.Add(objDef);

            SkipNewlinesAndSemicolons();

            if (Match(TokenType.Indent) || Match(TokenType.LeftBrace))
            {
                bool isBrace = _tokens[_index - 1].Type == TokenType.LeftBrace;
                TokenType endToken = isBrace ? TokenType.RightBrace : TokenType.Dedent;

                while (!IsAtEnd && Current.Type != endToken)
                {
                    var subDefs = ParseTopLevelDefinition(fullPath);
                    if (subDefs != null)
                    {
                        objDef.Members.AddRange(subDefs);
                    }
                    SkipNewlinesAndSemicolons();
                }

                if (Match(endToken))
                {
                }
            }

            return result;
        }

        private List<DMASTDefinition> ParseVarDeclarationAfterVarKeyword(Location loc, DreamPath currentScope)
        {
            List<DMASTDefinition> result = new List<DMASTDefinition>();

            bool isGlobal = false;
            bool isConst = false;
            bool isStatic = false;

            DreamPath typePath = DreamPath.Root;
            string varName = "";

            if (Match(TokenType.Divide) || Match(TokenType.Path))
            {
            }

            while (Current.Type == TokenType.Global || Current.Type == TokenType.Const || Current.Type == TokenType.Static || Current.Type == TokenType.Tmp)
            {
                if (Current.Type == TokenType.Global) isGlobal = true;
                if (Current.Type == TokenType.Const) isConst = true;
                if (Current.Type == TokenType.Static) isStatic = true;
                Advance();
                Match(TokenType.Divide);
            }

            if (Current.Type == TokenType.Path)
            {
                DreamPath p = (DreamPath)Current.Value;
                Advance();
                varName = p.LastElement;
                typePath = p.Parent;
            }
            else if (Current.Type == TokenType.Identifier)
            {
                varName = Current.Text;
                Advance();

                if (Match(TokenType.Divide) && Current.Type == TokenType.Identifier)
                {
                    typePath = new DreamPath("/" + varName);
                    varName = Current.Text;
                    Advance();
                }
            }

            DMASTExpression initVal = null;
            if (Match(TokenType.Assign))
            {
                initVal = ParseExpression();
            }

            SkipNewlinesAndSemicolons();

            var vdef = new DMASTVarDefinition(loc, currentScope, varName, typePath, initVal, isGlobal, isConst, isStatic);
            result.Add(vdef);
            return result;
        }

        private List<DMASTDefinition> ParseProcDeclarationAfterProcKeyword(Location loc, DreamPath currentScope, bool isVerb)
        {
            Match(TokenType.Divide);

            string procName = "";
            if (Current.Type == TokenType.Identifier || Current.Type == TokenType.Path)
            {
                procName = Current.Type == TokenType.Path ? ((DreamPath)Current.Value).LastElement : Current.Text;
                Advance();
            }

            return ParseProcBody(loc, currentScope, procName, isVerb);
        }

        private List<DMASTDefinition> ParseProcAfterIdentifier(Location loc, DreamPath currentScope, string procName, bool isVerb)
        {
            return ParseProcBody(loc, currentScope, procName, isVerb);
        }

        private List<DMASTDefinition> ParseProcBody(Location loc, DreamPath objPath, string procName, bool isVerb)
        {
            List<DMASTDefinition> result = new List<DMASTDefinition>();
            List<DMASTProcParameter> parameters = new List<DMASTProcParameter>();

            if (Match(TokenType.LeftParen))
            {
                while (!IsAtEnd && Current.Type != TokenType.RightParen)
                {
                    Location paramLoc = Current.Location;
                    DreamPath paramType = DreamPath.Root;
                    string paramName = "";
                    DMASTExpression defaultVal = null;
                    string inputType = null;
                    DMASTExpression inList = null;

                    if (Match(TokenType.Var))
                    {
                        Match(TokenType.Divide);
                    }

                    if (Current.Type == TokenType.Path)
                    {
                        DreamPath p = (DreamPath)Current.Value;
                        Advance();
                        paramName = p.LastElement;
                        paramType = p.Parent;
                    }
                    else if (Current.Type == TokenType.Identifier)
                    {
                        paramName = Current.Text;
                        Advance();

                        while (Match(TokenType.Divide) && (Current.Type == TokenType.Identifier || Current.Type == TokenType.Path))
                        {
                            if (Current.Type == TokenType.Path)
                            {
                                DreamPath p = (DreamPath)Current.Value;
                                Advance();
                                paramType = DreamPath.Combine(paramType, new DreamPath("/" + paramName), p.Parent);
                                paramName = p.LastElement;
                            }
                            else
                            {
                                paramType = DreamPath.Combine(paramType, new DreamPath("/" + paramName));
                                paramName = Current.Text;
                                Advance();
                            }
                        }
                    }

                    if (Match(TokenType.As))
                    {
                        if (Current.Type == TokenType.Identifier)
                        {
                            inputType = Current.Text;
                            Advance();
                        }
                    }

                    if (Match(TokenType.In))
                    {
                        inList = ParseExpression();
                    }

                    if (Match(TokenType.Assign))
                    {
                        defaultVal = ParseExpression();
                    }

                    parameters.Add(new DMASTProcParameter(paramLoc, paramName, paramType, defaultVal, inputType, inList));

                    if (!Match(TokenType.Comma))
                    {
                        break;
                    }
                }
                Consume(TokenType.RightParen, "Expected ')' after proc parameters");
            }

            SkipNewlinesAndSemicolons();

            DMASTBlock body = null;
            if (Match(TokenType.Indent) || Match(TokenType.LeftBrace))
            {
                bool isBrace = _tokens[_index - 1].Type == TokenType.LeftBrace;
                TokenType endToken = isBrace ? TokenType.RightBrace : TokenType.Dedent;

                List<DMASTStatement> statements = new List<DMASTStatement>();
                while (!IsAtEnd && Current.Type != endToken)
                {
                    SkipNewlinesAndSemicolons();
                    if (Current.Type == endToken) break;

                    var stmt = ParseStatement();
                    if (stmt != null)
                    {
                        statements.Add(stmt);
                    }
                    SkipNewlinesAndSemicolons();
                }

                Match(endToken);
                body = new DMASTBlock(loc, statements);
            }

            var pdef = new DMASTProcDefinition(loc, objPath, procName, parameters, body, isVerb);
            result.Add(pdef);
            return result;
        }

        private DMASTBlock ParseBlock(TokenType endToken)
        {
            Location loc = Current.Location;
            List<DMASTStatement> statements = new List<DMASTStatement>();

            SkipNewlinesAndSemicolons();

            while (!IsAtEnd && Current.Type != endToken)
            {
                try
                {
                    DMASTStatement stmt = ParseStatement();
                    if (stmt != null)
                    {
                        statements.Add(stmt);
                    }
                }
                catch (Exception ex)
                {
                    _diagnostics.Add(CompilerDiagnostic.Error(Current.Location, "Statement parse error: " + ex.Message));
                    Synchronize();
                }

                SkipNewlinesAndSemicolons();
            }

            Match(endToken);
            return new DMASTBlock(loc, statements);
        }

        // ================= STATEMENT PARSER =================

        private DMASTStatement ParseBranchBody(Location loc)
        {
            SkipNewlinesAndSemicolons();
            if (Match(TokenType.Indent) || Match(TokenType.LeftBrace))
            {
                bool isBrace = _tokens[_index - 1].Type == TokenType.LeftBrace;
                TokenType endToken = isBrace ? TokenType.RightBrace : TokenType.Dedent;
                List<DMASTStatement> stmts = new List<DMASTStatement>();
                while (!IsAtEnd && Current.Type != endToken)
                {
                    SkipNewlinesAndSemicolons();
                    if (Current.Type == endToken) break;
                    var s = ParseStatement();
                    if (s != null) stmts.Add(s);
                    SkipNewlinesAndSemicolons();
                }
                Match(endToken);
                return new DMASTBlock(loc, stmts);
            }
            return ParseStatement();
        }

        public DMASTStatement ParseStatement()
        {
            Location loc = Current.Location;

            if (Match(TokenType.LeftBrace))
            {
                List<DMASTStatement> statements = new List<DMASTStatement>();
                while (!IsAtEnd && Current.Type != TokenType.RightBrace)
                {
                    SkipNewlinesAndSemicolons();
                    if (Current.Type == TokenType.RightBrace) break;
                    var s = ParseStatement();
                    if (s != null) statements.Add(s);
                    SkipNewlinesAndSemicolons();
                }
                Consume(TokenType.RightBrace, "Expected '}'");
                return new DMASTBlock(loc, statements);
            }

            if (Match(TokenType.Var))
            {
                Match(TokenType.Divide);

                bool isGlobal = false;
                bool isConst = false;
                while (Current.Type == TokenType.Global || Current.Type == TokenType.Const || Current.Type == TokenType.Static || Current.Type == TokenType.Tmp)
                {
                    if (Current.Type == TokenType.Global) isGlobal = true;
                    if (Current.Type == TokenType.Const) isConst = true;
                    Advance();
                    Match(TokenType.Divide);
                }

                Location varLoc = Current.Location;
                string varName = "";
                DreamPath typePath = DreamPath.Root;

                if (Current.Type == TokenType.Path)
                {
                    DreamPath p = (DreamPath)Current.Value;
                    Advance();
                    varName = p.LastElement;
                    typePath = p.Parent;
                }
                else if (Current.Type == TokenType.Identifier)
                {
                    varName = Current.Text;
                    Advance();

                    while (Match(TokenType.Divide) && (Current.Type == TokenType.Identifier || Current.Type == TokenType.Path))
                    {
                        if (Current.Type == TokenType.Path)
                        {
                            DreamPath p = (DreamPath)Current.Value;
                            Advance();
                            typePath = DreamPath.Combine(typePath, new DreamPath("/" + varName), p.Parent);
                            varName = p.LastElement;
                        }
                        else
                        {
                            typePath = DreamPath.Combine(typePath, new DreamPath("/" + varName));
                            varName = Current.Text;
                            Advance();
                        }
                    }
                }

                DMASTExpression initVal = null;
                if (Match(TokenType.Assign))
                {
                    initVal = ParseExpression();
                }

                SkipNewlinesAndSemicolons();
                return new DMASTVarDeclarationStatement(varLoc, varName, typePath, initVal, isGlobal, isConst);
            }

            if (Match(TokenType.If))
            {
                Consume(TokenType.LeftParen, "Expected '(' after 'if'");
                DMASTExpression cond = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after if condition");

                DMASTStatement thenBranch = ParseBranchBody(loc);
                DMASTStatement elseBranch = null;

                SkipNewlinesAndSemicolons();
                if (Match(TokenType.Else))
                {
                    elseBranch = ParseBranchBody(loc);
                }

                return new DMASTIfStatement(loc, cond, thenBranch, elseBranch);
            }

            if (Match(TokenType.While))
            {
                Consume(TokenType.LeftParen, "Expected '(' after 'while'");
                DMASTExpression cond = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after while condition");

                DMASTStatement body = ParseBranchBody(loc);
                return new DMASTWhileStatement(loc, cond, body);
            }

            if (Match(TokenType.Do))
            {
                DMASTStatement body = ParseBranchBody(loc);
                SkipNewlinesAndSemicolons();
                Consume(TokenType.While, "Expected 'while' after 'do' body");
                Consume(TokenType.LeftParen, "Expected '(' after 'while'");
                DMASTExpression cond = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after while condition");
                return new DMASTDoWhileStatement(loc, cond, body);
            }

            if (Match(TokenType.For))
            {
                Consume(TokenType.LeftParen, "Expected '(' after 'for'");

                bool hasVar = Match(TokenType.Var);
                if (hasVar) Match(TokenType.Divide);

                Location varLoc = Current.Location;
                string varName = "";
                DreamPath varType = DreamPath.Root;

                if (Current.Type == TokenType.Path)
                {
                    DreamPath p = (DreamPath)Current.Value;
                    Advance();
                    varName = p.LastElement;
                    varType = p.Parent;
                }
                else if (Current.Type == TokenType.Identifier)
                {
                    varName = Current.Text;
                    Advance();

                    while (Match(TokenType.Divide) && (Current.Type == TokenType.Identifier || Current.Type == TokenType.Path))
                    {
                        if (Current.Type == TokenType.Path)
                        {
                            DreamPath p = (DreamPath)Current.Value;
                            Advance();
                            varType = DreamPath.Combine(varType, new DreamPath("/" + varName), p.Parent);
                            varName = p.LastElement;
                        }
                        else
                        {
                            varType = DreamPath.Combine(varType, new DreamPath("/" + varName));
                            varName = Current.Text;
                            Advance();
                        }
                    }
                }

                if (Match(TokenType.As))
                {
                    Advance(); // skip type specifier like 'anything', 'num', 'mob' etc.
                }

                if (Match(TokenType.In))
                {
                    DMASTExpression container = ParseExpression();
                    Consume(TokenType.RightParen, "Expected ')' after for-in container");
                    DMASTStatement forBody = ParseBranchBody(loc);
                    return new DMASTForInStatement(loc, varName, varType, container, forBody);
                }

                if (Match(TokenType.Assign))
                {
                    DMASTExpression startExpr = ParseExpression();
                    DMASTExpression endExpr = null;
                    DMASTExpression stepExpr = null;

                    if (startExpr is DMASTBinaryExpression && ((DMASTBinaryExpression)startExpr).Operator == BinaryOperator.To)
                    {
                        DMASTBinaryExpression bin = (DMASTBinaryExpression)startExpr;
                        startExpr = bin.Left;
                        if (bin.Right is DMASTBinaryExpression && ((DMASTBinaryExpression)bin.Right).Operator == BinaryOperator.Step)
                        {
                            DMASTBinaryExpression stepBin = (DMASTBinaryExpression)bin.Right;
                            endExpr = stepBin.Left;
                            stepExpr = stepBin.Right;
                        }
                        else
                        {
                            endExpr = bin.Right;
                        }
                        Consume(TokenType.RightParen, "Expected ')' after for-range");
                        DMASTStatement forBody = ParseBranchBody(loc);
                        return new DMASTForRangeStatement(loc, varName, varType, startExpr, endExpr, stepExpr, forBody);
                    }
                    else if (Match(TokenType.To))
                    {
                        endExpr = ParseExpression();
                        if (Match(TokenType.Step))
                        {
                            stepExpr = ParseExpression();
                        }
                        Consume(TokenType.RightParen, "Expected ')' after for-range");
                        DMASTStatement forBody = ParseBranchBody(loc);
                        return new DMASTForRangeStatement(loc, varName, varType, startExpr, endExpr, stepExpr, forBody);
                    }
                    else
                    {
                        Consume(TokenType.Semicolon, "Expected ';' in standard for loop");
                        DMASTExpression cond = ParseExpression();
                        Consume(TokenType.Semicolon, "Expected ';' after for loop condition");
                        DMASTExpression incr = ParseExpression();
                        Consume(TokenType.RightParen, "Expected ')' after for loop header");

                        DMASTStatement initStmt = new DMASTVarDeclarationStatement(varLoc, varName, varType, startExpr);
                        DMASTStatement forBody = ParseBranchBody(loc);
                        return new DMASTForStandardStatement(loc, initStmt, cond, incr, forBody);
                    }
                }

                DMASTStatement altInit = null;
                if (!string.IsNullOrEmpty(varName))
                {
                    altInit = new DMASTExpressionStatement(varLoc, new DMASTIdentifier(varLoc, varName));
                }

                Consume(TokenType.Semicolon, "Expected ';' in for loop");
                DMASTExpression altCond = ParseExpression();
                Consume(TokenType.Semicolon, "Expected ';' after for condition");
                DMASTExpression altIncr = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')'");

                DMASTStatement altForBody = ParseBranchBody(loc);
                return new DMASTForStandardStatement(loc, altInit, altCond, altIncr, altForBody);
            }

            if (Match(TokenType.Switch))
            {
                Consume(TokenType.LeftParen, "Expected '(' after 'switch'");
                DMASTExpression swVal = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after switch value");
                SkipNewlinesAndSemicolons();

                List<DMASTCaseClause> cases = new List<DMASTCaseClause>();
                if (Match(TokenType.LeftBrace) || Match(TokenType.Indent))
                {
                    bool isBrace = _tokens[_index - 1].Type == TokenType.LeftBrace;
                    TokenType endToken = isBrace ? TokenType.RightBrace : TokenType.Dedent;

                    while (!IsAtEnd && Current.Type != endToken)
                    {
                        SkipNewlinesAndSemicolons();
                        if (Current.Type == endToken) break;

                        if (Match(TokenType.Else) || (Current.Type == TokenType.Identifier && Current.Text == "default"))
                        {
                            if (Current.Type == TokenType.Identifier) Advance();
                            DMASTStatement caseBody = ParseBranchBody(loc);
                            cases.Add(new DMASTCaseClause(loc, new List<DMASTExpression>(), caseBody, true));
                        }
                        else if (Match(TokenType.If))
                        {
                            Consume(TokenType.LeftParen, "Expected '(' after 'if' in switch case");
                            List<DMASTExpression> caseExprs = new List<DMASTExpression>();
                            caseExprs.Add(ParseExpression());
                            while (Match(TokenType.Comma))
                            {
                                caseExprs.Add(ParseExpression());
                            }
                            Consume(TokenType.RightParen, "Expected ')' after switch case values");
                            DMASTStatement caseBody = ParseBranchBody(loc);
                            cases.Add(new DMASTCaseClause(loc, caseExprs, caseBody, false));
                        }
                        else
                        {
                            if (Current.Type == TokenType.Identifier && Current.Text == "case") Advance();
                            List<DMASTExpression> caseExprs = new List<DMASTExpression>();
                            caseExprs.Add(ParseExpression());
                            while (Match(TokenType.Comma))
                            {
                                caseExprs.Add(ParseExpression());
                            }
                            DMASTStatement caseBody = ParseBranchBody(loc);
                            cases.Add(new DMASTCaseClause(loc, caseExprs, caseBody, false));
                        }
                        SkipNewlinesAndSemicolons();
                    }
                    Match(endToken);
                }

                return new DMASTSwitchStatement(loc, swVal, cases);
            }

            if (Match(TokenType.Return))
            {
                DMASTExpression retVal = null;
                if (Current.Type != TokenType.Newline && Current.Type != TokenType.Semicolon && Current.Type != TokenType.Dedent && Current.Type != TokenType.RightBrace && Current.Type != TokenType.EOF)
                {
                    retVal = ParseExpression();
                }
                SkipNewlinesAndSemicolons();
                return new DMASTReturnStatement(loc, retVal);
            }

            if (Match(TokenType.Break))
            {
                string label = null;
                if (Current.Type == TokenType.Identifier) { label = Current.Text; Advance(); }
                SkipNewlinesAndSemicolons();
                return new DMASTBreakStatement(loc, label);
            }

            if (Match(TokenType.Continue))
            {
                string label = null;
                if (Current.Type == TokenType.Identifier) { label = Current.Text; Advance(); }
                SkipNewlinesAndSemicolons();
                return new DMASTContinueStatement(loc, label);
            }

            if (Match(TokenType.Spawn))
            {
                DMASTExpression delay = null;
                if (Match(TokenType.LeftParen))
                {
                    if (Current.Type != TokenType.RightParen)
                    {
                        delay = ParseExpression();
                    }
                    Consume(TokenType.RightParen, "Expected ')' after spawn delay");
                }
                SkipNewlinesAndSemicolons();
                DMASTStatement spawnBody = ParseStatement();
                return new DMASTSpawnStatement(loc, delay, spawnBody);
            }

            if (Match(TokenType.Try))
            {
                DMASTStatement tryBlock = ParseBranchBody(loc);
                SkipNewlinesAndSemicolons();
                Consume(TokenType.Catch, "Expected 'catch' after 'try' block");

                string exVar = null;
                if (Match(TokenType.LeftParen))
                {
                    if (Match(TokenType.Var)) Match(TokenType.Divide);
                    if (Current.Type == TokenType.Path)
                    {
                        exVar = ((DreamPath)Current.Value).LastElement;
                        Advance();
                    }
                    else if (Current.Type == TokenType.Identifier)
                    {
                        exVar = Current.Text;
                        Advance();
                    }
                    Consume(TokenType.RightParen, "Expected ')' after catch parameter");
                }

                DMASTStatement catchBlock = ParseBranchBody(loc);
                return new DMASTTryCatchStatement(loc, tryBlock, exVar, catchBlock);
            }

            if (Match(TokenType.Del))
            {
                DMASTExpression target = null;
                if (Match(TokenType.LeftParen))
                {
                    target = ParseExpression();
                    Consume(TokenType.RightParen, "Expected ')' after del target");
                }
                else
                {
                    target = ParseExpression();
                }
                SkipNewlinesAndSemicolons();
                return new DMASTDelStatement(loc, target);
            }

            if (Match(TokenType.Goto))
            {
                string label = "";
                if (Current.Type == TokenType.Identifier)
                {
                    label = Current.Text;
                    Advance();
                }
                SkipNewlinesAndSemicolons();
                return new DMASTGotoStatement(loc, label);
            }

            DMASTExpression expr = ParseExpression();
            SkipNewlinesAndSemicolons();
            return new DMASTExpressionStatement(loc, expr);
        }

        private DMASTVarDeclarationStatement ParseLocalVarDeclaration(Location loc)
        {
            Match(TokenType.Divide);

            bool isGlobal = false;
            bool isConst = false;
            if (Current.Type == TokenType.Global || Current.Type == TokenType.Const || Current.Type == TokenType.Static)
            {
                if (Current.Type == TokenType.Global) isGlobal = true;
                if (Current.Type == TokenType.Const) isConst = true;
                Advance();
                Match(TokenType.Divide);
            }

            DreamPath typePath = DreamPath.Root;
            string varName = "";

            if (Current.Type == TokenType.Path)
            {
                DreamPath p = (DreamPath)Current.Value;
                Advance();
                varName = p.LastElement;
                typePath = p.Parent;
            }
            else if (Current.Type == TokenType.Identifier)
            {
                varName = Current.Text;
                Advance();

                if (Match(TokenType.Divide) && Current.Type == TokenType.Identifier)
                {
                    typePath = new DreamPath("/" + varName);
                    varName = Current.Text;
                    Advance();
                }
            }

            DMASTExpression initExpr = null;
            if (Match(TokenType.Assign))
            {
                initExpr = ParseExpression();
            }

            SkipNewlinesAndSemicolons();
            return new DMASTVarDeclarationStatement(loc, varName, typePath, initExpr, isGlobal, isConst);
        }

        // ================= EXPRESSION PARSER =================

        public DMASTExpression ParseExpression()
        {
            return ParseAssignment();
        }

        private DMASTExpression ParseAssignment()
        {
            DMASTExpression expr = ParseTernary();

            if (Current.Type == TokenType.Assign ||
                Current.Type == TokenType.AddAssign || Current.Type == TokenType.SubtractAssign ||
                Current.Type == TokenType.MultiplyAssign || Current.Type == TokenType.DivideAssign ||
                Current.Type == TokenType.ModuloAssign || Current.Type == TokenType.BitwiseAndAssign ||
                Current.Type == TokenType.BitwiseOrAssign || Current.Type == TokenType.BitwiseXorAssign ||
                Current.Type == TokenType.ShiftLeftAssign || Current.Type == TokenType.ShiftRightAssign)
            {
                Token opToken = Advance();
                AssignmentOperator op = GetAssignOp(opToken.Type);
                DMASTExpression right = ParseAssignment();
                return new DMASTAssignExpression(opToken.Location, op, expr, right);
            }

            return expr;
        }

        private DMASTExpression ParseTernary()
        {
            DMASTExpression expr = ParseLogicalOr();

            if (Match(TokenType.Question))
            {
                Location loc = _tokens[_index - 1].Location;
                DMASTExpression trueVal = ParseExpression();
                Consume(TokenType.Colon, "Expected ':' in ternary operator");
                DMASTExpression falseVal = ParseExpression();
                return new DMASTTernaryExpression(loc, expr, trueVal, falseVal);
            }

            return expr;
        }

        private DMASTExpression ParseLogicalOr()
        {
            DMASTExpression expr = ParseLogicalAnd();

            while (Match(TokenType.LogicalOr))
            {
                Location loc = _tokens[_index - 1].Location;
                DMASTExpression right = ParseLogicalAnd();
                expr = new DMASTBinaryExpression(loc, BinaryOperator.LogicalOr, expr, right);
            }

            return expr;
        }

        private DMASTExpression ParseLogicalAnd()
        {
            DMASTExpression expr = ParseBitwiseOr();

            while (Match(TokenType.LogicalAnd))
            {
                Location loc = _tokens[_index - 1].Location;
                DMASTExpression right = ParseBitwiseOr();
                expr = new DMASTBinaryExpression(loc, BinaryOperator.LogicalAnd, expr, right);
            }

            return expr;
        }

        private DMASTExpression ParseBitwiseOr()
        {
            DMASTExpression expr = ParseBitwiseXor();

            while (Match(TokenType.BitwiseOr))
            {
                Location loc = _tokens[_index - 1].Location;
                DMASTExpression right = ParseBitwiseXor();
                expr = new DMASTBinaryExpression(loc, BinaryOperator.BitwiseOr, expr, right);
            }

            return expr;
        }

        private DMASTExpression ParseBitwiseXor()
        {
            DMASTExpression expr = ParseBitwiseAnd();

            while (Match(TokenType.BitwiseXor))
            {
                Location loc = _tokens[_index - 1].Location;
                DMASTExpression right = ParseBitwiseAnd();
                expr = new DMASTBinaryExpression(loc, BinaryOperator.BitwiseXor, expr, right);
            }

            return expr;
        }

        private DMASTExpression ParseBitwiseAnd()
        {
            DMASTExpression expr = ParseEquality();

            while (Match(TokenType.BitwiseAnd))
            {
                Location loc = _tokens[_index - 1].Location;
                DMASTExpression right = ParseEquality();
                expr = new DMASTBinaryExpression(loc, BinaryOperator.BitwiseAnd, expr, right);
            }

            return expr;
        }

        private DMASTExpression ParseEquality()
        {
            DMASTExpression expr = ParseRelational();

            while (Current.Type == TokenType.Equal || Current.Type == TokenType.NotEqual || Current.Type == TokenType.Equivalent || Current.Type == TokenType.NotEquivalent)
            {
                Token opToken = Advance();
                BinaryOperator op = (opToken.Type == TokenType.Equal) ? BinaryOperator.Equal :
                                   (opToken.Type == TokenType.NotEqual) ? BinaryOperator.NotEqual :
                                   (opToken.Type == TokenType.Equivalent) ? BinaryOperator.Equivalent : BinaryOperator.NotEquivalent;
                DMASTExpression right = ParseRelational();
                expr = new DMASTBinaryExpression(opToken.Location, op, expr, right);
            }

            return expr;
        }

        private DMASTExpression ParseRelational()
        {
            DMASTExpression expr = ParseShift();

            while (Current.Type == TokenType.Less || Current.Type == TokenType.LessEqual ||
                   Current.Type == TokenType.Greater || Current.Type == TokenType.GreaterEqual ||
                   Current.Type == TokenType.In || Current.Type == TokenType.To || Current.Type == TokenType.Step)
            {
                Token opToken = Advance();
                BinaryOperator op = (opToken.Type == TokenType.Less) ? BinaryOperator.Less :
                                   (opToken.Type == TokenType.LessEqual) ? BinaryOperator.LessOrEqual :
                                   (opToken.Type == TokenType.Greater) ? BinaryOperator.Greater :
                                   (opToken.Type == TokenType.GreaterEqual) ? BinaryOperator.GreaterOrEqual :
                                   (opToken.Type == TokenType.In) ? BinaryOperator.In :
                                   (opToken.Type == TokenType.To) ? BinaryOperator.To : BinaryOperator.Step;
                DMASTExpression right = ParseShift();
                expr = new DMASTBinaryExpression(opToken.Location, op, expr, right);
            }

            return expr;
        }

        private DMASTExpression ParseShift()
        {
            DMASTExpression expr = ParseAdditive();

            while (Current.Type == TokenType.ShiftLeft || Current.Type == TokenType.ShiftRight)
            {
                Token opToken = Advance();
                BinaryOperator op = (opToken.Type == TokenType.ShiftLeft) ? BinaryOperator.ShiftLeft : BinaryOperator.ShiftRight;
                DMASTExpression right = ParseAdditive();
                expr = new DMASTBinaryExpression(opToken.Location, op, expr, right);
            }

            return expr;
        }

        private DMASTExpression ParseAdditive()
        {
            DMASTExpression expr = ParseMultiplicative();

            while (Current.Type == TokenType.Plus || Current.Type == TokenType.Minus)
            {
                Token opToken = Advance();
                BinaryOperator op = (opToken.Type == TokenType.Plus) ? BinaryOperator.Add : BinaryOperator.Subtract;
                DMASTExpression right = ParseMultiplicative();
                expr = new DMASTBinaryExpression(opToken.Location, op, expr, right);
            }

            return expr;
        }

        private DMASTExpression ParseMultiplicative()
        {
            DMASTExpression expr = ParsePower();

            while (Current.Type == TokenType.Multiply || Current.Type == TokenType.Divide || Current.Type == TokenType.Modulo)
            {
                Token opToken = Advance();
                BinaryOperator op = (opToken.Type == TokenType.Multiply) ? BinaryOperator.Multiply :
                                   (opToken.Type == TokenType.Divide) ? BinaryOperator.Divide : BinaryOperator.Modulo;
                DMASTExpression right = ParsePower();
                expr = new DMASTBinaryExpression(opToken.Location, op, expr, right);
            }

            return expr;
        }

        private DMASTExpression ParsePower()
        {
            DMASTExpression expr = ParseUnary();

            while (Match(TokenType.Power))
            {
                Location loc = _tokens[_index - 1].Location;
                DMASTExpression right = ParseUnary();
                expr = new DMASTBinaryExpression(loc, BinaryOperator.Power, expr, right);
            }

            return expr;
        }

        private DMASTExpression ParseUnary()
        {
            if (Current.Type == TokenType.Bang || Current.Type == TokenType.Minus || Current.Type == TokenType.Tilde ||
                Current.Type == TokenType.Increment || Current.Type == TokenType.Decrement)
            {
                Token opToken = Advance();
                UnaryOperator op = (opToken.Type == TokenType.Bang) ? UnaryOperator.Not :
                                  (opToken.Type == TokenType.Minus) ? UnaryOperator.Negate :
                                  (opToken.Type == TokenType.Tilde) ? UnaryOperator.BitwiseNot :
                                  (opToken.Type == TokenType.Increment) ? UnaryOperator.PreIncrement : UnaryOperator.PreDecrement;
                DMASTExpression operand = ParseUnary();
                return new DMASTUnaryExpression(opToken.Location, op, operand);
            }

            return ParsePostfix();
        }

        private DMASTExpression ParsePostfix()
        {
            DMASTExpression expr = ParsePrimary();

            while (true)
            {
                if (Match(TokenType.Dot))
                {
                    Location loc = _tokens[_index - 1].Location;
                    string member = "";
                    if (Current.Type == TokenType.Identifier || Current.Type == TokenType.Proc || Current.Type == TokenType.Verb || Current.Type == TokenType.Var)
                    {
                        member = Current.Text;
                        Advance();
                    }
                    expr = new DMASTMemberAccessExpression(loc, expr, member, false);
                }
                else if (Match(TokenType.Colon))
                {
                    Location loc = _tokens[_index - 1].Location;
                    string member = "";
                    if (Current.Type == TokenType.Identifier)
                    {
                        member = Current.Text;
                        Advance();
                    }
                    expr = new DMASTMemberAccessExpression(loc, expr, member, true);
                }
                else if (Match(TokenType.LeftParen))
                {
                    Location loc = _tokens[_index - 1].Location;
                    List<DMASTCallArgument> args = ParseCallArguments();
                    Consume(TokenType.RightParen, "Expected ')' after call arguments");

                    if (expr is DMASTIdentifier)
                    {
                        DMASTIdentifier id = (DMASTIdentifier)expr;
                        expr = new DMASTCallExpression(loc, null, id.Identifier, args);
                    }
                    else if (expr is DMASTMemberAccessExpression)
                    {
                        DMASTMemberAccessExpression mem = (DMASTMemberAccessExpression)expr;
                        expr = new DMASTCallExpression(loc, mem.Target, mem.Member, args);
                    }
                    else
                    {
                        expr = new DMASTCallExpression(loc, expr, "", args);
                    }
                }
                else if (Match(TokenType.LeftBracket))
                {
                    Location loc = _tokens[_index - 1].Location;
                    DMASTExpression index = ParseExpression();
                    Consume(TokenType.RightBracket, "Expected ']' after index expression");
                    expr = new DMASTIndexAccessExpression(loc, expr, index);
                }
                else if (Match(TokenType.Increment))
                {
                    Location loc = _tokens[_index - 1].Location;
                    expr = new DMASTUnaryExpression(loc, UnaryOperator.PostIncrement, expr);
                }
                else if (Match(TokenType.Decrement))
                {
                    Location loc = _tokens[_index - 1].Location;
                    expr = new DMASTUnaryExpression(loc, UnaryOperator.PostDecrement, expr);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        private DMASTExpression ParsePrimary()
        {
            Location loc = Current.Location;

            if (Match(TokenType.Null)) return new DMASTConstantNull(loc);
            if (Match(TokenType.True)) return new DMASTConstantNumber(loc, 1.0);
            if (Match(TokenType.False)) return new DMASTConstantNumber(loc, 0.0);

            if (Current.Type == TokenType.Number)
            {
                double val = (double)Current.Value;
                Advance();
                return new DMASTConstantNumber(loc, val);
            }

            if (Current.Type == TokenType.String || Current.Type == TokenType.VerbatimString)
            {
                string text = (string)Current.Value;
                Advance();

                if (text.Contains("[") && text.Contains("]"))
                {
                    return ParseInterpolatedString(loc, text);
                }
                return new DMASTConstantString(loc, text);
            }

            if (Current.Type == TokenType.Resource)
            {
                string path = (string)Current.Value;
                Advance();
                return new DMASTConstantResource(loc, path);
            }

            if (Current.Type == TokenType.Path)
            {
                DreamPath path = (DreamPath)Current.Value;
                Advance();
                return new DMASTConstantPath(loc, path);
            }

            if (Match(TokenType.Usr)) return new DMASTIdentifier(loc, "usr");
            if (Match(TokenType.Src)) return new DMASTIdentifier(loc, "src");
            if (Match(TokenType.Args)) return new DMASTIdentifier(loc, "args");
            if (Match(TokenType.World)) return new DMASTIdentifier(loc, "world");
            if (Match(TokenType.Dot)) return new DMASTIdentifier(loc, ".");

            if (Match(TokenType.DotDot))
            {
                if (Match(TokenType.LeftParen))
                {
                    List<DMASTCallArgument> superArgs = ParseCallArguments();
                    Consume(TokenType.RightParen, "Expected ')' after '..' super call");
                    return new DMASTSuperCallExpression(loc, superArgs, false);
                }
                return new DMASTSuperCallExpression(loc, null, true);
            }

            if (Match(TokenType.New))
            {
                DMASTExpression typeExpr = null;
                if (Current.Type == TokenType.Path || Current.Type == TokenType.Identifier || Current.Type == TokenType.Divide)
                {
                    if (Match(TokenType.Divide)) { }
                    if (Current.Type == TokenType.Path)
                    {
                        typeExpr = new DMASTConstantPath(Current.Location, (DreamPath)Current.Value);
                        Advance();
                    }
                    else if (Current.Type == TokenType.Identifier)
                    {
                        typeExpr = new DMASTIdentifier(Current.Location, Current.Text);
                        Advance();
                    }
                }

                List<DMASTCallArgument> newArgs = null;
                if (Match(TokenType.LeftParen))
                {
                    newArgs = ParseCallArguments();
                    Consume(TokenType.RightParen, "Expected ')' after new constructor arguments");
                }
                return new DMASTNewExpression(loc, typeExpr, newArgs);
            }

            if (Current.Type == TokenType.Identifier && string.Equals(Current.Text, "list", StringComparison.OrdinalIgnoreCase) && Peek().Type == TokenType.LeftParen)
            {
                Advance();
                Advance();
                List<DMASTCallArgument> listArgs = ParseCallArguments();
                Consume(TokenType.RightParen, "Expected ')' after list elements");
                return new DMASTListExpression(loc, listArgs);
            }

            if (Match(TokenType.Locate))
            {
                Consume(TokenType.LeftParen, "Expected '(' after locate");
                DMASTExpression first = ParseExpression();
                if (Match(TokenType.In))
                {
                    DMASTExpression container = ParseExpression();
                    Consume(TokenType.RightParen, "Expected ')' after locate expression");
                    return new DMASTLocateExpression(loc, first, container);
                }
                else if (Match(TokenType.Comma))
                {
                    DMASTExpression y = ParseExpression();
                    Consume(TokenType.Comma, "Expected ',' in locate(x, y, z)");
                    DMASTExpression z = ParseExpression();
                    Consume(TokenType.RightParen, "Expected ')' after locate(x, y, z)");
                    return new DMASTLocateExpression(loc, first, y, z);
                }
                else
                {
                    Consume(TokenType.RightParen, "Expected ')' after locate");
                    return new DMASTLocateExpression(loc, first);
                }
            }

            if (Match(TokenType.Input))
            {
                Consume(TokenType.LeftParen, "Expected '(' after input");
                List<DMASTCallArgument> inputArgs = ParseCallArguments();
                Consume(TokenType.RightParen, "Expected ')' after input");
                return new DMASTInputExpression(loc, inputArgs);
            }

            if (Current.Type == TokenType.IsType || Current.Type == TokenType.Initial)
            {
                string fnName = Current.Text;
                Advance();
                if (Match(TokenType.LeftParen))
                {
                    List<DMASTCallArgument> fnArgs = ParseCallArguments();
                    Consume(TokenType.RightParen, "Expected ')' after function arguments");
                    return new DMASTCallExpression(loc, null, fnName, fnArgs);
                }
                return new DMASTIdentifier(loc, fnName);
            }

            if (Current.Type == TokenType.Identifier)
            {
                string id = Current.Text;
                Advance();
                return new DMASTIdentifier(loc, id);
            }

            if (Match(TokenType.LeftParen))
            {
                DMASTExpression inner = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after expression");
                return inner;
            }

            _diagnostics.Add(CompilerDiagnostic.Error(loc, string.Format("Unexpected token '{0}' of type {1}", Current.Text, Current.Type)));
            Advance();
            return new DMASTConstantNull(loc);
        }

        private List<DMASTCallArgument> ParseCallArguments()
        {
            List<DMASTCallArgument> args = new List<DMASTCallArgument>();
            while (!IsAtEnd && Current.Type != TokenType.RightParen)
            {
                string argName = null;
                if (Current.Type == TokenType.Identifier && Peek().Type == TokenType.Assign)
                {
                    argName = Current.Text;
                    Advance();
                    Advance();
                }

                DMASTExpression val = ParseExpression();
                args.Add(new DMASTCallArgument(val, argName));

                if (!Match(TokenType.Comma))
                {
                    break;
                }
            }
            return args;
        }

        private DMASTInterpolatedString ParseInterpolatedString(Location loc, string text)
        {
            List<DMASTInterpolatedPart> parts = new List<DMASTInterpolatedPart>();
            int i = 0;
            int len = text.Length;

            while (i < len)
            {
                int openBracket = text.IndexOf('[', i);
                if (openBracket == -1)
                {
                    parts.Add(new DMASTInterpolatedPart(text.Substring(i)));
                    break;
                }

                if (openBracket > i)
                {
                    parts.Add(new DMASTInterpolatedPart(text.Substring(i, openBracket - i)));
                }

                int depth = 1;
                int closeBracket = -1;
                bool inQuotes = false;
                for (int pos = openBracket + 1; pos < len; pos++)
                {
                    if (text[pos] == '"' && (pos == 0 || text[pos - 1] != '\\'))
                    {
                        inQuotes = !inQuotes;
                    }
                    else if (!inQuotes)
                    {
                        if (text[pos] == '[') depth++;
                        else if (text[pos] == ']')
                        {
                            depth--;
                            if (depth == 0)
                            {
                                closeBracket = pos;
                                break;
                            }
                        }
                    }
                }

                if (closeBracket == -1)
                {
                    parts.Add(new DMASTInterpolatedPart(text.Substring(openBracket)));
                    break;
                }

                string exprCode = text.Substring(openBracket + 1, closeBracket - openBracket - 1);
                i = closeBracket + 1;

                if (!string.IsNullOrWhiteSpace(exprCode))
                {
                    try
                    {
                        var subPreprocessor = new DMPreprocessor();
                        var subLines = subPreprocessor.ProcessSource(loc.SourceFile, exprCode);
                        var subLexer = new DMLexer();
                        var subTokens = subLexer.Tokenize(subLines);
                        var subParser = new DMParser(subTokens);
                        DMASTExpression subExpr = subParser.ParseExpression();
                        parts.Add(new DMASTInterpolatedPart(subExpr));
                    }
                    catch
                    {
                        parts.Add(new DMASTInterpolatedPart(new DMASTIdentifier(loc, exprCode.Trim())));
                    }
                }
            }

            return new DMASTInterpolatedString(loc, parts);
        }

        private AssignmentOperator GetAssignOp(TokenType type)
        {
            switch (type)
            {
                case TokenType.AddAssign: return AssignmentOperator.AddAssign;
                case TokenType.SubtractAssign: return AssignmentOperator.SubtractAssign;
                case TokenType.MultiplyAssign: return AssignmentOperator.MultiplyAssign;
                case TokenType.DivideAssign: return AssignmentOperator.DivideAssign;
                case TokenType.ModuloAssign: return AssignmentOperator.ModuloAssign;
                case TokenType.BitwiseAndAssign: return AssignmentOperator.BitwiseAndAssign;
                case TokenType.BitwiseOrAssign: return AssignmentOperator.BitwiseOrAssign;
                case TokenType.BitwiseXorAssign: return AssignmentOperator.BitwiseXorAssign;
                case TokenType.ShiftLeftAssign: return AssignmentOperator.ShiftLeftAssign;
                case TokenType.ShiftRightAssign: return AssignmentOperator.ShiftRightAssign;
                default: return AssignmentOperator.Assign;
            }
        }
    }
}
