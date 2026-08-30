using System;
using System.Collections.Generic;
using System.IO;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime
{
    public class DMWorld : DMObject
    {
        private static DMWorld _instance;
        public static DMWorld Instance
        {
            get
            {
                if (_instance == null) _instance = new DMWorld();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public override DreamPath TypePath
        {
            get { return DreamPath.World; }
        }

        public override DreamPath ParentTypePath
        {
            get { return DreamPath.Datum; }
        }

        public TextWriter Log { get; set; }
        public double time { get; set; }
        public double tick_lag { get; set; }
        public double fps { get; set; }
        public double maxx { get; set; }
        public double maxy { get; set; }
        public double maxz { get; set; }

        public event Action<DMValue> OnOutput;

        public DMWorld()
        {
            name = new DMValue("DreamMaker World");
            Log = Console.Out;
            time = 0.0;
            tick_lag = 0.5;
            fps = 20.0;
            maxx = 100;
            maxy = 100;
            maxz = 1;
        }

        public void Output(DMValue value)
        {
            if (OnOutput != null)
            {
                OnOutput(value);
            }
            else
            {
                Log.WriteLine(value.ToString());
            }
        }

        public void Output(string value)
        {
            Output(new DMValue(value));
        }

        public override DMValue GetVar(string varName)
        {
            if (string.Equals(varName, "time", StringComparison.OrdinalIgnoreCase)) return time;
            if (string.Equals(varName, "tick_lag", StringComparison.OrdinalIgnoreCase)) return tick_lag;
            if (string.Equals(varName, "fps", StringComparison.OrdinalIgnoreCase)) return fps;
            if (string.Equals(varName, "maxx", StringComparison.OrdinalIgnoreCase)) return maxx;
            if (string.Equals(varName, "maxy", StringComparison.OrdinalIgnoreCase)) return maxy;
            if (string.Equals(varName, "maxz", StringComparison.OrdinalIgnoreCase)) return maxz;
            if (string.Equals(varName, "log", StringComparison.OrdinalIgnoreCase)) return new DMValue("world.log");

            return base.GetVar(varName);
        }

        public override DMValue SetVar(string varName, DMValue value)
        {
            if (string.Equals(varName, "time", StringComparison.OrdinalIgnoreCase)) { time = value.ToNumber(); return value; }
            if (string.Equals(varName, "tick_lag", StringComparison.OrdinalIgnoreCase)) { tick_lag = value.ToNumber(); return value; }
            if (string.Equals(varName, "fps", StringComparison.OrdinalIgnoreCase)) { fps = value.ToNumber(); return value; }
            if (string.Equals(varName, "maxx", StringComparison.OrdinalIgnoreCase)) { maxx = value.ToNumber(); return value; }
            if (string.Equals(varName, "maxy", StringComparison.OrdinalIgnoreCase)) { maxy = value.ToNumber(); return value; }
            if (string.Equals(varName, "maxz", StringComparison.OrdinalIgnoreCase)) { maxz = value.ToNumber(); return value; }

            return base.SetVar(varName, value);
        }
    }
}
