using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Religion
{
    public class ReligionCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, AfterNewGameStart);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
        }

        private void AfterNewGameStart(CampaignGameStarter starter, int index)
        {
            if(index == CampaignEvents.OnNewGameCreatedPartialFollowUpEventMaxIndex - 1)
            {
                foreach (var religion in ReligionObject.All)
                {
                    foreach (var keyvaluepair in religion.InitialFollowers)
                    {
                        keyvaluepair.Key.AddReligiousInfluence(religion, keyvaluepair.Value);
                    }
                }
            }
        }

        private void OnSessionStart(CampaignGameStarter starter)
        {
            
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
