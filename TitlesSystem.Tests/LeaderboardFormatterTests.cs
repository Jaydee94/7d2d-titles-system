using System.Collections.Generic;
using Xunit;

namespace TitlesSystem.Tests
{
    public class LeaderboardFormatterTests
    {
        [Fact]
        public void SortEntries_OrdersByKillsDescendingThenName()
        {
            var entries = new List<LeaderboardEntry>
            {
                new LeaderboardEntry("3", "Charlie", 25, "Duke"),
                new LeaderboardEntry("1", "alpha", 100, "Warlord"),
                new LeaderboardEntry("2", "Bravo", 100, "Raider"),
            };

            List<LeaderboardEntry> sorted = LeaderboardFormatter.SortEntries(entries);

            Assert.Equal("alpha", sorted[0].PlayerName);
            Assert.Equal("Bravo", sorted[1].PlayerName);
            Assert.Equal("Charlie", sorted[2].PlayerName);
        }

        [Fact]
        public void BuildCompactLine_UsesRequestedTopCount()
        {
            var entries = new List<LeaderboardEntry>
            {
                new LeaderboardEntry("1", "AlphaOne", 100, "Warlord"),
                new LeaderboardEntry("2", "BravoTwo", 90, "Raider"),
                new LeaderboardEntry("3", "CharlieThree", 80, "Duke"),
            };

            string line = LeaderboardFormatter.BuildCompactLine(entries, 2);

            Assert.Contains("Live Top 2", line);
            Assert.Contains("#1 AlphaOne[Warlord] 100", line);
            Assert.Contains("#2 BravoTwo[Raider] 90", line);
            Assert.DoesNotContain("Charlie", line);
        }

        [Fact]
        public void BuildCompactLine_TrimsLongNames()
        {
            var entries = new List<LeaderboardEntry>
            {
                new LeaderboardEntry("1", "VeryLongPlayerName", 100, "Warlord"),
            };

            string line = LeaderboardFormatter.BuildCompactLine(entries, 1);

            Assert.Contains("VeryLongPla~", line);
        }
    }
}