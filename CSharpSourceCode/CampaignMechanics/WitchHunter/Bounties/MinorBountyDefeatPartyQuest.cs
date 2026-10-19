using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;

namespace TOR_Core.CampaignMechanics.WitchHunter.Bounties
{
    /// <summary>
    /// Minor Bounty quest for <see cref="BountyObjectiveType.DefeatParties"/>:
    /// count defeated enemy parties whose composition includes any troop
    /// matching the template's <see cref="BountyTemplate.TargetMatches"/>
    /// predicate. One defeated party ticks progress by 1, regardless of how
    /// many matching troops it held.
    ///
    /// <para>Reuses the same vanilla event as <see cref="MinorBountyKillQuest"/>
    /// (<c>OnPlayerBattleEndEvent</c>) but counts parties instead of individual
    /// troops. Natural fit for "defeat N bands of X" spreadsheet entries.</para>
    /// </summary>
    public class MinorBountyDefeatPartyQuest : MinorBountyQuestBase
    {
        public MinorBountyDefeatPartyQuest(
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

            int defeatedParties = 0;
            foreach (var party in defeatedSide.Parties)
            {
                // Count the party if any of its troops match. Garrisons / village
                // militias technically count here if they contain a matching troop
                // — acceptable for v1; could tighten by filtering on
                // party.MobileParty if specific-raider-party semantics matter.
                if (party.Troops.Any(t => template.TargetMatches(t.Troop)))
                    defeatedParties++;
            }
            BumpProgressBy(defeatedParties);
        }
    }
}
