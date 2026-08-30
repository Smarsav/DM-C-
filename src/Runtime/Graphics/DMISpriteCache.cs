using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace DMToCSharp.Runtime.Graphics
{
    public static class DMISpriteCache
    {
        private static readonly Dictionary<string, Image> _loadedImages = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            string baseDir = Directory.GetCurrentDirectory();
            string iconsDir = Path.Combine(baseDir, @"psychonaut_station\icons");

            if (Directory.Exists(iconsDir))
            {
                TryLoadImage("floors", Path.Combine(iconsDir, @"turf\floors.dmi"));
                TryLoadImage("walls", Path.Combine(iconsDir, @"turf\walls.dmi"));
                TryLoadImage("space", Path.Combine(iconsDir, @"turf\space.dmi"));
                TryLoadImage("airlock_command", Path.Combine(iconsDir, @"obj\doors\airlocks\station\command.dmi"));
                TryLoadImage("airlock_medical", Path.Combine(iconsDir, @"obj\doors\airlocks\station\medical.dmi"));
                TryLoadImage("airlock_security", Path.Combine(iconsDir, @"obj\doors\airlocks\station\security.dmi"));
                TryLoadImage("airlock_engineering", Path.Combine(iconsDir, @"obj\doors\airlocks\station\engineering.dmi"));
                TryLoadImage("airlock_public", Path.Combine(iconsDir, @"obj\doors\airlocks\station\public.dmi"));
                TryLoadImage("computer", Path.Combine(iconsDir, @"obj\machines\computer.dmi"));
                TryLoadImage("sleeper", Path.Combine(iconsDir, @"obj\machines\sleeper.dmi"));
                TryLoadImage("human", Path.Combine(iconsDir, @"mob\human\human.dmi"));
                TryLoadImage("structures", Path.Combine(iconsDir, @"obj\structures.dmi"));
                TryLoadImage("clothing", Path.Combine(iconsDir, @"mob\clothing\under\color.dmi"));
            }
        }

        private static void TryLoadImage(string key, string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    // DMI is standard PNG image file
                    var img = Image.FromFile(path);
                    _loadedImages[key] = img;
                }
            }
            catch { }
        }

        public static bool HasSprite(string key)
        {
            Initialize();
            return _loadedImages.ContainsKey(key);
        }

        public static Image GetImage(string key)
        {
            Initialize();
            Image img;
            if (_loadedImages.TryGetValue(key, out img))
            {
                return img;
            }
            return null;
        }

        public static void DrawDMIFrame(System.Drawing.Graphics g, string key, int frameIndex, float destX, float destY, float destSize, int tileSize = 32)
        {
            Initialize();
            Image img = GetImage(key);
            if (img != null)
            {
                int cols = Math.Max(1, img.Width / tileSize);
                int srcX = (frameIndex % cols) * tileSize;
                int srcY = (frameIndex / cols) * tileSize;

                if (srcX + tileSize <= img.Width && srcY + tileSize <= img.Height)
                {
                    RectangleF destRect = new RectangleF(destX, destY, destSize, destSize);
                    Rectangle srcRect = new Rectangle(srcX, srcY, tileSize, tileSize);
                    g.DrawImage(img, destRect, srcRect, GraphicsUnit.Pixel);
                    return;
                }
            }

            // Fallback drawing if DMI frame is not available
            if (key == "floors") SS13PixelRenderer.DrawStationFloor(g, destX, destY, destSize);
            else if (key == "walls") SS13PixelRenderer.DrawReinforcedWall(g, destX, destY, destSize, 0);
            else if (key == "space") SS13PixelRenderer.DrawSpaceTile(g, destX, destY, destSize, (int)destX * 37 + (int)destY * 91);
            else if (key.StartsWith("airlock")) SS13PixelRenderer.DrawAirlock(g, destX, destY, destSize, false, false);
            else if (key == "human") SS13PixelRenderer.DrawPlayerMob(g, destX, destY, destSize, FacingDir.South);
            else if (key == "computer") SS13PixelRenderer.DrawConsole(g, destX, destY, destSize, "console");
        }
    }
}
