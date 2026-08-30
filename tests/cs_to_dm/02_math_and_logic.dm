// ==========================================================================
// Generated from C# by DMToCSharp (C# to DreamMaker Transpiler)
// ==========================================================================

// Global Variables
/var/global/station_name = "Nanotrasen 13"
/var/global/oxygen_level = 98.5

// Global Procedures
/proc/calculate_oxygen(crew_count, minutes)
	var/consumption = crew_count * 0.05 * minutes
	var/remaining = GlobalVars.oxygen_level - consumption
	if(remaining < 20)
			world << "WARNING: Critical Oxygen Depletion!"
	return remaining

/proc/format_crew_list(crew_list)
	var/result = ""
	for(var/member in crew_list)
			world << "Crew member: [member]"
	return result

