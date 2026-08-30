using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime
{
    public enum DMValueType
    {
        Null,
        Number,
        String,
        List,
        Object,
        Path,
        Resource
    }

    public struct DMValue : IEquatable<DMValue>, IComparable<DMValue>, IEnumerable<DMValue>
    {
        public static readonly DMValue Null = new DMValue(DMValueType.Null, 0, null, null, null, default(DreamPath));

        private readonly DMValueType _type;
        private readonly double _num;
        private readonly string _str;
        private readonly DMList _list;
        private readonly DMObject _obj;
        private readonly DreamPath _path;

        public DMValueType Type { get { return _type; } }
        public double AsNumber { get { return _num; } }
        public string AsString
        {
            get
            {
                if (_str != null) return _str;
                if (_num != 0) return _num.ToString(CultureInfo.InvariantCulture);
                if (_obj != null) return _obj.ToString();
                if (_list != null) return _list.ToString();
                return "";
            }
        }
        public DMList AsList { get { return _list; } }
        public DMObject AsObject { get { return _obj; } }
        public DreamPath AsPath { get { return _path; } }

        public bool IsNull { get { return _type == DMValueType.Null; } }
        public bool IsNumber { get { return _type == DMValueType.Number; } }
        public bool IsString { get { return _type == DMValueType.String; } }
        public bool IsList { get { return _type == DMValueType.List; } }
        public bool IsObject { get { return _type == DMValueType.Object; } }
        public bool IsPath { get { return _type == DMValueType.Path; } }
        public bool IsResource { get { return _type == DMValueType.Resource; } }

        public DMValue(bool b) : this()
        {
            _type = DMValueType.Number;
            _num = b ? 1.0 : 0.0;
            _str = null;
            _list = null;
            _obj = null;
            _path = default(DreamPath);
        }

        public DMValue(double num) : this()
        {
            _type = DMValueType.Number;
            _num = num;
            _str = null;
            _list = null;
            _obj = null;
            _path = default(DreamPath);
        }

        public int ToNumberAsInt()
        {
            return (int)ToNumber();
        }

        public DMValue(string str) : this()
        {
            if (str == null)
            {
                _type = DMValueType.Null;
                _num = 0;
                _str = null;
                _list = null;
                _obj = null;
                _path = default(DreamPath);
            }
            else
            {
                _type = DMValueType.String;
                _num = 0;
                _str = str;
                _list = null;
                _obj = null;
                _path = default(DreamPath);
            }
        }

        public DMValue(DMList list) : this()
        {
            if (list == null)
            {
                _type = DMValueType.Null;
                _num = 0;
                _str = null;
                _list = null;
                _obj = null;
                _path = default(DreamPath);
            }
            else
            {
                _type = DMValueType.List;
                _num = 0;
                _str = null;
                _list = list;
                _obj = null;
                _path = default(DreamPath);
            }
        }

        public DMValue(DMObject obj) : this()
        {
            if (obj == null)
            {
                _type = DMValueType.Null;
                _num = 0;
                _str = null;
                _list = null;
                _obj = null;
                _path = default(DreamPath);
            }
            else
            {
                _type = DMValueType.Object;
                _num = 0;
                _str = null;
                _list = null;
                _obj = obj;
                _path = default(DreamPath);
            }
        }

        public DMValue(DreamPath path) : this()
        {
            _type = DMValueType.Path;
            _num = 0;
            _str = null;
            _list = null;
            _obj = null;
            _path = path;
        }

        private DMValue(DMValueType type, double num, string str, DMList list, DMObject obj, DreamPath path) : this()
        {
            _type = type;
            _num = num;
            _str = str;
            _list = list;
            _obj = obj;
            _path = path;
        }

        public static DMValue CreateResource(string path)
        {
            return new DMValue(DMValueType.Resource, 0, path, null, null, default(DreamPath));
        }

        // Implicit conversions
        public static implicit operator DMValue(double v) { return new DMValue(v); }
        public static implicit operator DMValue(int v) { return new DMValue((double)v); }
        public static implicit operator DMValue(float v) { return new DMValue((double)v); }
        public static implicit operator DMValue(long v) { return new DMValue((double)v); }
        public static implicit operator DMValue(string v) { return new DMValue(v); }
        public static implicit operator DMValue(bool v) { return new DMValue(v ? 1.0 : 0.0); }
        public static implicit operator DMValue(DMList v) { return new DMValue(v); }
        public static implicit operator DMValue(DMObject v) { return new DMValue(v); }
        public static implicit operator DMValue(DreamPath v) { return new DMValue(v); }

        // Explicit conversions
        public static explicit operator double(DMValue v) { return v.ToNumber(); }
        public static explicit operator int(DMValue v) { return (int)v.ToNumber(); }
        public static explicit operator string(DMValue v) { return v.AsString; }
        public static explicit operator bool(DMValue v) { return v.ToBool(); }
        public static explicit operator DMList(DMValue v) { return v._list; }
        public static explicit operator DMObject(DMValue v) { return v._obj; }
        public static explicit operator DreamPath(DMValue v) { return v._path; }

        public double ToNumber()
        {
            switch (Type)
            {
                case DMValueType.Number: return _num;
                case DMValueType.String:
                    double d;
                    return double.TryParse(_str, NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : 0;
                case DMValueType.List: return _list != null ? _list.Length : 0;
                default: return 0;
            }
        }

        public bool ToBool()
        {
            switch (Type)
            {
                case DMValueType.Null: return false;
                case DMValueType.Number: return _num != 0.0 && !double.IsNaN(_num);
                case DMValueType.String: return !string.IsNullOrEmpty(_str);
                case DMValueType.List: return _list != null && _list.Length > 0;
                case DMValueType.Object: return _obj != null;
                case DMValueType.Path: return _path != default(DreamPath);
                case DMValueType.Resource: return !string.IsNullOrEmpty(_str);
                default: return false;
            }
        }

        public static bool operator true(DMValue v) { return v.ToBool(); }
        public static bool operator false(DMValue v) { return !v.ToBool(); }

        // Arithmetic & Operators
        public static DMValue operator +(DMValue a, DMValue b)
        {
            if (a.Type == DMValueType.String || b.Type == DMValueType.String)
            {
                return new DMValue(a.ToString() + b.ToString());
            }
            if (a.Type == DMValueType.List)
            {
                DMList res = a._list.Copy();
                if (b.Type == DMValueType.List)
                {
                    foreach (var item in b._list) res.Add(item);
                }
                else
                {
                    res.Add(b);
                }
                return new DMValue(res);
            }
            return new DMValue(a.ToNumber() + b.ToNumber());
        }

        public static DMValue operator -(DMValue a, DMValue b)
        {
            if (a.Type == DMValueType.List)
            {
                DMList res = a._list.Copy();
                if (b.Type == DMValueType.List)
                {
                    foreach (var item in b._list) res.Remove(item);
                }
                else
                {
                    res.Remove(b);
                }
                return new DMValue(res);
            }
            return new DMValue(a.ToNumber() - b.ToNumber());
        }

        public static DMValue operator *(DMValue a, DMValue b)
        {
            if (a.Type == DMValueType.String && b.Type == DMValueType.Number)
            {
                int count = (int)b._num;
                if (count <= 0) return new DMValue("");
                StringBuilder sb = new StringBuilder(a._str.Length * count);
                for (int i = 0; i < count; i++) sb.Append(a._str);
                return new DMValue(sb.ToString());
            }
            return new DMValue(a.ToNumber() * b.ToNumber());
        }

        public static DMValue operator /(DMValue a, DMValue b)
        {
            double den = b.ToNumber();
            if (den == 0) return new DMValue(0);
            return new DMValue(a.ToNumber() / den);
        }

        public static DMValue operator %(DMValue a, DMValue b)
        {
            double den = b.ToNumber();
            if (den == 0) return new DMValue(0);
            return new DMValue(a.ToNumber() % den);
        }

        public static DMValue operator ++(DMValue v)
        {
            return new DMValue(v.ToNumber() + 1.0);
        }

        public static DMValue operator --(DMValue v)
        {
            return new DMValue(v.ToNumber() - 1.0);
        }

        public static DMValue operator -(DMValue a)
        {
            return new DMValue(-a.ToNumber());
        }

        public static DMValue operator !(DMValue a)
        {
            return new DMValue(!a.ToBool() ? 1.0 : 0.0);
        }

        public static DMValue operator ~(DMValue a)
        {
            return new DMValue((double)(~(long)a.ToNumber()));
        }

        public static DMValue operator &(DMValue a, DMValue b)
        {
            return new DMValue((double)((long)a.ToNumber() & (long)b.ToNumber()));
        }

        public static DMValue operator |(DMValue a, DMValue b)
        {
            return new DMValue((double)((long)a.ToNumber() | (long)b.ToNumber()));
        }

        public static DMValue operator ^(DMValue a, DMValue b)
        {
            return new DMValue((double)((long)a.ToNumber() ^ (long)b.ToNumber()));
        }

        public static DMValue operator <<(DMValue a, int shift)
        {
            return new DMValue((double)((long)a.ToNumber() << shift));
        }

        public static DMValue operator >>(DMValue a, int shift)
        {
            return new DMValue((double)((long)a.ToNumber() >> shift));
        }

        public static bool operator ==(DMValue a, DMValue b) { return a.Equals(b); }
        public static bool operator !=(DMValue a, DMValue b) { return !a.Equals(b); }
        public static bool operator <(DMValue a, DMValue b) { return a.CompareTo(b) < 0; }
        public static bool operator <=(DMValue a, DMValue b) { return a.CompareTo(b) <= 0; }
        public static bool operator >(DMValue a, DMValue b) { return a.CompareTo(b) > 0; }
        public static bool operator >=(DMValue a, DMValue b) { return a.CompareTo(b) >= 0; }

        public DMValue this[DMValue index]
        {
            get
            {
                if (Type == DMValueType.List && _list != null)
                {
                    return _list[index];
                }
                if (Type == DMValueType.String && _str != null && index.IsNumber)
                {
                    int idx = (int)index._num - 1;
                    if (idx >= 0 && idx < _str.Length)
                        return new DMValue(_str[idx].ToString());
                    return DMValue.Null;
                }
                return DMValue.Null;
            }
            set
            {
                if (Type == DMValueType.List && _list != null)
                {
                    _list[index] = value;
                }
            }
        }

        public DMValue GetVar(string varName)
        {
            if (_obj != null) return _obj.GetVar(varName);
            if (_list != null && varName.Equals("len", StringComparison.OrdinalIgnoreCase)) return _list.Length;
            return DMValue.Null;
        }

        public DMValue SetVar(string varName, DMValue value)
        {
            if (_obj != null) return _obj.SetVar(varName, value);
            if (_list != null && varName.Equals("len", StringComparison.OrdinalIgnoreCase))
            {
                _list.SetLength((int)value.ToNumber());
                return value;
            }
            return value;
        }

        public DMValue CallProc(string procName, params DMValue[] args)
        {
            if (_obj != null) return _obj.CallProc(procName, args);
            return DMValue.Null;
        }

        public bool In(DMValue container)
        {
            if (container.Type == DMValueType.List && container._list != null)
            {
                return container._list.Contains(this);
            }
            if (container.Type == DMValueType.String && container._str != null)
            {
                return container._str.Contains(this.ToString());
            }
            if (container.Type == DMValueType.Object && container._obj != null)
            {
                return container._obj.Contents.Contains(this);
            }
            return false;
        }

        public static string Format(params object[] parts)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var p in parts)
            {
                if (p is DMValue) sb.Append(((DMValue)p).ToString());
                else if (p != null) sb.Append(p.ToString());
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            switch (Type)
            {
                case DMValueType.Null: return "null";
                case DMValueType.Number: return _num.ToString(CultureInfo.InvariantCulture);
                case DMValueType.String: return _str ?? "";
                case DMValueType.List: return _list != null ? _list.ToString() : "/list";
                case DMValueType.Object: return _obj != null ? _obj.ToString() : "null";
                case DMValueType.Path: return _path.ToString();
                case DMValueType.Resource: return "'" + _str + "'";
                default: return "";
            }
        }

        public bool Equals(DMValue other)
        {
            if (Type != other.Type)
            {
                if (IsNumber && other.IsNumber) return _num == other._num;
                return false;
            }
            switch (Type)
            {
                case DMValueType.Null: return true;
                case DMValueType.Number: return _num == other._num;
                case DMValueType.String: return string.Equals(_str, other._str, StringComparison.Ordinal);
                case DMValueType.List: return ReferenceEquals(_list, other._list);
                case DMValueType.Object: return ReferenceEquals(_obj, other._obj);
                case DMValueType.Path: return _path == other._path;
                case DMValueType.Resource: return string.Equals(_str, other._str, StringComparison.OrdinalIgnoreCase);
                default: return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is DMValue && Equals((DMValue)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Type;
                if (IsNumber) hash = (hash * 397) ^ _num.GetHashCode();
                else if (IsString && _str != null) hash = (hash * 397) ^ _str.GetHashCode();
                else if (IsObject && _obj != null) hash = (hash * 397) ^ _obj.GetHashCode();
                else if (IsList && _list != null) hash = (hash * 397) ^ _list.GetHashCode();
                else if (IsPath) hash = (hash * 397) ^ _path.GetHashCode();
                return hash;
            }
        }

        public int CompareTo(DMValue other)
        {
            if (IsNumber && other.IsNumber) return _num.CompareTo(other._num);
            if (IsString && other.IsString) return string.Compare(_str, other._str, StringComparison.Ordinal);
            return 0;
        }

        public IEnumerator<DMValue> GetEnumerator()
        {
            if (Type == DMValueType.List && _list != null)
            {
                return _list.GetEnumerator();
            }
            return EmptyEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        private static IEnumerator<DMValue> EmptyEnumerator()
        {
            yield break;
        }
    }
}
