// Test 04: Control Flow (if/else, while, for-in, for-range, switch, try/catch)

/proc/evaluate_risk(threat_level)
	switch(threat_level)
		if(1)
			return "Green: Normal"
		if(2)
			return "Blue: Caution"
		if(3)
			return "Red: Emergency"
		else
			return "Delta: Self Destruct"

/proc/main()
	world << "=== DM Control Flow Test ==="
	
	// If-else
	var/power = 85
	if (power > 50)
		world << "Power grid is optimal: [power]%"
	else
		world << "Power grid failure!"
		
	// For range
	var/count = 0
	for (var/i = 1 to 5)
		count += i
	world << "Sum 1 to 5: [count]"
	
	// While loop
	var/w = 3
	while (w > 0)
		world << "Countdown: [w]"
		w--
		
	// Switch
	world << evaluate_risk(2)
	world << evaluate_risk(3)
	
	// Try / Catch
	try
		var/bad = 0
		world << "Inside try block"
	catch(var/e)
		world << "Caught error: [e]"
		
	world << "Control flow test completed!"
