using System;
using System.Collections.Generic;
using System.IO;

namespace TitlesSystem
{
    /// <summary>
    /// Simple key/value localization provider loaded from Config/Localization.txt.
    /// </summary>
    public static class Localization
    {
        private static readonly Dictionary<string, string> _strings =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void Load(string filePath)
        {
            _strings.Clear();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

            try
            {
                foreach (string rawLine in File.ReadAllLines(filePath))
                {
                    if (rawLine == null) continue;

                    string line = rawLine.Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("#", StringComparison.Ordinal)) continue;

                    int separator = line.IndexOf('=');
                    if (separator <= 0) continue;

                    string key = line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();

                    if (key.Length == 0) continue;

                    value = value
                        .Replace("\\n", "\n")
                        .Replace("\\r", "\r")
                        .Replace("\\t", "\t");

                    _strings[key] = value;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[TitlesSystem] Failed to load localization file: " + e.Message);
            }
        }

        public static string Get(string key, string fallback)
        {
            if (!string.IsNullOrEmpty(key) && _strings.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            return fallback;
        }

        public static string Format(string key, string fallback, params object[] args)
        {
            string format = Get(key, fallback);
            return string.Format(format, args);
        }
    }
}
