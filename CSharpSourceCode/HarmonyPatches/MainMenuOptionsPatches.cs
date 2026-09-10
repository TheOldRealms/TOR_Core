using HarmonyLib;
using SandBox.AdvancedStartOptions;
using SandBox.View;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;
using TOR_Core.Extensions.UI.MainMenu;
using TOR_Core.GameManagers;

namespace TOR_Core.HarmonyPatches
{
    // TORTextHelper is unavailable at this startup stage because GameTextManager is not initialized.
    // Tagged TextObject fallbacks keep these options startup-safe and localizable.
    [HarmonyPatch]
    public class MainMenuOptionsPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Module), "GetInitialStateOptions")]
        public static void ReplaceVanillaNewGameOptions(ref IEnumerable<InitialStateOption> __result)
        {
            var options = __result.Where(x => x.Id != "StoryModeNewGame" && x.Id != "SandBoxNewGame").ToList();
            // Low OrderIndex to appear at top (UI changed from BottomToTop to TopToBottom in 1.4)
            var enterOldWorldOption = new InitialStateOption("TORNewgame", new TextObject("{=str_tor_menu_enter_game}Enter the Old World"), 1, OnClick, IsDisabledAndReason);
            var buildShaderCacheOption = new InitialStateOption("TORForceLoad", new TextObject("{=str_tor_menu_shader_cache}Build Shader Cache"), 2, OnForceClick, IsDisabledAndReason);
            options.Add(enterOldWorldOption);
            options.Add(buildShaderCacheOption);
            options.Sort((x, y) => x.OrderIndex.CompareTo(y.OrderIndex));
            __result = options;
        }

        private static void OnForceClick()
        {
            TORShaderCacheWarning.Show();
        }

        private static void OnClick()
        {
            ////Action keeps getting a null target and therefore nothing happens when the button is clicked.
            ////Sly : code copied from SandBoxViewSubModule with replacement actions fetched via reflection.
            AdvancedStartOptions options = AdvancedStartOptionsManager.CreateCampaignStartOptions();
            //if (!options.IsEmpty())
            //{
            //    var sandBoxViewSubModuleInstance = Activator.CreateInstance(typeof(SandBoxViewSubModule));
            //    var onStartingOptionsConfirmedMethod = AccessTools.Method(typeof(SandBoxViewSubModule), "OnStartingOptionsConfirmed", [typeof(AdvancedStartOptions)]);
            //    Action<AdvancedStartOptions> action = (Action<AdvancedStartOptions>)onStartingOptionsConfirmedMethod.Invoke(sandBoxViewSubModuleInstance, new object[] {options});
            //    //var action = new Action<AdvancedStartOptions>(OnStartingOptionsConfirmedMethod);
            //    ScreenManager.AddGlobalLayer(SandBoxViewCreator.CreateCampaignAdvancedStartOptions(options, action, new Action(OnStartingOptionsClosedMethod)), true);
            //    return;
            //}

            // Campaign creator delegate that creates a new Campaign in Campaign mode
            MBGameManager.StartNewGame(new TorCampaignGameManager(() => new Campaign(CampaignGameMode.Campaign, options.GetChangedOptions())));

            static void OnStartingOptionsConfirmedMethod(SandBox.AdvancedStartOptions.AdvancedStartOptions options)
            {
                var sandBoxViewSubModuleInstance = Activator.CreateInstance(typeof(SandBoxViewSubModule));
                var onStartingOptionsConfirmedMethod = AccessTools.Method(typeof(SandBoxViewSubModule), "OnStartingOptionsConfirmed", [typeof(AdvancedStartOptions)]);
                onStartingOptionsConfirmedMethod.Invoke(sandBoxViewSubModuleInstance, [options]);
            }

            static void OnStartingOptionsClosedMethod()
            {
                var sandBoxViewSubModuleInstance = Activator.CreateInstance(typeof(SandBoxViewSubModule));
                var onStartingOptionsClosedMethod = AccessTools.Method(typeof(SandBoxViewSubModule), "OnStartingOptionsClosed");
                onStartingOptionsClosedMethod.Invoke(sandBoxViewSubModuleInstance, []);
            }
        }

        private static (bool, TextObject) IsDisabledAndReason()
        {
            TextObject coreContentDisabledReason = new TextObject("{=str_tor_disabled_during_installation}Disabled during installation.");
            return (Module.CurrentModule.IsOnlyCoreContentEnabled, coreContentDisabledReason);
        }
    }
}
