using System;
using System.Text.RegularExpressions;

namespace TitlesSystem.Client
{
    /// <summary>
    /// XUI controller for the TitlesRankPanel window.
    ///
    /// The server-side TitlesSystem mod sets each EntityPlayer's entityName to
    /// "[ShortTitle] PlayerName" and syncs it to all clients via
    /// NetPackageEntityNameChange.  This controller reads those names from the
    /// world, parses out the rank prefix, and exposes them as XUI bindings so
    /// the rank panel window can display them.
    ///
    /// Bindings (for rows 0–14):
    ///   pos_N   — row number ("1", "2", … or "" if no player in that slot)
    ///   name_N  — player name (without rank prefix)
    ///   rank_N  — rank short title (e.g. "Civilian", "Raider")
    /// </summary>
    public class XUiC_RankPanel : XUiController
    {
        private const int MaxRows = 15;
        private const int MaxPlayerNameLength = 20;

        // Matches "[ShortTitle] PlayerName" – the format set by TitlesSystem.dll.
        private static readonly Regex PrefixRegex =
            new Regex(@"^\[([^\]]+)\]\s+(.+)$", RegexOptions.Compiled);

        private readonly string[] _positions = new string[MaxRows];
        private readonly string[] _names     = new string[MaxRows];
        private readonly string[] _ranks     = new string[MaxRows];

        // ------------------------------------------------------------------ //
        //  XUiController overrides
        // ------------------------------------------------------------------ //

        public override void Init()
        {
            base.Init();
            ClearRows();
        }

        public override void OnOpen()
        {
            base.OnOpen();
            RefreshData();
        }

        public override bool GetBindingValue(ref string value, string bindingName)
        {
            // Fast-path: bindings are of the form  pos_N / name_N / rank_N
            int underscoreIdx = bindingName.IndexOf('_');
            if (underscoreIdx > 0
                && int.TryParse(bindingName.Substring(underscoreIdx + 1), out int rowIndex)
                && rowIndex >= 0 && rowIndex < MaxRows)
            {
                string prefix = bindingName.Substring(0, underscoreIdx);
                switch (prefix)
                {
                    case "pos":  value = _positions[rowIndex]; return true;
                    case "name": value = _names[rowIndex];     return true;
                    case "rank": value = _ranks[rowIndex];     return true;
                }
            }

            return base.GetBindingValue(ref value, bindingName);
        }

        // ------------------------------------------------------------------ //
        //  Private helpers
        // ------------------------------------------------------------------ //

        private void ClearRows()
        {
            for (int i = 0; i < MaxRows; i++)
            {
                _positions[i] = string.Empty;
                _names[i]     = string.Empty;
                _ranks[i]     = string.Empty;
            }
        }

        private void RefreshData()
        {
            ClearRows();

            try
            {
                var world   = GameManager.Instance?.World;
                var clients = ConnectionManager.Instance?.Clients?.list;

                if (world == null || clients == null)
                {
                    RefreshBindings();
                    return;
                }

                int row = 0;
                foreach (var client in clients)
                {
                    if (row >= MaxRows) break;

                    EntityPlayer entity;
                    if (!world.Players.dict.TryGetValue(client.entityId, out entity))
                        continue;

                    string playerName = client.playerName ?? "Unknown";
                    string rankName   = string.Empty;

                    var match = PrefixRegex.Match(entity.entityName ?? string.Empty);
                    if (match.Success)
                    {
                        rankName   = match.Groups[1].Value;
                        playerName = match.Groups[2].Value;
                    }

                    // Truncate very long names so they fit the column width
                    if (playerName.Length > MaxPlayerNameLength)
                        playerName = playerName.Substring(0, MaxPlayerNameLength);

                    _positions[row] = (row + 1).ToString();
                    _names[row]     = playerName;
                    _ranks[row]     = rankName;
                    row++;
                }
            }
            catch (Exception e)
            {
                Log.Warning("[TitlesClientMod] Error refreshing rank panel: " + e.Message);
            }

            RefreshBindings();
        }
    }
}
