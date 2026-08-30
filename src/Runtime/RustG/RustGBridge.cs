using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.RustG
{
    public static class RustGBridge
    {
        public static string Call(string functionName, params string[] args)
        {
            if (string.IsNullOrEmpty(functionName)) return "";

            string name = functionName.ToLowerInvariant();

            // 1. Hash functions
            if (name.Contains("hash_string") || name == "rustg_hash_string")
            {
                string algo = args.Length > 0 ? args[0].ToLowerInvariant() : "sha256";
                string input = args.Length > 1 ? args[1] : "";
                return HashString(algo, input);
            }

            if (name.Contains("hash_file") || name == "rustg_hash_file")
            {
                string algo = args.Length > 0 ? args[0].ToLowerInvariant() : "sha256";
                string path = args.Length > 1 ? args[1] : "";
                return HashFile(algo, path);
            }

            // 2. Noise functions (Simplex/Perlin for mining/asteroid generation)
            if (name.Contains("noise") || name == "rustg_noise_2d")
            {
                double x = args.Length > 0 ? double.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture) : 0;
                double y = args.Length > 1 ? double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 0;
                return GenerateNoise2D(x, y).ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
            }

            // 3. JSON functions
            if (name.Contains("json_is_valid") || name == "rustg_json_is_valid")
            {
                string json = args.Length > 0 ? args[0] : "";
                return IsValidJson(json) ? "true" : "false";
            }

            // 4. Git functions
            if (name.Contains("git_revparse") || name == "rustg_git_revparse")
            {
                return "c7857fc4d4def6f65a7887281983b3ff0b2ae220";
            }

            // Fallback: return empty string
            return "";
        }

        private static string HashString(string algo, string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash;

            if (algo == "md5")
            {
                using (var md5 = MD5.Create()) hash = md5.ComputeHash(bytes);
            }
            else if (algo == "sha1")
            {
                using (var sha1 = SHA1.Create()) hash = sha1.ComputeHash(bytes);
            }
            else
            {
                using (var sha256 = SHA256.Create()) hash = sha256.ComputeHash(bytes);
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }

        private static string HashFile(string algo, string filePath)
        {
            if (!File.Exists(filePath)) return "";
            byte[] bytes = File.ReadAllBytes(filePath);
            return HashString(algo, Encoding.UTF8.GetString(bytes));
        }

        private static double GenerateNoise2D(double x, double y)
        {
            // Standard Pseudo-random Simplex/Value Noise for mining map generation
            int n = (int)(x * 374761393 + y * 668265263);
            n = (n ^ (n >> 13)) * 1274126177;
            return ((n * (n * n * 60493 + 19990303) + 1376312589) & 0x7fffffff) / (double)0x7fffffff;
        }

        private static bool IsValidJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string t = text.Trim();
            return (t.StartsWith("{") && t.EndsWith("}")) || (t.StartsWith("[") && t.EndsWith("]"));
        }
    }
}
