// Test 05: Space Station 13 Mini-Game Simulation

/obj/item
	name = "Generic Item"
	var/weight = 1
	
/obj/item/weapon
	name = "Weapon"
	var/damage = 10
	
/obj/item/weapon/stun_baton
	name = "Stun Baton"
	damage = 25
	var/charges = 5
	
	proc/attack(mob/target)
		if (charges <= 0)
			world << "[name] has no charges left!"
			return 0
		charges--
		world << "*BZZZT* [name] zaps [target.name]! ([charges] charges left)"
		target.health -= damage
		return damage

/obj/item/weapon/laser_gun
	name = "Laser Gun"
	damage = 35

/mob/living
	name = "Living Being"
	var/health = 100
	var/max_health = 100
	var/obj/item/equipped_item = null
	
	proc/equip(obj/item/new_item)
		equipped_item = new_item
		world << "[name] equips [new_item.name]."
		
	proc/attack_target(mob/living/target)
		if (!equipped_item)
			world << "[name] punches [target.name] for 5 damage!"
			target.health -= 5
		else if (istype(equipped_item, /obj/item/weapon/stun_baton))
			var/obj/item/weapon/stun_baton/baton = equipped_item
			baton.attack(target)
		else
			world << "[name] attacks [target.name] with [equipped_item.name]!"
			target.health -= 15
			
		world << "[target.name] HP: [target.health]/[target.max_health]"

/proc/main()
	world << "=================================================="
	world << "    SPACE STATION 13 - COMBAT & INTERACTION DEMO  "
	world << "=================================================="
	
	var/mob/living/security = new()
	security.name = "Officer Johnson"
	
	var/mob/living/syndicate = new()
	syndicate.name = "Syndicate Operative"
	
	var/obj/item/weapon/stun_baton/baton = new()
	security.equip(baton)
	
	world << "--- Round 1 ---"
	security.attack_target(syndicate)
	
	world << "--- Round 2 ---"
	security.attack_target(syndicate)
	
	world << "--- Round 3 ---"
	syndicate.attack_target(security)
	
	world << "=== SS13 Simulation Completed Successfully ==="
