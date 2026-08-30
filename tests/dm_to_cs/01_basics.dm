// Test 01: Basics - Variables, Output, Math, and String Formatting

/var/global/server_name = "DreamMaker Station"
/var/global/round_id = 42

/proc/add_numbers(a, b)
	return a + b

/proc/main()
	world << "=== DreamMaker Basics Test ==="
	world << "Server: [server_name], Round: [round_id]"
	
	var/x = 10
	var/y = 25
	var/sum = add_numbers(x, y)
	world << "10 + 25 = [sum]"
	
	var/str = "Space" + " " + "Station"
	world << "String concat: [str]"
	
	var/flt = 3.14159
	var/rounded = round(flt, 0.01)
	world << "PI rounded: [rounded]"
	world << "Basics test completed successfully!"
