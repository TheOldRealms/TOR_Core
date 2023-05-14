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
            CampaignEvents.TickEvent.AddNonSerializedListener(this, Tick);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyPartyTick);
        }

        private void HourlyPartyTick(MobileParty party)
        {
            if (party.IsRaidingParty())
            {
                var component = (IRaidingParty)party.PartyComponent;
                component.HourlyTick();
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
            if(settlement.SettlementComponent is BaseRaiderSpawnerComponent)
            {
                var component = settlement.SettlementComponent as BaseRaiderSpawnerComponent;
                if (component.RaidingPartyCount < 5) component.SpawnNewParty();
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            //throw new NotImplementedException();
        }
    }
}
