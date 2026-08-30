using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DMToCSharp.Runtime
{
    public class DMList : IEnumerable<DMValue>
    {
        private readonly List<DMValue> _items = new List<DMValue>();
        private readonly Dictionary<DMValue, DMValue> _associative = new Dictionary<DMValue, DMValue>();

        public int Length
        {
            get { return _items.Count; }
        }

        public List<DMValue> Items
        {
            get { return _items; }
        }

        public DMList() { }

        public DMList(int initialCapacity)
        {
            if (initialCapacity > 0)
            {
                for (int i = 0; i < initialCapacity; i++)
                {
                    _items.Add(DMValue.Null);
                }
            }
        }

        public DMList(IEnumerable<DMValue> items)
        {
            if (items != null)
            {
                _items.AddRange(items);
            }
        }

        public DMValue this[DMValue index]
        {
            get
            {
                if (index.IsNumber)
                {
                    int idx = (int)index.AsNumber - 1;
                    if (idx >= 0 && idx < _items.Count)
                    {
                        DMValue key = _items[idx];
                        DMValue assocVal;
                        if (_associative.TryGetValue(key, out assocVal))
                        {
                            return assocVal;
                        }
                        return key;
                    }
                    return DMValue.Null;
                }
                else
                {
                    DMValue assocVal;
                    if (_associative.TryGetValue(index, out assocVal))
                    {
                        return assocVal;
                    }
                    return DMValue.Null;
                }
            }
            set
            {
                if (index.IsNumber)
                {
                    int idx = (int)index.AsNumber - 1;
                    if (idx >= 0 && idx < _items.Count)
                    {
                        _items[idx] = value;
                    }
                    else if (idx == _items.Count)
                    {
                        _items.Add(value);
                    }
                }
                else
                {
                    if (!_items.Contains(index))
                    {
                        _items.Add(index);
                    }
                    _associative[index] = value;
                }
            }
        }

        public void Add(DMValue val)
        {
            _items.Add(val);
        }

        public bool Remove(DMValue val)
        {
            _associative.Remove(val);
            return _items.Remove(val);
        }

        public void Insert(int index, DMValue val)
        {
            int idx = index - 1;
            if (idx >= 0 && idx <= _items.Count)
            {
                _items.Insert(idx, val);
            }
            else
            {
                _items.Add(val);
            }
        }

        public void Cut(int start = 1, int end = 0)
        {
            int s = Math.Max(0, start - 1);
            int e = (end <= 0 || end > _items.Count) ? _items.Count : end - 1;

            if (s < _items.Count && e >= s)
            {
                int count = e - s;
                for (int i = s; i < s + count && i < _items.Count; i++)
                {
                    _associative.Remove(_items[i]);
                }
                if (s + count <= _items.Count)
                    _items.RemoveRange(s, count);
            }
        }

        public DMList Copy(int start = 1, int end = 0)
        {
            int s = Math.Max(0, start - 1);
            int e = (end <= 0 || end > _items.Count) ? _items.Count : end - 1;

            DMList copy = new DMList();
            for (int i = s; i < e && i < _items.Count; i++)
            {
                DMValue item = _items[i];
                copy._items.Add(item);
                DMValue assoc;
                if (_associative.TryGetValue(item, out assoc))
                {
                    copy._associative[item] = assoc;
                }
            }
            return copy;
        }

        public int Find(DMValue val, int start = 1, int end = 0)
        {
            int s = Math.Max(0, start - 1);
            int e = (end <= 0 || end > _items.Count) ? _items.Count : end - 1;

            for (int i = s; i < e && i < _items.Count; i++)
            {
                if (_items[i] == val)
                {
                    return i + 1;
                }
            }
            return 0;
        }

        public string Join(string glue)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _items.Count; i++)
            {
                if (i > 0) sb.Append(glue);
                sb.Append(_items[i].ToString());
            }
            return sb.ToString();
        }

        public void Swap(int index1, int index2)
        {
            int i1 = index1 - 1;
            int i2 = index2 - 1;
            if (i1 >= 0 && i1 < _items.Count && i2 >= 0 && i2 < _items.Count)
            {
                DMValue temp = _items[i1];
                _items[i1] = _items[i2];
                _items[i2] = temp;
            }
        }

        public bool Contains(DMValue val)
        {
            return _items.Contains(val) || _associative.ContainsKey(val);
        }

        public void SetLength(int newLen)
        {
            if (newLen <= 0)
            {
                _items.Clear();
                _associative.Clear();
                return;
            }
            while (_items.Count > newLen)
            {
                DMValue last = _items[_items.Count - 1];
                _associative.Remove(last);
                _items.RemoveAt(_items.Count - 1);
            }
            while (_items.Count < newLen)
            {
                _items.Add(DMValue.Null);
            }
        }

        public IEnumerator<DMValue> GetEnumerator() { return _items.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public override string ToString()
        {
            return "/list";
        }
    }
}
