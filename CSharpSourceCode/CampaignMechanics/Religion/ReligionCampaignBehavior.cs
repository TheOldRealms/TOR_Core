using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Religion
{
    public class ReligionCampaignBehavior : CampaignBehaviorBase, IDisposable
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, AfterNewGameStart);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
            TORCampaignEvents.Instance.DevotionLevelChanged += OnDevotionLevelChanged;
        }

        private void OnDevotionLevelChanged(object sender, DevotionLevelChangedEventArgs e)
        {
            if((int)e.NewDevotionLevel > (int)e.OldDevotionLevel)
            {
                MBInformationManager.AddQuickInformation(new TextObject(e.Hero.Name.ToString() + " is now a " + e.NewDevotionLevel.ToString() + " of the " + e.Religion.Name));
            }
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

        public void Dispose()
        {
            TORCampaignEvents.Instance.DevotionLevelChanged -= OnDevotionLevelChanged;
        }
    }
}
