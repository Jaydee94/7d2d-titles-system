using System;
using System.Xml.Serialization;

namespace TitlesSystem
{
    /// <summary>
    /// Represents a single rank tier definition loaded from TitlesRanks.xml.
    /// </summary>
    [XmlRoot("Rank")]
    public class RankDefinition
    {
        /// <summary>Minimum zombie kills required to reach this rank.</summary>
        [XmlAttribute("kills")]
        public int KillsRequired { get; set; }

        /// <summary>Full display title used in rank-up announcements and the 'rank' command.</summary>
        [XmlAttribute("title")]
        public string Title { get; set; }

        /// <summary>Short title displayed in brackets above the player model.</summary>
        [XmlAttribute("shortTitle")]
        public string ShortTitle { get; set; }

        public RankDefinition() { }

        public RankDefinition(int killsRequired, string title, string shortTitle)
        {
            KillsRequired = killsRequired;
            Title = title;
            ShortTitle = shortTitle;
        }

        public override string ToString()
        {
            return $"[{ShortTitle}] {Title} (requires {KillsRequired} kills)";
        }
    }
}
