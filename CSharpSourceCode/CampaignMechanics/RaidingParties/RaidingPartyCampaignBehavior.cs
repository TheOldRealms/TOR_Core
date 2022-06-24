using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.CampaignMechanics.TORCustomSettlement;

namespace TOR_Core.CampaignMechanics.RaidingParties
{
    public class RaidingPartyCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
            CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, HourlyTickPartyAI);
        }

        private void DailyTickSettlement(Settlement settlement)
        {
            if(settlement.SettlementComponent is TORCustomSettlementComponent)
            {
                var component = settlement.SettlementComponent as TORCustomSettlementComponent;
                if (component.SettlementType.IsRaidingPartySpawner && component.RaidingParties.Count < 5) component.SettlementType.SpawnNewParty();
            }
        }
        private void HourlyTickPartyAI(MobileParty party, PartyThinkParams partyThinkParams)
        {
            if(party.PartyComponent is IRaidingParty)
            {
                var component = (IRaidingParty)party.PartyComponent;
                component.SetBehavior(party, partyThinkParams);
                party.Ai.SetDoNotMakeNewDecisions(false);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            //throw new NotImplementedException();
        }
    }
}
