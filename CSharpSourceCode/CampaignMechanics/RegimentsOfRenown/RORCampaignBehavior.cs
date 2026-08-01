using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.RegimentsOfRenown
{
    public class RORCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, SettlementEntered);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter obj) { }

        //used to contain the artillery crewman recruitment for empire nobles which is now in TORAIRecruitmentCampaignBehavior
        private void SettlementEntered(MobileParty mobileParty, Settlement settlement, Hero leaderHero) { }

        public override void SyncData(IDataStore dataStore) { }
    }
}