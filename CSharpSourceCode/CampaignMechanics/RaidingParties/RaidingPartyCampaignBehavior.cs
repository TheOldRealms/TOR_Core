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
            CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, HourlyPartyTick);
        }

        private void HourlyPartyTick(MobileParty party, PartyThinkParams thinkParams)
        {
            if (party.IsRaidingParty())
            {
                var component = (IRaidingParty)party.PartyComponent;
                component.HourlyTickAI(thinkParams);
            }
        }

        private void DailyTickSettlement(Settlement settlement)
        {
            if(settlement.SettlementComponent is BaseRaiderSpawnerComponent)
            {
                var component = settlement.SettlementComponent as BaseRaiderSpawnerComponent;
                if (component.RaidingPartyCount < 5 && component.IsActive) component.SpawnNewParty(out _, null);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            //throw new NotImplementedException();
        }
    }
}
