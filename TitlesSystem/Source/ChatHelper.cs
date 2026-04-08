namespace TitlesSystem
{
    /// <summary>
    /// Utility helpers for formatting chat messages sent by the Titles System.
    /// </summary>
    internal static class ChatHelper
    {
        /// <summary>
        /// Wraps <paramref name="message"/> in 7DTD BBCode color tags when
        /// <paramref name="colorHex"/> is a valid 6-character hex string (RRGGBB, no '#' prefix).
        /// Returns the original message unchanged if the color is empty or invalid.
        /// </summary>
        public static string ColorizeMessage(string message, string colorHex)
        {
            if (string.IsNullOrEmpty(message)) return message;
            if (string.IsNullOrEmpty(colorHex)) return message;

            // Must be exactly 6 hex characters — no '#' prefix, no 3-char shorthand.
            if (colorHex.Length != 6) return message;
            foreach (char c in colorHex)
            {
                if (!IsHexChar(c)) return message;
            }

            return $"[{colorHex}]{message}[-]";
        }

        private static bool IsHexChar(char c) =>
            (c >= '0' && c <= '9') ||
            (c >= 'a' && c <= 'f') ||
            (c >= 'A' && c <= 'F');
    }
}
