using System;
using System.Collections.Generic;
using DMToCSharp.Core;
using DMToCSharp.Core.AST;

namespace DMToCSharp.Semantics
{
    public class DMObjectTree
    {
        private readonly Dictionary<DreamPath, DMTypeDefinition> _types = new Dictionary<DreamPath, DMTypeDefinition>();
        private readonly Dictionary<string, DMVarInfo> _globalVars = new Dictionary<string, DMVarInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DMProcInfo> _globalProcs = new Dictionary<string, DMProcInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly List<CompilerDiagnostic> _diagnostics = new List<CompilerDiagnostic>();

        public Dictionary<DreamPath, DMTypeDefinition> Types
        {
            get { return _types; }
        }

        public Dictionary<string, DMVarInfo> GlobalVars
        {
            get { return _globalVars; }
        }

        public Dictionary<string, DMProcInfo> GlobalProcs
        {
            get { return _globalProcs; }
        }

        public List<CompilerDiagnostic> Diagnostics
        {
            get { return _diagnostics; }
        }

        public DMTypeDefinition RootType
        {
            get { return _types[DreamPath.Root]; }
        }

        public DMObjectTree()
        {
            InitializeStandardTypes();
        }

        private void InitializeStandardTypes()
        {
            GetOrCreateType(DreamPath.Root, null);
            GetOrCreateType(DreamPath.Datum, DreamPath.Root);
            GetOrCreateType(DreamPath.Atom, DreamPath.Datum);
            GetOrCreateType(DreamPath.Movable, DreamPath.Atom);
            GetOrCreateType(DreamPath.Obj, DreamPath.Movable);
            GetOrCreateType(DreamPath.Mob, DreamPath.Movable);
            GetOrCreateType(DreamPath.Turf, DreamPath.Atom);
            GetOrCreateType(DreamPath.Area, DreamPath.Atom);
            GetOrCreateType(DreamPath.World, DreamPath.Datum);
            GetOrCreateType(DreamPath.Client, DreamPath.Datum);
            GetOrCreateType(DreamPath.List, DreamPath.Datum);
            GetOrCreateType(DreamPath.Sound, DreamPath.Datum);
            GetOrCreateType(DreamPath.Image, DreamPath.Datum);
            GetOrCreateType(DreamPath.Icon, DreamPath.Datum);
            GetOrCreateType(DreamPath.Matrix, DreamPath.Datum);
            GetOrCreateType(DreamPath.Regex, DreamPath.Datum);
            GetOrCreateType(DreamPath.Savefile, DreamPath.Datum);

            var atom = _types[DreamPath.Atom];
            atom.Variables["name"] = new DMVarInfo("name", DreamPath.Atom, DreamPath.Root, new DMASTConstantString(Location.Unknown, ""));
            atom.Variables["desc"] = new DMVarInfo("desc", DreamPath.Atom, DreamPath.Root, new DMASTConstantString(Location.Unknown, ""));
            atom.Variables["icon"] = new DMVarInfo("icon", DreamPath.Atom, DreamPath.Root, new DMASTConstantNull(Location.Unknown));
            atom.Variables["icon_state"] = new DMVarInfo("icon_state", DreamPath.Atom, DreamPath.Root, new DMASTConstantString(Location.Unknown, ""));
            atom.Variables["loc"] = new DMVarInfo("loc", DreamPath.Atom, DreamPath.Root, new DMASTConstantNull(Location.Unknown));
            atom.Variables["x"] = new DMVarInfo("x", DreamPath.Atom, DreamPath.Root, new DMASTConstantNumber(Location.Unknown, 0));
            atom.Variables["y"] = new DMVarInfo("y", DreamPath.Atom, DreamPath.Root, new DMASTConstantNumber(Location.Unknown, 0));
            atom.Variables["z"] = new DMVarInfo("z", DreamPath.Atom, DreamPath.Root, new DMASTConstantNumber(Location.Unknown, 0));
            atom.Variables["density"] = new DMVarInfo("density", DreamPath.Atom, DreamPath.Root, new DMASTConstantNumber(Location.Unknown, 0));
            atom.Variables["opacity"] = new DMVarInfo("opacity", DreamPath.Atom, DreamPath.Root, new DMASTConstantNumber(Location.Unknown, 0));
            atom.Variables["contents"] = new DMVarInfo("contents", DreamPath.Atom, DreamPath.List, new DMASTConstantNull(Location.Unknown));

            var mob = _types[DreamPath.Mob];
            mob.Variables["key"] = new DMVarInfo("key", DreamPath.Mob, DreamPath.Root, new DMASTConstantString(Location.Unknown, ""));
            mob.Variables["ckey"] = new DMVarInfo("ckey", DreamPath.Mob, DreamPath.Root, new DMASTConstantString(Location.Unknown, ""));
            mob.Variables["client"] = new DMVarInfo("client", DreamPath.Mob, DreamPath.Client, new DMASTConstantNull(Location.Unknown));
        }

