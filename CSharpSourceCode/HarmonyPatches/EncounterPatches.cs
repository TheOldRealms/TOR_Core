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
        private static readonly FieldInfo PlayerEncounterStateHandledField = AccessTools.Field(typeof(PlayerEncounter), "_stateHandled");
        internal static bool ShouldBypassDeadHirelingEncounter()
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
        [HarmonyPatch(typeof(PlayerTownVisitCampaignBehavior), "game_menu_settlement_leave_on_consequence")]
        public static bool SettlementLeaveConsequencePrefix(MenuCallbackArgs args)
        {
            var behavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            if (behavior == null || !behavior.IsEnlisted())
            {
                return true;
            }

            ServeAsAHirelingCampaignBehavior.TryLeaveSettlementToHirelingMenu(false);
            Campaign.Current.SaveHandler.SignalAutoSave();
            return false;
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
                if (currentEncounter != null)
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
            var finalizedHirelingVictory =
                ServeAsAHirelingCampaignBehavior.TryFinalizeTrackedHirelingVictory()
                || ServeAsAHirelingCampaignBehavior.TryFinalizeTrackedHirelingVictoryFromCaptureHeroes()
                || ServeAsAHirelingCampaignBehavior.TryFinalizeCurrentWinningFieldHirelingBattle();

            if (finalizedHirelingVictory)
            {
                PlayerEncounterStateHandledField.SetValue(__instance, true);
                return false;
            }

            PreparePrisonerLootBeforePlayerCapture(__instance);
            return true;
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerEncounter), "DoFreeOrCapturePrisonerHeroes")]
        public static void DoFreeOrCapturePrisonerHeroesPrefix(PlayerEncounter __instance)
        {
            PrepareMemberLootBeforePlayerPrisonerChoice(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerEncounter), "DoLootMembersAndPrisonersOfParty")]
        public static void DoLootMembersAndPrisonersOfPartyPrefix(PlayerEncounter __instance)
        {
            PrepareMemberLootBeforePlayerPrisonerChoice(__instance);
            PreparePrisonerLootBeforePlayerCapture(__instance);
        }

        private static void PrepareMemberLootBeforePlayerPrisonerChoice(PlayerEncounter encounter)
        {
            var mapEvent = PlayerEncounter.Battle;
            if (ServeAsAHirelingCampaignBehavior.ShouldSuppressTrackedHirelingBattleLoot(mapEvent))
            {
                ClearRoster(encounter.RosterToReceiveLootMembers);
                ClearRoster(encounter.RosterToReceiveLootPrisoners);
                encounter.RosterToReceiveLootItems.Clear();
                PendingLootedTroopManager.ResetAllPendingState();
                return;
            }

            if (PendingLootedTroopManager.HasPendingModifications)
            {
                PendingLootedTroopManager.ApplyMemberModifications(encounter.RosterToReceiveLootMembers);
                PendingLootedTroopManager.ConsumeMemberModifications();
            }
        }

        private static void PreparePrisonerLootBeforePlayerCapture(PlayerEncounter encounter)
        {
            var mapEvent = PlayerEncounter.Battle;
            if (ServeAsAHirelingCampaignBehavior.ShouldSuppressTrackedHirelingBattleLoot(mapEvent))
            {
                ClearRoster(encounter.RosterToReceiveLootMembers);
                ClearRoster(encounter.RosterToReceiveLootPrisoners);
                encounter.RosterToReceiveLootItems.Clear();
                PendingLootedTroopManager.ResetAllPendingState();
                return;
            }

            if (PendingLootedTroopManager.HasPendingModifications)
            {
                PendingLootedTroopManager.ApplyPrisonerModifications(encounter.RosterToReceiveLootPrisoners);
                PendingLootedTroopManager.ConsumePrisonerModifications();
            }
        }
    }
    
    [HarmonyPatch]
    [HarmonyPatchCategory("LatePatches")]
    public static class EncounterGameMenuBehaviorPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EncounterGameMenuBehavior), "UpdateVillageHostileActionEncounter")]
        public static bool UpdateVillageHostileActionEncounterPrefix(MenuCallbackArgs args)
        {
            if (!Hero.MainHero.IsEnlisted() || ServeAsAHirelingCampaignBehavior.IsStartingBattle)
            {
                return true;
            }

            var battle = PlayerEncounter.Battle;
            if (battle?.MapEventSettlement?.IsVillage != true)
            {
                return true;
            }

            var playerSide = battle.PlayerSide;
            var playerHasValidBattleSide =
                playerSide == BattleSideEnum.Attacker
                || playerSide == BattleSideEnum.Defender;

            if (playerHasValidBattleSide)
            {
                return true;
            }

            var hirelingBehavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            var enlistingLordParty = hirelingBehavior?.EnlistingLord?.PartyBelongedTo;
            var enlistingLordSide = enlistingLordParty?.MapEventSide?.MissionSide;

            var lordIsInThisBattle =
                enlistingLordParty?.MapEvent == battle
                && enlistingLordSide != null;

            var battleIsJoinableForHirelingLord =
                lordIsInThisBattle
                && ServeAsAHirelingCampaignBehavior.IsJoinableHirelingMapEvent(battle, enlistingLordSide);

            var isVillageHostileAction =
                battle.IsRaid
                || battle.IsForcingSupplies
                || battle.IsForcingVolunteers;

            var lordIsOnVillageHostileActionSide =
                isVillageHostileAction
                && enlistingLordParty != null
                && battle.AttackerSide.Parties.Any(attackerParty => attackerParty.Party == enlistingLordParty.Party);

            var hasExternalRaidInterrupter = false;
            if (isVillageHostileAction)
            {
                var defaultSettlementDefenders = battle.MapEventSettlement.GetInvolvedPartiesForEventType(battle.EventType);
                hasExternalRaidInterrupter = battle.DefenderSide.Parties.Any(defenderParty => !defaultSettlementDefenders.Contains(defenderParty.Party));
            }

            var shouldKeepLordRaidOutOfBattleMenu =
                lordIsOnVillageHostileActionSide
                && !hasExternalRaidInterrupter;

            var shouldOpenHirelingBattleMenu =
                battleIsJoinableForHirelingLord
                && !shouldKeepLordRaidOutOfBattleMenu;

            ServeAsAHirelingCampaignBehavior.ClearCurrentHirelingLoot();

            if (PlayerEncounter.LocationEncounter != null)
            {
                PlayerEncounter.LocationEncounter = null;
            }

            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.Finish(false);
            }

            var mainParty = MobileParty.MainParty;
            mainParty.MapEventSide = null;
            mainParty.BesiegerCamp = null;
            mainParty.CurrentSettlement = null;

            if (shouldOpenHirelingBattleMenu)
            {
                GameMenu.SwitchToMenu("hireling_battle_menu");
                ServeAsAHirelingCampaignBehavior.MarkHirelingWaitMenuShown();
                return false;
            }

            GameMenu.SwitchToMenu("hireling_menu");
            ServeAsAHirelingCampaignBehavior.MarkHirelingWaitMenuShown();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EncounterGameMenuBehavior), "game_menu_encounter_on_init")]
        public static bool EncounterGameMenuOnInitPrefix(MenuCallbackArgs args)
        {
            if (!EncounterPatches.ShouldBypassDeadHirelingEncounter())
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
    }
}
