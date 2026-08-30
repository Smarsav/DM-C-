// Test 03: Lists, Associative Lists, and List Builtins

/proc/main()
	world << "=== DM Lists Test ==="
	
	var/list/crew = list("Alice", "Bob", "Charlie", "David")
	world << "Crew count: [crew.len]"
	
	crew.Add("Eve")
	world << "Added Eve, new length: [crew.len]"
	
	var/first = crew[1]
	var/third = crew[3]
	world << "First: [first], Third: [third]"
	
	var/joined = jointext(crew, ", ")
	world << "Joined crew: [joined]"
	
	var/found = crew.Find("Charlie")
	world << "Index of Charlie: [found]"
	
	var/list/roles = list("Alice" = "Engineer", "Bob" = "Doctor")
	roles["Charlie"] = "Clown"
	world << "Charlie's role: [roles[\"Charlie\"]]"
	world << "Lists test completed!"
