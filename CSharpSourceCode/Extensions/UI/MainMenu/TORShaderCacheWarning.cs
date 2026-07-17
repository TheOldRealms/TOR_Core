using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.GameManagers;

namespace TOR_Core.Extensions.UI.MainMenu
{
    public static class TORShaderCacheWarning
    {
        public static void Show()
        {
            var text = new TextObject("{=str_tor_menu_shader_cache_popup_message}This will load a scene with all the unique troops and NPCs present in our mod. The purpose of this is to compile the local shader cache on your PC.{newline}" +
                       "When you see the deployment phase, the process is complete!{newline}{newline}" +
                       "THIS WILL TAKE A LONG TIME!!!{newline}" +
                       "Our users report anything between 20 and 70 minutes.{newline}{newline}" +
                       "This ensures that you won't need to compile the shaders individually during normal gameplay as it can cause issues with stability.{newline}" +
                       "This is meant to reduce the number of UI portrait generation crashes and also eliminate the long battle loading times during normal gameplay.").SetTextVariable("newline", "\n").ToString();

            var data = new InquiryData(
                new TextObject("{=str_tor_menu_shader_cache_popup_title}Important warning").ToString(),
                text,
                true,
                true,
                new TextObject("{=str_tor_menu_shader_cache_popup_confirm}Do it").ToString(),
                new TextObject("{=str_tor_menu_shader_cache_popup_reject}Not now").ToString(),
                BuildShaderCache,
                Hide);
            InformationManager.ShowInquiry(data);
        }

        private static void Hide()
        {
            InformationManager.HideInquiry();
        }

        private static void BuildShaderCache()
        {
            MBGameManager.StartNewGame(new TORShaderGameManager());
        }
    }
}
