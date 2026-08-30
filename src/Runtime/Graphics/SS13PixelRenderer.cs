using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace DMToCSharp.Runtime.Graphics
{
    public enum FacingDir
    {
        North = 1,
        South = 2,
        East = 4,
        West = 8
    }

    public enum CombatIntent
    {
        Help,
        Disarm,
        Grab,
        Harm
    }

    public static class SS13PixelRenderer
    {
        // 32x32 Pixel Art Procedural Sprites with authentic SS13 textures
        public static void DrawSpaceTile(System.Drawing.Graphics g, float x, float y, float size, int seed)
        {
            // Deep space background
            using (Brush b = new SolidBrush(Color.FromArgb(5, 7, 15)))
                g.FillRectangle(b, x, y, size, size);

            // Subtle parallax stars
            Random r = new Random(seed);
            int starCount = r.Next(2, 5);
            for (int i = 0; i < starCount; i++)
            {
                float sx = x + (float)(r.NextDouble() * (size - 2));
                float sy = y + (float)(r.NextDouble() * (size - 2));
                int brightness = r.Next(160, 255);
                int blueTint = r.Next(200, 255);
                using (Brush sb = new SolidBrush(Color.FromArgb(brightness, brightness, blueTint)))
                {
                    float sSize = (r.Next(0, 5) == 0) ? 2f : 1.2f;
                    g.FillRectangle(sb, sx, sy, sSize, sSize);
                }
            }
        }

        public static void DrawStationFloor(System.Drawing.Graphics g, float x, float y, float size, bool isHazard = false)
        {
            if (isHazard)
            {
                // Yellow & Black 45-degree hazard stripes
                using (Brush b = new SolidBrush(Color.FromArgb(234, 179, 8)))
                    g.FillRectangle(b, x, y, size, size);

                using (Pen p = new Pen(Color.FromArgb(20, 20, 25), size * 0.28f))
                {
                    g.DrawLine(p, x - size * 0.5f, y, x + size, y + size * 1.5f);
                    g.DrawLine(p, x, y - size * 0.5f, x + size * 1.5f, y + size);
                }
            }
            else
            {
                // Classic Nanotrasen steel tile (32x32 with seams & corner rivets)
                using (Brush b = new SolidBrush(Color.FromArgb(64, 73, 90)))
                    g.FillRectangle(b, x, y, size, size);

                using (Brush inner = new SolidBrush(Color.FromArgb(75, 85, 104)))
                    g.FillRectangle(inner, x + 2, y + 2, size - 4, size - 4);

                // Subtle grid bevel & rivet screws
                using (Pen dark = new Pen(Color.FromArgb(45, 52, 66), 1.5f))
                    g.DrawRectangle(dark, x, y, size - 1, size - 1);

                using (Pen light = new Pen(Color.FromArgb(100, 112, 136), 1f))
                {
                    g.DrawLine(light, x + 2, y + 2, x + size - 3, y + 2);
                    g.DrawLine(light, x + 2, y + 2, x + 2, y + size - 3);
                }

                // Rivets in 4 corners
                using (Brush rb = new SolidBrush(Color.FromArgb(120, 130, 150)))
                {
                    g.FillRectangle(rb, x + 3, y + 3, 2, 2);
                    g.FillRectangle(rb, x + size - 5, y + 3, 2, 2);
                    g.FillRectangle(rb, x + 3, y + size - 5, 2, 2);
                    g.FillRectangle(rb, x + size - 5, y + size - 5, 2, 2);
                }
            }
        }

        public static void DrawReinforcedWall(System.Drawing.Graphics g, float x, float y, float size, int mask)
        {
            // Reinforced dark blue/steel wall
            using (Brush b = new SolidBrush(Color.FromArgb(28, 38, 56)))
                g.FillRectangle(b, x, y, size, size);

            using (Brush inner = new SolidBrush(Color.FromArgb(41, 56, 82)))
                g.FillRectangle(inner, x + 3, y + 3, size - 6, size - 6);

            // Wall structure reinforcement beam
            using (Pen beam = new Pen(Color.FromArgb(59, 130, 246), 2f))
            {
                g.DrawLine(beam, x + 4, y + 4, x + size - 4, y + size - 4);
                g.DrawLine(beam, x + size - 4, y + 4, x + 4, y + size - 4);
            }

            // Outer bevel
            using (Pen border = new Pen(Color.FromArgb(15, 23, 42), 2f))
                g.DrawRectangle(border, x, y, size, size);

            using (Pen highlight = new Pen(Color.FromArgb(96, 165, 250), 1.5f))
            {
                g.DrawLine(highlight, x + 1, y + 1, x + size - 1, y + 1);
                g.DrawLine(highlight, x + 1, y + 1, x + 1, y + size - 1);
            }
        }

        public static void DrawGlassWindow(System.Drawing.Graphics g, float x, float y, float size)
        {
            // Floor underneath window
            DrawStationFloor(g, x, y, size);

            // Cyan translucent glass pane
            using (Brush b = new SolidBrush(Color.FromArgb(130, 34, 211, 238)))
                g.FillRectangle(b, x + 4, y + 4, size - 8, size - 8);

            // Specular glare line
            using (Pen p = new Pen(Color.FromArgb(200, 255, 255, 255), 1.5f))
                g.DrawLine(p, x + 8, y + size - 8, x + size - 8, y + 8);

            // Reinforced frame brackets
            using (Brush fb = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.FillRectangle(fb, x, y, size, 4);
                g.FillRectangle(fb, x, y + size - 4, size, 4);
            }
        }

        public static void DrawAirlock(System.Drawing.Graphics g, float x, float y, float size, bool opened, bool bolted)
        {
            // Floor background
            DrawStationFloor(g, x, y, size);

            if (opened)
            {
                // Open airlock frames on side
                using (Brush frame = new SolidBrush(Color.FromArgb(30, 41, 59)))
                {
                    g.FillRectangle(frame, x, y, 6, size);
                    g.FillRectangle(frame, x + size - 6, y, 6, size);
                }
                // Green indicator lights
                using (Brush light = new SolidBrush(Color.FromArgb(16, 185, 129)))
                {
                    g.FillEllipse(light, x + 1, y + size * 0.4f, 4, 6);
                    g.FillEllipse(light, x + size - 5, y + size * 0.4f, 4, 6);
                }
            }
            else
            {
                // Closed solid blast door with hazard stripes
                using (Brush door = new SolidBrush(Color.FromArgb(71, 85, 105)))
                    g.FillRectangle(door, x + 2, y, size - 4, size);

                using (Brush inner = new SolidBrush(Color.FromArgb(51, 65, 85)))
                    g.FillRectangle(inner, x + 6, y + 4, size - 12, size - 8);

                // Central access panel & status light
                Color lightColor = bolted ? Color.FromArgb(239, 68, 68) : Color.FromArgb(59, 130, 246);
                using (Brush lb = new SolidBrush(lightColor))
                    g.FillRectangle(lb, x + size * 0.5f - 4, y + size * 0.5f - 4, 8, 8);

                // Seam down the middle
                using (Pen seam = new Pen(Color.FromArgb(15, 23, 42), 2f))
                    g.DrawLine(seam, x + size * 0.5f, y, x + size * 0.5f, y + size);
            }
        }

        public static void DrawConsole(System.Drawing.Graphics g, float x, float y, float size, string name)
        {
            DrawStationFloor(g, x, y, size);

            // Computer Desk frame
            using (Brush desk = new SolidBrush(Color.FromArgb(30, 41, 59)))
                g.FillRectangle(desk, x + 2, y + 2, size - 4, size - 4);

            // Glowing Monitor Screen
            using (Brush screen = new SolidBrush(Color.FromArgb(6, 78, 59)))
                g.FillRectangle(screen, x + 6, y + 4, size - 12, size * 0.55f);

            // Green telemetry lines on CRT
            using (Pen p = new Pen(Color.FromArgb(52, 211, 153), 1.2f))
            {
                g.DrawLine(p, x + 8, y + 8, x + size - 8, y + 8);
                g.DrawLine(p, x + 8, y + 13, x + size * 0.6f, y + 13);
            }

            // Keyboard buttons
            using (Brush kb = new SolidBrush(Color.FromArgb(148, 163, 184)))
                g.FillRectangle(kb, x + 6, y + size * 0.68f, size - 12, size * 0.22f);
        }

        public static void DrawPlayerMob(System.Drawing.Graphics g, float x, float y, float size, FacingDir dir, string activeItem = "")
        {
            float cx = x + size * 0.5f;
            float cy = y + size * 0.5f;

            // Drop shadow
            using (Brush shadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                g.FillEllipse(shadow, cx - size * 0.35f, cy + size * 0.2f, size * 0.7f, size * 0.3f);

            // Character body (White jumpsuit with blue trim)
            using (Brush suit = new SolidBrush(Color.FromArgb(241, 245, 249)))
                g.FillEllipse(suit, cx - size * 0.28f, cy - size * 0.15f, size * 0.56f, size * 0.55f);

            // Hands / Gloves
            using (Brush gloves = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.FillEllipse(gloves, cx - size * 0.38f, cy, size * 0.18f, size * 0.18f);
                g.FillEllipse(gloves, cx + size * 0.20f, cy, size * 0.18f, size * 0.18f);
            }

            // Helmet (Spaceman Suit)
            using (Brush helmet = new SolidBrush(Color.FromArgb(226, 232, 240)))
                g.FillEllipse(helmet, cx - size * 0.26f, cy - size * 0.42f, size * 0.52f, size * 0.48f);

            // Reflective Gold/Cyan Visor according to facing direction
            using (Brush visor = new SolidBrush(Color.FromArgb(245, 158, 11)))
            {
                if (dir == FacingDir.South)
                    g.FillEllipse(visor, cx - size * 0.18f, cy - size * 0.32f, size * 0.36f, size * 0.24f);
                else if (dir == FacingDir.North)
                    g.FillEllipse(visor, cx - size * 0.18f, cy - size * 0.42f, size * 0.36f, size * 0.14f);
                else if (dir == FacingDir.East)
                    g.FillEllipse(visor, cx - size * 0.05f, cy - size * 0.34f, size * 0.26f, size * 0.24f);
                else if (dir == FacingDir.West)
                    g.FillEllipse(visor, cx - size * 0.21f, cy - size * 0.34f, size * 0.26f, size * 0.24f);
            }

            // Visor reflection shine
            using (Pen shine = new Pen(Color.FromArgb(255, 255, 255), 1.2f))
            {
                g.DrawArc(shine, cx - size * 0.14f, cy - size * 0.30f, size * 0.18f, size * 0.12f, 180, 100);
            }

            // Active Tool in hand icon
            if (!string.IsNullOrEmpty(activeItem) && activeItem != "Empty Hand")
            {
                using (Brush tb = new SolidBrush(Color.FromArgb(239, 68, 68)))
                    g.FillRectangle(tb, cx + size * 0.28f, cy - size * 0.1f, 6, 12);
                using (Pen tp = new Pen(Color.FromArgb(255, 255, 255), 1f))
                    g.DrawRectangle(tp, cx + size * 0.28f, cy - size * 0.1f, 6, 12);
            }
        }
    }
}
