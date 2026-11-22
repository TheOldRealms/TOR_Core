using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Event listeners and menu additions for Roguery perks that are not
    /// routed through a <c>GameModel</c>. Covers Raider secondary,
    /// BurntEarth secondary, Highwayman primary and secondary, Master Thief,
    /// Strider secondary, Parlay primary, and Diversion primary on two menus.
    /// </summary>
    public class RogueryPerkBehavior : CampaignBehaviorBase
    {
        /// <summary>
        /// Crime added on a neutral caravan ambush with witnesses.
        /// </summary>
        private const float NeutralCaravanAmbushCrimeDelta = 20f;

        /// <summary>
        /// Relation drop with the caravan owner on a neutral caravan ambush.
        /// </summary>
        private const int NeutralCaravanAmbushRelationDelta = -8;

        /// <summary>
        /// Latch for the Parlay dialog roll result.
        /// </summary>
        private bool _parlayLastRollSucceeded;

        private const string TryToGetAwayMenuId = "try_to_get_away";
        private const string DiversionOptionId = "diversion_use_prisoners";
        private const string DiversionDebriefMenuId = "tor_diversion_debrief";
        private const string BreakInMenuId = "break_in_menu";
        private const string BreakInDiversionOptionId = "diversion_use_prisoners_break_in";
        private const string BreakInDiversionDebriefMenuId = "tor_diversion_break_in_debrief";

        public override void RegisterEvents()
        {
            CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
            CampaignEvents.VillageLooted.AddNonSerializedListener(this, OnVillageLooted);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, OnDailyTickTown);
            CampaignEvents.OnCollectLootsItemsEvent.AddNonSerializedListener(this, OnCollectLootItems);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        /// <summary>Strider secondary. Grants the governor's clan a flat
        /// tournament-income bonus when a tournament is running.</summary>
        private void OnDailyTickTown(Town town)
        {
            if (town == null || town.Governor == null) return;
            if (!town.HasTournament) return;
            var income = new ExplainedNumber(0f);
            TOR_Core.CharacterDevelopment.TORSkillPerkHelper.ApplyPerkPassives(
                town.Governor, ref income,
                TOR_Core.CharacterDevelopment.SkillEffectType.GovernorTournamentIncome);
            int denars = (int)income.ResultNumber;
            if (denars <= 0) return;
            var governorClan = town.Governor.Clan;
            if (governorClan?.Leader != null)
                GiveGoldAction.ApplyBetweenCharacters(null, governorClan.Leader, denars);
        }

        /// <summary>Master Thief. Tops up the staged loot roster by one
        /// percent per Roguery point above 200.</summary>
        private void OnCollectLootItems(PartyBase winnerParty, ItemRoster gainedLoots)
        {
            if (winnerParty != PartyBase.MainParty || gainedLoots == null) return;
            if (Hero.MainHero == null
                || !Hero.MainHero.GetPerkValue(TORPerks.Roguery.MasterThief)) return;

            int roguery = Hero.MainHero.GetSkillValue(DefaultSkills.Roguery);
            int excess = roguery - 200;
            if (excess <= 0) return;

            float bonusFactor = excess * TORPerks.Roguery.MasterThief.PrimaryBonus;
            if (bonusFactor <= 0f) return;

            // Snapshot avoids modifying the roster while iterating.
            var items = gainedLoots.Where(x =>
                x.EquipmentElement.Item != null
                && !x.EquipmentElement.Item.NotMerchandise
                && !x.EquipmentElement.IsQuestItem
                && !x.EquipmentElement.Item.IsBannerItem).ToList();
            foreach (var element in items)
            {
                int extra = (int)(element.Amount * bonusFactor);
                if (extra > 0)
                    gainedLoots.AddToCounts(element.EquipmentElement, extra);
            }
        }

        /// <summary>Highwayman secondary. Grants daily XP to Master Ambusher
        /// troops in the main party while in stealth stance.</summary>
        private void OnDailyTick()
        {
            var main = MobileParty.MainParty;
            if (main?.MemberRoster == null) return;
            if (!main.HasPerk(TORPerks.Roguery.Highwayman)) return;

            var ambush = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (ambush == null || !ambush.IsPlayerInStealth) return;

            int xp = (int)TORPerks.Roguery.Highwayman.SecondaryBonus;
            if (xp <= 0) return;

            var roster = main.MemberRoster;
            for (int i = 0; i < roster.Count; i++)
            {
                var c = roster.GetCharacterAtIndex(i);
                if (c == null || c.IsHero) continue;
                if (roster.GetElementNumber(i) <= 0) continue;
                if (!c.HasAttribute(Utilities.TORConstants.CharacterAttributes.MASTER_AMBUSHER)) continue;
                roster.AddXpToTroopAtIndex(i, xp);
            }
        }

        /// <summary>
        /// Register perk menus, subscribe to ambush resolutions, and wipe vanilla Roguery perks.
        /// </summary>
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddMenus(starter);
            Utilities.TORCampaignEvents.Instance.AmbushResolved -= OnAmbushResolved;
            Utilities.TORCampaignEvents.Instance.AmbushResolved += OnAmbushResolved;
            SweepVanillaRogueryPerks();
        }

        /// <summary>
        /// Clear vanilla Roguery perks from every alive hero. Idempotent.
        /// </summary>
        private static void SweepVanillaRogueryPerks()
        {
            foreach (var hero in Hero.AllAliveHeroes)
            {
                PerkHelper.ClearPerksForSkill(hero, DefaultSkills.Roguery);
            }
        }

        /// <summary>Highwayman primary. Applies neutral-caravan ambush
        /// consequences. Perk clean-wipe suppresses both relation and crime.
        /// Perk with witnesses still drops relation. No perk applies both.</summary>
        private void OnAmbushResolved(object sender, Utilities.AmbushResolvedEventArgs args)
        {
            var record = args?.Record;
            if (record == null) return;
            if (record.Ambusher != MobileParty.MainParty) return;
            var victim = record.Victim;
            if (victim == null || !victim.IsCaravan) return;
            var victimFaction = victim.MapFaction;
            if (victimFaction == null) return;
            // Wartime caravans fall under normal defeated-party rules.
            if (FactionManager.IsAtWarAgainstFaction(MobileParty.MainParty.MapFaction, victimFaction)) return;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null || !model.IsSuccessfulStrike(record)) return;

            bool hasPerk = Hero.MainHero != null
                            && Hero.MainHero.GetPerkValue(TORPerks.Roguery.Highwayman);
            bool cleanWipe = record.Resolution == AmbushResolution.AmbusherTotalVictory;

            if (hasPerk && cleanWipe) return;

            var ownerClanLeader = victim.LeaderHero?.Clan?.Leader
                                   ?? victim.MapFaction?.Leader;
            if (ownerClanLeader != null && ownerClanLeader != Hero.MainHero)
            {
                ChangeRelationAction.ApplyPlayerRelation(ownerClanLeader,
                    NeutralCaravanAmbushRelationDelta, affectRelatives: false, showQuickNotification: true);
            }

            if (!hasPerk)
            {
                ChangeCrimeRatingAction.Apply(victimFaction, NeutralCaravanAmbushCrimeDelta);
            }
        }

        /// <summary>
        /// Registers the Diversion options and the Parlay dialog.
        /// </summary>
        private void AddMenus(CampaignGameStarter starter)
        {
            string diversionOption = TORTextHelper.GetTextForNative(
                "tor_perk_diversion_option_text",
                "Use your prisoners as a diversion. ({DIVERSION_PRISONER_COUNT} will be lost instead of your own men.)");

            starter.AddGameMenuOption(TryToGetAwayMenuId, DiversionOptionId,
                diversionOption,
                DiversionOnCondition, DiversionOnConsequence);

            starter.AddGameMenu(DiversionDebriefMenuId,
                TORTextHelper.GetTextForNative("tor_perk_diversion_debrief_text",
                    "Your prisoners stage a loud, panicked break and bolt into the trees. The pursuers wheel to run them down, and in the confusion you and your men slip away."),
                null, GameMenu.MenuOverlayType.Encounter);

            starter.AddGameMenuOption(DiversionDebriefMenuId, "tor_diversion_debrief_continue",
                "{=veWOovVv}Continue...",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Continue; return true; },
                FinishDiversionEncounter);

            // Siege break-in variant of the same Diversion mechanic.
            starter.AddGameMenuOption(BreakInMenuId, BreakInDiversionOptionId,
                diversionOption,
                BreakInDiversionOnCondition, BreakInDiversionOnConsequence);

            starter.AddGameMenu(BreakInDiversionDebriefMenuId,
                TORTextHelper.GetTextForNative("tor_perk_diversion_break_in_debrief_text",
                    "You set the prisoners loose at the gate. They scatter into the siege lines screaming for their lives. The defenders waste arrows on them while your column slips through intact."),
                null, GameMenu.MenuOverlayType.Encounter);

            starter.AddGameMenuOption(BreakInDiversionDebriefMenuId, "tor_diversion_break_in_debrief_continue",
                "{=veWOovVv}Continue...",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Continue; return true; },
                FinishBreakInDiversion);

            // Parlay primary. Peaceful-exit option on the bandit robbery dialog.
            starter.AddPlayerLine("tor_parlay_bandit_peaceful",
                "bandit_defender", "tor_parlay_bandit_roll",
                TORTextHelper.GetTextForNative("tor_perk_parlay_player_line",
                    "Let's see if we can resolve this peacefully."),
                ParlayOptionCondition, OnParlayAttempted);

            starter.AddDialogLine("tor_parlay_bandit_success",
                "tor_parlay_bandit_roll", "close_window",
                TORTextHelper.GetTextForNative("tor_perk_parlay_success_line",
                    "Hmph. Fine then, we'll find easier marks elsewhere. Move along, lads."),
                () => _parlayLastRollSucceeded,
                () => PlayerEncounter.LeaveEncounter = true);

            starter.AddDialogLine("tor_parlay_bandit_fail",
                "tor_parlay_bandit_roll", "bandit_defender",
                TORTextHelper.GetTextForNative("tor_perk_parlay_fail_line",
                    "Save your sweet words. Your coin or your blood, your choice."),
                () => !_parlayLastRollSucceeded, null);
        }

        /// <summary>
        /// Show the Parlay dialog line only when the main hero has the Parlay perk.
        /// </summary>
        private bool ParlayOptionCondition()
        {
            return Hero.MainHero != null
                && Hero.MainHero.GetPerkValue(TORPerks.Roguery.Parlay);
        }

        /// <summary>
        /// Roll the Parlay success check once when the player picks the peaceful-exit line.
        /// </summary>
        private void OnParlayAttempted()
        {
            _parlayLastRollSucceeded = MBRandom.RandomFloat < TORPerks.Roguery.Parlay.PrimaryBonus;
        }

        /// <summary>
        /// Show the Diversion escape option when the perk is active and prisoners are available.
        /// </summary>
        private bool DiversionOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Escape;
            if (Hero.MainHero == null) return false;
            if (!Hero.MainHero.GetPerkValue(TORPerks.Roguery.Diversion)) return false;
            if (MobileParty.MainParty == null || MobileParty.MainParty.IsCurrentlyAtSea) return false;
            if (PlayerEncounter.Current == null || PlayerEncounter.Battle == null) return false;

            var model = Campaign.Current?.Models?.TroopSacrificeModel as TORTroopSacrificeModel;
            if (model == null) return false;

            int count = model.GetNumberOfPrisonersSacrificedForDiversion(
                PlayerEncounter.Current.PlayerSide, PlayerEncounter.Battle);
            if (count <= 0) return false;

            MBTextManager.SetTextVariable("DIVERSION_PRISONER_COUNT", count);
            return true;
        }

        /// <summary>
        /// Spend prisoners to break contact and switch to the Diversion debrief menu.
        /// </summary>
        private void DiversionOnConsequence(MenuCallbackArgs args)
        {
            if (PlayerEncounter.Current == null || PlayerEncounter.Battle == null) return;
            var model = Campaign.Current?.Models?.TroopSacrificeModel as TORTroopSacrificeModel;
            if (model == null) return;

            int count = model.GetNumberOfPrisonersSacrificedForDiversion(
                PlayerEncounter.Current.PlayerSide, PlayerEncounter.Battle);
            if (count <= 0) return;

            model.ApplyPrisonerDiversion(count);
            CampaignEventDispatcher.Instance.OnPlayerDesertedBattle(0);
            if (MobileParty.MainParty.BesiegerCamp != null)
            {
                MobileParty.MainParty.BesiegerCamp = null;
            }

            if (Campaign.Current.CurrentMenuContext != null)
                GameMenu.SwitchToMenu(DiversionDebriefMenuId);
            else
                GameMenu.ActivateGameMenu(DiversionDebriefMenuId);
        }

        /// <summary>Tears the encounter down after a Diversion success.
        /// Mirrors vanilla <c>game_menu_try_to_get_away_end</c>.</summary>
        private void FinishDiversionEncounter(MenuCallbackArgs args)
        {
            if (PlayerEncounter.Current == null || PlayerEncounter.Battle == null) return;
            foreach (var item in PlayerEncounter.Battle.PartiesOnSide(BattleSideEnum.Defender))
            {
                var mobileParty = item.Party?.MobileParty;
                if (mobileParty == null) continue;
                if (mobileParty.BesiegerCamp != null) mobileParty.BesiegerCamp = null;
                if (mobileParty.CurrentSettlement != null && item.Party == PartyBase.MainParty)
                    LeaveSettlementAction.ApplyForParty(mobileParty);
            }
            PlayerEncounter.Battle.DiplomaticallyFinished = true;
            PlayerEncounter.ProtectPlayerSide();
            PlayerEncounter.Finish();
        }

        /// <summary>
        /// Show the Diversion siege break-in option when the perk is active and prisoners are available.
        /// </summary>
        private bool BreakInDiversionOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Escape;
            if (Hero.MainHero == null) return false;
            if (!Hero.MainHero.GetPerkValue(TORPerks.Roguery.Diversion)) return false;
            if (MobileParty.MainParty == null) return false;
            var siegeEvent = Settlement.CurrentSettlement?.SiegeEvent;
            if (siegeEvent == null) return false;

            var model = Campaign.Current?.Models?.TroopSacrificeModel as TORTroopSacrificeModel;
            if (model == null) return false;

            int count = model.GetNumberOfPrisonersSacrificedForBreakInSiege(
                MobileParty.MainParty, siegeEvent);
            if (count <= 0) return false;

            MBTextManager.SetTextVariable("DIVERSION_PRISONER_COUNT", count);
            return true;
        }

        /// <summary>
        /// Spend prisoners to force a siege break-in and switch to the break-in debrief menu.
        /// </summary>
        private void BreakInDiversionOnConsequence(MenuCallbackArgs args)
        {
            var siegeEvent = Settlement.CurrentSettlement?.SiegeEvent;
            if (siegeEvent == null) return;
            var model = Campaign.Current?.Models?.TroopSacrificeModel as TORTroopSacrificeModel;
            if (model == null) return;

            int count = model.GetNumberOfPrisonersSacrificedForBreakInSiege(
                MobileParty.MainParty, siegeEvent);
            if (count <= 0) return;

            model.ApplyPrisonerDiversion(count);
            GameMenu.SwitchToMenu(BreakInDiversionDebriefMenuId);
        }

        /// <summary>Finishes the siege break-in after a Diversion success.
        /// Mirrors vanilla <c>break_in_debrief_continue_on_consequence</c>.</summary>
        private void FinishBreakInDiversion(MenuCallbackArgs args)
        {
            if (Hero.MainHero.CurrentSettlement == null)
                PlayerEncounter.EnterSettlement();
            if (PlayerSiege.PlayerSiegeEvent == null)
                PlayerSiege.StartPlayerSiege(BattleSideEnum.Defender);
            if (Hero.MainHero.CurrentSettlement?.Party?.MapEvent != null)
            {
                GameMenu.SwitchToMenu("join_encounter");
            }
            else
            {
                PlayerEncounter.RestartPlayerEncounter(PartyBase.MainParty,
                    PlayerEncounter.EncounterSettlement.SiegeEvent.BesiegerCamp.LeaderParty.Party,
                    false);
                PlayerSiege.StartSiegePreparation();
            }
            if (MobileParty.MainParty.IsCurrentlyAtSea)
                MobileParty.MainParty.DisembarkToPosition(Settlement.CurrentSettlement.GatePosition);
        }

        /// <summary>BurntEarth secondary. Softens the notable relation drop
        /// after the player raids a village.</summary>
        private void OnVillageLooted(Village village)
        {
            if (village == null) return;
            var main = MobileParty.MainParty;
            if (main?.LeaderHero != Hero.MainHero) return;
            if (village.Settlement == null || main.Position.DistanceSquared(
                    village.Settlement.Position) > 1f) return;
            if (!main.HasPerk(TORPerks.Roguery.BurntEarth)) return;

            // Vanilla drop is -2. Net target with perk is -1.
            int offset = 1;
            foreach (var notable in village.Settlement.Notables)
            {
                if (notable == null || !notable.IsAlive) continue;
                ChangeRelationAction.ApplyPlayerRelation(notable, offset, affectRelatives: false, showQuickNotification: false);
            }
        }

        /// <summary>Raider secondary. Grants Tactics XP to the raider's
        /// quartermaster when a raid completes in their favor.</summary>
        private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent component)
        {
            var mapEvent = component?.MapEvent;
            if (mapEvent == null) return;
            if (winnerSide != BattleSideEnum.Attacker) return;
            var attackerParty = mapEvent.AttackerSide?.LeaderParty?.MobileParty;
            if (attackerParty == null) return;
            if (!attackerParty.HasPerk(TORPerks.Roguery.Raider, checkSecondaryRole: true)) return;
            var quartermaster = attackerParty.EffectiveQuartermaster;
            if (quartermaster == null) return;
            quartermaster.AddSkillXp(DefaultSkills.Tactics, TORPerks.Roguery.Raider.SecondaryBonus);
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
