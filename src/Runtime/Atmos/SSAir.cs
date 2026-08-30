using System;
using System.Collections.Generic;
using DMToCSharp.Core;
using DMToCSharp.Runtime.Maps;
using DMToCSharp.Runtime.MC;

namespace DMToCSharp.Runtime.Atmos
{
    public class SSAir : DMSubsystem
    {
        public static readonly SSAir Instance = new SSAir();

        private readonly Dictionary<DM_turf, GasMixture> _turfAir = new Dictionary<DM_turf, GasMixture>();
        private readonly List<DM_turf> _activeTurfs = new List<DM_turf>();

        public int ActiveTurfsCount { get { return _activeTurfs.Count; } }

        public SSAir()
        {
            SubsystemName = "Atmospherics";
            Priority = 60;
            WaitMilliseconds = 50; // 20 ticks / second
        }

        public GasMixture GetAir(DM_turf turf)
        {
            if (turf == null) return null;
            GasMixture air;
            if (_turfAir.TryGetValue(turf, out air))
            {
                return air;
            }
            return null;
        }

        public void SetAir(DM_turf turf, GasMixture air)
        {
            if (turf == null) return;
            _turfAir[turf] = air;
            if (!_activeTurfs.Contains(turf))
            {
                _activeTurfs.Add(turf);
            }
        }

        public override DMValue Initialize(DMValue timeofday = default(DMValue))
        {
            return new DMValue(true);
        }

        public override DMValue Fire(bool resumed = false)
        {
            base.Fire(resumed);

            int count = _activeTurfs.Count;
            for (int i = 0; i < count; i++)
            {
                DM_turf turf = _activeTurfs[i];
                if (turf == null || turf.density.ToBool()) continue;

                GasMixture myAir;
                if (!_turfAir.TryGetValue(turf, out myAir)) continue;

                // Diffuse with 4 cardinal neighbors
                int x = turf.x.ToNumberAsInt();
                int y = turf.y.ToNumberAsInt();
                int z = turf.z.ToNumberAsInt();

                int[] dirs = new int[] { DMSpatialGrid.NORTH, DMSpatialGrid.SOUTH, DMSpatialGrid.EAST, DMSpatialGrid.WEST };
                for (int d = 0; d < dirs.Length; d++)
                {
                    DM_turf neighbor = DMSpatialGrid.Instance.GetStep(turf, dirs[d]);
                    if (neighbor == null || neighbor.density.ToBool()) continue;

                    GasMixture neighborAir;
                    if (_turfAir.TryGetValue(neighbor, out neighborAir))
                    {
                        double pDiff = Math.Abs(myAir.Pressure - neighborAir.Pressure);
                        if (pDiff > 0.05) // Difference threshold
                        {
                            myAir.Equalize(neighborAir);
                        }
                    }
                }
            }

            return DMValue.Null;
        }

        public override string StatEntry()
        {
            return string.Format("{0}: {1}ms (Active Turfs: {2}, Fired: {3})", SubsystemName, Cost.ToString("F2"), ActiveTurfsCount, TimesFired);
        }
    }
}
