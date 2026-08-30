using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.CSharpToDM
{
    public abstract class CSNode
    {
        public Location Location { get; set; }
    }

    public class CSCompilationUnit : CSNode
    {
        public List<string> Usings { get; private set; }
        public List<CSClassDeclaration> Classes { get; private set; }

        public CSCompilationUnit()
        {
            Usings = new List<string>();
            Classes = new List<CSClassDeclaration>();
        }
    }

    public class CSClassDeclaration : CSNode
    {
        public string Name { get; set; }
        public string BaseClass { get; set; }
        public bool IsStatic { get; set; }
        public List<CSMemberDeclaration> Members { get; private set; }

        public CSClassDeclaration()
        {
            Members = new List<CSMemberDeclaration>();
        }
    }

    public abstract class CSMemberDeclaration : CSNode
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public bool IsStatic { get; set; }
        public bool IsOverride { get; set; }
        public bool IsVirtual { get; set; }
    }

    public class CSFieldDeclaration : CSMemberDeclaration
    {
        public CSExpression Initializer { get; set; }
    }

    public class CSPropertyDeclaration : CSMemberDeclaration
    {
        public CSExpression Initializer { get; set; }
    }

    public class CSParameter : CSNode
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public CSExpression DefaultValue { get; set; }
    }

    public class CSMethodDeclaration : CSMemberDeclaration
    {
        public List<CSParameter> Parameters { get; private set; }
        public CSBlockStatement Body { get; set; }

        public CSMethodDeclaration()
        {
            Parameters = new List<CSParameter>();
        }
    }

    public abstract class CSStatement : CSNode { }

    public class CSBlockStatement : CSStatement
    {
        public List<CSStatement> Statements { get; private set; }

        public CSBlockStatement()
        {
            Statements = new List<CSStatement>();
        }
    }

    public class CSExpressionStatement : CSStatement
    {
        public CSExpression Expression { get; set; }
    }

    public class CSVarDeclarationStatement : CSStatement
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public CSExpression Initializer { get; set; }
    }

    public class CSIfStatement : CSStatement
    {
        public CSExpression Condition { get; set; }
        public CSStatement ThenBranch { get; set; }
        public CSStatement ElseBranch { get; set; }
    }

    public class CSWhileStatement : CSStatement
    {
        public CSExpression Condition { get; set; }
        public CSStatement Body { get; set; }
    }

    public class CSForStatement : CSStatement
    {
        public CSStatement Initializer { get; set; }
        public CSExpression Condition { get; set; }
        public CSExpression Increment { get; set; }
        public CSStatement Body { get; set; }
    }

    public class CSForEachStatement : CSStatement
    {
        public string VarName { get; set; }
        public string TypeName { get; set; }
        public CSExpression Collection { get; set; }
        public CSStatement Body { get; set; }
    }

    public class CSSwitchStatement : CSStatement
    {
        public CSExpression Value { get; set; }
        public List<CSCaseClause> Cases { get; private set; }

        public CSSwitchStatement()
        {
            Cases = new List<CSCaseClause>();
        }
    }

    public class CSCaseClause : CSNode
    {
        public List<CSExpression> Values { get; private set; }
        public bool IsDefault { get; set; }
        public CSStatement Body { get; set; }

        public CSCaseClause()
        {
            Values = new List<CSExpression>();
        }
    }

    public class CSReturnStatement : CSStatement
    {
        public CSExpression Value { get; set; }
    }

    public class CSBreakStatement : CSStatement { }
    public class CSContinueStatement : CSStatement { }

    public class CSTryCatchStatement : CSStatement
    {
        public CSStatement TryBlock { get; set; }
        public string ExceptionVar { get; set; }
        public CSStatement CatchBlock { get; set; }
    }

    public abstract class CSExpression : CSNode { }

    public class CSLiteralExpression : CSExpression
    {
        public object Value { get; set; }
        public string RawText { get; set; }
    }

    public class CSIdentifierExpression : CSExpression
    {
        public string Identifier { get; set; }
    }

    public class CSInterpolatedStringExpression : CSExpression
    {
        public List<object> Parts { get; private set; }

        public CSInterpolatedStringExpression()
        {
            Parts = new List<object>();
        }
    }

    public class CSBinaryExpression : CSExpression
    {
        public string Operator { get; set; }
        public CSExpression Left { get; set; }
        public CSExpression Right { get; set; }
    }

    public class CSUnaryExpression : CSExpression
    {
        public string Operator { get; set; }
        public CSExpression Operand { get; set; }
        public bool IsPostfix { get; set; }
    }

    public class CSAssignmentExpression : CSExpression
    {
        public string Operator { get; set; }
        public CSExpression Target { get; set; }
        public CSExpression Value { get; set; }
    }

    public class CSInvocationExpression : CSExpression
    {
        public CSExpression Target { get; set; }
        public string MethodName { get; set; }
        public List<CSExpression> Arguments { get; private set; }

        public CSInvocationExpression()
        {
            Arguments = new List<CSExpression>();
        }
    }

    public class CSMemberAccessExpression : CSExpression
    {
        public CSExpression Target { get; set; }
        public string Member { get; set; }
    }

    public class CSElementAccessExpression : CSExpression
    {
        public CSExpression Target { get; set; }
        public CSExpression Argument { get; set; }
    }

    public class CSObjectCreationExpression : CSExpression
    {
        public string TypeName { get; set; }
        public List<CSExpression> Arguments { get; private set; }

        public CSObjectCreationExpression()
        {
            Arguments = new List<CSExpression>();
        }
    }

    public class CSTernaryExpression : CSExpression
    {
        public CSExpression Condition { get; set; }
        public CSExpression TrueExpr { get; set; }
        public CSExpression FalseExpr { get; set; }
    }
}
