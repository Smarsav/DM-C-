using System;
using System.Collections.Generic;
using DMToCSharp.Core;
using DMToCSharp.Core.AST;

namespace DMToCSharp.Semantics
{
    public class DMVarInfo
    {
        public string Name { get; set; }
        public DreamPath ObjectPath { get; set; }
        public DreamPath TypePath { get; set; }
        public DMASTExpression InitialValue { get; set; }
        public bool IsGlobal { get; set; }
        public bool IsConst { get; set; }
        public bool IsStatic { get; set; }
        public Location Location { get; set; }

        public DMVarInfo(string name, DreamPath objectPath, DreamPath typePath, DMASTExpression initialValue = null, bool isGlobal = false, bool isConst = false, bool isStatic = false, Location location = default(Location))
        {
            Name = name;
            ObjectPath = objectPath;
            TypePath = typePath;
            InitialValue = initialValue;
            IsGlobal = isGlobal;
            IsConst = isConst;
            IsStatic = isStatic;
            Location = location;
        }
    }

    public class DMProcInfo
    {
        public string Name { get; set; }
        public DreamPath ObjectPath { get; set; }
        public List<DMASTProcParameter> Parameters { get; set; }
        public DMASTBlock Body { get; set; }
        public bool IsVerb { get; set; }
        public bool IsOverride { get; set; }
        public Location Location { get; set; }

        public DMProcInfo(string name, DreamPath objectPath, List<DMASTProcParameter> parameters = null, DMASTBlock body = null, bool isVerb = false, bool isOverride = false, Location location = default(Location))
        {
            Name = name;
            ObjectPath = objectPath;
            Parameters = parameters ?? new List<DMASTProcParameter>();
            Body = body;
            IsVerb = isVerb;
            IsOverride = isOverride;
            Location = location;
        }
    }

    public class DMTypeDefinition
    {
        public DreamPath Path { get; private set; }
        public DMTypeDefinition Parent { get; set; }
        public Dictionary<string, DMVarInfo> Variables { get; private set; }
        public Dictionary<string, DMProcInfo> Procs { get; private set; }
        public List<DMTypeDefinition> Children { get; private set; }

        public DMTypeDefinition(DreamPath path, DMTypeDefinition parent = null)
        {
            Path = path;
            Parent = parent;
            Variables = new Dictionary<string, DMVarInfo>(StringComparer.OrdinalIgnoreCase);
            Procs = new Dictionary<string, DMProcInfo>(StringComparer.OrdinalIgnoreCase);
            Children = new List<DMTypeDefinition>();
        }

        public bool TryGetVariable(string name, out DMVarInfo varInfo)
        {
            if (Variables.TryGetValue(name, out varInfo))
                return true;

            if (Parent != null)
                return Parent.TryGetVariable(name, out varInfo);

            varInfo = null;
            return false;
        }

        public bool TryGetProc(string name, out DMProcInfo procInfo)
        {
            if (Procs.TryGetValue(name, out procInfo))
                return true;

            if (Parent != null)
                return Parent.TryGetProc(name, out procInfo);

            procInfo = null;
            return false;
        }

        public bool HasOverriddenProc(string name)
        {
            if (Parent == null) return false;
            DMProcInfo dummy;
            return Parent.TryGetProc(name, out dummy);
        }
    }
}
