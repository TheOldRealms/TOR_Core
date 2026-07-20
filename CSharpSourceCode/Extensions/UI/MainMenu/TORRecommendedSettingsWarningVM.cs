using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TOR_Core.Extensions.UI.MainMenu
{
    public class TORRecommendedSettingsWarningVM : ViewModel
    {
        private readonly Action _confirmAction;
        private readonly Action _cancelAction;

        public TORRecommendedSettingsWarningVM(Action confirmAction, Action cancelAction)
        {
            _confirmAction = confirmAction ?? throw new ArgumentNullException(nameof(confirmAction));
            _cancelAction = cancelAction ?? throw new ArgumentNullException(nameof(cancelAction));
        }

        [DataSourceProperty]
        public string TitleText => new TextObject("{=str_tor_main_menu_recommended_settings_title}Recommended Settings").ToString();

        [DataSourceProperty]
        public string DescriptionText => new TextObject("{=str_tor_main_menu_recommended_settings_description}Applying these settings may cause Bannerlord to rebuild shaders. Performance can be lower or uneven until shaders finish rebuilding, or until you rebuild shaders manually. Apply recommended settings now?").ToString();

        [DataSourceProperty]
        public string ConfirmButtonText => new TextObject("{=str_tor_main_menu_recommended_settings_apply_button}Apply").ToString();

        [DataSourceProperty]
        public string CancelButtonText => new TextObject("{=str_tor_main_menu_recommended_settings_cancel_button}Cancel").ToString();

        public void ExecuteConfirm()
        {
            _confirmAction();
        }

        public void ExecuteCancel()
        {
            _cancelAction();
        }
    }
}
