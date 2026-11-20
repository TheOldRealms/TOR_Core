using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Non-model Roguery-perk wiring: event listeners for perks that
    /// fire on a campaign event (Raider secondary, BurntEarth secondary,
    /// Highwayman primary), and menu additions for perks that attach new
    /// options to vanilla menus (Diversion primary on
    /// <c>try_to_get_away</c> and <c>break_in_menu</c>).
    /// </summary>
    public class RogueryPerkEventBehavior : CampaignBehaviorBase
    {
        /// <summary>Crime-rating hit when the player ambushes a neutral
        /// caravan and witnesses survive. Repeated ambushes accumulate
        /// toward the vanilla <c>CrimeModel.DeclareWarCrimeRatingThreshold</c>
        /// and eventually cascade to war declaration. Flat per-ambush
        /// value; roster-size scaling would read better but vanilla's
        /// own caravan-attack path uses flat deltas.</summary>
        private const float NeutralCaravanAmbushCrimeDelta = 20f;

        /// <summary>Relation drop with the caravan owner's clan leader
        /// when a neutral caravan ambush leaves witnesses alive. Fires
        /// on both perk-held and non-perk ambushes; only the clean-wipe
        /// Highwayman path suppresses it.</summary>
        private const int NeutralCaravanAmbushRelationDelta = -8;

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
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddMenus(starter);
            Utilities.TORCampaignEvents.Instance.AmbushResolved -= OnAmbushResolved;
            Utilities.TORCampaignEvents.Instance.AmbushResolved += OnAmbushResolved;
        }

        /// <summary>Highwayman primary (Roguery 175) dispatch gate. Fires
        /// on every ambush resolution. When the player ambushed a neutral
        /// (not-at-war) caravan, this applies the crime-rating + relation
        /// consequences that vanilla doesn't dispatch on its own — that
        /// crime path auto-cascades to a war declaration via
        /// <c>CrimeModel.DeclareWarCrimeRatingThreshold</c>, so repeated
        /// non-perk ambushes eventually trigger war. With the perk held,
        /// crime is suppressed entirely (no war cascade); relation still
        /// drops if witnesses survived. On a clean wipe
        /// (<c>AmbusherTotalVictory</c>) + perk held, nothing fires —
        /// "no witnesses, no evidence".</summary>
        private void OnAmbushResolved(object sender, Utilities.AmbushResolvedEventArgs args)
        {
            var record = args?.Record;
            if (record == null) return;
            if (record.Ambusher != MobileParty.MainParty) return;
            var victim = record.Victim;
            if (victim == null || !victim.IsCaravan) return;
            var victimFaction = victim.MapFaction;
            if (victimFaction == null) return;
            // At-war caravan ambushes fall under existing wartime rules —
            // no crime, no extra relation hit beyond vanilla's defeated-
            // party flow. Only neutral caravans trigger this path.
            if (FactionManager.IsAtWarAgainstFaction(MobileParty.MainParty.MapFaction, victimFaction)) return;
            // The ambusher-repelled / stalemate branches don't count as
            // an ambush landing — nobody got mugged, nobody saw you doing
            // the mugging. Mirror the IsSuccessfulStrike classifier.
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null || !model.IsSuccessfulStrike(record)) return;

            bool hasPerk = Hero.MainHero != null
                            && Hero.MainHero.GetPerkValue(TORPerks.Roguery.Highwayman);
            bool cleanWipe = record.Resolution == AmbushResolution.AmbusherTotalVictory;

            // Clean wipe + perk → no witnesses, no evidence, nothing fires.
            if (hasPerk && cleanWipe) return;

            // Relation always drops when witnesses survive — the owning
            // clan knows who did it. Hits the clan leader like every
            // other player-relation path.
            var ownerClanLeader = victim.LeaderHero?.Clan?.Leader
                                   ?? victim.MapFaction?.Leader;
            if (ownerClanLeader != null && ownerClanLeader != Hero.MainHero)
            {
                ChangeRelationAction.ApplyPlayerRelation(ownerClanLeader,
                    NeutralCaravanAmbushRelationDelta, affectRelatives: false, showQuickNotification: true);
            }

            // Crime fires for non-perk ambushes only. The perk lets the
            // player operate off-the-books: faction leadership doesn't
            // pin the ambush on them, so no crime rating accrues and
            // the war-declaration cascade never triggers.
            if (!hasPerk)
            {
                ChangeCrimeRatingAction.Apply(victimFaction, NeutralCaravanAmbushCrimeDelta);
            }
        }

        /// <summary>Diversion primary (Roguery 125): sibling option on
        /// vanilla's <c>try_to_get_away</c> encounter menu that spends
        /// prisoners instead of regulars. Fiction is a loud, panicked
        /// prisoner break drawing pursuers off the player's trail.
        /// Visible on land, with the perk, and when prisoner count
        /// covers <see cref="TORTroopSacrificeModel.GetNumberOfPrisonersSacrificedForDiversion"/>.</summary>
        private void AddMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption(TryToGetAwayMenuId, DiversionOptionId,
                "{=!}Use your prisoners as a diversion. ({DIVERSION_PRISONER_COUNT} will be lost instead of your own men.)",
                DiversionOnCondition, DiversionOnConsequence);

            starter.AddGameMenu(DiversionDebriefMenuId,
                "{=!}Your prisoners stage a loud, panicked break and bolt into the trees. The pursuers wheel to run them down, and in the confusion you and your men slip away.",
                null, GameMenu.MenuOverlayType.Encounter);

            starter.AddGameMenuOption(DiversionDebriefMenuId, "tor_diversion_debrief_continue",
                "{=veWOovVv}Continue...",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Continue; return true; },
                FinishDiversionEncounter);

            // Siege-break-in variant: same Diversion mechanic applied to
            // the break_in_menu that fires when the player assaults a
            // besieged settlement. Prisoners substitute for the vanilla
            // troop sacrifice; debrief uses our own flavor text.
            starter.AddGameMenuOption(BreakInMenuId, BreakInDiversionOptionId,
                "{=!}Use your prisoners as a diversion. ({DIVERSION_PRISONER_COUNT} will be lost instead of your own men.)",
                BreakInDiversionOnCondition, BreakInDiversionOnConsequence);

            starter.AddGameMenu(BreakInDiversionDebriefMenuId,
                "{=!}You set the prisoners loose at the gate — they scatter into the siege lines screaming for their lives. The defenders waste arrows on them while your column slips through intact.",
                null, GameMenu.MenuOverlayType.Encounter);

            starter.AddGameMenuOption(BreakInDiversionDebriefMenuId, "tor_diversion_break_in_debrief_continue",
                "{=veWOovVv}Continue...",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Continue; return true; },
                FinishBreakInDiversion);
        }

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

        /// <summary>Mirrors vanilla's <c>game_menu_try_to_get_away_end</c>
        /// so our diversion debrief tears the encounter down the same way.</summary>
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

        /// <summary>Mirrors vanilla's <c>break_in_debrief_continue_on_consequence</c>:
        /// enter the settlement if we're not already, start the player siege on
        /// the defender side (we broke in to defend), branch on whether there's
        /// an active map event there, and disembark if we came in by sea.</summary>
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

        /// <summary>BurntEarth secondary: "Village notables lose 75%
        /// less relation with you after their village is raided."
        /// Fires after vanilla's raid-complete relation drop; applies
        /// an offsetting positive nudge equal to
        /// <c>-BurntEarth.PrimaryBonus</c> (0.75) × the vanilla drop
        /// magnitude. Vanilla drops notable relation by -2 per raid
        /// (empirically consistent across vanilla raid handlers), so
        /// a +1.5 nudge (floored to +1 for integer relation) leaves
        /// a net -1 instead of -2.</summary>
        private void OnVillageLooted(Village village)
        {
            if (village == null) return;
            var main = MobileParty.MainParty;
            if (main?.LeaderHero != Hero.MainHero) return;
            // Only fire if the main party actually looted this village
            // (closest raider in range).
            if (village.Settlement == null || main.Position.DistanceSquared(
                    village.Settlement.Position) > 1f) return;
            if (!main.HasPerk(TORPerks.Roguery.BurntEarth)) return;

            // Vanilla notable-relation drop on raid is -2. Perk
            // reduces that by 75% → net -0.5, floored to integer
            // means +1 compensating nudge leaves -1 remaining.
            int offset = 1;
            foreach (var notable in village.Settlement.Notables)
            {
                if (notable == null || !notable.IsAlive) continue;
                ChangeRelationAction.ApplyPlayerRelation(notable, offset, affectRelatives: false, showQuickNotification: false);
            }
        }

        private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent component)
        {
            var mapEvent = component?.MapEvent;
            if (mapEvent == null) return;
            // Only credit the aggressor side — the raider, not the defenders.
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
