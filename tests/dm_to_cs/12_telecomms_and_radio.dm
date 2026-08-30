// Test 12: Telecomms & Subsystem Radio Broadcasts

/datum/radio_frequency
	var/frequency = 145.9
	var/channel_name = "Common"
	var/list/transmissions = list()
	
	proc/broadcast(sender, job, message)
		var/entry = "\[[channel_name] ([frequency])\] [sender] ([job]): \"[message]\""
		transmissions.Add(entry)
		world << entry
		return 1

/proc/main()
	world << "=== SS13 Telecomms & Radio Subsystem Test ==="
	
	var/datum/radio_frequency/common_freq = new()
	var/datum/radio_frequency/sec_freq = new()
	sec_freq.frequency = 135.9
	sec_freq.channel_name = "Security"
	
	common_freq.broadcast("Captain", "Command", "All crew report to bridge.")
	sec_freq.broadcast("Warden", "Security", "Armory is on lockdown.")
	
	world << "Common transmissions count: [common_freq.transmissions.len]"
	world << "Security transmissions count: [sec_freq.transmissions.len]"
	world << "Telecomms & Radio test completed successfully!"
