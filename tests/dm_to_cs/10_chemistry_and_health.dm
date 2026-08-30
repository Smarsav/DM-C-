// Test 10: Chemistry Reagents Synthesis & Organism Health Mechanics

/datum/reagent_beaker
	var/volume = 0
	var/max_volume = 100
	var/temperature = 293.15
	var/list/reagents = list()
	
	proc/add_reagent(id, amount)
		if(volume + amount > max_volume)
			amount = max_volume - volume
		volume += amount
		var/cur = reagents[id]
		if(!cur)
			cur = 0
		reagents[id] = cur + amount
		check_reactions()
		return amount
		
	proc/check_reactions()
		if(reagents["welding_fuel"] && reagents["oxygen"])
			var/fuel = reagents["welding_fuel"]
			var/oxy = reagents["oxygen"]
			var/react = fuel
			if(oxy < fuel)
				react = oxy
			reagents["welding_fuel"] = reagents["welding_fuel"] - react
			reagents["oxygen"] = reagents["oxygen"] - react
			var/carb = reagents["carbon"]
			if(!carb)
				carb = 0
			reagents["carbon"] = carb + (react * 0.8)
			temperature += (react * 15)
			world << "Chemical Reaction: Fuel + Oxygen -> Carbon Ash (Temp: [temperature]K)"

/datum/human_medical
	var/health = 100
	var/brute = 0
	var/burn = 0
	var/toxin = 0
	var/blood_volume = 560
	
	proc/take_damage(d_type, amount)
		if(d_type == "brute")
			brute += amount
		else if(d_type == "burn")
			burn += amount
		else if(d_type == "toxin")
			toxin += amount
		health = 100 - (brute + burn + toxin)
		
	proc/heal_damage(amount)
		if(brute > amount)
			brute -= amount
		else
			brute = 0
			
		if(burn > amount)
			burn -= amount
		else
			burn = 0
			
		health = 100 - (brute + burn + toxin)

/proc/main()
	world << "=== SS13 Chemistry & Health Simulation Test ==="
	
	// 1. Chemistry Simulation
	var/datum/reagent_beaker/beaker = new()
	beaker.add_reagent("welding_fuel", 10)
	beaker.add_reagent("oxygen", 10)
	world << "Beaker Volume: [beaker.volume]u, Temp: [beaker.temperature]K"
	
	// 2. Health & Damage Simulation
	var/datum/human_medical/patient = new()
	patient.take_damage("brute", 25)
	patient.take_damage("burn", 15)
	world << "Patient Health after damage: [patient.health]/100 HP (Brute: [patient.brute], Burn: [patient.burn])"
	
	patient.heal_damage(20)
	world << "Patient Health after medical treatment: [patient.health]/100 HP"
	
	world << "Chemistry & Health test completed successfully!"
