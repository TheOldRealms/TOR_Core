using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Non-model Roguery-perk wiring: event listeners for perks that
    /// fire on a campaign event (Raider secondary, BurntEarth secondary),
    /// and menu additions for perks that attach new options to vanilla
    /// menus (Diversion primary on <c>try_to_get_away</c>).
    /// </summary>
    public class RogueryPerkEventBehavior : CampaignBehaviorBase
    {
        private const string TryToGetAwayMenuId = "try_to_get_away";
        private const string DiversionOptionId = "diversion_use_prisoners";
        private const string DiversionDebriefMenuId = "tor_diversion_debrief";

        public override void RegisterEvents()
        {
            CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
            CampaignEvents.VillageLooted.AddNonSerializedListener(this, OnVillageLooted);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddMenus);
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
