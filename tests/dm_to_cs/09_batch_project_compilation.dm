// Test 09: Batch Project Compilation and Multi-Module Type Resolution

/datum/station_network
	var/network_name = "Telecomms Main Array"
	var/packets_processed = 0
	
	proc/send_packet(sender, recipient, data)
		packets_processed++
		world << "Network [network_name]: '[sender]' -> '[recipient]': [data]"
		return 1

/datum/station_network/security
	network_name = "Security Encrypted Channel"

/proc/main()
	world << "=== SS13 Batch Project & Network Test ==="
	
	var/datum/station_network/main_net = new()
	var/datum/station_network/security/sec_net = new()
	
	main_net.send_packet("Captain", "All", "Welcome to Space Station 13!")
	sec_net.send_packet("Head of Security", "Warden", "Brig is secure.")
	
	world << "Main net packets: [main_net.packets_processed]"
	world << "Sec net packets: [sec_net.packets_processed]"
	world << "Batch Project Compilation test completed successfully!"
