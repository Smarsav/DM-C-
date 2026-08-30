// Test 02: Object Tree, Inheritance, Super calls (..()), and Typechecks

/datum/entity
	var/name = "Generic Entity"
	var/health = 100
	
	proc/describe()
		return "Entity: [name] (HP: [health])"
		
	proc/take_damage(amount)
		health -= amount
		world << "[name] takes [amount] damage! Remaining: [health]"
		return health

/mob/human
	name = "Human Crewmember"
	var/job = "Assistant"
	
	describe()
		var/base = ..()
		return "[base] | Job: [job]"
		
	take_damage(amount)
		world << "Human flinches!"
		return ..(amount)

/mob/human/captain
	name = "Station Captain"
	job = "Captain"
	var/access_level = 5
	
	describe()
		var/base = ..()
		return "[base] | Access: [access_level]"

/proc/main()
	world << "=== DM Inheritance & Polymorphism Test ==="
	
	var/mob/human/captain/cap = new()
	world << cap.describe()
	cap.take_damage(15)
	
	var/is_mob = istype(cap, /mob)
	var/is_datum = istype(cap, /datum)
	world << "Is mob: [is_mob], Is datum: [is_datum]"
	world << "Inheritance test completed!"
