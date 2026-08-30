using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.Core.AST
{
    public abstract class DMASTDefinition : DMASTNode
    {
        protected DMASTDefinition(Location location) : base(location) { }
    }

    public class DMASTFile : DMASTNode
    {
        public List<DMASTDefinition> Definitions { get; set; }

        public DMASTFile(Location location, List<DMASTDefinition> definitions = null) : base(location)
        {
            Definitions = definitions ?? new List<DMASTDefinition>();
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTObjectDefinition : DMASTDefinition
    {
        public DreamPath Path { get; set; }
        public List<DMASTDefinition> Members { get; set; }

        public DMASTObjectDefinition(Location location, DreamPath path, List<DMASTDefinition> members = null) : base(location)
        {
            Path = path;
            Members = members ?? new List<DMASTDefinition>();
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTVarDefinition : DMASTDefinition
    {
        public DreamPath ObjectPath { get; set; }
        public string VarName { get; set; }
        public DreamPath TypePath { get; set; }
        public DMASTExpression InitialValue { get; set; }
        public bool IsGlobal { get; set; }
        public bool IsConst { get; set; }
        public bool IsStatic { get; set; }

        public DMASTVarDefinition(Location location, DreamPath objectPath, string varName, DreamPath typePath, DMASTExpression initialValue = null, bool isGlobal = false, bool isConst = false, bool isStatic = false) : base(location)
        {
            ObjectPath = objectPath;
            VarName = varName;
            TypePath = typePath;
            InitialValue = initialValue;
            IsGlobal = isGlobal;
            IsConst = isConst;
            IsStatic = isStatic;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTProcParameter : DMASTNode
    {
        public string Name { get; set; }
        public DreamPath TypePath { get; set; }
        public DMASTExpression DefaultValue { get; set; }
        public string InputType { get; set; }
        public DMASTExpression InList { get; set; }

        public DMASTProcParameter(Location location, string name, DreamPath typePath, DMASTExpression defaultValue = null, string inputType = null, DMASTExpression inList = null) : base(location)
        {
            Name = name;
            TypePath = typePath;
            DefaultValue = defaultValue;
            InputType = inputType;
            InList = inList;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }

    public class DMASTProcDefinition : DMASTDefinition
    {
        public DreamPath ObjectPath { get; set; }
        public string ProcName { get; set; }
        public List<DMASTProcParameter> Parameters { get; set; }
        public DMASTBlock Body { get; set; }
        public bool IsVerb { get; set; }
        public bool IsOverride { get; set; }

        public DMASTProcDefinition(Location location, DreamPath objectPath, string procName, List<DMASTProcParameter> parameters = null, DMASTBlock body = null, bool isVerb = false, bool isOverride = false) : base(location)
        {
            ObjectPath = objectPath;
            ProcName = procName;
            Parameters = parameters ?? new List<DMASTProcParameter>();
            Body = body;
            IsVerb = isVerb;
            IsOverride = isOverride;
        }

        public override void Accept(IDMASTVisitor visitor) { visitor.Visit(this); }
        public override T Accept<T>(IDMASTVisitor<T> visitor) { return visitor.Visit(this); }
    }
}
