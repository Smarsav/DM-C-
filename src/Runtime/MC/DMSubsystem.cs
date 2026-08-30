using System;
using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.MC
{
    public enum SubsystemState
    {
        Idle = 0,
        Queued = 1,
        Running = 2,
        Paused = 3,
        Sleeping = 4
    }

    [Flags]
    public enum SubsystemFlags
    {
        None = 0,
        NoFire = 1,
        NoInit = 2,
        Background = 4,
        PostInit = 8,
        Priority = 16
    }

    public class DMSubsystem : DM_datum
    {
        public string SubsystemName { get; set; }
        public int Priority { get; set; }
        public int WaitMilliseconds { get; set; }
        public SubsystemFlags SubsystemFlags { get; set; }
        public SubsystemState State { get; set; }
        public double Cost { get; set; }
        public int TimesFired { get; set; }
        public double LastFireTime { get; set; }
        public double NextFireTime { get; set; }

        public DMSubsystem()
        {
            SubsystemName = "Generic Subsystem";
            Priority = 50;
            WaitMilliseconds = 50; // 20 ticks / second default
            SubsystemFlags = SubsystemFlags.None;
            State = SubsystemState.Idle;
            Cost = 0.0;
            TimesFired = 0;
            LastFireTime = 0.0;
            NextFireTime = 0.0;
        }

        public virtual DMValue Initialize(DMValue timeofday = default(DMValue))
        {
            return new DMValue(true);
        }

        public virtual DMValue Fire(bool resumed = false)
        {
            TimesFired++;
            return DMValue.Null;
        }

        public virtual string StatEntry()
        {
            return string.Format("{0}: {1}ms (fired: {2})", SubsystemName, Cost.ToString("F2"), TimesFired);
        }

        public virtual void Recover()
        {
            State = SubsystemState.Idle;
        }

        public virtual void Shutdown()
        {
            State = SubsystemState.Sleeping;
        }
    }
}
