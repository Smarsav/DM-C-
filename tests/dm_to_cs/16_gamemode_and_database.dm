// Test 16: Game Modes, Antagonist Objectives & Database Persistence

/datum/objective
	var/desc = "Assassinate Target"
	var/completed = 0

/datum/antagonist_role
	var/role_name = "Traitor"
	var/telecrystals = 20
	var/list/objectives = list()
	
	proc/add_objective(d)
		var/datum/objective/O = new()
		O.desc = d
		objectives.Add(O)
		return O

/datum/player_profile
	var/ckey = "space_cadet"
	var/character_name = "Jane Doe"
	var/karma = 25
	var/rounds_played = 10

/proc/main()
	world << "=== SS13 Game Modes & Database Test ==="
	
	// 1. Antagonist & Objectives
	var/datum/antagonist_role/traitor = new()
	traitor.add_objective("Infiltrate Command")
	traitor.add_objective("Steal Nuclear Disk")
	world << "Antag: [traitor.role_name], TC: [traitor.telecrystals], Objectives: [traitor.objectives.len]"
	
	// 2. Player Profile DB
	var/datum/player_profile/profile = new()
	world << "Player: [profile.character_name] ([profile.ckey]), Karma: [profile.karma], Rounds: [profile.rounds_played]"
	
	world << "Game Mode & Database test completed successfully!"
