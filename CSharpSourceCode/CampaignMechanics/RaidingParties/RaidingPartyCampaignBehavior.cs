using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.LinQuick;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.RaidingParties
{
    public class RaidingPartyCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
            CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, HourlyTickPartyAI);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, Tick);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyPartyTick);
        }

        //newly created parties within the same game session only recieve this event, not HourlyTickPartyAI
        private void HourlyPartyTick(MobileParty party)
        {
            if (party.IsRaidingParty())
            {
                var component = (IRaidingParty)party.PartyComponent;
                component.SetBehavior(party, new PartyThinkParams(party));
                party.Ai.SetDoNotMakeNewDecisions(false);
            }
        }

        //destroy corrupted parties, not sure if this is needed anymore
        private void Tick(float dt)
        {
            var list = MobileParty.All.WhereQ(x => x.IsRaidingParty() && x.ActualClan == null).ToList();
            foreach(var item in list)
            {
                DestroyPartyAction.Apply(null, item);
            }
        }

        private void DailyTickSettlement(Settlement settlement)
        {
            if(settlement.SettlementComponent is TORCustomSettlementComponent)
            {
                var component = settlement.SettlementComponent as TORCustomSettlementComponent;
                if (component.SettlementType.IsRaidingPartySpawner && component.RaidingPartyCount < 5) component.SettlementType.SpawnNewParty();
            }
        }
        //parties only recieve this tick after loading a savegame...
        private void HourlyTickPartyAI(MobileParty party, PartyThinkParams partyThinkParams)
        {
            if(party.IsRaidingParty())
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
