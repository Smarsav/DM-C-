using System;
using System.Collections.Generic;
using DMToCSharp.Core;
using DMToCSharp.Runtime.Maps;
using DMToCSharp.Runtime.Power;

namespace DMToCSharp.Runtime.Silicon
{
    public class AIEye : DM_mob
    {
        public DM_atom Anchor { get; set; }

        public AIEye()
        {
            name = new DMValue("AI Camera Eye");
            density = new DMValue(false);
            opacity = new DMValue(false);
        }

        public void JumpTo(int x, int y, int z)
        {
            this.x = new DMValue(x);
            this.y = new DMValue(y);
            this.z = new DMValue(z);
        }
    }

    public class AICore : DM_mob
    {
        public LawSet Laws { get; set; }
        public AIEye Eye { get; private set; }
        public bool Malfunctioning { get; set; }
        public double PowerDrawWatts { get; set; }

        public AICore(string aiName = "Station Master AI")
        {
            name = new DMValue(aiName);
            density = new DMValue(true);
            opacity = new DMValue(true);
            Laws = new LawSet("Asimov");
            Eye = new AIEye();
            Malfunctioning = false;
            PowerDrawWatts = 2500.0;
        }

        public bool RemoteToggleAirlock(DM_obj airlock)
        {
            if (airlock == null) return false;
            DMValue bolted = airlock.GetVar("bolted");
            if (bolted.ToBool()) return false; // Cannot toggle bolted door

            DMValue open = airlock.GetVar("opened");
            airlock.SetVar("opened", new DMValue(!open.ToBool()));
            return true;
        }

        public bool RemoteBoltAirlock(DM_obj airlock)
        {
            if (airlock == null) return false;
            DMValue bolted = airlock.GetVar("bolted");
            airlock.SetVar("bolted", new DMValue(!bolted.ToBool()));
            return true;
        }

        public void EmergencyLockdown()
        {
            var grid = DMSpatialGrid.Instance;
            for (int z = 1; z <= grid.MaxZ; z++)
            {
                for (int x = 1; x <= grid.MaxX; x++)
                {
                    for (int y = 1; y <= grid.MaxY; y++)
                    {
                        var t = grid.GetTurf(x, y, z);
                        if (t != null)
                        {
                            foreach (var c in t.contents)
                            {
                                if (c.IsObject)
                                {
                                    string name = c.AsObject.name.AsString.ToLowerInvariant();
                                    if (name.Contains("airlock") || name.Contains("door"))
                                    {
                                        c.AsObject.SetVar("bolted", new DMValue(true));
                                        c.AsObject.SetVar("opened", new DMValue(false));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public enum CyborgModule
    {
        Standard,
        Medical,
        Engineering,
        Security,
        Janitor,
        Miner
    }

    public class CyborgMob : DM_mob
    {
        public CyborgModule Module { get; set; }
        public LawSet Laws { get; set; }
        public AICore MasterAI { get; set; }
        public double Battery { get; set; }
        public double MaxBattery { get; set; }

        public CyborgMob(string name = "Cyborg Unit 01", CyborgModule module = CyborgModule.Standard)
        {
            this.name = new DMValue(name);
            Module = module;
            Laws = new LawSet("Asimov");
            Battery = 15000.0;
            MaxBattery = 15000.0;
        }

        public double BatteryPercentage
        {
            get { return MaxBattery > 0 ? (Battery / MaxBattery) * 100.0 : 0.0; }
        }
    }
}
