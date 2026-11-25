using HarmonyLib;
using SandBox.AdvancedStartOptions;
using SandBox;
using SandBox.GauntletUI;
using SandBox.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.ScreenSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.Extensions.UI;
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
            DisplayShaderCacheWarning();
        }

        private static void DisplayShaderCacheWarning()
        {
            var text = new TextObject("{=str_tor_menu_shader_cache_popup_message}This will load a scene with all the unique troops and NPCs present in our mod. The purpose of this is to compile the local shader cache on your PC.{newline}" +
                       "When you see the deployment phase, the process is complete!{newline}{newline}" +
                       "THIS WILL TAKE A LONG TIME!!!{newline}" +
                       "Our users report anything between 20 and 70 minutes.{newline}{newline}" +
                       "This ensures that you won't need to compile the shaders individually during normal gameplay as it can cause issues with stability.{newline}" +
                       "This is meant to reduce the number of UI portrait generation crashes and also eliminate the long battle loading times during normal gameplay.").ToString();

            var data = new InquiryData(
                new TextObject("{=str_tor_menu_shader_cache_popup_title}Important warning").ToString(),
                text,
                true,
                true,
                new TextObject("{=str_tor_menu_shader_cache_popup_confirm}Do it").ToString(),
                new TextObject("{=str_tor_menu_shader_cache_popup_reject}Not now").ToString(),
                BuildShaderCache,
                InformationManager.HideInquiry);
            InformationManager.ShowInquiry(data);
        }

        private static void BuildShaderCache()
        {
            MBGameManager.StartNewGame(new TORShaderGameManager());
        }

        private static void OnClick(InitialStateOption sandBoxNewGameOption)
        {
            _torCampaignStartPending = true;
            sandBoxNewGameOption.DoAction();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GauntletCampaignStartingOptionsView), "OnTick")]
        private static IEnumerable<CodeInstruction> AllowTorInitialScreenForAdvancedStart(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var initialScreenCheck = codes.FindIndex(x => x.opcode == OpCodes.Isinst && Equals(x.operand, typeof(GauntletInitialScreen)));

            if (initialScreenCheck < 0)
                throw new ArgumentException("couldnt find advanced start initial screen check.");

            codes[initialScreenCheck].opcode = OpCodes.Call;
            codes[initialScreenCheck].operand = AccessTools.Method(typeof(MainMenuOptionsPatches), nameof(IsAdvancedStartHostScreen));

            return codes;
        }

        private static bool IsAdvancedStartHostScreen(ScreenBase screen)
        {
            return screen is GauntletInitialScreen || screen is TORInitialScreen;
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
