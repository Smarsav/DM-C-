// Test 14: DMI Metadata & Autotiling Bitmask Calculation

/datum/dmi_metadata
	var/width = 32
	var/height = 32
	var/version = 4.0
	var/list/states = list("floor", "wall", "door_open", "door_closed")
	
	proc/get_state_count()
		return states.len

/proc/calculate_wall_autotile(n, s, e, w)
	var/mask = 0
	if(n)
		mask += 1
	if(s)
		mask += 2
	if(e)
		mask += 4
	if(w)
		mask += 8
	return mask

/proc/main()
	world << "=== SS13 DMI Metadata & Autotiling Test ==="
	
	var/datum/dmi_metadata/dmi = new()
	world << "DMI Dimensions: [dmi.width]x[dmi.height], Version: [dmi.version], States: [dmi.get_state_count()]"
	
	// Test corner autotile mask (North + East = 1 + 4 = 5)
	var/mask_ne = calculate_wall_autotile(1, 0, 1, 0)
	// Test full cross autotile mask (North + South + East + West = 1 + 2 + 4 + 8 = 15)
	var/mask_full = calculate_wall_autotile(1, 1, 1, 1)
	
	world << "Autotile Corner (N+E): [mask_ne]"
	world << "Autotile Cross (All): [mask_full]"
	world << "DMI and Graphics test completed successfully!"
