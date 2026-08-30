using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.Graphics
{
    public class DMIState
    {
        public string Name { get; set; }
        public int Dirs { get; set; }
        public int Frames { get; set; }
        public double Delay { get; set; }
        public bool Movement { get; set; }

        public DMIState(string name = "")
        {
            Name = name;
            Dirs = 1;
            Frames = 1;
            Delay = 1.0;
            Movement = false;
        }
    }

    public class DMIModel
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public double Version { get; set; }
        public Dictionary<string, DMIState> States { get; private set; }

        public DMIModel()
        {
            Width = 32;
            Height = 32;
            Version = 4.0;
            States = new Dictionary<string, DMIState>(StringComparer.OrdinalIgnoreCase);
        }

        public void AddState(DMIState state)
        {
            if (state != null) States[state.Name] = state;
        }
    }

    public static class DMIParser
    {
        public static DMIModel ParseMetadata(string dmiText)
        {
            DMIModel model = new DMIModel();
            if (string.IsNullOrEmpty(dmiText)) return model;

            string[] lines = dmiText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            DMIState currentState = null;

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("version ="))
                {
                    double v;
                    if (double.TryParse(trimmed.Substring(9).Trim(), out v)) model.Version = v;
                }
                else if (trimmed.StartsWith("width ="))
                {
                    int w;
                    if (int.TryParse(trimmed.Substring(7).Trim(), out w)) model.Width = w;
                }
                else if (trimmed.StartsWith("height ="))
                {
                    int h;
                    if (int.TryParse(trimmed.Substring(8).Trim(), out h)) model.Height = h;
                }
                else if (trimmed.StartsWith("state = \"") && trimmed.EndsWith("\""))
                {
                    string name = trimmed.Substring(9, trimmed.Length - 10);
                    currentState = new DMIState(name);
                    model.AddState(currentState);
                }
                else if (trimmed.StartsWith("dirs =") && currentState != null)
                {
                    int d;
                    if (int.TryParse(trimmed.Substring(6).Trim(), out d)) currentState.Dirs = d;
                }
                else if (trimmed.StartsWith("frames =") && currentState != null)
                {
                    int f;
                    if (int.TryParse(trimmed.Substring(8).Trim(), out f)) currentState.Frames = f;
                }
            }

            return model;
        }

        // Computes 4-bit autotiling mask (North=1, South=2, East=4, West=8) for smooth wall corner joins
        public static int CalculateAutotileMask(bool north, bool south, bool east, bool west)
        {
            int mask = 0;
            if (north) mask |= 1;
            if (south) mask |= 2;
            if (east) mask |= 4;
            if (west) mask |= 8;
            return mask;
        }
    }
}
