using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace TOR_Core.Utilities
{
    public static class TORGameStarterHelper
    {
        public static void CleanCampaignStarter(CampaignGameStarter starter)
        {
            starter.RemoveBehaviors<BackstoryCampaignBehavior>();
            var issues = starter.CampaignBehaviors.Where(x => x.GetType().FullName.Contains("Issue")).ToList();
            foreach(var issue in issues)
            {
                starter.RemoveBehavior(issue);
            }
        }
    }
}
