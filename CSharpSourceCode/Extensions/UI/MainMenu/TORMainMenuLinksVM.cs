using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TOR_Core.Extensions.UI.MainMenu
{
    public class TORMainMenuLinksVM : ViewModel
    {
        private readonly Action _requestRecommendedSettingsConfirmation;

        public TORMainMenuLinksVM(Action requestRecommendedSettingsConfirmation)
        {
            _requestRecommendedSettingsConfirmation = requestRecommendedSettingsConfirmation ?? throw new ArgumentNullException(nameof(requestRecommendedSettingsConfirmation));
        }

        [DataSourceProperty]
        public string ManualButtonText => new TextObject("{=str_tor_main_menu_manual_button}Read Manual").ToString();

        [DataSourceProperty]
        public string DiscordButtonText => new TextObject("{=str_tor_main_menu_discord_button}Join Discord").ToString();

        [DataSourceProperty]
        public string RecommendedSettingsButtonText => new TextObject("{=str_tor_main_menu_recommended_settings_button}Recommended settings").ToString();

        public void ExecuteOpenManual()
        {
            TORMainMenuLinkLauncher.OpenManual();
        }

        public void ExecuteOpenDiscord()
        {
            TORMainMenuLinkLauncher.OpenDiscord();
        }

        public void ExecuteApplyRecommendedSettings()
        {
            _requestRecommendedSettingsConfirmation();
        }
    }
}
