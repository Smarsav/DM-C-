using System;
using DMToCSharp.Runtime;

namespace CSharpProject
{
    public static class GlobalVars
    {
        public static DMValue station_name = "Nanotrasen 13";
        public static DMValue oxygen_level = 98.5;
    }

    public static class GlobalProcs
    {
        public static DMValue calculate_oxygen(DMValue crew_count, DMValue minutes)
        {
            var consumption = crew_count * 0.05 * minutes;
            var remaining = GlobalVars.oxygen_level - consumption;
            if (remaining < 20)
            {
                world.Output("WARNING: Critical Oxygen Depletion!");
            }
            return remaining;
        }

        public static DMValue format_crew_list(DMValue crew_list)
        {
            var result = "";
            foreach (var member in crew_list)
            {
                world.Output($"Crew member: {member}");
            }
            return result;
        }
    }
}
