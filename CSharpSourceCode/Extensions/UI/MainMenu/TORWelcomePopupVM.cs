using System;
using System.IO;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Extensions.UI.MainMenu
{
    public class TORWelcomePopupVM : ViewModel
    {
        private const string SettingsFolderName = "TheOldRealms";
        private const string DisabledFlagFileName = "welcome_popup_disabled.txt";

        private readonly Action _closeAction;

        public TORWelcomePopupVM(Action closeAction)
        {
            _closeAction = closeAction ?? throw new ArgumentNullException(nameof(closeAction));
        }

        [DataSourceProperty]
        public string TitleText => new TextObject("{=str_tor_main_menu_welcome_title}Welcome to The Old Realms!").ToString();

        [DataSourceProperty]
        public string DescriptionText => new TextObject("{=str_tor_main_menu_welcome_description}This is a total conversion mod for Mount and Blade II: Bannerlord. We have put a lot of work and new features into this mod for you to enjoy, so much that we needed a manual to let you know about it all. Don't hesitate to reach out to our discord if you have issues installing or running the mod.").ToString();

        [DataSourceProperty]
        public string ManualButtonText => new TextObject("{=str_tor_main_menu_manual_button}Read Manual").ToString();

        [DataSourceProperty]
        public string DiscordButtonText => new TextObject("{=str_tor_main_menu_discord_button}Join Discord").ToString();

        [DataSourceProperty]
        public string CloseButtonText => new TextObject("{=str_tor_main_menu_welcome_continue_button}Continue").ToString();

        [DataSourceProperty]
        public string DisableButtonText => new TextObject("{=str_tor_main_menu_welcome_disable_button}Don't Show Again").ToString();

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
            TORMainMenuLinkLauncher.OpenManual();
        }

        public void ExecuteOpenDiscord()
        {
            TORMainMenuLinkLauncher.OpenDiscord();
        }

        public void ExecuteClose()
        {
            _closeAction();
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
                var text = new TextObject("{=str_tor_main_menu_welcome_preference_save_failed}Could not save the welcome popup preference: {ERROR}");
                text.SetTextVariable("ERROR", ex.Message);
                InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
            }

            ExecuteClose();
        }

        private static string GetDisabledFlagPath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, SettingsFolderName, DisabledFlagFileName);
        }
    }
}
