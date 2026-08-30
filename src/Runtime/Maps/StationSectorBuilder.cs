using System;
using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.Maps
{
    public static class StationSectorBuilder
    {
        public static void BuildFullStationSector(DMSpatialGrid grid)
        {
            int size = 24;
            grid.Resize(size, size, 1);

            for (int y = 1; y <= size; y++)
            {
                for (int x = 1; x <= size; x++)
                {
                    // Default deep space tile
                    var spaceTurf = new DM_turf();
                    spaceTurf.name = new DMValue("space");
                    spaceTurf.density = new DMValue(false);
                    spaceTurf.opacity = new DMValue(false);
                    spaceTurf.x = new DMValue(x);
                    spaceTurf.y = new DMValue(y);
                    spaceTurf.z = new DMValue(1);
                    grid.SetTurf(x, y, 1, spaceTurf);
                }
            }

            // Build Station Hull (x: 3 to 22, y: 3 to 22)
            for (int y = 3; y <= 22; y++)
            {
                for (int x = 3; x <= 22; x++)
                {
                    var floor = new DM_turf();
                    floor.name = new DMValue("station floor");
                    floor.density = new DMValue(false);
                    floor.opacity = new DMValue(false);
                    floor.x = new DMValue(x);
                    floor.y = new DMValue(y);
                    floor.z = new DMValue(1);

                    // Outer perimeter hull walls
                    bool isOuter = (x == 3 || x == 22 || y == 3 || y == 22);

                    // Central Hallway: x: 11..14 or y: 11..14
                    bool isHallway = (x >= 11 && x <= 14) || (y >= 11 && y <= 14);

                    // Room divider walls
                    bool isBridgeWall = (y == 16 && (x <= 11 || x >= 14));
                    bool isEngiWall = (y == 9 && (x <= 11 || x >= 14));
                    bool isDividerV = (x == 10 && !isHallway) || (x == 15 && !isHallway);

                    if (isOuter)
                    {
                        // Bridge observation window at top
                        if (y == 22 && x >= 11 && x <= 14)
                        {
                            floor.name = new DMValue("reinforced window");
                            floor.density = new DMValue(true);
                            floor.opacity = new DMValue(false);
                        }
                        else
                        {
                            floor.name = new DMValue("reinforced wall");
                            floor.density = new DMValue(true);
                            floor.opacity = new DMValue(true);
                        }
                    }
                    else if (isBridgeWall || isEngiWall || isDividerV)
                    {
                        // Check if airlock doorway
                        bool isDoor = (x == 10 && y == 18) || (x == 15 && y == 18) ||
                                      (x == 10 && y == 6)  || (x == 15 && y == 6)  ||
                                      (y == 16 && (x == 12 || x == 13)) ||
                                      (y == 9 && (x == 12 || x == 13));

                        if (isDoor)
                        {
                            floor.name = new DMValue("station floor");
                            var door = new DM_obj();
                            door.name = new DMValue("secure airlock");
                            door.density = new DMValue(false);
                            door.SetVar("bolted", new DMValue(false));
                            door.SetVar("opened", new DMValue(false));
                            floor.contents.Add(new DMValue(door));
                        }
                        else
                        {
                            floor.name = new DMValue("reinforced wall");
                            floor.density = new DMValue(true);
                            floor.opacity = new DMValue(true);
                        }
                    }
                    else
                    {
                        // Add furniture / consoles
                        if (x == 12 && y == 20)
                        {
                            var console = new DM_obj();
                            console.name = new DMValue("command communications console");
                            console.density = new DMValue(true);
                            floor.contents.Add(new DMValue(console));
                        }
                        else if (x == 13 && y == 20)
                        {
                            var console = new DM_obj();
                            console.name = new DMValue("helm navigation computer");
                            console.density = new DMValue(true);
                            floor.contents.Add(new DMValue(console));
                        }
                        else if (x == 18 && y == 19)
                        {
                            var med = new DM_obj();
                            med.name = new DMValue("operating table");
                            med.density = new DMValue(true);
                            floor.contents.Add(new DMValue(med));
                        }
                        else if (x == 6 && y == 6)
                        {
                            var sec = new DM_obj();
                            sec.name = new DMValue("weapons storage locker");
                            sec.density = new DMValue(true);
                            floor.contents.Add(new DMValue(sec));
                        }
                        else if (x == 18 && y == 6)
                        {
                            var engi = new DM_obj();
                            engi.name = new DMValue("supermatter monitor console");
                            engi.density = new DMValue(true);
                            floor.contents.Add(new DMValue(engi));
                        }
                    }

                    grid.SetTurf(x, y, 1, floor);
                }
            }
        }
    }
}
