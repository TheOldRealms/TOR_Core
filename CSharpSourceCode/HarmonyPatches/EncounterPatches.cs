using HarmonyLib;
using Helpers;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.RaiseDead;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class EncounterPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(VillageHostileActionCampaignBehavior), "village_raid_game_menu_init")]
        public static bool VillageRaidGameMenuInitPrefix(MenuCallbackArgs args)
        {
            if (PlayerEncounter.EncounterSettlement == null)
            {
                if (Hero.MainHero.IsEnlisted())
                {
                    var lord = Hero.MainHero.GetEnlistingHero();

                    var t = MapEvent.PlayerMapEvent;
                    var settlement = lord.CurrentSettlement;
                    if (settlement != null)
                    {
                        MBTextManager.SetTextVariable("VILLAGE_NAME", settlement.Name, false);
                    }
                }
                else MBTextManager.SetTextVariable("VILLAGE_NAME", "unknown_settlement", false);
                return false;
            }
            else return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(VillageHostileActionCampaignBehavior), "wait_menu_start_raiding_on_condition")]
        public static bool WaitMenuStartRaidingOnConditionPrefix(MenuCallbackArgs args, ref bool __result)
        {
            if (Hero.MainHero.IsEnlisted())
            {
                __result = false;

                GameMenu.SwitchToMenu("hireling_menu");

                return false;
            }

            return true;
        }

        // Cached MethodInfo for internal methods (using AccessTools)
        private static System.Reflection.MethodInfo _getMemberRosterMethod;
        private static System.Reflection.MethodInfo _getPrisonerRosterMethod;

        /// <summary>
        /// Prefix patch for PlayerEncounter.DoLootParty
        /// Applies pending loot modifications (greenskin recruitment) before the loot screen is shown.
        /// At this point, the prisoner roster is populated and can be modified.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerEncounter), "DoLootParty")]
        public static void DoLootPartyPrefix(PlayerEncounter __instance)
        {
            // Only apply if there are pending modifications
            if (!PostBattleCampaignBehavior.HasPendingModifications)
                return;

            // Get the MapEvent from PlayerEncounter
            MapEvent mapEvent = MapEvent.PlayerMapEvent;
            if (mapEvent == null)
                return;

            // Get the actual rosters using AccessTools (methods are internal)
            TroopRoster memberRoster = GetMemberRosterReceivingLootShare(mapEvent, PartyBase.MainParty);
            TroopRoster prisonerRoster = GetPrisonerRosterReceivingLootShare(mapEvent, PartyBase.MainParty);

            if (memberRoster == null || prisonerRoster == null)
                return;

            // Apply pending modifications (add recruited troops, remove from prisoners)
            PostBattleCampaignBehavior.ApplyPendingLootModifications(memberRoster, prisonerRoster);
        }

        /// <summary>
        /// Access internal MapEvent.GetMemberRosterReceivingLootShare via reflection
        /// </summary>
        private static TroopRoster GetMemberRosterReceivingLootShare(MapEvent mapEvent, PartyBase party)
        {
            if (_getMemberRosterMethod == null)
            {
                _getMemberRosterMethod = AccessTools.Method(typeof(MapEvent), "GetMemberRosterReceivingLootShare", new[] { typeof(PartyBase) });
            }
            return _getMemberRosterMethod?.Invoke(mapEvent, new object[] { party }) as TroopRoster;
        }

        /// <summary>
        /// Access internal MapEvent.GetPrisonerRosterReceivingLootShare via reflection
        /// </summary>
        private static TroopRoster GetPrisonerRosterReceivingLootShare(MapEvent mapEvent, PartyBase party)
        {
            if (_getPrisonerRosterMethod == null)
            {
                _getPrisonerRosterMethod = AccessTools.Method(typeof(MapEvent), "GetPrisonerRosterReceivingLootShare", new[] { typeof(PartyBase) });
            }
            return _getPrisonerRosterMethod?.Invoke(mapEvent, new object[] { party }) as TroopRoster;
        }
    }
}