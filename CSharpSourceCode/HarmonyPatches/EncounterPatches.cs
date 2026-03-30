using HarmonyLib;
using Helpers;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.PostBattleLoot;
using TOR_Core.CampaignMechanics.ServeAsAHireling;
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



        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMenu), "ActivateGameMenu")]
        public static bool ActivateGameMenuPrefix(ref string menuId)
        {
            // When enlisted, intercept encounter menu activation
            if (menuId == "encounter" && Hero.MainHero.IsEnlisted())
            {
                // Let encounter through if player clicked "Join Battle"
                if (ServeAsAHirelingCampaignBehavior.IsStartingBattle)
                {
                    return true;
                }
                var currentEncounter = PlayerEncounter.Current;

                if (currentEncounter?.IsJoinedBattle == true
                    && currentEncounter.EncounterState != PlayerEncounterState.End)
                {
                    return true;
                }

                if (currentEncounter != null
                    && currentEncounter.EncounterState == PlayerEncounterState.End
                    && PlayerEncounter.EncounterSettlement == null)
                {
                    PlayerEncounter.Finish(false);
                }

                var hirelingBehavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
                var enlistingLordParty = hirelingBehavior?.EnlistingLord?.PartyBelongedTo;
                var playerMapEvent = Hero.MainHero.PartyBelongedTo?.MapEvent;

                var hasOngoingHirelingBattle =
                    (playerMapEvent != null && !playerMapEvent.HasWinner) ||
                    (enlistingLordParty?.MapEvent != null && !enlistingLordParty.MapEvent.HasWinner);

                if (hasOngoingHirelingBattle)
                {
                    menuId = "hireling_battle_menu";
                }
                else
                {
                    menuId = "hireling_menu";
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerEncounter), "Finish")]
        public static bool PlayerEncounterFinishPrefix()
        {
            // Only block Finish during post-battle transitions when enlisted
            // This prevents crashes from AI party ticks while allowing normal siege/encounter flow
            if (!Hero.MainHero.IsEnlisted() || !ServeAsAHirelingCampaignBehavior.InPostBattleTransition)
            {
                return true;
            }

            if (PlayerEncounter.EncounterSettlement == null)
            {
                return true;
            }

            var hirelingBehavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            var enlistingLordParty = hirelingBehavior?.EnlistingLord?.PartyBelongedTo;

            var hasActiveHirelingBattle =
                MapEvent.PlayerMapEvent != null ||
                Hero.MainHero.PartyBelongedTo?.MapEvent != null ||
                enlistingLordParty?.MapEvent != null;

            if (!hasActiveHirelingBattle)
            {
                return true;
            }

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapEvent), "GetMemberRosterReceivingLootShare")]
        public static void GetMemberRosterPostfix(TroopRoster __result)
        {
            if (__result != null && PendingLootedTroopManager.HasPendingModifications)
            {
                PendingLootedTroopManager.ApplyMemberModifications(__result);
                PendingLootedTroopManager.ConsumeMemberModifications();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapEvent), "GetPrisonerRosterReceivingLootShare")]
        public static void GetPrisonerRosterPostfix(TroopRoster __result)
        {
            if (__result != null && PendingLootedTroopManager.HasPendingModifications)
            {
                PendingLootedTroopManager.ApplyPrisonerModifications(__result);
                PendingLootedTroopManager.ConsumePrisonerModifications();
            }
        }
    }
}