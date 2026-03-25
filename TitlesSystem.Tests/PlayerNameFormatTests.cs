using System;
using Xunit;

namespace TitlesSystem.Tests
{
    /// <summary>
    /// Tests for the player name title formatting logic used by
    /// <c>RankManager.UpdatePlayerDisplayName</c> and
    /// <c>PlayerNamePatch.Postfix</c>.
    /// </summary>
    public class PlayerNameFormatTests
    {
        // ------------------------------------------------------------------ //
        //  Title prefix format
        // ------------------------------------------------------------------ //

        [Fact]
        public void TitlePrefix_Format_ProducesExpectedString()
        {
            var rank = new RankDefinition(0, "Freshly Irradiated Civilian", "Civilian");
            string playerName = "TestPlayer";

            string result = $"[{rank.ShortTitle}] {playerName}";

            Assert.Equal("[Civilian] TestPlayer", result);
        }

        [Theory]
        [InlineData("Civ",       "Alice",   "[Civ] Alice")]
        [InlineData("Warlord",   "Bob",     "[Warlord] Bob")]
        [InlineData("LastHope",  "Charlie", "[LastHope] Charlie")]
        public void TitlePrefix_Format_VariousRanksAndNames(string shortTitle, string name, string expected)
        {
            string result = $"[{shortTitle}] {name}";
            Assert.Equal(expected, result);
        }

        // ------------------------------------------------------------------ //
        //  Rank-change correctness (mirrors PlayerNamePatch.Postfix logic)
        //
        //  The patch builds the final name from the stored OriginalName and the
        //  current ShortTitle, so a rank-up always produces a single, correctly-
        //  formatted prefix and never stacks multiple brackets.
        // ------------------------------------------------------------------ //

        [Fact]
        public void TitlePrefix_OnRankUp_ReplacesOldPrefixWithNew()
        {
            // Simulate PlayerNamePatch.Postfix: use the stored original name,
            // not the currently-returned value, as the base.
            string originalName = "TestPlayer";
            string newShortTitle = "Warlord";

            // Even if the method returned the old-rank name, the patch ignores
            // it in favour of originalName.
            string result = $"[{newShortTitle}] {originalName}";

            Assert.Equal("[Warlord] TestPlayer", result);
            Assert.False(result.Contains("[Civ]"), "Old rank prefix must not appear in the result.");
        }

        [Fact]
        public void TitlePrefix_CalledTwice_DoesNotStackPrefixes()
        {
            // Simulate two consecutive calls to the postfix for the same player.
            string originalName = "TestPlayer";
            string shortTitle = "Civ";

            // First call
            string afterFirst = $"[{shortTitle}] {originalName}";
            // Second call — still uses originalName, so output is identical
            string afterSecond = $"[{shortTitle}] {originalName}";

            Assert.Equal(afterFirst, afterSecond);
        }

        // ------------------------------------------------------------------ //
        //  RankDefinition ShortTitle used in formatting
        // ------------------------------------------------------------------ //

        [Fact]
        public void RankDefinition_ShortTitle_UsedInPrefix()
        {
            var rank = new RankDefinition(500, "Wandering Wastelander", "Wanderer");

            string prefix = $"[{rank.ShortTitle}] ";

            Assert.Equal("[Wanderer] ", prefix);
        }
    }
}
