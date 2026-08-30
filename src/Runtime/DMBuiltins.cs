using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime
{
    public static class DMBuiltins
    {
        private static readonly Random Rng = new Random();

        public static DMValue world_output(DMValue val)
        {
            DMWorld.Instance.Output(val);
            return DMValue.Null;
        }

        // Type checking
        public static DMValue istype(DMValue val, DreamPath typePath)
        {
            if (val.IsObject && val.AsObject != null)
            {
                DreamPath objType = val.AsObject.TypePath;
                return objType.IsDescendantOf(typePath) ? 1.0 : 0.0;
            }
            return 0.0;
        }

        public static DMValue istype(DMValue val, DMValue typeVal)
        {
            if (typeVal.IsPath)
            {
                return istype(val, typeVal.AsPath);
            }
            return 0.0;
        }

        public static DMValue isnum(DMValue val) { return val.IsNumber ? 1.0 : 0.0; }
        public static DMValue istext(DMValue val) { return val.IsString ? 1.0 : 0.0; }
        public static DMValue islist(DMValue val) { return val.IsList ? 1.0 : 0.0; }
        public static DMValue isnull(DMValue val) { return val.IsNull ? 1.0 : 0.0; }
        public static DMValue isloc(DMValue val) { return (val.IsObject && val.AsObject != null && val.AsObject.TypePath.IsDescendantOf(DreamPath.Atom)) ? 1.0 : 0.0; }

        // Strings & Lists
        public static DMValue length(DMValue val)
        {
            if (val.IsString) return (double)val.AsString.Length;
            if (val.IsList && val.AsList != null) return (double)val.AsList.Length;
            return 0.0;
        }

        public static DMValue copytext(DMValue textVal, DMValue startVal = default(DMValue), DMValue endVal = default(DMValue))
        {
            string s = textVal.AsString;
            if (string.IsNullOrEmpty(s)) return "";

            int len = s.Length;
            int start = startVal.IsNull ? 1 : (int)startVal.ToNumber();
            int end = endVal.IsNull ? 0 : (int)endVal.ToNumber();

            if (start < 0) start = len + start + 1;
            if (end < 0) end = len + end + 1;
            if (end == 0) end = len + 1;

            start = Math.Max(1, Math.Min(start, len + 1));
            end = Math.Max(start, Math.Min(end, len + 1));

            int strStart = start - 1;
            int strLen = end - start;
            if (strLen <= 0 || strStart >= len) return "";

            return s.Substring(strStart, Math.Min(strLen, len - strStart));
        }

        public static DMValue findtext(DMValue haystackVal, DMValue needleVal, DMValue startVal = default(DMValue), DMValue endVal = default(DMValue))
        {
            string haystack = haystackVal.AsString;
            string needle = needleVal.AsString;
            if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle)) return 0.0;

            int start = startVal.IsNull ? 1 : (int)startVal.ToNumber();
            int end = endVal.IsNull ? 0 : (int)endVal.ToNumber();

            int strStart = Math.Max(0, start - 1);
            int idx = haystack.IndexOf(needle, strStart, StringComparison.OrdinalIgnoreCase);

            if (idx >= 0)
            {
                int matchEnd = idx + needle.Length;
                if (end > 0 && matchEnd > end) return 0.0;
                return (double)(idx + 1);
            }
            return 0.0;
        }

        public static DMValue uppertext(DMValue textVal)
        {
            return textVal.AsString.ToUpperInvariant();
        }

        public static DMValue lowertext(DMValue textVal)
        {
            return textVal.AsString.ToLowerInvariant();
        }

        public static DMValue num2text(DMValue numVal, DMValue digitsVal = default(DMValue), DMValue radixVal = default(DMValue))
        {
            double num = numVal.ToNumber();
            int radix = radixVal.IsNull ? 10 : (int)radixVal.ToNumber();

            if (radix == 16)
            {
                return Convert.ToString((long)num, 16).ToUpperInvariant();
            }
            if (radix == 2)
            {
                return Convert.ToString((long)num, 2);
            }

            if (!digitsVal.IsNull)
            {
                int digits = (int)digitsVal.ToNumber();
                return num.ToString("F" + digits, CultureInfo.InvariantCulture);
            }

            return num.ToString(CultureInfo.InvariantCulture);
        }

        public static DMValue text2num(DMValue textVal, DMValue radixVal = default(DMValue))
        {
            string s = textVal.AsString.Trim();
            if (string.IsNullOrEmpty(s)) return DMValue.Null;

            int radix = radixVal.IsNull ? 10 : (int)radixVal.ToNumber();
            try
            {
                if (radix == 16 || s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    string hex = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s.Substring(2) : s;
                    return (double)Convert.ToInt64(hex, 16);
                }
                if (radix == 2)
                {
                    return (double)Convert.ToInt64(s, 2);
                }

                double res;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out res))
                {
                    return res;
                }
            }
            catch { }

            return DMValue.Null;
        }

        public static DMValue splittext(DMValue textVal, DMValue delimVal)
        {
            string text = textVal.AsString;
            string delim = delimVal.AsString;

            DMList list = new DMList();
            if (string.IsNullOrEmpty(delim))
            {
                foreach (char c in text)
                {
                    list.Add(new DMValue(c.ToString()));
                }
            }
            else
            {
                string[] parts = text.Split(new[] { delim }, StringSplitOptions.None);
                foreach (var p in parts)
                {
                    list.Add(new DMValue(p));
                }
            }
            return new DMValue(list);
        }

        public static DMValue jointext(DMValue listVal, DMValue glueVal)
        {
            if (listVal.IsList && listVal.AsList != null)
            {
                return listVal.AsList.Join(glueVal.AsString);
            }
            return "";
        }

        public static DMValue replacetext(DMValue textVal, DMValue findVal, DMValue replaceVal)
        {
            string text = textVal.AsString;
            string find = findVal.AsString;
            string rep = replaceVal.AsString;

            if (string.IsNullOrEmpty(find)) return text;
            return Regex.Replace(text, Regex.Escape(find), rep, RegexOptions.IgnoreCase);
        }

        public static DMValue ascii2text(DMValue asciiVal)
        {
            int code = (int)asciiVal.ToNumber();
            return new string((char)code, 1);
        }

        public static DMValue text2ascii(DMValue textVal, DMValue posVal = default(DMValue))
        {
            string s = textVal.AsString;
            int pos = posVal.IsNull ? 1 : (int)posVal.ToNumber();
            int idx = pos - 1;
            if (idx >= 0 && idx < s.Length)
            {
                return (double)(int)s[idx];
            }
            return 0.0;
        }

        // Math
        public static DMValue round(DMValue val, DMValue stepVal = default(DMValue))
        {
            double num = val.ToNumber();
            if (stepVal.IsNull || stepVal.ToNumber() == 0)
            {
                return Math.Floor(num);
            }
            double step = stepVal.ToNumber();
            return Math.Round(num / step) * step;
        }

        public static DMValue abs(DMValue val) { return Math.Abs(val.ToNumber()); }
        public static DMValue sqrt(DMValue val) { return Math.Sqrt(Math.Max(0, val.ToNumber())); }
        public static DMValue sin(DMValue val) { return Math.Sin(val.ToNumber() * Math.PI / 180.0); }
        public static DMValue cos(DMValue val) { return Math.Cos(val.ToNumber() * Math.PI / 180.0); }

        public static DMValue min(params DMValue[] values)
        {
            if (values == null || values.Length == 0) return 0.0;
            if (values.Length == 1 && values[0].IsList && values[0].AsList != null)
            {
                var list = values[0].AsList;
                if (list.Length == 0) return 0.0;
                double m = double.MaxValue;
                foreach (var it in list)
                {
                    double num = it.ToNumber();
                    if (num < m) m = num;
                }
                return m;
            }

            double minVal = double.MaxValue;
            foreach (var v in values)
            {
                double num = v.ToNumber();
                if (num < minVal) minVal = num;
            }
            return minVal;
        }

        public static DMValue max(params DMValue[] values)
        {
            if (values == null || values.Length == 0) return 0.0;
            if (values.Length == 1 && values[0].IsList && values[0].AsList != null)
            {
                var list = values[0].AsList;
                if (list.Length == 0) return 0.0;
                double m = double.MinValue;
                foreach (var it in list)
                {
                    double num = it.ToNumber();
                    if (num > m) m = num;
                }
                return m;
            }

            double maxVal = double.MinValue;
            foreach (var v in values)
            {
                double num = v.ToNumber();
                if (num > maxVal) maxVal = num;
            }
            return maxVal;
        }

        public static DMValue prob(DMValue chance)
        {
            double c = chance.ToNumber();
            if (c <= 0) return 0.0;
            if (c >= 100) return 1.0;
            return (Rng.NextDouble() * 100.0 <= c) ? 1.0 : 0.0;
        }

        public static DMValue rand(DMValue a = default(DMValue), DMValue b = default(DMValue))
        {
            if (a.IsNull && b.IsNull)
            {
                return Rng.NextDouble();
            }
            if (b.IsNull)
            {
                int maxVal = (int)a.ToNumber();
                return (double)Rng.Next(1, maxVal + 1);
            }

            int minVal = (int)a.ToNumber();
            int maxLimit = (int)b.ToNumber();
            if (minVal > maxLimit)
            {
                int tmp = minVal;
                minVal = maxLimit;
                maxLimit = tmp;
            }
            return (double)Rng.Next(minVal, maxLimit + 1);
        }

        public static DMValue roll(DMValue diceVal)
        {
            string s = diceVal.AsString.Trim();
            Match m = Regex.Match(s, @"^(\d+)[dD](\d+)(?:([+-])(\d+))?$");
            if (m.Success)
            {
                int numDice = int.Parse(m.Groups[1].Value);
                int sides = int.Parse(m.Groups[2].Value);
                int mod = 0;
                if (m.Groups[3].Success)
                {
                    mod = int.Parse(m.Groups[4].Value);
                    if (m.Groups[3].Value == "-") mod = -mod;
                }

                int total = 0;
                for (int i = 0; i < numDice; i++)
                {
                    total += Rng.Next(1, sides + 1);
                }
                return (double)(total + mod);
            }
            return 0.0;
        }

        public static DMValue pick(params DMValue[] items)
        {
            if (items == null || items.Length == 0) return DMValue.Null;

            if (items.Length == 1 && items[0].IsList && items[0].AsList != null)
            {
                var list = items[0].AsList;
                if (list.Length == 0) return DMValue.Null;
                int idx = Rng.Next(0, list.Length);
                return list.Items[idx];
            }

            int choice = Rng.Next(0, items.Length);
            return items[choice];
        }

        public static void sleep(DMValue tenths)
        {
            double delaySeconds = tenths.ToNumber() / 10.0;
            int ms = (int)(delaySeconds * 1000.0);
            if (ms > 0)
            {
                Thread.Sleep(ms);
            }
        }

        public static void spawn(DMValue tenths, Action action)
        {
            double delaySeconds = tenths.ToNumber() / 10.0;
            int ms = (int)(delaySeconds * 1000.0);
            ThreadPool.QueueUserWorkItem(delegate
            {
                if (ms > 0) Thread.Sleep(ms);
                action();
            });
        }

        public static KeyValuePair<DMValue, DMValue> list_pair(DMValue key, DMValue val)
        {
            return new KeyValuePair<DMValue, DMValue>(key, val);
        }

        public static DMValue list_init(params object[] items)
        {
            DMList list = new DMList();
            if (items != null)
            {
                foreach (var it in items)
                {
                    if (it is KeyValuePair<DMValue, DMValue>)
                    {
                        KeyValuePair<DMValue, DMValue> kvp = (KeyValuePair<DMValue, DMValue>)it;
                        list[kvp.Key] = kvp.Value;
                    }
                    else if (it is DMValue)
                    {
                        list.Add((DMValue)it);
                    }
                }
            }
            return new DMValue(list);
        }

        public static DMValue list(params DMValue[] items)
        {
            return new DMValue(new DMList(items));
        }

        public static DMValue locate(DMValue typeOrTag, DMValue inContainer = default(DMValue))
        {
            if (typeOrTag.IsPath)
            {
                DreamPath p = typeOrTag.AsPath;
                foreach (var item in DMWorld.Instance.Contents)
                {
                    if (item.IsObject && item.AsObject != null && item.AsObject.TypePath.IsDescendantOf(p))
                    {
                        return item;
                    }
                }
            }
            else if (typeOrTag.IsString)
            {
                string tag = typeOrTag.AsString;
                foreach (var item in DMWorld.Instance.Contents)
                {
                    if (item.IsObject && item.AsObject != null && item.AsObject.tag.AsString == tag)
                    {
                        return item;
                    }
                }
            }
            return DMValue.Null;
        }

        public static DMValue locate(DMValue xVal, DMValue yVal, DMValue zVal)
        {
            int x = (int)xVal.ToNumber();
            int y = (int)yVal.ToNumber();
            int z = (int)zVal.ToNumber();
            var turf = Maps.DMSpatialGrid.Instance.GetTurf(x, y, z);
            return turf != null ? new DMValue(turf) : DMValue.Null;
        }

        // Spatial & Map Builtins
        public static DMValue range(DMValue distVal, DMValue centerVal)
        {
            int dist = (int)distVal.ToNumber();
            DM_atom center = centerVal.IsObject ? centerVal.AsObject as DM_atom : null;
            return new DMValue(Maps.DMSpatialGrid.Instance.GetRange(center, dist));
        }

        public static DMValue orange(DMValue distVal, DMValue centerVal)
        {
            int dist = (int)distVal.ToNumber();
            DM_atom center = centerVal.IsObject ? centerVal.AsObject as DM_atom : null;
            return new DMValue(Maps.DMSpatialGrid.Instance.GetOrange(center, dist));
        }

        public static DMValue view(DMValue distVal, DMValue centerVal)
        {
            int dist = (int)distVal.ToNumber();
            DM_atom center = centerVal.IsObject ? centerVal.AsObject as DM_atom : null;
            return new DMValue(Maps.DMSpatialGrid.Instance.GetView(center, dist));
        }

        public static DMValue oview(DMValue distVal, DMValue centerVal)
        {
            int dist = (int)distVal.ToNumber();
            DM_atom center = centerVal.IsObject ? centerVal.AsObject as DM_atom : null;
            return new DMValue(Maps.DMSpatialGrid.Instance.GetOView(center, dist));
        }

        public static DMValue get_dist(DMValue aVal, DMValue bVal)
        {
            DM_atom a = aVal.IsObject ? aVal.AsObject as DM_atom : null;
            DM_atom b = bVal.IsObject ? bVal.AsObject as DM_atom : null;
            return (double)Maps.DMSpatialGrid.GetDist(a, b);
        }

        public static DMValue get_dir(DMValue aVal, DMValue bVal)
        {
            DM_atom a = aVal.IsObject ? aVal.AsObject as DM_atom : null;
            DM_atom b = bVal.IsObject ? bVal.AsObject as DM_atom : null;
            return (double)Maps.DMSpatialGrid.GetDir(a, b);
        }

        public static DMValue get_step(DMValue refVal, DMValue dirVal)
        {
            DM_atom atom = refVal.IsObject ? refVal.AsObject as DM_atom : null;
            int dir = (int)dirVal.ToNumber();
            var stepTurf = Maps.DMSpatialGrid.Instance.GetStep(atom, dir);
            return stepTurf != null ? new DMValue(stepTurf) : DMValue.Null;
        }

        public static DMValue step(DMValue refVal, DMValue dirVal)
        {
            DM_atom_movable movable = refVal.IsObject ? refVal.AsObject as DM_atom_movable : null;
            int dir = (int)dirVal.ToNumber();
            return Maps.DMSpatialGrid.Instance.Step(movable, dir) ? 1.0 : 0.0;
        }

        // Rust-g FFI Builtin
        public static DMValue call_ext(DMValue dllVal, DMValue procVal, params DMValue[] args)
        {
            string dll = dllVal.AsString;
            string proc = procVal.AsString;
            List<string> strArgs = new List<string>();
            if (args != null)
            {
                foreach (var a in args) strArgs.Add(a.AsString);
            }
            string result = RustG.RustGBridge.Call(proc, strArgs.ToArray());
            return new DMValue(result);
        }

        // Map Loader Builtin
        public static DMValue load_map(DMValue dmmContentVal)
        {
            string content = dmmContentVal.AsString;
            var model = Maps.DMMParser.Parse(content);
            return new DMValue(model != null);
        }

        public static DMValue initial(DMValue obj, string varName)
        {
            return obj.GetVar(varName);
        }

        public static DMValue alert(DMValue msg, DMValue title = default(DMValue), DMValue btn1 = default(DMValue), DMValue btn2 = default(DMValue), DMValue btn3 = default(DMValue))
        {
            DMWorld.Instance.Output(new DMValue(string.Format("[ALERT: {0}] {1}", title.IsNull ? "Alert" : title.AsString, msg.AsString)));
            return btn1.IsNull ? new DMValue("Ok") : btn1;
        }

        public static DMValue set_index(DMValue container, DMValue key, DMValue val)
        {
            if (container.Type == DMValueType.List && container.AsList != null)
            {
                container.AsList[key] = val;
            }
            return val;
        }

        public static DMValue get_index(DMValue container, DMValue key)
        {
            if (container.Type == DMValueType.List && container.AsList != null)
            {
                return container.AsList[key];
            }
            return DMValue.Null;
        }
    }
}