        public DMTypeDefinition GetOrCreateType(DreamPath path, DreamPath? parentPath = null)
        {
            DMTypeDefinition existing;
            if (_types.TryGetValue(path, out existing))
            {
                return existing;
            }

            DMTypeDefinition parent = null;
            if (path != DreamPath.Root)
            {
                DreamPath actualParentPath = parentPath.HasValue ? parentPath.Value : path.Parent;
                parent = GetOrCreateType(actualParentPath);
            }

            var newType = new DMTypeDefinition(path, parent);
            if (parent != null)
            {
                parent.Children.Add(newType);
            }
            _types[path] = newType;
            return newType;
        }

        public void ProcessAST(DMASTFile ast)
        {
            if (ast == null) return;

            foreach (var def in ast.Definitions)
            {
                ProcessDefinition(def, DreamPath.Root);
            }
        }

        private void ProcessDefinition(DMASTDefinition def, DreamPath currentScope)
        {
            if (def is DMASTObjectDefinition)
            {
                DMASTObjectDefinition objDef = (DMASTObjectDefinition)def;
                DreamPath fullPath = currentScope == DreamPath.Root ? objDef.Path : DreamPath.Combine(currentScope, objDef.Path);
                GetOrCreateType(fullPath);

                foreach (var member in objDef.Members)
                {
                    ProcessDefinition(member, fullPath);
                }
            }
            else if (def is DMASTVarDefinition)
            {
                DMASTVarDefinition varDef = (DMASTVarDefinition)def;
                DreamPath objPath = varDef.ObjectPath == DreamPath.Root ? currentScope : varDef.ObjectPath;

                if (varDef.IsGlobal || objPath == DreamPath.Root)
                {
                    _globalVars[varDef.VarName] = new DMVarInfo(varDef.VarName, DreamPath.Root, varDef.TypePath, varDef.InitialValue, true, varDef.IsConst, varDef.IsStatic, varDef.Location);
                }
                else
                {
                    var typeDef = GetOrCreateType(objPath);
                    typeDef.Variables[varDef.VarName] = new DMVarInfo(varDef.VarName, objPath, varDef.TypePath, varDef.InitialValue, false, varDef.IsConst, varDef.IsStatic, varDef.Location);
                }
            }
            else if (def is DMASTProcDefinition)
            {
                DMASTProcDefinition procDef = (DMASTProcDefinition)def;
                DreamPath objPath = procDef.ObjectPath == DreamPath.Root ? currentScope : procDef.ObjectPath;

                if (objPath == DreamPath.Root)
                {
                    _globalProcs[procDef.ProcName] = new DMProcInfo(procDef.ProcName, DreamPath.Root, procDef.Parameters, procDef.Body, procDef.IsVerb, false, procDef.Location);
                }
                else
                {
                    var typeDef = GetOrCreateType(objPath);
                    bool isOverride = typeDef.HasOverriddenProc(procDef.ProcName);
                    typeDef.Procs[procDef.ProcName] = new DMProcInfo(procDef.ProcName, objPath, procDef.Parameters, procDef.Body, procDef.IsVerb, isOverride, procDef.Location);
                }
            }
        }
    }
}
