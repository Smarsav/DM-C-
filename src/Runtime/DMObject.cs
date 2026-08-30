using System;
using System.Collections.Generic;
using System.Reflection;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime
{
    public class DMObject
    {
        private readonly Dictionary<string, DMValue> _dynamicVars = new Dictionary<string, DMValue>(StringComparer.OrdinalIgnoreCase);

        public virtual DreamPath TypePath
        {
            get { return DreamPath.Datum; }
        }

        public virtual DreamPath ParentTypePath
        {
            get { return DreamPath.Root; }
        }

        public DMValue name { get; set; }
        public DMValue desc { get; set; }
        public DMValue tag { get; set; }
        public DMValue loc { get; set; }
        public DMList contents { get; private set; }

        public DMList Contents
        {
            get { return contents; }
        }

        public DMObject()
        {
            name = new DMValue(GetType().Name.Replace("DM_", "").Replace('_', '/'));
            desc = new DMValue("");
            tag = DMValue.Null;
            loc = DMValue.Null;
            contents = new DMList();
        }

        public virtual DMValue New(params DMValue[] args)
        {
            return DMValue.Null;
        }

        public virtual DMValue Del()
        {
            if (loc.IsObject && loc.AsObject != null)
            {
                loc.AsObject.Contents.Remove(this);
            }
            return DMValue.Null;
        }

        public virtual DMValue GetVar(string varName)
        {
            if (string.Equals(varName, "type", StringComparison.OrdinalIgnoreCase)) return new DMValue(TypePath);
            if (string.Equals(varName, "parent_type", StringComparison.OrdinalIgnoreCase)) return new DMValue(ParentTypePath);
            if (string.Equals(varName, "name", StringComparison.OrdinalIgnoreCase)) return name;
            if (string.Equals(varName, "desc", StringComparison.OrdinalIgnoreCase)) return desc;
            if (string.Equals(varName, "tag", StringComparison.OrdinalIgnoreCase)) return tag;
            if (string.Equals(varName, "loc", StringComparison.OrdinalIgnoreCase)) return loc;
            if (string.Equals(varName, "contents", StringComparison.OrdinalIgnoreCase)) return new DMValue(contents);

            var field = GetType().GetField(varName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (field != null)
            {
                object raw = field.GetValue(this);
                if (raw is DMValue) return (DMValue)raw;
                if (raw != null) return new DMValue(raw.ToString());
            }

            var prop = GetType().GetProperty(varName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanRead)
            {
                object raw = prop.GetValue(this, null);
                if (raw is DMValue) return (DMValue)raw;
                if (raw != null) return new DMValue(raw.ToString());
            }

            DMValue dynVal;
            if (_dynamicVars.TryGetValue(varName, out dynVal))
            {
                return dynVal;
            }

            return DMValue.Null;
        }

        public virtual DMValue SetVar(string varName, DMValue value)
        {
            if (string.Equals(varName, "name", StringComparison.OrdinalIgnoreCase)) { name = value; return value; }
            if (string.Equals(varName, "desc", StringComparison.OrdinalIgnoreCase)) { desc = value; return value; }
            if (string.Equals(varName, "tag", StringComparison.OrdinalIgnoreCase)) { tag = value; return value; }
            if (string.Equals(varName, "loc", StringComparison.OrdinalIgnoreCase))
            {
                if (loc.IsObject && loc.AsObject != null)
                {
                    loc.AsObject.Contents.Remove(this);
                }
                loc = value;
                if (loc.IsObject && loc.AsObject != null)
                {
                    loc.AsObject.Contents.Add(this);
                }
                return value;
            }

            var field = GetType().GetField(varName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (field != null)
            {
                if (field.FieldType == typeof(DMValue))
                {
                    field.SetValue(this, value);
                    return value;
                }
            }

            var prop = GetType().GetProperty(varName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanWrite)
            {
                if (prop.PropertyType == typeof(DMValue))
                {
                    prop.SetValue(this, value, null);
                    return value;
                }
            }

            _dynamicVars[varName] = value;
            return value;
        }

        public virtual DMValue CallProc(string procName, params DMValue[] args)
        {
            var method = GetType().GetMethod(procName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (method != null)
            {
                var methodParams = method.GetParameters();
                object[] passArgs = new object[methodParams.Length];
                for (int i = 0; i < methodParams.Length; i++)
                {
                    if (i < args.Length)
                    {
                        passArgs[i] = args[i];
                    }
                    else
                    {
                        passArgs[i] = methodParams[i].DefaultValue != DBNull.Value ? methodParams[i].DefaultValue : DMValue.Null;
                    }
                }

                object result = method.Invoke(this, passArgs);
                if (result is DMValue) return (DMValue)result;
                return DMValue.Null;
            }

            return DMValue.Null;
        }

        public override string ToString()
        {
            return name.AsString;
        }
    }
}
