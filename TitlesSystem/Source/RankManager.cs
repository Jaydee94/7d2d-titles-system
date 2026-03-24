using System;
using System.Collections.Generic;
using System.IO;
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
                string saveDir = GamePrefs.GetString(EnumGamePrefs.SaveGameFolder);
                string gameName = GamePrefs.GetString(EnumGamePrefs.GameName);
                _dataPath = Path.Combine(saveDir, gameName, "TitlesSystem");
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
                new RankDefinition(0,      "Freshly Irradiated Civilian",  "Civilian"),
                new RankDefinition(10,     "Dumpster Diver of Doom",       "Diver"),
                new RankDefinition(25,     "Rusty Nail Enthusiast",        "Rusty"),
                new RankDefinition(50,     "Can Opener Connoisseur",       "CanOpener"),
                new RankDefinition(100,    "Wandering Wastelander",        "Wanderer"),
                new RankDefinition(200,    "Scavenger of the Fallen",      "Scavenger"),
                new RankDefinition(350,    "Honorary Raider",              "Raider"),
                new RankDefinition(500,    "Vault Door Kickboxer",         "VaultKicker"),
                new RankDefinition(750,    "Mutant Whisperer",             "Whisperer"),
                new RankDefinition(1000,   "The Last Shotgun Shell",       "LastShell"),
                new RankDefinition(1500,   "Duke of the Dead Lands",       "Duke"),
                new RankDefinition(2500,   "Horde Night Headliner",        "Headliner"),
                new RankDefinition(4000,   "Ambassador of Annihilation",   "Ambassador"),
                new RankDefinition(6000,   "Warlord of the Wasteland",     "Warlord"),
                new RankDefinition(9000,   "Shepherd of the Apocalypse",   "Shepherd"),
                new RankDefinition(15000,  "The Undying Ghoul Hunter",     "Undying"),
                new RankDefinition(25000,  "Harbinger of the Final Horde", "Harbinger"),
                new RankDefinition(50000,  "The Rad-Scorpion King",        "RadKing"),
                new RankDefinition(100000, "Chosen One of the Wasteland",  "ChosenOne"),
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

            if (!_playerData.TryGetValue(clientInfo.playerId, out var data))
            {
                // Try to load from disk first
                data = LoadPlayerData(clientInfo.playerId);

                if (data == null)
                {
                    // First time this player has connected
                    string originalName = GetOriginalName(clientInfo);
                    data = new PlayerRankData(clientInfo.playerId, originalName);
                    Log.Out($"[TitlesSystem] New player '{originalName}' registered.");
                }

                _playerData[clientInfo.playerId] = data;
            }

            data.Touch();
            UpdatePlayerDisplayName(clientInfo.entityId, data);

            Log.Out($"[TitlesSystem] '{data.OriginalName}' spawned with rank [{_ranks[data.CurrentRankIndex].ShortTitle}] ({data.ZombieKills} kills).");
        }

        /// <summary>
        /// Called when a player disconnects.
        /// Saves their data and optionally removes them from the in-memory cache.
        /// </summary>
        public void OnPlayerDisconnected(ClientInfo clientInfo)
        {
            if (clientInfo == null) return;

            if (_playerData.TryGetValue(clientInfo.playerId, out var data))
            {
                data.Touch();
                SavePlayerData(data);
                Log.Out($"[TitlesSystem] Saved rank data for '{data.OriginalName}'.");
            }
        }

        /// <summary>
        /// Called when a zombie is killed by a player. Increments the kill count
        /// and triggers a rank-up if the new kill count meets the next threshold.
        /// </summary>
        public void OnZombieKilled(int killerEntityId, string killerPlayerId)
        {
            if (!_playerData.TryGetValue(killerPlayerId, out var data)) return;

            data.ZombieKills++;
            int newRankIndex = ComputeRankIndex(data.ZombieKills);

            if (newRankIndex > data.CurrentRankIndex)
            {
                data.CurrentRankIndex = newRankIndex;
                var newRank = _ranks[newRankIndex];

                Log.Out($"[TitlesSystem] '{data.OriginalName}' ranked up to [{newRank.Title}] with {data.ZombieKills} kills!");

                if (_announceRankUp)
                {
                    string message = $"[TitlesSystem] {data.OriginalName} has been promoted to [{newRank.Title}]! ({data.ZombieKills} zombies slain)";
                    GameManager.Instance.ChatMessageServer(
                        null,
                        EChatType.Global,
                        -1,
                        message,
                        "TitlesSystem",
                        false,
                        null);
                }
            }

            UpdatePlayerDisplayName(killerEntityId, data);
        }

        /// <summary>
        /// Saves all currently loaded player data to disk (e.g. on server shutdown).
        /// </summary>
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

            data.ZombieKills = Math.Max(0, kills);
            data.CurrentRankIndex = ComputeRankIndex(data.ZombieKills);

            ClientInfo clientInfo = GetClientInfo(playerId);
            if (clientInfo != null)
                UpdatePlayerDisplayName(clientInfo.entityId, data);

            SavePlayerData(data);
            return true;
        }

        // ------------------------------------------------------------------ //
        //  Private Helpers
        // ------------------------------------------------------------------ //

        private int ComputeRankIndex(int kills)
        {
            int index = 0;
            for (int i = 0; i < _ranks.Count; i++)
            {
                if (kills >= _ranks[i].KillsRequired)
                    index = i;
                else
                    break;
            }
            return index;
        }

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
                    player.bPlayerDirty = true;
                }
            }
            catch (Exception e)
            {
                Log.Error($"[TitlesSystem] UpdatePlayerDisplayName error: {e.Message}");
            }
        }

        private string GetOriginalName(ClientInfo clientInfo)
        {
            try
            {
                World world = GameManager.Instance?.World;
                if (world != null)
                {
                    EntityPlayer player = world.Players.dict.TryGetValue(clientInfo.entityId, out var p) ? p : null;
                    if (player != null && !string.IsNullOrEmpty(player.entityName))
                        return player.entityName;
                }
            }
            catch { /* fall through to clientInfo fallback */ }

            return !string.IsNullOrEmpty(clientInfo.playerName) ? clientInfo.playerName : "Unknown";
        }

        private static ClientInfo GetClientInfo(string playerId)
        {
            try
            {
                return ConnectionManager.Instance?.Clients?.GetForPlayerId(playerId);
            }
            catch
            {
                return null;
            }
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
