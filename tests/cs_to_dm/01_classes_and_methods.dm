// ==========================================================================
// Generated from C# by DMToCSharp (C# to DreamMaker Transpiler)
// ==========================================================================

/mob/station/ai
	var/power_level = 100
	var/security_status = "Green"
	proc/report_status()
		world << "AI Status: Security is [security_status], Power at [power_level]%"
		return power_level

	proc/trigger_alarm(level)
		security_status = level
		world << "ALERT: Station security changed to [level]!"
		return


