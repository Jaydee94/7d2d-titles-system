using System;
using System.Collections.Generic;
using TitlesSystem;

namespace TitlesSystem.Commands
{
    /// <summary>
    /// Console commands for the TitlesSystem mod.
    ///
    /// Usage (server console / telnet / CSMM):
    ///   rank                       — List all rank tiers
    ///   rank check [name/entityId] — Show a player's current rank
    ///   rank set <name/entityId> <kills> — Forcibly set a player's kill count (admin)
    ///   rank top [n]               — Show top-N online players (default 10)
    ///   rank top all [n]           — Show top-N all-time players from disk (default 10)
    /// </summary>
    public class ConsoleCmdRank : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new[] { "rank", "/rank", "title", "/title", "ranks", "/ranks" };
        }

        public override string getDescription()
        {
            return "Manage and view player ranks in the TitlesSystem. Type 'help rank' for details.";
        }

        public override string getHelp()
        {
            return
                "Usage:\n" +
                "  rank                       — List all rank tiers and their kill thresholds\n" +
                "  rank check [name/entityId] — Show a player's current rank (defaults to self)\n" +
                "  rank set <name/entityId> <kills> — Forcibly set kill count (admin only)\n" +
                "  rank top [n]               — Show top-N online players by kill count (default 10)\n" +
                "  rank top all [n]           — Show top-N all-time players from disk (default 10)\n";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            bool isInGameSender = _senderInfo.RemoteClientInfo != null;
            string sub = _params.Count > 0
                ? _params[0].ToLower()
                : (isInGameSender ? "check" : "list");

            switch (sub)
            {
                case "list":
                case "ranks":
                    CmdListRanks(_senderInfo);
                    break;

                case "check":
                    CmdCheckRank(_params, _senderInfo);
                    break;

                case "set":
                    CmdSetKills(_params, _senderInfo);
                    break;

                case "top":
                    CmdTopPlayers(_params, _senderInfo);
                    break;

                default:
                    // If first param looks like a player name, treat as implicit 'check'
                    CmdCheckRank(_params, _senderInfo);
                    break;
            }
        }

        // ------------------------------------------------------------------ //
        //  Sub-command implementations
        // ------------------------------------------------------------------ //

        private static void CmdListRanks(CommandSenderInfo sender)
        {
            var ranks = RankManager.Instance.Ranks;
            Output($"=== TitlesSystem — {ranks.Count} Ranks ===", sender);
            for (int i = 0; i < ranks.Count; i++)
            {
                var r = ranks[i];
                string next = (i + 1 < ranks.Count)
                    ? $"next rank at {ranks[i + 1].KillsRequired} kills"
                    : "MAX RANK";
                Output($"  {i + 1,2}. [{r.ShortTitle,10}] {r.Title} — {r.KillsRequired}+ kills ({next})", sender);
            }
        }

        private static void CmdCheckRank(List<string> _params, CommandSenderInfo sender)
        {
            ClientInfo target = null;
            string targetId = null;
            string displayName = null;

            if (_params.Count > 1)
            {
                string query = _params[1];
                
                // Try to find online player first
                target = FindClientByNameOrId(query);
                if (target != null)
                {
                    targetId = GameApiCompat.GetPlayerId(target);
                    displayName = target.playerName;
                }
                else
                {
                    // Try to find offline player by name
                    targetId = RankManager.Instance.FindPlayerIdByName(query);
                    if (targetId != null)
                    {
                        displayName = RankManager.Instance.FindPlayerNameById(targetId);
                    }
                    else
                    {
                        Output($"[TitlesSystem] Player '{query}' not found (online or offline).", sender);
                        return;
                    }
                }
            }
            else
            {
                target = sender.RemoteClientInfo;
                if (target == null)
                {
                    Output("[TitlesSystem] No player specified. Usage: rank check <name>", sender);
                    return;
                }
                targetId = GameApiCompat.GetPlayerId(target);
                displayName = target.playerName;
            }

            var data = RankManager.Instance.GetPlayerData(targetId);
            if (data == null)
            {
                // Try loading from disk if not in cache
                data = RankManager.Instance.GetAllPlayerData().Find(p => p.PlayerId == targetId);
            }

            if (data == null)
            {
                Output($"[TitlesSystem] No rank data found for '{displayName}'. They may not have logged in yet.", sender);
                return;
            }

            var ranks = RankManager.Instance.Ranks;
            var current = ranks[data.CurrentRankIndex];

            // Build rank progress info
            string rankProgress = "";
            if (data.CurrentRankIndex + 1 < ranks.Count)
            {
                var next = ranks[data.CurrentRankIndex + 1];
                int needed = next.KillsRequired - data.ZombieKills;
                rankProgress = $" | Next: {next.ShortTitle} ({needed} more kills)";
            }
            else
            {
                rankProgress = " | MAX RANK!";
            }

            // Calculate stats
            double kdRatio = data.GetKDRatio();
            double kpd = data.GetKillsPerDay();
            double kph = data.GetKillsPerHour();
            string playtimeStr = FormatPlaytime(data.PlaytimeSeconds);

            // Header and rank
            Output($"═══════════════════════════════════════════════════════════", sender);
            Output($"=== {data.OriginalName} [{current.ShortTitle}] ===", sender);
            Output($"=== {current.Title} ===", sender);
            Output($"═══════════════════════════════════════════════════════════", sender);

            // Core stats
            Output($"Rank     : #{data.CurrentRankIndex + 1} of {ranks.Count}{rankProgress}", sender);
            Output($"", sender);

            // Kill stats
            Output($"Kills    : {data.ZombieKills}", sender);
            Output($"Deaths   : {data.Deaths}", sender);
            Output($"K/D Ratio: {kdRatio:F2}", sender);
            Output($"", sender);

            // Streaks
            Output($"Streaks  : Current {data.CurrentStreak} | Best {data.BestStreak}", sender);
            Output($"", sender);

            // Activity stats
            Output($"Activity :", sender);
            Output($"  Playtime       : {playtimeStr}", sender);
            Output($"  Kills/Day      : {kpd:F1}", sender);
            Output($"  Kills/Hour     : {kph:F1}", sender);

            if (!string.IsNullOrEmpty(data.LastKillTime) && DateTime.TryParse(data.LastKillTime, out var lastKill))
            {
                TimeSpan timeSinceKill = DateTime.UtcNow - lastKill;
                Output($"  Last Kill      : {FormatTimeAgo(timeSinceKill)} ago", sender);
            }

            Output($"", sender);

            // Top weapons
            if (data.WeaponKills.Count > 0)
            {
                Output($"Top Weapons:", sender);
                var topWeapons = new List<WeaponKillData>(data.WeaponKills);
                topWeapons.Sort((a, b) => b.Kills.CompareTo(a.Kills));

                int weaponsToShow = Math.Min(5, topWeapons.Count);
                for (int i = 0; i < weaponsToShow; i++)
                {
                    var w = topWeapons[i];
                    double pct = (double)w.Kills / data.ZombieKills * 100;
                    Output($"  {i + 1}. {w.WeaponId,-20} {w.Kills,5} kills ({pct:F1}%)", sender);
                }
            }

            Output($"═══════════════════════════════════════════════════════════", sender);
        }

        private static void CmdSetKills(List<string> _params, CommandSenderInfo sender)
        {
            if (!IsAdmin(sender))
            {
                Output("[TitlesSystem] Permission denied — admin only.", sender);
                return;
            }

            if (_params.Count < 3)
            {
                Output("[TitlesSystem] Usage: rank set <name> <kills>", sender);
                return;
            }

            // Find player by name (online or offline)
            string targetId = RankManager.Instance.FindPlayerIdByName(_params[1]);
            if (targetId == null)
            {
                Output($"[TitlesSystem] Player '{_params[1]}' not found (online or offline).", sender);
                return;
            }

            if (!int.TryParse(_params[2], out int kills) || kills < 0)
            {
                Output("[TitlesSystem] Kill count must be a non-negative integer.", sender);
                return;
            }

            if (!RankManager.Instance.SetPlayerKills(targetId, kills))
            {
                Output($"[TitlesSystem] Could not update kills — player has no rank data loaded.", sender);
                return;
            }

            var data = RankManager.Instance.GetPlayerData(targetId);
            var displayName = data?.OriginalName ?? _params[1];
            var rank = RankManager.Instance.Ranks[data.CurrentRankIndex];
            Output($"[TitlesSystem] Set {displayName}'s kills to {kills} → Rank: [{rank.Title}]", sender);
        }

        private static void CmdTopPlayers(List<string> _params, CommandSenderInfo sender)
        {
            int n = 10;
            bool showAll = false;

            // Parse parameters: rank top [all|n]
            if (_params.Count > 1)
            {
                if (string.Equals(_params[1], "all", StringComparison.OrdinalIgnoreCase))
                {
                    showAll = true;
                    if (_params.Count > 2)
                        int.TryParse(_params[2], out n);
                }
                else
                {
                    int.TryParse(_params[1], out n);
                }
            }

            n = Math.Max(1, Math.Min(n, 50));

            var entries = new List<(string name, int kills, string title)>();

            if (showAll)
            {
                // Load all player data from disk (online and offline)
                var allPlayers = RankManager.Instance.GetAllPlayerData();
                if (allPlayers.Count == 0)
                {
                    Output("[TitlesSystem] No player records found.", sender);
                    return;
                }

                foreach (var data in allPlayers)
                {
                    var rank = RankManager.Instance.Ranks[data.CurrentRankIndex];
                    entries.Add((data.OriginalName, data.ZombieKills, rank.ShortTitle));
                }

                entries.Sort((a, b) => b.kills.CompareTo(a.kills));
                Output($"=== All-Time Top {Math.Min(n, entries.Count)} Players by Zombie Kills ===", sender);
            }
            else
            {
                // Show only online players
                var clientList = ConnectionManager.Instance?.Clients?.list;
                if (clientList == null || clientList.Count == 0)
                {
                    Output("[TitlesSystem] No players currently online. Use 'rank top all' to see all-time leaderboard.", sender);
                    return;
                }

                foreach (var client in clientList)
                {
                    var data = RankManager.Instance.GetPlayerData(GameApiCompat.GetPlayerId(client));
                    if (data != null)
                    {
                        var rank = RankManager.Instance.Ranks[data.CurrentRankIndex];
                        entries.Add((data.OriginalName, data.ZombieKills, rank.ShortTitle));
                    }
                }

                entries.Sort((a, b) => b.kills.CompareTo(a.kills));
                Output($"=== Top {Math.Min(n, entries.Count)} Online Players by Zombie Kills ===", sender);
            }

            for (int i = 0; i < Math.Min(n, entries.Count); i++)
            {
                var (name, kills, title) = entries[i];
                Output($"  {i + 1,2}. {name,-20} [{title,10}]  {kills} kills", sender);
            }
        }

        // ------------------------------------------------------------------ //
        //  Helpers
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Looks up an online client by entity ID (numeric) or player name (case-insensitive substring).
        /// </summary>
        private static ClientInfo FindClientByNameOrId(string nameOrId)
        {
            if (int.TryParse(nameOrId, out int entityId))
                return GameApiCompat.GetClientInfoByEntityId(entityId);

            var clientList = ConnectionManager.Instance?.Clients?.list;
            if (clientList == null) return null;

            // Exact match first, then partial
            ClientInfo exact = clientList.Find(c =>
                string.Equals(c.playerName, nameOrId, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            return clientList.Find(c =>
                c.playerName.IndexOf(nameOrId, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Returns true if the sender is the server console or has admin permissions
        /// (permission level less than the default player permission level of 1000).
        /// </summary>
        private static bool IsAdmin(CommandSenderInfo sender)
        {
            return GameApiCompat.IsAdmin(sender);
        }

        /// <summary>
        /// Outputs a line to the command sender (server console, Telnet, CSMM, etc.).
        /// </summary>
        private static void Output(string message, CommandSenderInfo sender)
        {
            if (sender.RemoteClientInfo != null)
            {
                GameApiCompat.ChatMessageToClient(sender.RemoteClientInfo, message);
            }

            SdtdConsole.Instance.Output(message);
        }

        /// <summary>
        /// Formats seconds into human-readable playtime (e.g., "2d 5h 30m").
        /// </summary>
        private static string FormatPlaytime(long seconds)
        {
            if (seconds < 0) return "0m";

            long days = seconds / 86400;
            long hours = (seconds % 86400) / 3600;
            long minutes = (seconds % 3600) / 60;

            if (days > 0)
                return $"{days}d {hours}h {minutes}m";
            if (hours > 0)
                return $"{hours}h {minutes}m";
            return $"{minutes}m";
        }

        /// <summary>
        /// Formats a TimeSpan into human-readable format (e.g., "2 days ago" or "5 minutes ago").
        /// </summary>
        private static string FormatTimeAgo(TimeSpan ts)
        {
            if (ts.TotalSeconds < 60)
                return $"{(int)ts.TotalSeconds} seconds";
            if (ts.TotalMinutes < 60)
                return $"{(int)ts.TotalMinutes} minute(s)";
            if (ts.TotalHours < 24)
                return $"{(int)ts.TotalHours} hour(s)";
            return $"{(int)ts.TotalDays} day(s)";
        }
    }
}

