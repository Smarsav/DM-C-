using System;
using System.Collections;
using System.Collections.Generic;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.Maps
{
    public class DMSpatialGrid
    {
        public static readonly DMSpatialGrid Instance = new DMSpatialGrid(255, 255, 7);

        public int MaxX { get; private set; }
        public int MaxY { get; private set; }
        public int MaxZ { get; private set; }

        private DM_turf[,,] _turfs;
        private readonly List<DM_area> _areas = new List<DM_area>();

        public const int NORTH = 1;
        public const int SOUTH = 2;
        public const int EAST = 4;
        public const int WEST = 8;
        public const int NORTHEAST = 5;
        public const int NORTHWEST = 9;
        public const int SOUTHEAST = 6;
        public const int SOUTHWEST = 10;
        public const int UP = 16;
        public const int DOWN = 32;

        public DMSpatialGrid(int maxX, int maxY, int maxZ)
        {
            Resize(maxX, maxY, maxZ);
        }

        public void Resize(int maxX, int maxY, int maxZ)
        {
            MaxX = Math.Max(1, maxX);
            MaxY = Math.Max(1, maxY);
            MaxZ = Math.Max(1, maxZ);
            _turfs = new DM_turf[MaxX + 1, MaxY + 1, MaxZ + 1];
        }

        public bool InBounds(int x, int y, int z)
        {
            return x >= 1 && x <= MaxX && y >= 1 && y <= MaxY && z >= 1 && z <= MaxZ;
        }

        public DM_turf GetTurf(int x, int y, int z)
        {
            if (!InBounds(x, y, z)) return null;
            return _turfs[x, y, z];
        }

        public void SetTurf(int x, int y, int z, DM_turf turf)
        {
            if (!InBounds(x, y, z)) return;
            _turfs[x, y, z] = turf;
            if (turf != null)
            {
                turf.x = new DMValue(x);
                turf.y = new DMValue(y);
                turf.z = new DMValue(z);
            }
        }

        public DMList GetRange(DM_atom center, int distance)
        {
            DMList result = new DMList();
            if (center == null) return result;

            int cx = center.x.ToNumberAsInt();
            int cy = center.y.ToNumberAsInt();
            int cz = center.z.ToNumberAsInt();

            if (!InBounds(cx, cy, cz)) return result;

            int minX = Math.Max(1, cx - distance);
            int maxX = Math.Min(MaxX, cx + distance);
            int minY = Math.Max(1, cy - distance);
            int maxY = Math.Min(MaxY, cy + distance);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    DM_turf t = _turfs[x, y, cz];
                    if (t != null)
                    {
                        result.Add(new DMValue(t));
                        foreach (DMValue content in t.contents)
                        {
                            result.Add(content);
                        }
                    }
                }
            }

            return result;
        }

        public DMList GetOrange(DM_atom center, int distance)
        {
            DMList result = GetRange(center, distance);
            result.Cut(1, 1);
            return result;
        }

        public DMList GetView(DM_atom center, int distance)
        {
            // Full Field of View (FOV) with opacity ray-casting check
            DMList result = new DMList();
            if (center == null) return result;

            int cx = center.x.ToNumberAsInt();
            int cy = center.y.ToNumberAsInt();
            int cz = center.z.ToNumberAsInt();

            if (!InBounds(cx, cy, cz)) return result;

            int minX = Math.Max(1, cx - distance);
            int maxX = Math.Min(MaxX, cx + distance);
            int minY = Math.Max(1, cy - distance);
            int maxY = Math.Min(MaxY, cy + distance);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (HasLineOfSight(cx, cy, x, y, cz))
                    {
                        DM_turf t = _turfs[x, y, cz];
                        if (t != null)
                        {
                            result.Add(new DMValue(t));
                            foreach (DMValue content in t.contents)
                            {
                                result.Add(content);
                            }
                        }
                    }
                }
            }

            return result;
        }

        public DMList GetOView(DM_atom center, int distance)
        {
            DMList result = GetView(center, distance);
            return result;
        }

        private bool HasLineOfSight(int x0, int y0, int x1, int y1, int z)
        {
            if (x0 == x1 && y0 == y1) return true;

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int curX = x0;
            int curY = y0;

            while (true)
            {
                if (curX == x1 && curY == y1) return true;

                if ((curX != x0 || curY != y0) && (curX != x1 || curY != y1))
                {
                    DM_turf t = GetTurf(curX, curY, z);
                    if (t != null && t.opacity.ToBool())
                    {
                        return false;
                    }
                }

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    curX += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    curY += sy;
                }
            }
        }

        public static int GetDist(DM_atom a, DM_atom b)
        {
            if (a == null || b == null) return 0;
            int dx = Math.Abs(a.x.ToNumberAsInt() - b.x.ToNumberAsInt());
            int dy = Math.Abs(a.y.ToNumberAsInt() - b.y.ToNumberAsInt());
            return Math.Max(dx, dy);
        }

        public static int GetDir(DM_atom from, DM_atom to)
        {
            if (from == null || to == null) return 0;
            int dx = to.x.ToNumberAsInt() - from.x.ToNumberAsInt();
            int dy = to.y.ToNumberAsInt() - from.y.ToNumberAsInt();

            if (dx == 0 && dy == 0) return 0;

            if (dx > 0)
            {
                if (dy > 0) return NORTHEAST;
                if (dy < 0) return SOUTHEAST;
                return EAST;
            }
            if (dx < 0)
            {
                if (dy > 0) return NORTHWEST;
                if (dy < 0) return SOUTHWEST;
                return WEST;
            }

            return dy > 0 ? NORTH : SOUTH;
        }

        public DM_turf GetStep(DM_atom atom, int dir)
        {
            if (atom == null) return null;
            int x = atom.x.ToNumberAsInt();
            int y = atom.y.ToNumberAsInt();
            int z = atom.z.ToNumberAsInt();

            if ((dir & NORTH) != 0) y++;
            if ((dir & SOUTH) != 0) y--;
            if ((dir & EAST) != 0) x++;
            if ((dir & WEST) != 0) x--;
            if ((dir & UP) != 0) z++;
            if ((dir & DOWN) != 0) z--;

            return GetTurf(x, y, z);
        }

        public bool Step(DM_atom_movable movable, int dir)
        {
            if (movable == null) return false;
            DM_turf target = GetStep(movable, dir);
            if (target == null) return false;

            if (target.density.ToBool())
            {
                return false;
            }

            foreach (DMValue content in target.contents)
            {
                if (content.IsObject)
                {
                    DM_atom atom = content.AsObject as DM_atom;
                    if (atom != null && atom.density.ToBool())
                    {
                        return false;
                    }
                }
            }

            movable.Move(new DMValue(target), new DMValue(dir));
            return true;
        }
    }
}
