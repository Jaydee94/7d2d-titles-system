using System;
using System.Collections.Generic;

namespace TitlesSystem.Client
{
    /// <summary>
    /// Client-side console command that opens or closes the TitlesRankPanel window.
    ///
    /// Commands:  rankpanel   (alias: rp)
    ///
    /// The panel shows all currently connected players alongside the rank prefix
    /// embedded in their entityName by the server-side TitlesSystem mod.
    /// </summary>
    public class ConsoleCmdRankPanel : ConsoleCmdAbstract
    {
        public override string[] getCommands() => new[] { "rankpanel", "rp" };

        public override string getDescription() =>
            "Opens or closes the Rank Panel window showing online player ranks.";

        public override string getHelp() =>
            "Usage: rankpanel\n" +
            "Toggles the Rank Panel window that shows the rank of every online player.\n" +
            "The rank data is provided by the server-side TitlesSystem mod.\n" +
            "Alias: rp";

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            try
            {
                var ui = LocalPlayerUI.GetUIForPrimaryPlayer();
                if (ui == null)
                {
                    SdtdConsole.Instance.Output("[TitlesClientMod] Local player UI not available.");
                    return;
                }

                var group = ui.xui.FindWindowGroupByName("TitlesRankPanel");
                if (group == null)
                {
                    SdtdConsole.Instance.Output("[TitlesClientMod] TitlesRankPanel window not found. " +
                        "Make sure TitlesSystemClientMod is installed correctly.");
                    return;
                }

                group.isShown = !group.isShown;
            }
            catch (Exception e)
            {
                Log.Warning("[TitlesClientMod] Could not toggle rank panel: " + e.Message);
            }
        }
    }
}
