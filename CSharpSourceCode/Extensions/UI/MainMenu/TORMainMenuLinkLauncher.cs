using System;
using System.Diagnostics;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TOR_Core.Extensions.UI.MainMenu
{
    internal static class TORMainMenuLinkLauncher
    {
        private const string ManualUrl = "https://docs.google.com/document/d/1CzK7T661DsFGVLyxDF5Gk3piXT9DbiaGW2BTv667fTA/edit?usp=drive_link";
        private const string DiscordUrl = "https://discord.gg/U6fqhPx38";

        internal static void OpenManual()
        {
            Open(ManualUrl);
        }

        internal static void OpenDiscord()
        {
            Open(DiscordUrl);
        }

        private static void Open(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                var text = new TextObject("{=str_tor_main_menu_link_open_failed}Could not open link: {ERROR}");
                text.SetTextVariable("ERROR", ex.Message);
                InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
            }
        }
    }
}
