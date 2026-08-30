// Test 08: Rust-g FFI Module & Spatial Query Functions

/proc/main()
	world << "=== SS13 Rust-g FFI & Spatial Queries Test ==="
	
	// 1. Rust-g SHA256 & MD5 Hashing
	var/hash_res = call_ext("rust_g.dll", "rustg_hash_string", "sha256", "SpaceStation13")
	world << "Rust-g SHA256 of 'SpaceStation13': [hash_res]"
	
	// 2. Rust-g 2D Perlin/Simplex Noise
	var/noise_val = call_ext("rust_g.dll", "rustg_noise_2d", "12.5", "45.8")
	world << "Rust-g Noise at (12.5, 45.8): [noise_val]"
	
	// 3. Rust-g JSON Validation
	var/is_valid = call_ext("rust_g.dll", "rustg_json_is_valid", "{\"station\": \"SpaceStation13\", \"id\": 13}")
	world << "Rust-g JSON is valid: [is_valid]"
	
	// 4. Spatial Geometry & Distance
	var/atom/mob1 = new /mob()
	mob1.x = 10
	mob1.y = 10
	
	var/atom/mob2 = new /mob()
	mob2.x = 14
	mob2.y = 13
	
	var/dist = get_dist(mob1, mob2)
	world << "Distance between (10,10) and (14,13): [dist]"
	
	var/dir = get_dir(mob1, mob2)
	world << "Direction from mob1 to mob2: [dir]"
	
	world << "Rust-g & Spatial test completed successfully!"
