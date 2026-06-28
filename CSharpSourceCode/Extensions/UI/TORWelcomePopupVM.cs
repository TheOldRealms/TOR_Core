using System;
using System.Diagnostics;
using System.IO;
using TaleWorlds.Library;

namespace TOR_Core.Extensions.UI
{
    public class TORWelcomePopupVM : ViewModel
    {
        private const string ManualUrl = "https://docs.google.com/document/d/1CzK7T661DsFGVLyxDF5Gk3piXT9DbiaGW2BTv667fTA/edit?usp=drive_link";
        private const string DiscordUrl = "https://discord.gg/AFC4pTQVB";
        private const string SettingsFolderName = "TheOldRealms";
        private const string DisabledFlagFileName = "welcome_popup_disabled.txt";

        private readonly Action _closeAction;

        public TORWelcomePopupVM(Action closeAction)
        {
            _closeAction = closeAction;
        }

        [DataSourceProperty]
        public string TitleText => "Welcome to The Old Realms!";

        [DataSourceProperty]
        public string DescriptionText => "A total conversion mod for Mount and Blade II: Bannerlord. We have put a lot of work and new features into this mod for you to enjoy, so much that we needed a manual to let you know about it all. Don't hesitate to reach out to our discord if you have issues installing or running the mod.";

        [DataSourceProperty]
        public string ManualButtonText => "Read Manual";

        [DataSourceProperty]
        public string DiscordButtonText => "Join Discord";

        [DataSourceProperty]
        public string CloseButtonText => "Continue";

        [DataSourceProperty]
        public string DisableButtonText => "Don't Show Again";

        public static bool IsWelcomePopupDisabled()
        {
            try
            {
                return File.Exists(GetDisabledFlagPath());
            }
            catch
            {
                return false;
            }
        }

        public void ExecuteOpenManual()
        {
            OpenUrl(ManualUrl);
        }

        public void ExecuteOpenDiscord()
        {
            OpenUrl(DiscordUrl);
        }

        public void ExecuteClose()
        {
            _closeAction?.Invoke();
        }

        public void ExecuteDisableWelcomePopup()
        {
            try
            {
                string flagPath = GetDisabledFlagPath();
                Directory.CreateDirectory(Path.GetDirectoryName(flagPath));
                File.WriteAllText(flagPath, DateTime.UtcNow.ToString("O"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("Could not save the welcome popup preference: " + ex.Message));
            }

            ExecuteClose();
        }

        public static void OpenUrl(string url)
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
                InformationManager.DisplayMessage(new InformationMessage("Could not open link: " + ex.Message));
            }
        }

        private static string GetDisabledFlagPath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, SettingsFolderName, DisabledFlagFileName);
        }
    }

    public class TORMainMenuLinksVM : ViewModel
    {
        [DataSourceProperty]
        public string ManualButtonText => "Read Manual";

        [DataSourceProperty]
        public string DiscordButtonText => "Join Discord";

        public void ExecuteOpenManual()
        {
            TORWelcomePopupVM.OpenUrl("https://docs.google.com/document/d/1CzK7T661DsFGVLyxDF5Gk3piXT9DbiaGW2BTv667fTA/edit?usp=drive_link");
        }

        public void ExecuteOpenDiscord()
        {
            TORWelcomePopupVM.OpenUrl("https://discord.gg/AFC4pTQVB");
        }
    }
}
