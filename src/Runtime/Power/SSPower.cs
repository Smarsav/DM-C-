using System;
using System.Collections.Generic;
using DMToCSharp.Core;
using DMToCSharp.Runtime.MC;

namespace DMToCSharp.Runtime.Power
{
    public class SSPower : DMSubsystem
    {
        public static readonly SSPower Instance = new SSPower();

        public List<APC> APCs { get; private set; }
        public List<SMES> SMESUnits { get; private set; }

        public SSPower()
        {
            SubsystemName = "Power";
            Priority = 55;
            WaitMilliseconds = 50;
            APCs = new List<APC>();
            SMESUnits = new List<SMES>();
        }

        public void RegisterAPC(APC apc)
        {
            if (apc != null && !APCs.Contains(apc)) APCs.Add(apc);
        }

        public void RegisterSMES(SMES smes)
        {
            if (smes != null && !SMESUnits.Contains(smes)) SMESUnits.Add(smes);
        }

        public override DMValue Fire(bool resumed = false)
        {
            base.Fire(resumed);

            double deltaSeconds = 0.05; // 50ms

            // 1. Process SMES discharge to grid
            double totalGridAvailable = 0;
            for (int i = 0; i < SMESUnits.Count; i++)
            {
                var smes = SMESUnits[i];
                if (smes.OutputAttempt)
                {
                    totalGridAvailable += smes.Discharge(smes.OutputRate * deltaSeconds);
                }
            }

            // 2. Process APC consumption and recharge
            for (int i = 0; i < APCs.Count; i++)
            {
                var apc = APCs[i];
                apc.ProcessPowerTick(deltaSeconds);
            }

            return DMValue.Null;
        }

        public override string StatEntry()
        {
            return string.Format("{0}: {1}ms (APCs: {2}, SMES: {3})", SubsystemName, Cost.ToString("F2"), APCs.Count, SMESUnits.Count);
        }
    }
}
