using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace TitlesSystem
{
    /// <summary>
    /// Core singleton that manages rank definitions, player rank data, persistence,
    /// kill tracking, and player name updates.
    /// </summary>
    public class RankManager
    {
        // ------------------------------------------------------------------ //
        //  Singleton
        // ------------------------------------------------------------------ //

        private static RankManager _instance;
        public static RankManager Instance => _instance ?? (_instance = new RankManager());

        private RankManager() { }

        // ------------------------------------------------------------------ //
        //  Fields
        // ------------------------------------------------------------------ //

        private List<RankDefinition> _ranks = new List<RankDefinition>();

        /// <summary>Keyed by platform player ID (SteamID).</summary>
        private Dictionary<string, PlayerRankData> _playerData =
            new Dictionary<string, PlayerRankData>(StringComparer.OrdinalIgnoreCase);

        private string _dataPath;
        private bool _showRankInName = true;
        private bool _announceRankUp = true;
        private bool _showLeaderboardOnLogin = true;
        private int _showLeaderboardIntervalHours = 6;
        private int _leaderboardTopPlayers = 10;
        private DateTime _lastLeaderboardTime = DateTime.MinValue;

        private static readonly XmlSerializer PlayerDataSerializer =
            new XmlSerializer(typeof(PlayerRankData));

        // ------------------------------------------------------------------ //
        //  Initialization
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Called from TitlesSystemMod.InitMod. Loads rank config.
        /// </summary>
        public void Initialize(string modConfigPath)
        {
            LoadRankConfig(modConfigPath);
            Log.Out($"[TitlesSystem] RankManager initialized with {_ranks.Count} ranks.");
        }

        /// <summary>
        /// Called when the game world is fully started and the save path is available.
        /// </summary>
        public void OnGameStartDone()
        {
            try
            {
                string saveDir = GameApiCompat.GetSaveGameDir();
                if (string.IsNullOrEmpty(saveDir))
                {
                    throw new InvalidOperationException("Save game directory could not be resolved.");
                }

                _dataPath = Path.Combine(saveDir, "TitlesSystem");
                Directory.CreateDirectory(_dataPath);
                Log.Out($"[TitlesSystem] Data directory: {_dataPath}");
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] Failed to set up data path: {e.Message}");
            }
        }

        // ------------------------------------------------------------------ //
        //  Rank Config Loading
        // ------------------------------------------------------------------ //

        private void LoadRankConfig(string modConfigPath)
        {
            string configFile = Path.Combine(modConfigPath, "Config", "TitlesRanks.xml");

            if (File.Exists(configFile))
            {
                try
                {
                    LoadRankConfigFromXml(configFile);
                    Log.Out($"[TitlesSystem] Loaded {_ranks.Count} ranks from {configFile}");
                    return;
                }
                catch (Exception e)
                {
                    Log.Warning($"[TitlesSystem] Could not parse {configFile}: {e.Message}. Falling back to defaults.");
                }
            }
            else
            {
                Log.Warning($"[TitlesSystem] Config file not found at {configFile}. Using built-in defaults.");
            }

            LoadDefaultRanks();
        }

        private void LoadRankConfigFromXml(string filePath)
        {
            var doc = new XmlDocument();
            doc.Load(filePath);

            // Read settings
            XmlNode showNode = doc.SelectSingleNode("/TitlesConfig/Settings/ShowRankInName");
            if (showNode?.Attributes?["value"] != null)
                bool.TryParse(showNode.Attributes["value"].Value, out _showRankInName);

            XmlNode announceNode = doc.SelectSingleNode("/TitlesConfig/Settings/AnnounceRankUp");
            if (announceNode?.Attributes?["value"] != null)
                bool.TryParse(announceNode.Attributes["value"].Value, out _announceRankUp);

            XmlNode leaderboardLoginNode = doc.SelectSingleNode("/TitlesConfig/Settings/ShowLeaderboardOnLogin");
            if (leaderboardLoginNode?.Attributes?["value"] != null)
                bool.TryParse(leaderboardLoginNode.Attributes["value"].Value, out _showLeaderboardOnLogin);

            XmlNode leaderboardIntervalNode = doc.SelectSingleNode("/TitlesConfig/Settings/ShowLeaderboardIntervalHours");
            if (leaderboardIntervalNode?.Attributes?["value"] != null)
                int.TryParse(leaderboardIntervalNode.Attributes["value"].Value, out _showLeaderboardIntervalHours);

            XmlNode leaderboardTopNode = doc.SelectSingleNode("/TitlesConfig/Settings/LeaderboardTopPlayers");
            if (leaderboardTopNode?.Attributes?["value"] != null)
                int.TryParse(leaderboardTopNode.Attributes["value"].Value, out _leaderboardTopPlayers);

            // Read ranks
            _ranks.Clear();
            XmlNodeList rankNodes = doc.SelectNodes("/TitlesConfig/Ranks/Rank");
            if (rankNodes == null) return;

            foreach (XmlNode node in rankNodes)
            {
                if (node.Attributes == null) continue;

                int kills = 0;
                int.TryParse(node.Attributes["kills"]?.Value, out kills);
                string title = node.Attributes["title"]?.Value ?? "Unknown";
                string shortTitle = node.Attributes["shortTitle"]?.Value ?? "???";

                _ranks.Add(new RankDefinition(kills, title, shortTitle));
            }

            // Ensure sorted ascending by kill count
            _ranks.Sort((a, b) => a.KillsRequired.CompareTo(b.KillsRequired));
        }

        private void LoadDefaultRanks()
        {
            _ranks = new List<RankDefinition>
            {
                new RankDefinition(0,      "Freshly Irradiated Civilian",   "Civilian"),
                new RankDefinition(10,     "Dumpster Diver of Doom",        "Diver"),
                new RankDefinition(25,     "Rusty Nail Enthusiast",         "Rusty"),
                new RankDefinition(50,     "Can Opener Connoisseur",        "CanOpener"),
                new RankDefinition(75,     "Tin Can Knight",                "TinCan"),
                new RankDefinition(100,    "Wandering Wastelander",         "Wanderer"),
                new RankDefinition(150,    "Dead Road Drifter",             "Drifter"),
                new RankDefinition(200,    "Scavenger of the Fallen",       "Scavenger"),
                new RankDefinition(275,    "Junkyard Prophet",              "Prophet"),
                new RankDefinition(350,    "Honorary Raider",               "Raider"),
                new RankDefinition(500,    "Vault Door Kickboxer",          "VaultKicker"),
                new RankDefinition(625,    "Bunker Buster",                 "Buster"),
                new RankDefinition(750,    "Mutant Whisperer",              "Whisperer"),
                new RankDefinition(1000,   "The Last Shotgun Shell",        "LastShell"),
                new RankDefinition(1250,   "Bottle Cap Baron",              "Baron"),
                new RankDefinition(1500,   "Duke of the Dead Lands",        "Duke"),
                new RankDefinition(2500,   "Horde Night Headliner",         "Headliner"),
                new RankDefinition(3250,   "Ghoul Trapper",                 "Trapper"),
                new RankDefinition(4000,   "Ambassador of Annihilation",    "Ambassador"),
                new RankDefinition(6000,   "Warlord of the Wasteland",      "Warlord"),
                new RankDefinition(7500,   "Ironclad Wastelander",          "Ironclad"),
                new RankDefinition(9000,   "Shepherd of the Apocalypse",    "Shepherd"),
                new RankDefinition(12000,  "Post-Apocalyptic Saint",        "Saint"),
                new RankDefinition(15000,  "The Undying Ghoul Hunter",      "Undying"),
                new RankDefinition(25000,  "Harbinger of the Final Horde",  "Harbinger"),
                new RankDefinition(37500,  "Nuclear Winter Survivor",       "NukeSurvivor"),
                new RankDefinition(50000,  "The Rad-Scorpion King",         "RadKing"),
                new RankDefinition(75000,  "Irradiated Overlord",           "Overlord"),
                new RankDefinition(100000, "Chosen One of the Wasteland",   "ChosenOne"),
                new RankDefinition(200000, "Last Hope of Humanity",         "LastHope"),
            };
        }

        // ------------------------------------------------------------------ //
        //  Public Accessors
        // ------------------------------------------------------------------ //

        /// <summary>Returns a read-only view of all rank definitions, ordered by kill count.</summary>
        public IReadOnlyList<RankDefinition> Ranks => _ranks.AsReadOnly();

        /// <summary>
        /// Returns the current rank data for the given playerId, or null if not loaded.
        /// </summary>
        public PlayerRankData GetPlayerData(string playerId)
        {
            _playerData.TryGetValue(playerId, out var data);
            return data;
        }

        // ------------------------------------------------------------------ //
        //  Event Handlers (called from TitlesSystemMod)
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Called when a player spawns into the world.
        /// Loads their data if not in memory, then updates their displayed name.
        /// </summary>
        public void OnPlayerSpawned(ClientInfo clientInfo)
        {
            if (clientInfo == null) return;

            string playerId = GameApiCompat.GetPlayerId(clientInfo);
            if (string.IsNullOrEmpty(playerId)) return;

            if (!_playerData.TryGetValue(playerId, out var data))
            {
                // Try to load from disk first
                data = LoadPlayerData(playerId);

                if (data == null)
                {
                    // First time this player has connected
                    string originalName = GetOriginalName(clientInfo);
                    data = new PlayerRankData(playerId, originalName);
                    Log.Out($"[TitlesSystem] New player '{originalName}' registered.");
                }

                _playerData[playerId] = data;
            }

            // Mark session start
            data.SessionJoinTime = DateTime.UtcNow.ToString("o");
            data.Touch();
            UpdatePlayerDisplayName(GameApiCompat.GetEntityId(clientInfo), data);

            Log.Out($"[TitlesSystem] '{data.OriginalName}' spawned with rank [{_ranks[data.CurrentRankIndex].ShortTitle}] ({data.ZombieKills} kills).");

            // Show leaderboard on login if enabled
            if (_showLeaderboardOnLogin)
            {
                BroadcastLeaderboard();
            }
            // Or show if interval timer is ready
            else if (_showLeaderboardIntervalHours > 0 && 
                     DateTime.UtcNow.Subtract(_lastLeaderboardTime).TotalHours >= _showLeaderboardIntervalHours)
            {
                BroadcastLeaderboard();
            }
        }

        /// <summary>
        /// Called when a player disconnects.
        /// Saves their data and optionally removes them from the in-memory cache.
        /// </summary>
        public void OnPlayerDisconnected(ClientInfo clientInfo)
        {
            if (clientInfo == null) return;

            string playerId = GameApiCompat.GetPlayerId(clientInfo);
            if (string.IsNullOrEmpty(playerId)) return;

            if (_playerData.TryGetValue(playerId, out var data))
            {
                // Add session playtime
                if (!string.IsNullOrEmpty(data.SessionJoinTime) &&
                    DateTime.TryParse(data.SessionJoinTime, out var sessionStart))
                {
                    long sessionSeconds = (long)(DateTime.UtcNow - sessionStart).TotalSeconds;
                    data.AddSessionTime(sessionSeconds);
                }

                data.Touch();
                SavePlayerData(data);
                Log.Out($"[TitlesSystem] Saved rank data for '{data.OriginalName}'.");
            }
        }

        /// <summary>
        /// Called when a zombie is killed by a player. Increments the kill count
        /// and triggers a rank-up if the new kill count meets the next threshold.
        /// </summary>
        public void OnZombieKilled(int killerEntityId, string killerPlayerId, string weaponId = "unknown")
        {
            if (!_playerData.TryGetValue(killerPlayerId, out var data)) return;

            int oldKills = data.ZombieKills;
            data.RecordKill(weaponId);

            int newRankIndex = ComputeRankIndex(data.ZombieKills);

            if (newRankIndex > data.CurrentRankIndex)
            {
                data.CurrentRankIndex = newRankIndex;
                var newRank = _ranks[newRankIndex];

                Log.Out($"[TitlesSystem] '{data.OriginalName}' ranked up to [{newRank.Title}] with {data.ZombieKills} kills!");

                if (_announceRankUp)
                {
                    string message = $"[TitlesSystem] {data.OriginalName} has been promoted to [{newRank.Title}]! ({data.ZombieKills} zombies slain)";
                    GameApiCompat.ChatMessageGlobal(message);
                }
            }

            UpdatePlayerDisplayName(killerEntityId, data);
        }

        /// <summary>
        /// Called when a player dies. Tracks death count and resets survival streak.
        /// </summary>
        public void OnPlayerDied(string playerId)
        {
            if (!_playerData.TryGetValue(playerId, out var data)) return;
            data.RecordDeath();
        }

        /// <summary>
        /// Saves all currently loaded player data to disk (e.g. on server shutdown).
        /// </summary>
            /// <summary>
            /// Broadcasts the top player leaderboard to all connected players.
            /// </summary>
            public void BroadcastLeaderboard()
            {
                try
                {
                    _lastLeaderboardTime = DateTime.UtcNow;

                    var allPlayerData = GetAllPlayerData();
                    if (allPlayerData.Count == 0)
                    {
                        return;
                    }

                    // Sort by zombie kills descending
                    var topPlayers = allPlayerData
                        .OrderByDescending(p => p.ZombieKills)
                        .Take(_leaderboardTopPlayers)
                        .ToList();

                    // Build compact one-line header for small chat windows.
                    List<string> leaderboardLines = new List<string>
                    {
                        $"[TitlesSystem] Leaderboard Top {topPlayers.Count}"
                    };

                    // Add player lines
                    int position = 1;
                    foreach (var player in topPlayers)
                    {
                        string rank =
                            player.CurrentRankIndex >= 0 && player.CurrentRankIndex < _ranks.Count
                                ? _ranks[player.CurrentRankIndex].ShortTitle
                                : "Unknown";
                        string line = $"#{position} {player.OriginalName} [{rank}] {player.ZombieKills}";
                        leaderboardLines.Add(line);
                        position++;
                    }

                    // Broadcast each line to all players
                    foreach (var line in leaderboardLines)
                    {
                        GameApiCompat.ChatMessageGlobal(line);
                    }

                    Log.Out($"[TitlesSystem] Leaderboard broadcast to all players.");
                }
                catch (Exception ex)
                {
                    Log.Error($"[TitlesSystem] Error broadcasting leaderboard: {ex.Message}");
                }
            }

        public void SaveAllPlayerData()
        {
            foreach (var data in _playerData.Values)
            {
                SavePlayerData(data);
            }
            Log.Out($"[TitlesSystem] Saved data for {_playerData.Count} players.");
        }

        // ------------------------------------------------------------------ //
        //  Admin / Command Helpers
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Forcibly set a player's zombie kill count and recalculate their rank.
        /// Used by the admin 'rank set' console command.
        /// </summary>
        public bool SetPlayerKills(string playerId, int kills)
        {
            if (!_playerData.TryGetValue(playerId, out var data)) return false;

            int oldRankIndex = data.CurrentRankIndex;
            data.ZombieKills = Math.Max(0, kills);
            data.CurrentRankIndex = ComputeRankIndex(data.ZombieKills);

            if (data.CurrentRankIndex > oldRankIndex)
            {
                var newRank = _ranks[data.CurrentRankIndex];
                Log.Out($"[TitlesSystem] '{data.OriginalName}' ranked up to [{newRank.Title}] via admin set with {data.ZombieKills} kills.");

                if (_announceRankUp)
                {
                    string message = $"[TitlesSystem] {data.OriginalName} has been promoted to [{newRank.Title}]! ({data.ZombieKills} zombies slain)";
                    GameApiCompat.ChatMessageGlobal(message);
                }
            }

            ClientInfo clientInfo = GetClientInfo(playerId);
            if (clientInfo != null)
                UpdatePlayerDisplayName(GameApiCompat.GetEntityId(clientInfo), data);

            SavePlayerData(data);
            return true;
        }

        // ------------------------------------------------------------------ //
        //  Private Helpers
        // ------------------------------------------------------------------ //

        private int ComputeRankIndex(int kills) =>
            RankCalculator.ComputeRankIndex(_ranks, kills);

        /// <summary>
        /// Modifies the EntityPlayer's entityName to "[ShortTitle] OriginalName"
        /// and flags the entity dirty so the change is synced to all clients.
        /// </summary>
        private void UpdatePlayerDisplayName(int entityId, PlayerRankData data)
        {
            if (!_showRankInName) return;
            if (entityId < 0) return;

            try
            {
                World world = GameManager.Instance?.World;
                if (world == null) return;

                EntityPlayer player = world.Players.dict.TryGetValue(entityId, out var p) ? p : null;
                if (player == null) return;

                string rank = _ranks[data.CurrentRankIndex].ShortTitle;
                string newName = $"[{rank}] {data.OriginalName}";

                if (player.entityName != newName)
                {
                    player.entityName = newName;
                    GameApiCompat.MarkPlayerNameDirty(player);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] UpdatePlayerDisplayName error: {e.Message}");
            }
        }

        private string GetOriginalName(ClientInfo clientInfo)
        {
            // clientInfo.playerName is the authoritative, unmodified platform name.
            // Prefer it over entityName, which may already carry a title prefix
            // if the player previously connected in the same session, causing
            // stacked prefixes like "[NewRank] [OldRank] PlayerName" on rank-up.
            if (!string.IsNullOrEmpty(clientInfo.playerName))
                return clientInfo.playerName;

            try
            {
                World world = GameManager.Instance?.World;
                if (world != null)
                {
                    int entityId = GameApiCompat.GetEntityId(clientInfo);
                    if (entityId >= 0)
                    {
                        EntityPlayer player = world.Players.dict.TryGetValue(entityId, out var p) ? p : null;
                        if (player != null && !string.IsNullOrEmpty(player.entityName))
                            return player.entityName;
                    }
                }
            }
            catch { /* fall through */ }

            return "Unknown";
        }

        private static ClientInfo GetClientInfo(string playerId)
        {
            try
            {
                return GameApiCompat.GetClientInfoByPlayerId(playerId);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Find a player by name from all known players (online or offline).
        /// Returns the player's ID if found, or null if not found.
        /// </summary>
        public string FindPlayerIdByName(string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return null;

            // First try online players
            try
            {
                var clientList = ConnectionManager.Instance?.Clients?.list;
                if (clientList != null)
                {
                    // Exact match
                    var client = clientList.Find(c =>
                        string.Equals(c.playerName, playerName, StringComparison.OrdinalIgnoreCase));
                    if (client != null)
                        return GameApiCompat.GetPlayerId(client);

                    // Partial match
                    client = clientList.Find(c =>
                        c.playerName.IndexOf(playerName, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (client != null)
                        return GameApiCompat.GetPlayerId(client);
                }
            }
            catch { /* fall through to offline check */ }

            // Then try offline players from disk
            try
            {
                var allPlayers = GetAllPlayerData();
                
                // Exact match
                var player = allPlayers.Find(p =>
                    string.Equals(p.OriginalName, playerName, StringComparison.OrdinalIgnoreCase));
                if (player != null)
                    return player.PlayerId;

                // Partial match
                player = allPlayers.Find(p =>
                    p.OriginalName.IndexOf(playerName, StringComparison.OrdinalIgnoreCase) >= 0);
                if (player != null)
                    return player.PlayerId;
            }
            catch { /* return null below */ }

            return null;
        }

        /// <summary>
        /// Find a player's original name by ID (online or offline).
        /// </summary>
        public string FindPlayerNameById(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return null;

            // Check online first
            try
            {
                var clientInfo = GameApiCompat.GetClientInfoByPlayerId(playerId);
                if (clientInfo != null)
                    return clientInfo.playerName;
            }
            catch { /* fall through to offline check */ }

            // Check offline
            try
            {
                var data = LoadPlayerData(playerId);
                if (data != null)
                    return data.OriginalName;
            }
            catch { /* return null below */ }

            return null;
        }

        // ------------------------------------------------------------------ //
        //  Data Persistence
        // ------------------------------------------------------------------ //

        private void SavePlayerData(PlayerRankData data)
        {
            if (_dataPath == null || data?.PlayerId == null) return;

            try
            {
                string filePath = Path.Combine(_dataPath, $"{SanitizeFileName(data.PlayerId)}.xml");
                using (var writer = new StreamWriter(filePath, append: false, encoding: System.Text.Encoding.UTF8))
                {
                    PlayerDataSerializer.Serialize(writer, data);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] Failed to save data for player {data.PlayerId}: {e.Message}");
            }
        }

        /// <summary>
        /// Returns all player data from disk, including offline players.
        /// </summary>
        public List<PlayerRankData> GetAllPlayerData()
        {
            var allPlayers = new List<PlayerRankData>();

            if (_dataPath == null || !Directory.Exists(_dataPath))
                return allPlayers;

            try
            {
                var xmlFiles = Directory.GetFiles(_dataPath, "*.xml");
                foreach (var file in xmlFiles)
                {
                    try
                    {
                        using (var reader = new StreamReader(file, System.Text.Encoding.UTF8))
                        {
                            var data = (PlayerRankData)PlayerDataSerializer.Deserialize(reader);
                            if (data != null)
                                allPlayers.Add(data);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"[TitlesSystem] Failed to load player data from {Path.GetFileName(file)}: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] Failed to read player data directory: {e.Message}");
            }

            return allPlayers;
        }

        private PlayerRankData LoadPlayerData(string playerId)
        {
            if (_dataPath == null) return null;

            string filePath = Path.Combine(_dataPath, $"{SanitizeFileName(playerId)}.xml");
            if (!File.Exists(filePath)) return null;

            try
            {
                using (var reader = new StreamReader(filePath, System.Text.Encoding.UTF8))
                {
                    return (PlayerRankData)PlayerDataSerializer.Deserialize(reader);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] Failed to load data for player {playerId}: {e.Message}");
                return null;
            }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
