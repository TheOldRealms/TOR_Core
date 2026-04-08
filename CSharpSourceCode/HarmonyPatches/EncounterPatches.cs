using HarmonyLib;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
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
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class EncounterPatches
    {
        private static bool ShouldBypassDeadHirelingEncounter()
        {
            if (!Hero.MainHero.IsEnlisted() || ServeAsAHirelingCampaignBehavior.IsStartingBattle)
            {
                return false;
            }

            if (PlayerEncounter.EncounterSettlement != null)
            {
                return false;
            }

            var currentEncounter = PlayerEncounter.Current;
            if (currentEncounter == null)
            {
                return false;
            }

            var playerMapEvent = MapEvent.PlayerMapEvent;
            return currentEncounter.EncounterState == PlayerEncounterState.End
                || playerMapEvent?.State == MapEventState.WaitingRemoval
                || PlayerEncounter.Battle?.HasWinner == true;
        }

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
                else
                {
                    MBTextManager.SetTextVariable("VILLAGE_NAME", "unknown_settlement", false);
                }

                return false;
            }

            return true;
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

            if (menuId == "encounter" && Hero.MainHero.IsEnlisted())
            {
                if (ServeAsAHirelingCampaignBehavior.IsStartingBattle)
                {
                    return true;
                }

                if (ShouldBypassDeadHirelingEncounter())
                {
                    if (ServeAsAHirelingCampaignBehavior.TryFinalizeTrackedHirelingVictory())
                    {
                        return false;
                    }

                    if (ServeAsAHirelingCampaignBehavior.CleanupTrackedDeadHirelingResultEncounter())
                    {
                        menuId = "hireling_menu";
                        ServeAsAHirelingCampaignBehavior.MarkHirelingWaitMenuShown();
                        return true;
                    }

                    menuId = "hireling_menu";
                    ServeAsAHirelingCampaignBehavior.MarkHirelingWaitMenuShown();
                    return true;
                }

                if (ServeAsAHirelingCampaignBehavior.TryFinalizeTrackedHirelingVictory())
                {
                    return false;
                }

                if (ServeAsAHirelingCampaignBehavior.CleanupTrackedDeadHirelingResultEncounter())
                {
                    menuId = "hireling_menu";
                    ServeAsAHirelingCampaignBehavior.MarkHirelingWaitMenuShown();
                    return true;
                }

                var currentEncounter = PlayerEncounter.Current;
                if (currentEncounter != null && PlayerEncounter.EncounterSettlement == null)
                {
                    return true;
                }

                var hirelingBehavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
                var enlistingLordParty = hirelingBehavior?.EnlistingLord?.PartyBelongedTo;
                var playerMapEvent = Hero.MainHero.PartyBelongedTo?.MapEvent;
                var playerMapEventSide = Hero.MainHero.PartyBelongedTo?.MapEventSide?.MissionSide;
                var enlistingLordMapEventSide = enlistingLordParty?.MapEventSide?.MissionSide;

                var hasJoinableHirelingBattle =
                    ServeAsAHirelingCampaignBehavior.IsJoinableHirelingMapEvent(playerMapEvent, playerMapEventSide) ||
                    ServeAsAHirelingCampaignBehavior.IsJoinableHirelingMapEvent(enlistingLordParty?.MapEvent, enlistingLordMapEventSide);

                var hasPendingNativeCleanup =
                    ServeAsAHirelingCampaignBehavior.HasPendingNativeEncounterCleanup();

                if (hasJoinableHirelingBattle)
                {
                    menuId = "hireling_battle_menu";
                }
                else if (hasPendingNativeCleanup)
                {
                    return true;
                }
                else
                {
                    menuId = "hireling_menu";
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMenu), "SwitchToMenu")]
        public static bool SwitchToMenuPrefix(ref string menuId)
        {
            if (!Hero.MainHero.IsEnlisted()
                || !ServeAsAHirelingCampaignBehavior.InPostBattleTransition)
            {
                return true;
            }

            if (menuId != "menu_settlement_taken"
                && menuId != "menu_settlement_taken_player_leader"
                && menuId != "menu_settlement_taken_player_army_member"
                && menuId != "menu_settlement_taken_player_participant")
            {
                return true;
            }

            menuId = "hireling_menu";
            ServeAsAHirelingCampaignBehavior.MarkHirelingWaitMenuShown();

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EncounterGameMenuBehavior), "game_menu_encounter_on_init")]
        public static bool EncounterGameMenuOnInitPrefix(MenuCallbackArgs args)
        {
            if (!ShouldBypassDeadHirelingEncounter())
            {
                return true;
            }

            if (ServeAsAHirelingCampaignBehavior.TryFinalizeTrackedHirelingVictory())
            {
                return false;
            }

            if (ServeAsAHirelingCampaignBehavior.CleanupTrackedDeadHirelingResultEncounter())
            {
                GameMenu.SwitchToMenu("hireling_menu");
                ServeAsAHirelingCampaignBehavior.MarkHirelingWaitMenuShown();
                return false;
            }

            GameMenu.SwitchToMenu("hireling_menu");
            ServeAsAHirelingCampaignBehavior.MarkHirelingWaitMenuShown();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerEncounter), "Finish")]
        public static bool PlayerEncounterFinishPrefix()
        {
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
                ServeAsAHirelingCampaignBehavior.IsOngoingHirelingMapEvent(MapEvent.PlayerMapEvent) ||
                ServeAsAHirelingCampaignBehavior.IsOngoingHirelingMapEvent(Hero.MainHero.PartyBelongedTo?.MapEvent) ||
                ServeAsAHirelingCampaignBehavior.IsOngoingHirelingMapEvent(enlistingLordParty?.MapEvent);

            if (!hasActiveHirelingBattle)
            {
                return true;
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GainKingdomInfluenceAction), nameof(GainKingdomInfluenceAction.ApplyForBattle))]
        public static bool GainKingdomInfluenceActionApplyForBattlePrefix(Hero hero, float value)
        {
            if (ServeAsAHirelingCampaignBehavior.ShouldSuppressHirelingBattleInfluence(hero, value))
            {
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerEncounter), "DoCaptureHeroes")]
        public static bool DoCaptureHeroesPrefix(PlayerEncounter __instance)
        {
            if (!ServeAsAHirelingCampaignBehavior.TryFinalizeTrackedHirelingVictory())
            {
                return true;
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChangeClanInfluenceAction), nameof(ChangeClanInfluenceAction.Apply), new[] { typeof(Clan), typeof(float) })]
        public static bool ChangeClanInfluenceActionApplyPrefix(Clan clan, float amount)
        {
            if (ServeAsAHirelingCampaignBehavior.ShouldSuppressHirelingInfluenceGain(clan, amount))
            {
                return false;
            }

            return true;
        }

        private static void ClearRoster(TroopRoster roster)
        {
            if (roster == null)
            {
                return;
            }

            var rosterElements = roster.GetTroopRoster().ToList();
            foreach (var rosterElement in rosterElements)
            {
                if (rosterElement.Number > 0)
                {
                    roster.AddToCounts(rosterElement.Character, -rosterElement.Number);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerEncounter), "DoApplyMapEventResults")]
        public static void DoApplyMapEventResultsPostfix()
        {
            if (!Hero.MainHero.IsEnlisted())
            {
                return;
            }

            ServeAsAHirelingCampaignBehavior.ClearCurrentHirelingLoot();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapEvent), "GetMemberRosterReceivingLootShare")]
        public static void GetMemberRosterPostfix(MapEvent __instance, TroopRoster __result)
        {
            if (__result == null)
            {
                return;
            }

            if (ServeAsAHirelingCampaignBehavior.ShouldSuppressTrackedHirelingBattleLoot(__instance))
            {
                ClearRoster(__result);
                PendingLootedTroopManager.ResetAllPendingState();
                return;
            }

            if (PendingLootedTroopManager.HasPendingModifications)
            {
                PendingLootedTroopManager.ApplyMemberModifications(__result);
                PendingLootedTroopManager.ConsumeMemberModifications();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapEvent), "GetPrisonerRosterReceivingLootShare")]
        public static void GetPrisonerRosterPostfix(MapEvent __instance, TroopRoster __result)
        {
            if (__result == null)
            {
                return;
            }

            if (ServeAsAHirelingCampaignBehavior.ShouldSuppressTrackedHirelingBattleLoot(__instance))
            {
                ClearRoster(__result);
                PendingLootedTroopManager.ResetAllPendingState();
                return;
            }

            if (PendingLootedTroopManager.HasPendingModifications)
            {
                PendingLootedTroopManager.ApplyPrisonerModifications(__result);
                PendingLootedTroopManager.ConsumePrisonerModifications();
            }
        }
    }
}
