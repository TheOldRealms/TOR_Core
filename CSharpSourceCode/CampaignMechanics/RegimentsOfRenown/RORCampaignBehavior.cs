using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

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
