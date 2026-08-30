using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.Core.AST
{
    public abstract class DMASTExpression : DMASTNode
    {
        protected DMASTExpression(Location location) : base(location) { }
    }

    public enum UnaryOperator
    {
        Not,
        Negate,
        BitwiseNot,
        PreIncrement,
        PreDecrement,
        PostIncrement,
        PostDecrement
    }

    public enum BinaryOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        Power,
        Equal,
        NotEqual,
        Equivalent,
        NotEquivalent,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual,
        LogicalAnd,
        LogicalOr,
        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        ShiftLeft,
        ShiftRight,
        In,
        To,
        Step
    }

    public enum AssignmentOperator
    {
        Assign,
        AddAssign,
        SubtractAssign,
        MultiplyAssign,
        DivideAssign,
        ModuloAssign,
        BitwiseAndAssign,
        BitwiseOrAssign,
        BitwiseXorAssign,
        ShiftLeftAssign,
        ShiftRightAssign
    }

    public class DMASTCallArgument
    {
        public string Name { get; set; }
        public DMASTExpression Value { get; set; }

        public DMASTCallArgument(DMASTExpression value, string name = null)
        {
            Value = value;
            Name = name;
        }
    }

    public class DMASTConstantNull : DMASTExpression
    {
        public DMASTConstantNull(Location location) : base(location) { }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTConstantNumber : DMASTExpression
    {
        public double Value { get; set; }

        public DMASTConstantNumber(Location location, double value) : base(location)
        {
            Value = value;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTConstantString : DMASTExpression
    {
        public string Value { get; set; }

        public DMASTConstantString(Location location, string value) : base(location)
        {
            Value = value;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTConstantResource : DMASTExpression
    {
        public string Path { get; set; }

        public DMASTConstantResource(Location location, string path) : base(location)
        {
            Path = path;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTConstantPath : DMASTExpression
    {
        public DreamPath Path { get; set; }

        public DMASTConstantPath(Location location, DreamPath path) : base(location)
        {
            Path = path;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTIdentifier : DMASTExpression
    {
        public string Identifier { get; set; }

        public DMASTIdentifier(Location location, string identifier) : base(location)
        {
            Identifier = identifier;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTUnaryExpression : DMASTExpression
    {
        public UnaryOperator Operator { get; set; }
        public DMASTExpression Operand { get; set; }

        public DMASTUnaryExpression(Location location, UnaryOperator op, DMASTExpression operand) : base(location)
        {
            Operator = op;
            Operand = operand;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTBinaryExpression : DMASTExpression
    {
        public BinaryOperator Operator { get; set; }
        public DMASTExpression Left { get; set; }
        public DMASTExpression Right { get; set; }

        public DMASTBinaryExpression(Location location, BinaryOperator op, DMASTExpression left, DMASTExpression right) : base(location)
        {
            Operator = op;
            Left = left;
            Right = right;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTAssignExpression : DMASTExpression
    {
        public AssignmentOperator Operator { get; set; }
        public DMASTExpression Left { get; set; }
        public DMASTExpression Right { get; set; }

        public DMASTAssignExpression(Location location, AssignmentOperator op, DMASTExpression left, DMASTExpression right) : base(location)
        {
            Operator = op;
            Left = left;
            Right = right;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTCallExpression : DMASTExpression
    {
        public DMASTExpression Target { get; set; }
        public string ProcName { get; set; }
        public List<DMASTCallArgument> Arguments { get; set; }

        public DMASTCallExpression(Location location, DMASTExpression target, string procName, List<DMASTCallArgument> arguments = null) : base(location)
        {
            Target = target;
            ProcName = procName;
            Arguments = arguments ?? new List<DMASTCallArgument>();
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTSuperCallExpression : DMASTExpression
    {
        public List<DMASTCallArgument> Arguments { get; set; }
        public bool PassAllArgs { get; set; }

        public DMASTSuperCallExpression(Location location, List<DMASTCallArgument> arguments = null, bool passAllArgs = true) : base(location)
        {
            Arguments = arguments ?? new List<DMASTCallArgument>();
            PassAllArgs = passAllArgs;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTMemberAccessExpression : DMASTExpression
    {
        public DMASTExpression Target { get; set; }
        public string Member { get; set; }
        public bool IsColonAccess { get; set; }

        public DMASTMemberAccessExpression(Location location, DMASTExpression target, string member, bool isColon = false) : base(location)
        {
            Target = target;
            Member = member;
            IsColonAccess = isColon;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTIndexAccessExpression : DMASTExpression
    {
        public DMASTExpression Target { get; set; }
        public DMASTExpression Index { get; set; }

        public DMASTIndexAccessExpression(Location location, DMASTExpression target, DMASTExpression index) : base(location)
        {
            Target = target;
            Index = index;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTNewExpression : DMASTExpression
    {
        public DMASTExpression TypePath { get; set; }
        public List<DMASTCallArgument> Arguments { get; set; }

        public DMASTNewExpression(Location location, DMASTExpression typePath, List<DMASTCallArgument> arguments = null) : base(location)
        {
            TypePath = typePath;
            Arguments = arguments ?? new List<DMASTCallArgument>();
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTTernaryExpression : DMASTExpression
    {
        public DMASTExpression Condition { get; set; }
        public DMASTExpression TrueValue { get; set; }
        public DMASTExpression FalseValue { get; set; }

        public DMASTTernaryExpression(Location location, DMASTExpression condition, DMASTExpression trueVal, DMASTExpression falseVal) : base(location)
        {
            Condition = condition;
            TrueValue = trueVal;
            FalseValue = falseVal;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTListExpression : DMASTExpression
    {
        public List<DMASTCallArgument> Elements { get; set; }

        public DMASTListExpression(Location location, List<DMASTCallArgument> elements = null) : base(location)
        {
            Elements = elements ?? new List<DMASTCallArgument>();
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTInterpolatedPart
    {
        public bool IsExpression { get; set; }
        public string Text { get; set; }
        public DMASTExpression Expression { get; set; }

        public DMASTInterpolatedPart(string text)
        {
            IsExpression = false;
            Text = text;
        }

        public DMASTInterpolatedPart(DMASTExpression expr)
        {
            IsExpression = true;
            Expression = expr;
        }
    }

    public class DMASTInterpolatedString : DMASTExpression
    {
        public List<DMASTInterpolatedPart> Parts { get; set; }

        public DMASTInterpolatedString(Location location, List<DMASTInterpolatedPart> parts = null) : base(location)
        {
            Parts = parts ?? new List<DMASTInterpolatedPart>();
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTLocateExpression : DMASTExpression
    {
        public DMASTExpression TypeOrTag { get; set; }
        public DMASTExpression InContainer { get; set; }
        public DMASTExpression X { get; set; }
        public DMASTExpression Y { get; set; }
        public DMASTExpression Z { get; set; }

        public DMASTLocateExpression(Location location, DMASTExpression typeOrTag, DMASTExpression inContainer = null) : base(location)
        {
            TypeOrTag = typeOrTag;
            InContainer = inContainer;
        }

        public DMASTLocateExpression(Location location, DMASTExpression x, DMASTExpression y, DMASTExpression z) : base(location)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTInputExpression : DMASTExpression
    {
        public List<DMASTCallArgument> Arguments { get; set; }
        public string AsType { get; set; }
        public DMASTExpression InList { get; set; }

        public DMASTInputExpression(Location location, List<DMASTCallArgument> args, string asType = null, DMASTExpression inList = null) : base(location)
        {
            Arguments = args ?? new List<DMASTCallArgument>();
            AsType = asType;
            InList = inList;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }
}
