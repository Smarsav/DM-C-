using System;
using System.Collections.Generic;
using DMToCSharp.Core;
using DMToCSharp.Runtime.Maps;

namespace DMToCSharp.Runtime.Network
{
    public class DMClient : DM_datum
    {
        public string CKey { get; set; }
        public DM_mob Mob { get; set; }
        public int ViewDistance { get; set; }
        public bool Authenticated { get; set; }

        public DMClient(string ckey = "Player_1")
        {
            CKey = ckey;
            ViewDistance = 7;
            Authenticated = true;

            // Spawn player avatar mob in the station grid
            Mob = new DM_mob();
            Mob.name = new DMValue("Assistant (" + ckey + ")");
            Mob.ckey = new DMValue(ckey);
            Mob.client = new DMValue(this);

            // Locate spawn turf in grid (find first passable floor)
            var grid = DMSpatialGrid.Instance;
            bool spawned = false;
            for (int z = 1; z <= grid.MaxZ && !spawned; z++)
            {
                for (int x = 1; x <= grid.MaxX && !spawned; x++)
                {
                    for (int y = 1; y <= grid.MaxY && !spawned; y++)
                    {
                        var t = grid.GetTurf(x, y, z);
                        if (t != null && !t.density.ToBool())
                        {
                            Mob.loc = new DMValue(t);
                            Mob.x = new DMValue(x);
                            Mob.y = new DMValue(y);
                            Mob.z = new DMValue(z);
                            t.contents.Add(new DMValue(Mob));
                            spawned = true;
                        }
                    }
                }
            }

            if (!spawned)
            {
                // Default center spawn
                Mob.x = new DMValue(2);
                Mob.y = new DMValue(2);
                Mob.z = new DMValue(1);
            }
        }

        public bool HandleMovement(string directionName)
        {
            if (Mob == null) return false;

            int dir = 0;
            string d = directionName.ToLowerInvariant();
            if (d == "north" || d == "up" || d == "w") dir = DMSpatialGrid.NORTH;
            else if (d == "south" || d == "down" || d == "s") dir = DMSpatialGrid.SOUTH;
            else if (d == "east" || d == "right" || d == "d") dir = DMSpatialGrid.EAST;
            else if (d == "west" || d == "left" || d == "a") dir = DMSpatialGrid.WEST;

            if (dir == 0) return false;

            return DMSpatialGrid.Instance.Step(Mob, dir);
        }
    }

    public static class ClientManager
    {
        private static readonly Dictionary<string, DMClient> _clients = new Dictionary<string, DMClient>(StringComparer.OrdinalIgnoreCase);
        public static DMClient DefaultPlayer { get; private set; }

        static ClientManager()
        {
            DefaultPlayer = GetOrCreateClient("Captain");
        }

        public static DMClient GetOrCreateClient(string ckey)
        {
            if (string.IsNullOrEmpty(ckey)) ckey = "Player_1";
            DMClient client;
            if (!_clients.TryGetValue(ckey, out client))
            {
                client = new DMClient(ckey);
                _clients[ckey] = client;
            }
            return client;
        }

        public static IEnumerable<DMClient> AllClients
        {
            get { return _clients.Values; }
        }
    }
}
