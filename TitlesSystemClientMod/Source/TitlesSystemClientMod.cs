namespace TitlesSystem.Client
{
    /// <summary>
    /// Entry point for the Titles System Client Mod.
    /// Implements IModApi so the 7DTD engine discovers and loads this assembly.
    ///
    /// No active initialization is required: the XUiC_RankPanel controller is
    /// registered automatically by the XUI system when the window XML is loaded,
    /// and ConsoleCmdRankPanel is discovered automatically by the console
    /// command manager.
    /// </summary>
    public class TitlesSystemClientMod : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            Log.Out("[TitlesClientMod] Titles System Rank Panel mod loaded.");
            Log.Out("[TitlesClientMod] Use 'rankpanel' (alias 'rp') to open the rank panel.");
        }
    }
}
