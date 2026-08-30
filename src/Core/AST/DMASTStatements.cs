using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.Core.AST
{
    public abstract class DMASTStatement : DMASTNode
    {
        protected DMASTStatement(Location location) : base(location) { }
    }

    public class DMASTBlock : DMASTStatement
    {
        public List<DMASTStatement> Statements { get; set; }

        public DMASTBlock(Location location, List<DMASTStatement> statements = null) : base(location)
        {
            Statements = statements ?? new List<DMASTStatement>();
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTExpressionStatement : DMASTStatement
    {
        public DMASTExpression Expression { get; set; }

        public DMASTExpressionStatement(Location location, DMASTExpression expression) : base(location)
        {
            Expression = expression;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTVarDeclarationStatement : DMASTStatement
    {
        public string Name { get; set; }
        public DreamPath TypePath { get; set; }
        public DMASTExpression Initializer { get; set; }
        public bool IsGlobal { get; set; }
        public bool IsConst { get; set; }

        public DMASTVarDeclarationStatement(Location location, string name, DreamPath typePath, DMASTExpression initializer = null, bool isGlobal = false, bool isConst = false) : base(location)
        {
            Name = name;
            TypePath = typePath;
            Initializer = initializer;
            IsGlobal = isGlobal;
            IsConst = isConst;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTIfStatement : DMASTStatement
    {
        public DMASTExpression Condition { get; set; }
        public DMASTStatement ThenBranch { get; set; }
        public DMASTStatement ElseBranch { get; set; }

        public DMASTIfStatement(Location location, DMASTExpression condition, DMASTStatement thenBranch, DMASTStatement elseBranch = null) : base(location)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTWhileStatement : DMASTStatement
    {
        public DMASTExpression Condition { get; set; }
        public DMASTStatement Body { get; set; }

        public DMASTWhileStatement(Location location, DMASTExpression condition, DMASTStatement body) : base(location)
        {
            Condition = condition;
            Body = body;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTDoWhileStatement : DMASTStatement
    {
        public DMASTExpression Condition { get; set; }
        public DMASTStatement Body { get; set; }

        public DMASTDoWhileStatement(Location location, DMASTExpression condition, DMASTStatement body) : base(location)
        {
            Condition = condition;
            Body = body;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTForStandardStatement : DMASTStatement
    {
        public DMASTStatement Initializer { get; set; }
        public DMASTExpression Condition { get; set; }
        public DMASTExpression Increment { get; set; }
        public DMASTStatement Body { get; set; }

        public DMASTForStandardStatement(Location location, DMASTStatement initializer, DMASTExpression condition, DMASTExpression increment, DMASTStatement body) : base(location)
        {
            Initializer = initializer;
            Condition = condition;
            Increment = increment;
            Body = body;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTForInStatement : DMASTStatement
    {
        public string VarName { get; set; }
        public DreamPath VarType { get; set; }
        public DMASTExpression Container { get; set; }
        public DMASTStatement Body { get; set; }

        public DMASTForInStatement(Location location, string varName, DreamPath varType, DMASTExpression container, DMASTStatement body) : base(location)
        {
            VarName = varName;
            VarType = varType;
            Container = container;
            Body = body;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTForRangeStatement : DMASTStatement
    {
        public string VarName { get; set; }
        public DreamPath VarType { get; set; }
        public DMASTExpression Start { get; set; }
        public DMASTExpression End { get; set; }
        public DMASTExpression Step { get; set; }
        public DMASTStatement Body { get; set; }

        public DMASTForRangeStatement(Location location, string varName, DreamPath varType, DMASTExpression start, DMASTExpression end, DMASTExpression step, DMASTStatement body) : base(location)
        {
            VarName = varName;
            VarType = varType;
            Start = start;
            End = end;
            Step = step;
            Body = body;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTCaseClause : DMASTNode
    {
        public List<DMASTExpression> Values { get; set; }
        public DMASTStatement Body { get; set; }
        public bool IsDefault { get; set; }

        public DMASTCaseClause(Location location, List<DMASTExpression> values, DMASTStatement body, bool isDefault = false) : base(location)
        {
            Values = values ?? new List<DMASTExpression>();
            Body = body;
            IsDefault = isDefault;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTSwitchStatement : DMASTStatement
    {
        public DMASTExpression Value { get; set; }
        public List<DMASTCaseClause> Cases { get; set; }

        public DMASTSwitchStatement(Location location, DMASTExpression value, List<DMASTCaseClause> cases = null) : base(location)
        {
            Value = value;
            Cases = cases ?? new List<DMASTCaseClause>();
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTReturnStatement : DMASTStatement
    {
        public DMASTExpression Value { get; set; }

        public DMASTReturnStatement(Location location, DMASTExpression value = null) : base(location)
        {
            Value = value;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTBreakStatement : DMASTStatement
    {
        public string Label { get; set; }

        public DMASTBreakStatement(Location location, string label = null) : base(location)
        {
            Label = label;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTContinueStatement : DMASTStatement
    {
        public string Label { get; set; }

        public DMASTContinueStatement(Location location, string label = null) : base(location)
        {
            Label = label;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTSpawnStatement : DMASTStatement
    {
        public DMASTExpression Delay { get; set; }
        public DMASTStatement Body { get; set; }

        public DMASTSpawnStatement(Location location, DMASTExpression delay, DMASTStatement body) : base(location)
        {
            Delay = delay;
            Body = body;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTTryCatchStatement : DMASTStatement
    {
        public DMASTStatement TryBlock { get; set; }
        public string ExceptionVar { get; set; }
        public DMASTStatement CatchBlock { get; set; }

        public DMASTTryCatchStatement(Location location, DMASTStatement tryBlock, string exceptionVar, DMASTStatement catchBlock) : base(location)
        {
            TryBlock = tryBlock;
            ExceptionVar = exceptionVar;
            CatchBlock = catchBlock;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTDelStatement : DMASTStatement
    {
        public DMASTExpression Target { get; set; }

        public DMASTDelStatement(Location location, DMASTExpression target) : base(location)
        {
            Target = target;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTGotoStatement : DMASTStatement
    {
        public string Label { get; set; }

        public DMASTGotoStatement(Location location, string label) : base(location)
        {
            Label = label;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTLabelStatement : DMASTStatement
    {
        public string Label { get; set; }

        public DMASTLabelStatement(Location location, string label) : base(location)
        {
            Label = label;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }
}
