using System;
using System.Xml.Serialization;

namespace TitlesSystem
{
    /// <summary>
    /// Stores per-player ranking data that is persisted to disk.
    /// Serialized as XML using the player's SteamID as the filename.
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

        /// <summary>Parameterless constructor required for XML serialization.</summary>
        public PlayerRankData() { }

        public PlayerRankData(string playerId, string originalName)
        {
            PlayerId = playerId;
            OriginalName = originalName;
            ZombieKills = 0;
            CurrentRankIndex = 0;
            LastSeen = DateTime.UtcNow.ToString("o");
        }

        /// <summary>Update the LastSeen timestamp to right now.</summary>
        public void Touch()
        {
            LastSeen = DateTime.UtcNow.ToString("o");
        }
    }
}
