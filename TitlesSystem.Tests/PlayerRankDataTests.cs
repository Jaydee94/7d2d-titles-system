using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Xunit;

namespace TitlesSystem.Tests
{
    public class PlayerRankDataTests
    {
        private static readonly XmlSerializer Serializer =
            new XmlSerializer(typeof(PlayerRankData));

        // ------------------------------------------------------------------ //
        //  Construction
        // ------------------------------------------------------------------ //

        [Fact]
        public void Constructor_SetsFieldsCorrectly()
        {
            var data = new PlayerRankData("steam_12345", "WastelandBob");

            Assert.Equal("steam_12345", data.PlayerId);
            Assert.Equal("WastelandBob", data.OriginalName);
            Assert.Equal(0, data.ZombieKills);
            Assert.Equal(0, data.CurrentRankIndex);
            Assert.False(string.IsNullOrEmpty(data.LastSeen));
        }

        [Fact]
        public void DefaultConstructor_LeavesFieldsAtDefault()
        {
            var data = new PlayerRankData();

            Assert.Null(data.PlayerId);
            Assert.Null(data.OriginalName);
            Assert.Equal(0, data.ZombieKills);
            Assert.Equal(0, data.CurrentRankIndex);
        }

        // ------------------------------------------------------------------ //
        //  Touch
        // ------------------------------------------------------------------ //

        [Fact]
        public void Touch_UpdatesLastSeen()
        {
            var data = new PlayerRankData("id1", "Player");
            string before = data.LastSeen;

            // Small delay so the timestamp actually advances
            System.Threading.Thread.Sleep(10);
            data.Touch();

            Assert.NotEqual(before, data.LastSeen);
        }

        [Fact]
        public void Touch_LastSeenIsValidIso8601()
        {
            var data = new PlayerRankData("id1", "Player");
            data.Touch();

            Assert.True(DateTime.TryParse(data.LastSeen, out _));
        }

        // ------------------------------------------------------------------ //
        //  XML serialization round-trip
        // ------------------------------------------------------------------ //

        [Fact]
        public void XmlRoundTrip_PreservesAllFields()
        {
            var original = new PlayerRankData("steam_99999", "RadScorpionSlayer")
            {
                ZombieKills = 42000,
                CurrentRankIndex = 25,
            };

            string xml = Serialize(original);
            var restored = Deserialize(xml);

            Assert.Equal(original.PlayerId,         restored.PlayerId);
            Assert.Equal(original.OriginalName,     restored.OriginalName);
            Assert.Equal(original.ZombieKills,      restored.ZombieKills);
            Assert.Equal(original.CurrentRankIndex, restored.CurrentRankIndex);
            Assert.Equal(original.LastSeen,         restored.LastSeen);
        }

        [Fact]
        public void XmlRoundTrip_ZeroKillsNewPlayer()
        {
            var original = new PlayerRankData("steam_00001", "FreshCivilian");

            string xml = Serialize(original);
            var restored = Deserialize(xml);

            Assert.Equal(0, restored.ZombieKills);
            Assert.Equal(0, restored.CurrentRankIndex);
            Assert.Equal("FreshCivilian", restored.OriginalName);
        }

        [Fact]
        public void XmlOutput_ContainsPlayerId()
        {
            var data = new PlayerRankData("steam_abc", "Survivor");
            string xml = Serialize(data);

            Assert.Contains("steam_abc", xml);
        }

        // ------------------------------------------------------------------ //
        //  Helpers
        // ------------------------------------------------------------------ //

        private static string Serialize(PlayerRankData data)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
                Serializer.Serialize(writer, data);
            return sb.ToString();
        }

        private static PlayerRankData Deserialize(string xml)
        {
            using (var reader = new StringReader(xml))
                return (PlayerRankData)Serializer.Deserialize(reader);
        }
    }
}
