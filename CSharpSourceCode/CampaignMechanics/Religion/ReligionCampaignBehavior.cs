using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Religion
{
    public class ReligionCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
        }

        private void OnSessionStart(CampaignGameStarter starter)
        {
            ReligionManager.Initialize();
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
