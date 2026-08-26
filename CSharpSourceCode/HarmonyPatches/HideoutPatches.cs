using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class HideoutPatches
    {
        private static bool CanAttackHideout()
        {
            var model = Campaign.Current.Models.HideoutModel;
            float currentHour = CampaignTime.Now.CurrentHourInDay;
            return currentHour >= model.CanAttackHideoutStartTime || currentHour < model.CanAttackHideoutEndTime;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HideoutCampaignBehavior), "game_menu_hideout_sneak_in_on_condition")]
        public static bool DisableSneakForGreenskins(MenuCallbackArgs args, ref bool __result)
        {
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                __result = MenuHelper.SetOptionProperties(args, false, true,
                    TORTextHelper.GetTextObject("tor_hideout_greenskin_no_sneak", "You are too clumsy to sneak in undetected."));
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HideoutCampaignBehavior), "game_menu_wait_until_nightfall_on_condition")]
        public static void DisableWaitForNightfallForGreenskins(MenuCallbackArgs args, ref bool __result)
        {
            // Only disable "wait until nightfall" for greenskins during daytime
            // They can still wait until daytime if it's currently night
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN && !CanAttackHideout())
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                __result = MenuHelper.SetOptionProperties(args, false, true,
                    TORTextHelper.GetTextObject("tor_hideout_greenskin_no_wait", "You are too clumsy to sneak in undetected. Why even wait?"));
            }
        }
    }
}