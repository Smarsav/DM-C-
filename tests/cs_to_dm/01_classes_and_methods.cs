using System;
using DMToCSharp.Runtime;

namespace CSharpProject
{
    public class DM_mob_station_ai : DM_mob
    {
        public DMValue power_level = 100;
        public DMValue security_status = "Green";

        public virtual DMValue report_status()
        {
            world.Output($"AI Status: Security is {security_status}, Power at {power_level}%");
            return power_level;
        }

        public virtual DMValue trigger_alarm(DMValue level)
        {
            security_status = level;
            world.Output($"ALERT: Station security changed to {level}!");
            return DMValue.Null;
        }
    }
}
