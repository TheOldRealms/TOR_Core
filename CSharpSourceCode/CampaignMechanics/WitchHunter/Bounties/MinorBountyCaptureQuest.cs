using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;

namespace TOR_Core.CampaignMechanics.WitchHunter.Bounties
{
    /// <summary>
    /// Minor Bounty quest for <see cref="BountyObjectiveType.CaptureTroops"/>:
    /// the player captures matching enemies on the field, then donates them at
    /// any Witch Hunter Lodge via the Prisoners turn-in. Each matching donated
    /// troop ticks the quest's progress.
    ///
    /// <para>No <see cref="RegisterEvents"/> hook — progress is pushed from
    /// <see cref="WitchHunterInquisitionBehavior.OnPrisonersSold"/> via the base
    /// class's <see cref="MinorBountyQuestBase.OnLodgePrisonersDonated"/>
    /// extension point. This avoids racing against the behavior's suppression-
    /// flag teardown that happens inside the same event's finally block.</para>
    /// </summary>
    public class MinorBountyCaptureQuest : MinorBountyQuestBase
    {
        public MinorBountyCaptureQuest(
            string questId, Hero questGiver, CampaignTime duration,
            string templateId, int targetCount, int rewardGold, int rewardPrestige)
            : base(questId, questGiver, duration, templateId, targetCount, rewardGold, rewardPrestige) { }

        // Push model — nothing to subscribe to.
        protected override void RegisterEvents() { }
        protected override void OnFinalize() { }

        public override void OnLodgePrisonersDonated(FlattenedTroopRoster roster)
        {
            var template = Template;
            if (template.IsEmpty || template.TargetMatches == null) return;
            if (roster == null) return;

            int captured = 0;
            foreach (var elem in roster)
            {
                // Hero prisoners are released by the lodge's handler (ransom), not
                // counted toward bounty progress — the design is about troop-level
                // captures, not named enemies.
                if (elem.Troop == null || elem.Troop.IsHero) continue;
                if (template.TargetMatches(elem.Troop)) captured++;
            }
            BumpProgressBy(captured);
        }
    }
}
