using System;
using System.Collections.Generic;
using DMToCSharp.Core;
using DMToCSharp.Runtime.Maps;
using DMToCSharp.Runtime.MC;

namespace DMToCSharp.Runtime.Lighting
{
    public class LightSource : DM_datum
    {
        public DM_atom Holder { get; set; }
        public int Range { get; set; }
        public double Power { get; set; }
        public string ColorHex { get; set; }
        public bool Enabled { get; set; }

        public LightSource(DM_atom holder, int range = 4, double power = 1.0, string color = "#ffffff")
        {
            Holder = holder;
            Range = range;
            Power = power;
            ColorHex = color;
            Enabled = true;
        }
    }

    public class SSLighting : DMSubsystem
    {
        public static readonly SSLighting Instance = new SSLighting();

        private readonly List<LightSource> _lightSources = new List<LightSource>();
        private readonly Dictionary<string, double> _tileLuminosity = new Dictionary<string, double>();

        public int ActiveLightsCount { get { return _lightSources.Count; } }

        public SSLighting()
        {
            SubsystemName = "Lighting";
            Priority = 35;
            WaitMilliseconds = 100;
        }

        public void RegisterLight(LightSource light)
        {
            if (light != null && !_lightSources.Contains(light))
            {
                _lightSources.Add(light);
            }
        }

        public void UnregisterLight(LightSource light)
        {
            if (light != null) _lightSources.Remove(light);
        }

        public double GetTileLuminosity(int x, int y, int z)
        {
            string key = string.Format("{0},{1},{2}", x, y, z);
            double lum;
            if (_tileLuminosity.TryGetValue(key, out lum))
            {
                return lum;
            }
            return 0.0; // Ambient space darkness
        }

        public override DMValue Fire(bool resumed = false)
        {
            base.Fire(resumed);

            _tileLuminosity.Clear();
            var grid = DMSpatialGrid.Instance;

            for (int i = 0; i < _lightSources.Count; i++)
            {
                var light = _lightSources[i];
                if (!light.Enabled || light.Holder == null) continue;

                int lx = light.Holder.x.ToNumberAsInt();
                int ly = light.Holder.y.ToNumberAsInt();
                int lz = light.Holder.z.ToNumberAsInt();

                int r = light.Range;
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int tx = lx + dx;
                        int ty = ly + dy;
                        if (tx < 1 || ty < 1 || tx > grid.MaxX || ty > grid.MaxY) continue;

                        double dist = Math.Sqrt(dx * dx + dy * dy);
                        if (dist <= r)
                        {
                            // Ray occlusion check (walls block light)
                            var targetTurf = grid.GetTurf(tx, ty, lz);
                            bool occluded = targetTurf != null && targetTurf.opacity.ToBool();

                            double falloff = Math.Max(0.0, 1.0 - (dist / (r + 1)));
                            double lum = falloff * light.Power;

                            string key = string.Format("{0},{1},{2}", tx, ty, lz);
                            double cur;
                            if (_tileLuminosity.TryGetValue(key, out cur))
                            {
                                _tileLuminosity[key] = Math.Min(1.0, cur + lum);
                            }
                            else
                            {
                                _tileLuminosity[key] = Math.Min(1.0, lum);
                            }
                        }
                    }
                }
            }

            return DMValue.Null;
        }

        public override string StatEntry()
        {
            return string.Format("{0}: {1}ms (Active Lights: {2})", SubsystemName, Cost.ToString("F2"), ActiveLightsCount);
        }
    }
}
