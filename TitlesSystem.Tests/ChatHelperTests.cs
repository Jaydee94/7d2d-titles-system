using Xunit;

namespace TitlesSystem.Tests
{
    public class ChatHelperTests
    {
        // ------------------------------------------------------------------ //
        //  ColorizeMessage — happy path
        // ------------------------------------------------------------------ //

        [Fact]
        public void ColorizeMessage_ValidLowercaseHex_WrapsMessage()
        {
            string result = ChatHelper.ColorizeMessage("Hello World", "ffd700");
            Assert.Equal("[ffd700]Hello World[-]", result);
        }

        [Fact]
        public void ColorizeMessage_ValidUppercaseHex_WrapsMessage()
        {
            string result = ChatHelper.ColorizeMessage("Hello World", "FFD700");
            Assert.Equal("[FFD700]Hello World[-]", result);
        }

        [Fact]
        public void ColorizeMessage_ValidMixedCaseHex_WrapsMessage()
        {
            string result = ChatHelper.ColorizeMessage("Rank Up!", "00BfFf");
            Assert.Equal("[00BfFf]Rank Up![-]", result);
        }

        // ------------------------------------------------------------------ //
        //  ColorizeMessage — empty / null color disables coloring
        // ------------------------------------------------------------------ //

        [Fact]
        public void ColorizeMessage_EmptyColor_ReturnsOriginalMessage()
        {
            string result = ChatHelper.ColorizeMessage("Hello", string.Empty);
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void ColorizeMessage_NullColor_ReturnsOriginalMessage()
        {
            string result = ChatHelper.ColorizeMessage("Hello", null);
            Assert.Equal("Hello", result);
        }

        // ------------------------------------------------------------------ //
        //  ColorizeMessage — invalid colors fall back to plain message
        // ------------------------------------------------------------------ //

        [Fact]
        public void ColorizeMessage_ThreeCharHex_ReturnsOriginalMessage()
        {
            string result = ChatHelper.ColorizeMessage("Hello", "F00");
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void ColorizeMessage_SevenCharHex_ReturnsOriginalMessage()
        {
            string result = ChatHelper.ColorizeMessage("Hello", "FFD7001");
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void ColorizeMessage_HexWithHash_ReturnsOriginalMessage()
        {
            string result = ChatHelper.ColorizeMessage("Hello", "#FFD700");
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void ColorizeMessage_NonHexChars_ReturnsOriginalMessage()
        {
            string result = ChatHelper.ColorizeMessage("Hello", "GGHHII");
            Assert.Equal("Hello", result);
        }

        // ------------------------------------------------------------------ //
        //  ColorizeMessage — empty / null message
        // ------------------------------------------------------------------ //

        [Fact]
        public void ColorizeMessage_EmptyMessage_ReturnsEmpty()
        {
            string result = ChatHelper.ColorizeMessage(string.Empty, "FFD700");
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ColorizeMessage_NullMessage_ReturnsNull()
        {
            string result = ChatHelper.ColorizeMessage(null, "FFD700");
            Assert.Null(result);
        }
    }
}
