using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TOR_Core.CampaignMechanics.WitchHunter.Bounties
{
    /// <summary>
    /// Unified Minor Bounty quest covering all <see cref="BountyObjectiveType"/>s.
    /// The only part that varies per objective is <em>which event signals
    /// progress</em> and <em>how to count a match</em>; everything else (state,
    /// title, timeout, dialog, turn-in flow, reward) lives in the base. One
    /// switch inside <see cref="OnBattleEnd"/> + the push-model
    /// <see cref="OnLodgePrisonersDonated"/> override cover all four objective
    /// shapes, so adding a new objective type is a single <c>case</c> here plus
    /// an enum value, not a new class / save id / csproj entry.
    /// </summary>
    public class MinorBountyQuest : MinorBountyQuestBase
    {
        public MinorBountyQuest(
            string questId, Hero questGiver, CampaignTime duration,
            string templateId, int targetCount, int rewardGold, int rewardPrestige)
            : base(questId, questGiver, duration, templateId, targetCount, rewardGold, rewardPrestige) { }

        protected override void RegisterEvents()
        {
            // Two vanilla events cover all objective types. OnPlayerBattleEndEvent
            // handles Kill / DefeatParties — the switch inside OnBattleEnd picks
            // the counter. OnHideoutBattleCompletedEvent fires in addition for
            // hideout wins (vanilla also fires the general battle event, so
            // Kill/DefeatParties still count kills in a hideout naturally).
            // Capture is push-driven via OnLodgePrisonersDonated, no subscription.
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnBattleEnd);
            CampaignEvents.OnHideoutBattleCompletedEvent.AddNonSerializedListener(this, OnHideoutCompleted);
        }

        protected override void OnFinalize()
        {
            CampaignEvents.OnPlayerBattleEndEvent.ClearListeners(this);
            CampaignEvents.OnHideoutBattleCompletedEvent.ClearListeners(this);
        }

        private void OnBattleEnd(MapEvent mapEvent)
        {
            if (mapEvent == null || !mapEvent.IsPlayerMapEvent) return;
            if (!mapEvent.HasWinner || mapEvent.WinningSide != mapEvent.PlayerSide) return;

            var template = Template;
            if (template.IsEmpty || template.TargetMatches == null) return;

            var defeatedSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
            if (defeatedSide == null) return;

            switch (template.ObjectiveType)
            {
                case BountyObjectiveType.KillTroops:
                    BumpProgressBy(CountMatchingKills(defeatedSide, template));
                    break;
                case BountyObjectiveType.DefeatParties:
                    BumpProgressBy(CountMatchingParties(defeatedSide, template));
                    break;
                // CaptureTroops handled via OnLodgePrisonersDonated.
                // ClearHideout handled via OnHideoutCompleted — fires on its own event.
            }
        }

        private void OnHideoutCompleted(BattleSideEnum battleSide, HideoutEventComponent eventComponent, HideoutEventComponent.HideoutBattleEndState endState)
        {
            var template = Template;
            if (template.ObjectiveType != BountyObjectiveType.ClearHideout) return;
            if (template.IsEmpty || template.TargetMatches == null) return;

            var mapEvent = eventComponent?.MapEvent;
            if (mapEvent == null) return;
            if (!mapEvent.HasWinner || mapEvent.WinningSide != mapEvent.PlayerSide) return;

            // Hideout counts as 1 if any troop in its garrison matched the template.
            // "Chaos cultist hideouts" are hideouts whose defenders include cultists,
            // and so on for other categories; the troop-level predicate transfers
            // cleanly because the hideout's garrison composition names the hideout
            // (bandit hideouts contain bandits, cult lairs contain cultists, etc.).
            var defeatedSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
            if (defeatedSide == null) return;
            bool matched = defeatedSide.Parties
                .Any(p => p.Troops.Any(t => template.TargetMatches(t.Troop)));
            if (matched) BumpProgressBy(1);
        }

        public override void OnLodgePrisonersDonated(FlattenedTroopRoster roster)
        {
            var template = Template;
            if (template.ObjectiveType != BountyObjectiveType.CaptureTroops) return;
            if (template.IsEmpty || template.TargetMatches == null) return;
            if (roster == null) return;

            int captured = 0;
            foreach (var elem in roster)
            {
                // Hero prisoners are released by the lodge's handler (ransom) and
                // don't count toward troop-level capture bounties.
                if (elem.Troop == null || elem.Troop.IsHero) continue;
                if (template.TargetMatches(elem.Troop)) captured++;
            }
            BumpProgressBy(captured);
        }

        // ---- Per-objective counting helpers

        private static int CountMatchingKills(MapEventSide defeatedSide, BountyTemplate template)
        {
            int killed = 0;
            foreach (var party in defeatedSide.Parties)
                foreach (var elem in party.Troops.Where(t => t.IsKilled))
                    if (template.TargetMatches(elem.Troop)) killed++;
            return killed;
        }

        private static int CountMatchingParties(MapEventSide defeatedSide, BountyTemplate template)
        {
            // Garrisons / village militias technically count if they contain a
            // matching troop — acceptable for v1; tighten by filtering on
            // party.MobileParty if specific-raider-party semantics matter later.
            int defeatedParties = 0;
            foreach (var party in defeatedSide.Parties)
                if (party.Troops.Any(t => template.TargetMatches(t.Troop)))
                    defeatedParties++;
            return defeatedParties;
        }
    }
}
