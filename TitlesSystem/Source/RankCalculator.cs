using System.Collections.Generic;

namespace TitlesSystem
{
    /// <summary>
    /// Pure rank-calculation helpers — no game API dependencies.
    /// Isolated here so the logic can be unit-tested in CI without game stubs.
    /// </summary>
    public static class RankCalculator
    {
        /// <summary>
        /// Returns the index of the highest rank whose kill threshold the given
        /// <paramref name="kills"/> count meets or exceeds.
        /// </summary>
        /// <param name="ranks">
        /// Rank list sorted ascending by <see cref="RankDefinition.KillsRequired"/>.
        /// </param>
        /// <param name="kills">Current zombie kill count.</param>
        /// <returns>
        /// Zero-based rank index, or 0 when the list is empty or all thresholds
        /// exceed the current kill count.
        /// </returns>
        public static int ComputeRankIndex(IReadOnlyList<RankDefinition> ranks, int kills)
        {
            int index = 0;
            for (int i = 0; i < ranks.Count; i++)
            {
                if (kills >= ranks[i].KillsRequired)
                    index = i;
                else
                    break;
            }
            return index;
        }
    }
}
