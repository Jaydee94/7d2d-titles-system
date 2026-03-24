using System.Collections.Generic;
using Xunit;

namespace TitlesSystem.Tests
{
    public class RankCalculatorTests
    {
        // ------------------------------------------------------------------ //
        //  Helpers
        // ------------------------------------------------------------------ //

        private static List<RankDefinition> BuildRanks()
        {
            return new List<RankDefinition>
            {
                new RankDefinition(0,      "Freshly Irradiated Civilian",  "Civilian"),
                new RankDefinition(5,      "Dumpster Diver of Doom",       "Diver"),
                new RankDefinition(10,     "Rusty Nail Enthusiast",        "Rusty"),
                new RankDefinition(20,     "Can Opener Connoisseur",       "CanOpener"),
                new RankDefinition(35,     "Tin Can Knight",               "TinCan"),
                new RankDefinition(50,     "Wandering Wastelander",        "Wanderer"),
                new RankDefinition(75,     "Dead Road Drifter",            "Drifter"),
                new RankDefinition(100,    "Scavenger of the Fallen",      "Scavenger"),
                new RankDefinition(130,    "Junkyard Prophet",             "Prophet"),
                new RankDefinition(175,    "Honorary Raider",              "Raider"),
                new RankDefinition(250,    "Vault Door Kickboxer",         "VaultKicker"),
                new RankDefinition(300,    "Bunker Buster",                "Buster"),
                new RankDefinition(375,    "Mutant Whisperer",             "Whisperer"),
                new RankDefinition(500,    "The Last Shotgun Shell",       "LastShell"),
                new RankDefinition(600,    "Bottle Cap Baron",             "Baron"),
                new RankDefinition(750,    "Duke of the Dead Lands",       "Duke"),
                new RankDefinition(1250,   "Horde Night Headliner",        "Headliner"),
                new RankDefinition(1500,   "Ghoul Trapper",                "Trapper"),
                new RankDefinition(2000,   "Ambassador of Annihilation",   "Ambassador"),
                new RankDefinition(3000,   "Warlord of the Wasteland",     "Warlord"),
                new RankDefinition(3750,   "Ironclad Wastelander",         "Ironclad"),
                new RankDefinition(4500,   "Shepherd of the Apocalypse",   "Shepherd"),
                new RankDefinition(6000,   "Post-Apocalyptic Saint",       "Saint"),
                new RankDefinition(7500,   "The Undying Ghoul Hunter",     "Undying"),
                new RankDefinition(12500,  "Harbinger of the Final Horde", "Harbinger"),
                new RankDefinition(18000,  "Nuclear Winter Survivor",      "NukeSurvivor"),
                new RankDefinition(25000,  "The Rad-Scorpion King",        "RadKing"),
                new RankDefinition(37500,  "Irradiated Overlord",          "Overlord"),
                new RankDefinition(50000,  "Chosen One of the Wasteland",  "ChosenOne"),
                new RankDefinition(100000, "Last Hope of Humanity",        "LastHope"),
            };
        }

        // ------------------------------------------------------------------ //
        //  Edge cases
        // ------------------------------------------------------------------ //

        [Fact]
        public void EmptyRankList_AlwaysReturnsZero()
        {
            var ranks = new List<RankDefinition>();
            Assert.Equal(0, RankCalculator.ComputeRankIndex(ranks, 0));
            Assert.Equal(0, RankCalculator.ComputeRankIndex(ranks, 9999));
        }

        [Fact]
        public void ZeroKills_ReturnsFirstRank()
        {
            var ranks = BuildRanks();
            Assert.Equal(0, RankCalculator.ComputeRankIndex(ranks, 0));
        }

        [Fact]
        public void NegativeKills_ReturnsFirstRank()
        {
            var ranks = BuildRanks();
            Assert.Equal(0, RankCalculator.ComputeRankIndex(ranks, -100));
        }

        [Fact]
        public void MaxKills_ReturnsLastRank()
        {
            var ranks = BuildRanks();
            int lastIndex = ranks.Count - 1;
            Assert.Equal(lastIndex, RankCalculator.ComputeRankIndex(ranks, int.MaxValue));
        }

        // ------------------------------------------------------------------ //
        //  Boundary tests
        // ------------------------------------------------------------------ //

        [Theory]
        [InlineData(0,   0)]   // Civilian  — at threshold
        [InlineData(4,   0)]   // Civilian  — one below Diver threshold
        [InlineData(5,   1)]   // Diver     — exactly at threshold
        [InlineData(9,   1)]   // Diver     — one below Rusty threshold
        [InlineData(10,  2)]   // Rusty     — exactly at threshold
        [InlineData(19,  2)]   // Rusty     — one below CanOpener threshold
        [InlineData(20,  3)]   // CanOpener — exactly at threshold
        [InlineData(34,  3)]   // CanOpener — one below TinCan threshold
        [InlineData(35,  4)]   // TinCan    — exactly at threshold
        public void LowTierBoundaries_CorrectRankIndex(int kills, int expectedIndex)
        {
            var ranks = BuildRanks();
            Assert.Equal(expectedIndex, RankCalculator.ComputeRankIndex(ranks, kills));
        }

        [Theory]
        [InlineData(99999,  28)]  // ChosenOne — one below LastHope threshold
        [InlineData(100000, 29)]  // LastHope  — exactly at threshold
        [InlineData(999999, 29)]  // LastHope  — way above top threshold
        public void TopTierBoundaries_CorrectRankIndex(int kills, int expectedIndex)
        {
            var ranks = BuildRanks();
            Assert.Equal(expectedIndex, RankCalculator.ComputeRankIndex(ranks, kills));
        }

        // ------------------------------------------------------------------ //
        //  Short-title lookup
        // ------------------------------------------------------------------ //

        [Theory]
        [InlineData(0,      "Civilian")]
        [InlineData(5,      "Diver")]
        [InlineData(175,    "Raider")]
        [InlineData(500,    "LastShell")]
        [InlineData(12500,  "Harbinger")]
        [InlineData(100000, "LastHope")]
        public void ComputedRank_HasExpectedShortTitle(int kills, string expectedShortTitle)
        {
            var ranks = BuildRanks();
            int index = RankCalculator.ComputeRankIndex(ranks, kills);
            Assert.Equal(expectedShortTitle, ranks[index].ShortTitle);
        }

        // ------------------------------------------------------------------ //
        //  Single-rank list
        // ------------------------------------------------------------------ //

        [Fact]
        public void SingleRankList_AlwaysReturnsZero()
        {
            var ranks = new List<RankDefinition>
            {
                new RankDefinition(0, "Only Rank", "Only"),
            };
            Assert.Equal(0, RankCalculator.ComputeRankIndex(ranks, 0));
            Assert.Equal(0, RankCalculator.ComputeRankIndex(ranks, 99999));
        }
    }
}
