using System;
using System.Collections.Generic;

namespace TitlesSystem.Commands
{
    /// <summary>
    /// Console commands for the TitlesSystem mod.
    ///
    /// Usage (server console / telnet / CSMM):
    ///   rank                       — List all rank tiers
    ///   rank check [name/entityId] — Show a player's current rank
    ///   rank set &lt;name/entityId&gt; &lt;kills&gt; — Forcibly set a player's kill count (admin)
    ///   rank top [n]               — Show top-N players by kills (default 10)
    /// </summary>
    public class ConsoleCmdRank : ConsoleCmdAbstract
    {
        protected override string[] getCommands()
        {
            return new[] { "rank", "title", "ranks" };
        }

        protected override string getDescription()
        {
            return "Manage and view player ranks in the TitlesSystem. Type 'help rank' for details.";
        }

        protected override string getHelp()
        {
            return
                "Usage:\n" +
                "  rank                       — List all rank tiers and their kill thresholds\n" +
                "  rank check [name/entityId] — Show a player's current rank (defaults to self)\n" +
                "  rank set <name/entityId> <kills> — Forcibly set kill count (admin only)\n" +
                "  rank top [n]               — Show top-N players by kill count (default 10)\n";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            string sub = _params.Count > 0 ? _params[0].ToLower() : "list";

            switch (sub)
            {
                case "list":
                case "ranks":
                    CmdListRanks();
                    break;

                case "check":
                    CmdCheckRank(_params, _senderInfo);
                    break;

                case "set":
                    CmdSetKills(_params, _senderInfo);
                    break;

                case "top":
                    CmdTopPlayers(_params);
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

        private static void CmdListRanks()
        {
            var ranks = RankManager.Instance.Ranks;
            Output($"=== TitlesSystem — {ranks.Count} Ranks ===");
            for (int i = 0; i < ranks.Count; i++)
            {
                var r = ranks[i];
                string next = (i + 1 < ranks.Count)
                    ? $"next rank at {ranks[i + 1].KillsRequired} kills"
                    : "MAX RANK";
                Output($"  {i + 1,2}. [{r.ShortTitle,10}] {r.Title} — {r.KillsRequired}+ kills ({next})");
            }
        }

        private static void CmdCheckRank(List<string> _params, CommandSenderInfo sender)
        {
            ClientInfo target = null;

            if (_params.Count > 1)
            {
                string query = _params[1];
                target = FindClientByNameOrId(query);
                if (target == null)
                {
                    Output($"[TitlesSystem] Player '{query}' not found or not online.");
                    return;
                }
            }
            else
            {
                target = sender.RemoteClientInfo;
            }

            if (target == null)
            {
                Output("[TitlesSystem] No player specified. Usage: rank check <name/entityId>");
                return;
            }

            var data = RankManager.Instance.GetPlayerData(target.playerId);
            if (data == null)
            {
                Output($"[TitlesSystem] No rank data found for '{target.playerName}'. They may not have logged in yet.");
                return;
            }

            var ranks = RankManager.Instance.Ranks;
            var current = ranks[data.CurrentRankIndex];
            string nextInfo = data.CurrentRankIndex + 1 < ranks.Count
                ? $" | Next rank: [{ranks[data.CurrentRankIndex + 1].ShortTitle}] at {ranks[data.CurrentRankIndex + 1].KillsRequired} kills ({ranks[data.CurrentRankIndex + 1].KillsRequired - data.ZombieKills} more needed)"
                : " | MAX RANK ACHIEVED!";

            Output($"=== Rank for {data.OriginalName} ===");
            Output($"  Title : [{current.ShortTitle}] {current.Title}");
            Output($"  Kills : {data.ZombieKills}{nextInfo}");
            Output($"  Rank  : #{data.CurrentRankIndex + 1} of {ranks.Count}");
        }

        private static void CmdSetKills(List<string> _params, CommandSenderInfo sender)
        {
            if (!IsAdmin(sender))
            {
                Output("[TitlesSystem] Permission denied — admin only.");
                return;
            }

            if (_params.Count < 3)
            {
                Output("[TitlesSystem] Usage: rank set <name/entityId> <kills>");
                return;
            }

            ClientInfo target = FindClientByNameOrId(_params[1]);
            if (target == null)
            {
                Output($"[TitlesSystem] Player '{_params[1]}' not found or not online.");
                return;
            }

            if (!int.TryParse(_params[2], out int kills) || kills < 0)
            {
                Output("[TitlesSystem] Kill count must be a non-negative integer.");
                return;
            }

            if (!RankManager.Instance.SetPlayerKills(target.playerId, kills))
            {
                Output($"[TitlesSystem] Could not update kills — '{target.playerName}' has no rank data loaded.");
                return;
            }

            var data = RankManager.Instance.GetPlayerData(target.playerId);
            var rank = RankManager.Instance.Ranks[data.CurrentRankIndex];
            Output($"[TitlesSystem] Set {target.playerName}'s kills to {kills} \u2192 Rank: [{rank.Title}]");
        }

        private static void CmdTopPlayers(List<string> _params)
        {
            int n = 10;
            if (_params.Count > 1) int.TryParse(_params[1], out n);
            n = Math.Max(1, Math.Min(n, 50));

            // Collect all connected players who have rank data
            var clientList = ConnectionManager.Instance?.Clients?.list;
            if (clientList == null || clientList.Count == 0)
            {
                Output("[TitlesSystem] No players currently online.");
                return;
            }

            var entries = new List<(string name, int kills, string title)>();
            foreach (var client in clientList)
            {
                var data = RankManager.Instance.GetPlayerData(client.playerId);
                if (data != null)
                    entries.Add((data.OriginalName, data.ZombieKills, RankManager.Instance.Ranks[data.CurrentRankIndex].ShortTitle));
            }

            entries.Sort((a, b) => b.kills.CompareTo(a.kills));

            Output($"=== Top {Math.Min(n, entries.Count)} Players by Zombie Kills ===");
            for (int i = 0; i < Math.Min(n, entries.Count); i++)
            {
                var (name, kills, title) = entries[i];
                Output($"  {i + 1,2}. {name,-20} [{title,10}]  {kills} kills");
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
                return ConnectionManager.Instance?.Clients?.GetForEntityId(entityId);

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
            if (sender.RemoteClientInfo == null) return true;
            int level = GameManager.Instance.adminTools.GetUserPermissionLevel(sender.RemoteClientInfo.playerId);
            return level < 1000;
        }

        /// <summary>
        /// Outputs a line to the command sender (server console, Telnet, CSMM, etc.).
        /// </summary>
        private static void Output(string message)
        {
            SdtdConsole.Instance.Output(message);
        }
    }
}

