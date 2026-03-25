using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace TitlesSystem
{
    /// <summary>
    /// Stores per-player ranking data that is persisted to disk.
    /// Serialized as XML using the player's SteamID as the filename.
    /// Includes detailed statistics for kills, deaths, and playtime tracking.
    /// </summary>
    [XmlRoot("PlayerRankData")]
    public class PlayerRankData
    {
        /// <summary>Platform player ID (SteamID). Used as the unique key.</summary>
        [XmlAttribute("playerId")]
        public string PlayerId { get; set; }

        /// <summary>
        /// The player's original display name captured on first login.
        /// Stored so the rank prefix can be prepended without losing the base name.
        /// </summary>
        [XmlElement("OriginalName")]
        public string OriginalName { get; set; }

        /// <summary>Total number of zombie (non-player) kills accumulated on this server.</summary>
        [XmlElement("ZombieKills")]
        public int ZombieKills { get; set; }

        /// <summary>Index into the RankManager rank list representing the current rank.</summary>
        [XmlElement("CurrentRankIndex")]
        public int CurrentRankIndex { get; set; }

        /// <summary>ISO-8601 UTC timestamp of the player's last activity.</summary>
        [XmlElement("LastSeen")]
        public string LastSeen { get; set; }

        /// <summary>ISO-8601 UTC timestamp of the player's first login (for playtime calculation).</summary>
        [XmlElement("FirstSeen")]
        public string FirstSeen { get; set; }

        /// <summary>ISO-8601 UTC timestamp of the player's last recorded kill.</summary>
        [XmlElement("LastKillTime")]
        public string LastKillTime { get; set; }

        /// <summary>Total number of player deaths.</summary>
        [XmlElement("Deaths")]
        public int Deaths { get; set; }

        /// <summary>Current survival streak (kills since last death).</summary>
        [XmlElement("CurrentStreak")]
        public int CurrentStreak { get; set; }

        /// <summary>Best survival streak (kills without dying).</summary>
        [XmlElement("BestStreak")]
        public int BestStreak { get; set; }

        /// <summary>Cumulative seconds spent in the world while alive.</summary>
        [XmlElement("PlaytimeSeconds")]
        public long PlaytimeSeconds { get; set; }

        /// <summary>ISO-8601 timestamp when the player last joined (for session duration).</summary>
        [XmlElement("SessionJoinTime")]
        public string SessionJoinTime { get; set; }

        /// <summary>Kill count by weapon ID (e.g., "rifle" -> 45 kills).</summary>
        [XmlArray("WeaponKills")]
        [XmlArrayItem("Weapon")]
        public List<WeaponKillData> WeaponKills { get; set; } = new List<WeaponKillData>();

        /// <summary>Parameterless constructor required for XML serialization.</summary>
        public PlayerRankData() { }

        public PlayerRankData(string playerId, string originalName)
        {
            PlayerId = playerId;
            OriginalName = originalName;
            ZombieKills = 0;
            CurrentRankIndex = 0;
            Deaths = 0;
            CurrentStreak = 0;
            BestStreak = 0;
            PlaytimeSeconds = 0;
            LastSeen = DateTime.UtcNow.ToString("o");
            FirstSeen = DateTime.UtcNow.ToString("o");
            LastKillTime = null;
            SessionJoinTime = DateTime.UtcNow.ToString("o");
        }

        /// <summary>Update the LastSeen timestamp to right now.</summary>
        public void Touch()
        {
            LastSeen = DateTime.UtcNow.ToString("o");
        }

        /// <summary>Record a kill with weapon info and update timestamps.</summary>
        public void RecordKill(string weaponId)
        {
            ZombieKills++;
            LastKillTime = DateTime.UtcNow.ToString("o");
            CurrentStreak++;

            // Update best streak
            if (CurrentStreak > BestStreak)
                BestStreak = CurrentStreak;

            // Track weapon stats
            var weaponData = WeaponKills.Find(w => w.WeaponId == weaponId);
            if (weaponData != null)
                weaponData.Kills++;
            else
                WeaponKills.Add(new WeaponKillData { WeaponId = weaponId, Kills = 1 });

            Touch();
        }

        /// <summary>Record a player death and reset the current streak.</summary>
        public void RecordDeath()
        {
            Deaths++;
            CurrentStreak = 0;
            Touch();
        }

        /// <summary>Add session playtime (delta since last session ping).</summary>
        public void AddSessionTime(long seconds)
        {
            PlaytimeSeconds += Math.Max(0, seconds);
        }

        /// <summary>Calculate K/D ratio (deaths > 0 to avoid div by zero).</summary>
        public double GetKDRatio()
        {
            return Deaths > 0 ? (double)ZombieKills / Deaths : ZombieKills;
        }

        /// <summary>Calculate average kills per day (based on total playtime).</summary>
        public double GetKillsPerDay()
        {
            if (PlaytimeSeconds <= 0) return 0;
            double days = PlaytimeSeconds / 86400.0; // 86400 seconds per day
            return days > 0 ? ZombieKills / days : 0;
        }

        /// <summary>Calculate average kills per hour (based on total playtime).</summary>
        public double GetKillsPerHour()
        {
            if (PlaytimeSeconds <= 0) return 0;
            double hours = PlaytimeSeconds / 3600.0; // 3600 seconds per hour
            return hours > 0 ? ZombieKills / hours : 0;
        }
    }

    /// <summary>
    /// Stores weapon-specific kill statistics.
    /// </summary>
    public class WeaponKillData
    {
        [XmlAttribute("weaponId")]
        public string WeaponId { get; set; }

        [XmlAttribute("kills")]
        public int Kills { get; set; }
    }
}
