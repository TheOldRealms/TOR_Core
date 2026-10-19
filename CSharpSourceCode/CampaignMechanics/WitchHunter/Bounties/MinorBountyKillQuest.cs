using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;

namespace TOR_Core.CampaignMechanics.WitchHunter.Bounties
{
    /// <summary>
    /// Minor Bounty quest for <see cref="BountyObjectiveType.KillTroops"/>: count
    /// enemy-troop kills in winning battles whose troop matches the template's
    /// <see cref="BountyTemplate.TargetMatches"/> predicate. The current Phase B
    /// behavior lives here; shared quest machinery is in
    /// <see cref="MinorBountyQuestBase"/>.
    /// </summary>
    public class MinorBountyKillQuest : MinorBountyQuestBase
    {
        public MinorBountyKillQuest(
            string questId, Hero questGiver, CampaignTime duration,
            string templateId, int targetCount, int rewardGold, int rewardPrestige)
            : base(questId, questGiver, duration, templateId, targetCount, rewardGold, rewardPrestige) { }

        protected override void RegisterEvents()
        {
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
        }

        protected override void OnFinalize()
        {
            CampaignEvents.OnPlayerBattleEndEvent.ClearListeners(this);
        }

        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            if (mapEvent == null || !mapEvent.IsPlayerMapEvent) return;
            if (!mapEvent.HasWinner || mapEvent.WinningSide != mapEvent.PlayerSide) return;

            var template = Template;
            if (template.IsEmpty || template.TargetMatches == null) return;

            var defeatedSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
            if (defeatedSide == null) return;

            int killed = 0;
            foreach (var party in defeatedSide.Parties)
            {
                foreach (var elem in party.Troops.Where(t => t.IsKilled))
                {
                    if (template.TargetMatches(elem.Troop)) killed++;
                }
            }
            BumpProgressBy(killed);
        }
    }
}
