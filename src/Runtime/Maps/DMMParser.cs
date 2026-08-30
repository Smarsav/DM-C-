using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.Maps
{
    public class DMMModel
    {
        public int KeyLength { get; set; }
        public Dictionary<string, List<DMMTypeInstance>> TileDefinitions { get; private set; }
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public int SizeZ { get; set; }

        public DMMModel()
        {
            TileDefinitions = new Dictionary<string, List<DMMTypeInstance>>(StringComparer.Ordinal);
        }
    }

    public class DMMTypeInstance
    {
        public DreamPath TypePath { get; set; }
        public Dictionary<string, string> VarOverrides { get; private set; }

        public DMMTypeInstance(DreamPath typePath)
        {
            TypePath = typePath;
            VarOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public class DMMParser
    {
        public static DMMModel Parse(string mapContent)
        {
            DMMModel model = new DMMModel();
            if (string.IsNullOrEmpty(mapContent)) return model;

            string[] lines = mapContent.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;
            int len = lines.Length;

            // Phase 1: Parse tile dictionary definitions
            // Example: "aaa" = (/turf/open/floor{icon_state = "floor"}, /area/hallway)
            while (i < len)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("(") && line.Contains("=") && line.Contains("{"))
                {
                    // Start of coordinate grid block
                    break;
                }

                if (line.StartsWith("\"") && line.Contains("=") && line.Contains("("))
                {
                    ParseTileDefinition(line, model);
                }

                i++;
            }

            // Phase 2: Parse coordinate grid blocks
            // Example:
            // (1, 1, 1) = {"
            // aaaaaaaaaa
            // aaaaaaaaaa
            // "}
            while (i < len)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("(") && line.Contains("=") && line.Contains("{"))
                {
                    // Extract coord: (1, 1, 1)
                    int openParen = line.IndexOf('(');
                    int closeParen = line.IndexOf(')', openParen);
                    if (openParen != -1 && closeParen != -1)
                    {
                        string coordStr = line.Substring(openParen + 1, closeParen - openParen - 1);
                        string[] parts = coordStr.Split(',');
                        int startX = parts.Length > 0 ? int.Parse(parts[0].Trim()) : 1;
                        int startY = parts.Length > 1 ? int.Parse(parts[1].Trim()) : 1;
                        int z = parts.Length > 2 ? int.Parse(parts[2].Trim()) : 1;

                        i++;
                        List<string> gridRows = new List<string>();
                        while (i < len)
                        {
                            string gridLine = lines[i].Trim();
                            if (gridLine.EndsWith("\"}") || gridLine == "\"}")
                            {
                                break;
                            }
                            if (!string.IsNullOrEmpty(gridLine))
                            {
                                gridRows.Add(gridLine);
                            }
                            i++;
                        }

                        // DMM stores rows top-to-bottom (Y decreasing or increasing depending on BYOND orientation)
                        int rowCount = gridRows.Count;
                        int keyLen = model.KeyLength > 0 ? model.KeyLength : 3;

                        for (int r = 0; r < rowCount; r++)
                        {
                            string row = gridRows[r];
                            int y = startY + (rowCount - 1 - r);
                            int colIndex = 0;

                            for (int c = 0; c < row.Length; c += keyLen)
                            {
                                if (c + keyLen <= row.Length)
                                {
                                    string key = row.Substring(c, keyLen);
                                    int x = startX + colIndex;
                                    colIndex++;

                                    model.SizeX = Math.Max(model.SizeX, x);
                                    model.SizeY = Math.Max(model.SizeY, y);
                                    model.SizeZ = Math.Max(model.SizeZ, z);

                                    // Instantiate tile in spatial grid
                                    InstantiateTile(x, y, z, key, model);
                                }
                            }
                        }
                    }
                }
                i++;
            }

            return model;
        }

        private static void ParseTileDefinition(string line, DMMModel model)
        {
            int firstQuote = line.IndexOf('"');
            int secondQuote = line.IndexOf('"', firstQuote + 1);
            if (firstQuote == -1 || secondQuote == -1) return;

            string key = line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
            if (model.KeyLength == 0)
            {
                model.KeyLength = key.Length;
            }

            int openParen = line.IndexOf('(', secondQuote);
            int closeParen = line.LastIndexOf(')');
            if (openParen == -1 || closeParen == -1 || closeParen <= openParen) return;

            string content = line.Substring(openParen + 1, closeParen - openParen - 1);
            List<DMMTypeInstance> instances = new List<DMMTypeInstance>();

            // Parse list of types: e.g. /turf/open/floor{icon_state = "floor"}, /area/hallway
            string[] items = SplitTypeDeclarations(content);
            foreach (var item in items)
            {
                string trimmed = item.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                int braceOpen = trimmed.IndexOf('{');
                if (braceOpen != -1 && trimmed.EndsWith("}"))
                {
                    string pathStr = trimmed.Substring(0, braceOpen).Trim();
                    string varBlock = trimmed.Substring(braceOpen + 1, trimmed.Length - braceOpen - 2);
                    DMMTypeInstance inst = new DMMTypeInstance(new DreamPath(pathStr));

                    string[] varPairs = varBlock.Split(';');
                    foreach (var pair in varPairs)
                    {
                        int eq = pair.IndexOf('=');
                        if (eq != -1)
                        {
                            string vName = pair.Substring(0, eq).Trim();
                            string vVal = pair.Substring(eq + 1).Trim().Trim('"');
                            inst.VarOverrides[vName] = vVal;
                        }
                    }
                    instances.Add(inst);
                }
                else
                {
                    instances.Add(new DMMTypeInstance(new DreamPath(trimmed)));
                }
            }

            model.TileDefinitions[key] = instances;
        }

        private static string[] SplitTypeDeclarations(string s)
        {
            List<string> result = new List<string>();
            StringBuilder sb = new StringBuilder();
            int braceDepth = 0;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '{') braceDepth++;
                else if (c == '}') braceDepth--;
                else if (c == ',' && braceDepth == 0)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }
                sb.Append(c);
            }

            if (sb.Length > 0)
            {
                result.Add(sb.ToString());
            }

            return result.ToArray();
        }

        private static void InstantiateTile(int x, int y, int z, string key, DMMModel model)
        {
            List<DMMTypeInstance> instances;
            if (!model.TileDefinitions.TryGetValue(key, out instances)) return;

            DM_turf turf = null;
            DM_area area = null;
            List<DM_atom_movable> movables = new List<DM_atom_movable>();

            foreach (var inst in instances)
            {
                if (inst.TypePath.IsDescendantOf(DreamPath.Turf))
                {
                    turf = new DM_turf();
                    turf.name = new DMValue(inst.TypePath.LastElement);
                    string iconState;
                    if (inst.VarOverrides.TryGetValue("icon_state", out iconState))
                    {
                        turf.icon_state = new DMValue(iconState);
                    }
                    string density;
                    if (inst.VarOverrides.TryGetValue("density", out density))
                    {
                        turf.density = new DMValue(density == "1" || density.Equals("true", StringComparison.OrdinalIgnoreCase));
                    }
                }
                else if (inst.TypePath.IsDescendantOf(DreamPath.Area))
                {
                    area = new DM_area();
                    area.name = new DMValue(inst.TypePath.LastElement);
                }
                else if (inst.TypePath.IsDescendantOf(DreamPath.Obj))
                {
                    DM_obj obj = new DM_obj();
                    obj.name = new DMValue(inst.TypePath.LastElement);
                    movables.Add(obj);
                }
                else if (inst.TypePath.IsDescendantOf(DreamPath.Mob))
                {
                    DM_mob mob = new DM_mob();
                    mob.name = new DMValue(inst.TypePath.LastElement);
                    movables.Add(mob);
                }
            }

            if (turf == null)
            {
                turf = new DM_turf();
                turf.name = new DMValue("floor");
            }

            DMSpatialGrid.Instance.SetTurf(x, y, z, turf);

            foreach (var m in movables)
            {
                m.loc = new DMValue(turf);
                turf.contents.Add(new DMValue(m));
                m.x = new DMValue(x);
                m.y = new DMValue(y);
                m.z = new DMValue(z);
            }
        }
    }
}
