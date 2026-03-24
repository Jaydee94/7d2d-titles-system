using Xunit;

namespace TitlesSystem.Tests
{
    public class RankDefinitionTests
    {
        [Fact]
        public void Constructor_SetsAllProperties()
        {
            var rank = new RankDefinition(500, "The Last Shotgun Shell", "LastShell");

            Assert.Equal(500, rank.KillsRequired);
            Assert.Equal("The Last Shotgun Shell", rank.Title);
            Assert.Equal("LastShell", rank.ShortTitle);
        }

        [Fact]
        public void DefaultConstructor_LeavesPropertiesAtDefault()
        {
            var rank = new RankDefinition();

            Assert.Equal(0, rank.KillsRequired);
            Assert.Null(rank.Title);
            Assert.Null(rank.ShortTitle);
        }

        [Fact]
        public void ToString_ContainsShortTitle()
        {
            var rank = new RankDefinition(100, "Scavenger of the Fallen", "Scavenger");
            Assert.Contains("Scavenger", rank.ToString());
        }

        [Fact]
        public void ToString_ContainsKillCount()
        {
            var rank = new RankDefinition(12500, "Harbinger of the Final Horde", "Harbinger");
            Assert.Contains("12500", rank.ToString());
        }

        [Fact]
        public void ToString_ContainsFullTitle()
        {
            var rank = new RankDefinition(100000, "Last Hope of Humanity", "LastHope");
            Assert.Contains("Last Hope of Humanity", rank.ToString());
        }
    }
}
