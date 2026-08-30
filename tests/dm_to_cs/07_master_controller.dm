// Test 07: SS13 Master Controller & Subsystem Ticking Simulation

/datum/controller/subsystem/air
	name = "Atmospherics"
	var/oxygen_level = 100
	var/pressure = 101.3
	
	proc/fire()
		pressure -= 0.1
		world << "[name] tick: pressure at [pressure] kPa"
		return 1

/datum/controller/subsystem/machinery
	name = "Machinery"
	var/power_draw = 500
	
	proc/fire()
		power_draw += 25
		world << "[name] tick: total power load [power_draw] kW"
		return 1

/proc/main()
	world << "=== SS13 Master Controller Subsystem Test ==="
	
	var/datum/controller/subsystem/air/SSair = new()
	var/datum/controller/subsystem/machinery/SSmachines = new()
	
	world << "Initializing subsystems..."
	SSair.fire()
	SSmachines.fire()
	
	world << "Round 2 Ticking..."
	SSair.fire()
	SSmachines.fire()
	
	world << "Master Controller test completed successfully!"
