// Test 15: Dynamic Lighting & 3D Spatial Audio Mechanics

/datum/light_source
	var/range = 4
	var/power = 1.0
	var/color = "#ffffff"
	
	proc/calculate_lum(dist)
		if(dist > range)
			return 0
		var/falloff = 1.0 - (dist / (range + 1))
		return falloff * power

/datum/sound_emitter
	var/sound_name = "airlock_open"
	var/max_dist = 10
	var/volume = 100
	
	proc/get_perceived_volume(dist)
		if(dist > max_dist)
			return 0
		var/attenuation = 1.0 - (dist / max_dist)
		return (volume / 100) * attenuation

/proc/main()
	world << "=== SS13 Dynamic Lighting & Audio Simulation Test ==="
	
	// 1. Lighting Calculation Test
	var/datum/light_source/light = new()
	var/lum_center = light.calculate_lum(0)
	var/lum_edge = light.calculate_lum(3)
	world << "Center Luminosity: [lum_center], Edge Luminosity: [lum_edge]"
	
	// 2. Audio Distance Attenuation Test
	var/datum/sound_emitter/emitter = new()
	var/vol_near = emitter.get_perceived_volume(2)
	var/vol_far = emitter.get_perceived_volume(8)
	world << "Audio Near Volume: [vol_near], Audio Far Volume: [vol_far]"
	
	world << "Lighting & Audio test completed successfully!"
