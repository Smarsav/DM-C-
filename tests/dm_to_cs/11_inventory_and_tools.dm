// Test 11: Inventory Slots & Tool Interactions

/obj/item
	var/name = "item"
	var/weight = 1
	
/obj/item/tool
	var/tool_type = "generic"
	
/obj/item/tool/crowbar
	name = "mechanical crowbar"
	tool_type = "crowbar"
	
/obj/item/tool/screwdriver
	name = "screwdriver"
	tool_type = "screwdriver"

/datum/inventory
	var/obj/item/right_hand = null
	var/obj/item/left_hand = null
	var/obj/item/belt = null
	
	proc/equip_right(obj/item/I)
		right_hand = I
		world << "Equipped [I.name] to Right Hand"
		return 1
		
	proc/equip_belt(obj/item/I)
		belt = I
		world << "Equipped [I.name] to Belt"
		return 1

/obj/machinery/door/airlock
	name = "secure airlock"
	var/bolted = 0
	var/opened = 0
	var/panel_open = 0
	
	proc/use_tool(obj/item/tool/T)
		if(T.tool_type == "crowbar")
			if(!bolted)
				if(opened)
					opened = 0
				else
					opened = 1
				world << "Pried [name] state [opened] with crowbar."
				return 1
		else if(T.tool_type == "screwdriver")
			if(panel_open)
				panel_open = 0
			else
				panel_open = 1
			world << "Maintenance panel on [name] state is [panel_open]."
			return 1
		return 0

/proc/main()
	world << "=== SS13 Inventory & Tool Interactions Test ==="
	
	var/datum/inventory/inv = new()
	var/obj/item/tool/crowbar/C = new()
	var/obj/item/tool/screwdriver/S = new()
	
	inv.equip_right(C)
	inv.equip_belt(S)
	
	var/obj/machinery/door/airlock/door = new()
	door.use_tool(C)
	door.use_tool(S)
	
	world << "Door state: Opened=[door.opened], Panel=[door.panel_open]"
	world << "Inventory & Tool test completed successfully!"
