using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Roster;

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
            // All battle-driven objective types feed off the same vanilla event;
            // the switch inside OnBattleEnd picks the right counter. Capture
            // bounties are push-driven via OnLodgePrisonersDonated and don't
            // need the subscription, but subscribing uniformly is cheaper than
            // branching on type at Register time.
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnBattleEnd);
        }

        protected override void OnFinalize()
        {
            CampaignEvents.OnPlayerBattleEndEvent.ClearListeners(this);
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
                // CaptureTroops is handled via OnLodgePrisonersDonated.
                // ClearHideout will land here as a new case (Phase C step 4).
            }
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
