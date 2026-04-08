using System;
using System.Collections.Generic;
using TitlesSystem;

namespace TitlesSystem.Commands
{
    /// <summary>
    /// Console commands for the TitlesSystem mod.
    ///
    /// Usage (server console / telnet / CSMM / in-game chat):
    ///   rank                       — List all rank tiers
    ///   rank check [name]          — Show a player's rank and stats (online or offline)
    ///   rank set <name> <kills>    — Set a player's kill count (admin, online or offline)
    ///   rank top [n]               — Show top-N online players (default 10)
    ///   rank top all [n]           — Show top-N all-time players (default 10)
    /// </summary>
    public class ConsoleCmdRank : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new[] { "rank", "/rank", "title", "/title", "ranks", "/ranks" };
        }

        public override string getDescription()
        {
            return Localization.Get(
                "cmd.rank.description",
                "Manage and view player ranks in the TitlesSystem. Type 'help rank' for details.");
        }

        public override string getHelp()
        {
            return Localization.Get(
                "cmd.rank.help",
                "Usage:\n" +
                "  rank                       - List all rank tiers and their kill thresholds\n" +
                "  rank check [name]          - Show a player's current rank and stats (online or offline)\n" +
                "  rank set <name> <kills>    - Set a player's kill count (admin only, works offline)\n" +
                "  rank top [n]               - Show top-N online players by kill count (default 10)\n" +
                "  rank top <start-end>       - Show online leaderboard range (e.g. rank top 4-8)\n" +
                "  rank top all [n]           - Show top-N all-time players from disk (default 10)\n" +
                "  rank top all <start-end>   - Show all-time leaderboard range (e.g. rank top all 11-20)\n");
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
            Output(Localization.Format("cmd.rank.list.header", "=== TitlesSystem - {0} Ranks ===", ranks.Count), sender);
            for (int i = 0; i < ranks.Count; i++)
            {
                var r = ranks[i];
                string next = (i + 1 < ranks.Count)
                    ? Localization.Format("cmd.rank.list.next", "next rank at {0} kills", ranks[i + 1].KillsRequired)
                    : Localization.Get("cmd.rank.list.max", "MAX RANK");
                Output(Localization.Format("cmd.rank.list.line", "#{0} [{1}] {2}+ ({3})", i + 1, r.ShortTitle, r.KillsRequired, next), sender);
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
                        Output(Localization.Format("cmd.rank.playerNotFound", "[TitlesSystem] Player '{0}' not found (online or offline).", query), sender);
                        return;
                    }
                }
            }
            else
            {
                target = sender.RemoteClientInfo;
                if (target == null)
                {
                    Output(Localization.Get("cmd.rank.check.usage", "[TitlesSystem] No player specified. Usage: rank check <name>"), sender);
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
                Output(Localization.Format("cmd.rank.noData", "[TitlesSystem] No rank data found for '{0}'. They may not have logged in yet.", displayName), sender);
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
                rankProgress = Localization.Format("cmd.rank.progress.next", " | Next: {0} ({1} more kills)", next.ShortTitle, needed);
            }
            else
            {
                rankProgress = Localization.Get("cmd.rank.progress.max", " | MAX RANK!");
            }

            // Calculate stats
            double kdRatio = data.GetKDRatio();
            double kpd = data.GetKillsPerDay();
            double kph = data.GetKillsPerHour();
            string playtimeStr = FormatPlaytime(data.PlaytimeSeconds);

            // Compact chat-friendly summary.
            Output(Localization.Format("cmd.rank.summary.header", "[TitlesSystem] {0} [{1}] Rank #{2}/{3}{4}", data.OriginalName, current.ShortTitle, data.CurrentRankIndex + 1, ranks.Count, rankProgress), sender);
            Output(Localization.Format("cmd.rank.summary.stats1", "Kills {0} | Deaths {1} | K/D {2:F2} | Streak {3}/{4}", data.ZombieKills, data.Deaths, kdRatio, data.CurrentStreak, data.BestStreak), sender);
            Output(Localization.Format("cmd.rank.summary.stats2", "Play {0} | Kills/Day {1:F1} | Kills/Hour {2:F1}", playtimeStr, kpd, kph), sender);

            if (!string.IsNullOrEmpty(data.LastKillTime) && DateTime.TryParse(data.LastKillTime, out var lastKill))
            {
                TimeSpan timeSinceKill = DateTime.UtcNow - lastKill;
                Output(Localization.Format("cmd.rank.summary.lastKill", "Last kill: {0} ago", FormatTimeAgo(timeSinceKill)), sender);
            }

            // Top weapons
            if (data.WeaponKills.Count > 0)
            {
                var topWeapons = new List<WeaponKillData>(data.WeaponKills);
                topWeapons.Sort((a, b) => b.Kills.CompareTo(a.Kills));

                int weaponsToShow = Math.Min(3, topWeapons.Count);
                var parts = new List<string>(weaponsToShow);
                for (int i = 0; i < weaponsToShow; i++)
                {
                    var w = topWeapons[i];
                    parts.Add($"{w.WeaponId}:{w.Kills}");
                }

                Output(Localization.Format("cmd.rank.summary.topWeapons", "Top weapons: {0}", string.Join(", ", parts)), sender);
            }
        }

        private static void CmdSetKills(List<string> _params, CommandSenderInfo sender)
        {
            if (!IsAdmin(sender))
            {
                Output(Localization.Get("cmd.rank.set.permissionDenied", "[TitlesSystem] Permission denied - admin only."), sender);
                return;
            }

            if (_params.Count < 3)
            {
                Output(Localization.Get("cmd.rank.set.usage", "[TitlesSystem] Usage: rank set <name> <kills>"), sender);
                return;
            }

            // Find player by name (online or offline)
            string targetId = RankManager.Instance.FindPlayerIdByName(_params[1]);
            if (targetId == null)
            {
                Output(Localization.Format("cmd.rank.playerNotFound", "[TitlesSystem] Player '{0}' not found (online or offline).", _params[1]), sender);
                return;
            }

            if (!int.TryParse(_params[2], out int kills) || kills < 0)
            {
                Output(Localization.Get("cmd.rank.set.invalidKills", "[TitlesSystem] Kill count must be a non-negative integer."), sender);
                return;
            }

            if (!RankManager.Instance.SetPlayerKills(targetId, kills))
            {
                Output(Localization.Get("cmd.rank.set.noData", "[TitlesSystem] Could not update kills - player has no rank data loaded."), sender);
                return;
            }

            var data = RankManager.Instance.GetPlayerData(targetId);
            var displayName = data?.OriginalName ?? _params[1];
            var rank = RankManager.Instance.Ranks[data.CurrentRankIndex];
            Output(Localization.Format("cmd.rank.set.success", "[TitlesSystem] Set {0}'s kills to {1} -> Rank: [{2}]", displayName, kills, rank.Title), sender);
        }

        private static void CmdTopPlayers(List<string> _params, CommandSenderInfo sender)
        {
            int n = 10;
            int startRank = 1;
            bool showAll = false;
            bool useRange = false;

            // Parse parameters: rank top [all] [n|start-end]
            if (_params.Count > 1)
            {
                if (string.Equals(_params[1], "all", StringComparison.OrdinalIgnoreCase))
                {
                    showAll = true;
                    if (_params.Count > 2)
                    {
                        if (TryParseRange(_params[2], out int parsedStart, out int parsedEnd))
                        {
                            useRange = true;
                            startRank = parsedStart;
                            n = parsedEnd - parsedStart + 1;
                        }
                        else
                        {
                            int.TryParse(_params[2], out n);
                        }
                    }
                }
                else
                {
                    if (TryParseRange(_params[1], out int parsedStart, out int parsedEnd))
                    {
                        useRange = true;
                        startRank = parsedStart;
                        n = parsedEnd - parsedStart + 1;
                    }
                    else
                    {
                        int.TryParse(_params[1], out n);
                    }
                }
            }

            n = Math.Max(1, Math.Min(n, 50));
            startRank = Math.Max(1, startRank);

            var entries = new List<(string name, int kills, string title)>();

            if (showAll)
            {
                // Load all player data from disk (online and offline)
                var allPlayers = RankManager.Instance.GetAllPlayerData();
                if (allPlayers.Count == 0)
                {
                    Output(Localization.Get("cmd.rank.top.noRecords", "[TitlesSystem] No player records found."), sender);
                    return;
                }

                foreach (var data in allPlayers)
                {
                    var rank = RankManager.Instance.Ranks[data.CurrentRankIndex];
                    entries.Add((data.OriginalName, data.ZombieKills, rank.ShortTitle));
                }

                entries.Sort((a, b) => b.kills.CompareTo(a.kills));
            }
            else
            {
                // Show only online players
                var clientList = ConnectionManager.Instance?.Clients?.list;
                if (clientList == null || clientList.Count == 0)
                {
                    Output(Localization.Get("cmd.rank.top.noOnline", "[TitlesSystem] No players currently online. Use 'rank top all' to see all-time leaderboard."), sender);
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
            }

            int maxRank = entries.Count;
            if (maxRank == 0)
            {
                Output(Localization.Get("cmd.rank.top.noEntries", "[TitlesSystem] No leaderboard entries available."), sender);
                return;
            }

            int endRank = useRange ? Math.Min(startRank + n - 1, maxRank) : Math.Min(n, maxRank);
            int displayStart = useRange ? startRank : 1;

            if (displayStart > maxRank)
            {
                Output(Localization.Format("cmd.rank.top.outOfRange", "[TitlesSystem] Requested start rank #{0} is out of range (max #{1}).", displayStart, maxRank), sender);
                return;
            }

            string scope = showAll
                ? Localization.Get("cmd.rank.top.scope.all", "All-Time")
                : Localization.Get("cmd.rank.top.scope.online", "Online");
            Output(Localization.Format("cmd.rank.top.header", "[TitlesSystem] LB {0} #{1}-#{2}/{3}", scope, displayStart, endRank, maxRank), sender);

            for (int rankIndex = displayStart - 1; rankIndex < endRank; rankIndex++)
            {
                var (name, kills, title) = entries[rankIndex];
                Output(Localization.Format("cmd.rank.top.line", "#{0} {1} [{2}] {3}", rankIndex + 1, name, title, kills), sender);
            }
        }

        private static bool TryParseRange(string value, out int start, out int end)
        {
            start = 0;
            end = 0;

            if (string.IsNullOrWhiteSpace(value)) return false;

            var parts = value.Split('-');
            if (parts.Length != 2) return false;

            if (!int.TryParse(parts[0], out start)) return false;
            if (!int.TryParse(parts[1], out end)) return false;
            if (start < 1 || end < start) return false;

            // Limit max window size to existing top command max.
            if (end - start + 1 > 50) return false;

            return true;
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
                string compact = CompactForChat(message);
                if (!string.IsNullOrEmpty(compact))
                    GameApiCompat.ChatMessageToClient(sender.RemoteClientInfo, compact);
            }

            SdtdConsole.Instance.Output(message);
        }

        private static string CompactForChat(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return null;

            string compact = message.Trim();

            // Skip separator-only lines in in-game chat.
            bool onlySeparators = true;
            for (int i = 0; i < compact.Length; i++)
            {
                char c = compact[i];
                if (c != '=' && c != '-' && c != '_' && c != ' ' && c != '|')
                {
                    onlySeparators = false;
                    break;
                }
            }

            if (onlySeparators) return null;

            // Collapse excessive spaces.
            while (compact.Contains("  "))
                compact = compact.Replace("  ", " ");

            // Keep messages within the small in-game chat window.
            const int maxChatLen = 110;
            if (compact.Length > maxChatLen)
                compact = compact.Substring(0, maxChatLen - 3) + "...";

            return compact;
        }

        /// <summary>
        /// Formats seconds into human-readable playtime (e.g., "2d 5h 30m").
        /// </summary>
        private static string FormatPlaytime(long seconds)
        {
            if (seconds < 0) return Localization.Get("time.play.zero", "0m");

            long days = seconds / 86400;
            long hours = (seconds % 86400) / 3600;
            long minutes = (seconds % 3600) / 60;

            if (days > 0)
                return Localization.Format("time.play.days", "{0}d {1}h {2}m", days, hours, minutes);
            if (hours > 0)
                return Localization.Format("time.play.hours", "{0}h {1}m", hours, minutes);
            return Localization.Format("time.play.minutes", "{0}m", minutes);
        }

        /// <summary>
        /// Formats a TimeSpan into human-readable format (e.g., "2 days ago" or "5 minutes ago").
        /// </summary>
        private static string FormatTimeAgo(TimeSpan ts)
        {
            if (ts.TotalSeconds < 60)
                return Localization.Format("time.ago.seconds", "{0} seconds", (int)ts.TotalSeconds);
            if (ts.TotalMinutes < 60)
                return Localization.Format("time.ago.minutes", "{0} minute(s)", (int)ts.TotalMinutes);
            if (ts.TotalHours < 24)
                return Localization.Format("time.ago.hours", "{0} hour(s)", (int)ts.TotalHours);
            return Localization.Format("time.ago.days", "{0} day(s)", (int)ts.TotalDays);
        }
    }
}

