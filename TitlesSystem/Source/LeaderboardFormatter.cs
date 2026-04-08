using System;
using System.Collections.Generic;
using System.Linq;

namespace TitlesSystem
{
    public sealed class LeaderboardEntry
    {
        public LeaderboardEntry(string playerId, string playerName, int zombieKills, string rankShortTitle)
        {
            PlayerId = playerId ?? string.Empty;
            PlayerName = playerName ?? Localization.Get("common.unknown", "Unknown");
            ZombieKills = zombieKills;
            RankShortTitle = rankShortTitle ?? Localization.Get("common.unknown", "Unknown");
        }

        public string PlayerId { get; }
        public string PlayerName { get; }
        public int ZombieKills { get; }
        public string RankShortTitle { get; }
    }

    public static class LeaderboardFormatter
    {
        public static List<LeaderboardEntry> SortEntries(IEnumerable<LeaderboardEntry> entries)
        {
            if (entries == null)
                return new List<LeaderboardEntry>();

            return entries
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.PlayerName))
                .OrderByDescending(entry => entry.ZombieKills)
                .ThenBy(entry => entry.PlayerName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.PlayerId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string BuildCompactLine(IEnumerable<LeaderboardEntry> entries, int maxPlayers)
        {
            int requestedCount = Math.Max(1, maxPlayers);
            var topEntries = SortEntries(entries)
                .Take(requestedCount)
                .ToList();

            if (topEntries.Count == 0)
                return null;

            var parts = new List<string>(topEntries.Count);
            for (int i = 0; i < topEntries.Count; i++)
            {
                LeaderboardEntry entry = topEntries[i];
                string compactName = CompactName(entry.PlayerName, 12);
                parts.Add(Localization.Format(
                    "leaderboard.format.line",
                    "#{0} {1}[{2}] {3}",
                    i + 1,
                    compactName,
                    entry.RankShortTitle,
                    entry.ZombieKills));
            }

            return Localization.Format(
                "leaderboard.format.compact",
                "[TitlesSystem] Live Top {0}: {1}",
                topEntries.Count,
                string.Join(" | ", parts));
        }

        private static string CompactName(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Localization.Get("common.unknown", "Unknown");

            string trimmed = value.Trim();
            if (trimmed.Length <= maxLength)
                return trimmed;

            return trimmed.Substring(0, Math.Max(1, maxLength - 1)) + "~";
        }
    }
}