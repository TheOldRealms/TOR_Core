using HarmonyLib;
using SandBox.AdvancedStartOptions;
using SandBox;
using SandBox.View;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameState;
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
        private static bool _torCampaignStartPending;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Module), "GetInitialStateOptions")]
        public static void ReplaceVanillaNewGameOptions(ref IEnumerable<InitialStateOption> __result)
        {
            var options = __result.ToList();
            var sandBoxNewGameOption = options.First(x => x.Id == "SandBoxNewGame");
            options.RemoveAll(x => x.Id == "StoryModeNewGame" || x.Id == "SandBoxNewGame");
            // Low OrderIndex to appear at top (UI changed from BottomToTop to TopToBottom in 1.4)
            var enterOldWorldOption = new InitialStateOption("TORNewgame", new TextObject("{=str_tor_menu_enter_game}Enter the Old World"), 1, () => OnClick(sandBoxNewGameOption), IsDisabledAndReason);
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

        private static void OnClick(InitialStateOption sandBoxNewGameOption)
        {
            _torCampaignStartPending = true;
            sandBoxNewGameOption.DoAction();
        }

        // 1.5 creates its own SandBoxGameManager for advanced start that cant be provided with TorCampaignGameManager for now
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MBGameManager), nameof(MBGameManager.StartNewGame), [typeof(MBGameManager)])]
        private static void UseTorCampaignManager(ref MBGameManager gameLoader)
        {
            if (!_torCampaignStartPending || gameLoader.GetType() != typeof(SandBoxGameManager))
                return;

            var sandBoxGameManager = (SandBoxGameManager)gameLoader;
            if (sandBoxGameManager.LoadingSavedGame)
                return;

            var campaignCreator = (SandBoxGameManager.CampaignCreatorDelegate)AccessTools.Field(typeof(SandBoxGameManager), "_campaignCreator").GetValue(sandBoxGameManager);
            gameLoader = new TorCampaignGameManager(campaignCreator);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SandBoxViewSubModule), "OnStartingOptionsClosed")]
        private static void OnStartingOptionsClosed()
        {
            _torCampaignStartPending = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapState), "OnActivate")]
        private static void OnCampaignMapActivated()
        {
            _torCampaignStartPending = false;
        }

        private static (bool, TextObject) IsDisabledAndReason()
        {
            TextObject coreContentDisabledReason = new TextObject("{=str_tor_disabled_during_installation}Disabled during installation.");
            return (Module.CurrentModule.IsOnlyCoreContentEnabled, coreContentDisabledReason);
        }
    }
}
