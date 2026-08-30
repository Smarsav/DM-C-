// Test 06: DMM Map Parsing & 3D Spatial Grid Loading

/proc/main()
	world << "=== SS13 DMM Map Loading Test ==="
	
	// Create sample SS13 DMM Map content
	var/map_data = "\"aaa\" = (/turf/open/floor{icon_state = \"floor\"; density = 0}, /area/hallway)\n\"aab\" = (/turf/closed/wall{icon_state = \"wall\"; density = 1}, /area/hallway)\n\"aac\" = (/obj/machinery/door/airlock, /turf/open/floor, /area/hallway)\n\n(1, 1, 1) = {\"\naabaaa\naacaaa\n\"}\n"

	var/success = load_map(map_data)
	world << "DMM Map load success: [success]"
	
	// Query turfs using locate(x, y, z)
	var/turf/t1 = locate(1, 1, 1)
	var/turf/t2 = locate(2, 1, 1)
	
	world << "Turf (1,1,1) is: [t1.name], density: [t1.density]"
	world << "Turf (2,1,1) is: [t2.name], density: [t2.density]"
	
	world << "DMM Map loading test completed successfully!"
