using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TOR_Core.CampaignMechanics.Religion;

namespace TOR_Core.Utilities
{
    public class TORCampaignEvents
    {
        public static TORCampaignEvents Instance;
        public event EventHandler<DevotionLevelChangedEventArgs> DevotionLevelChanged;

        public TORCampaignEvents()
        {
            Instance = this;
        }

        public void OnDevotionLevelChanged(Hero hero, ReligionObject religion, DevotionLevel oldDevotionLevel, DevotionLevel newDevotionLevel)
        {
            var args = new DevotionLevelChangedEventArgs(hero, religion, oldDevotionLevel, newDevotionLevel);
            var devotionEvent = DevotionLevelChanged;
            if(devotionEvent != null)
            {
                devotionEvent(this, args);
            }
        }
    }

    public class DevotionLevelChangedEventArgs : EventArgs
    {
        public DevotionLevelChangedEventArgs(Hero hero, ReligionObject religion, DevotionLevel oldDevotionLevel, DevotionLevel newDevotionLevel)
        {
            Hero = hero;
            Religion = religion;
            OldDevotionLevel = oldDevotionLevel;
            NewDevotionLevel = newDevotionLevel;
        }

        public Hero Hero { get; set; }
        public ReligionObject Religion { get; set; }
        public DevotionLevel OldDevotionLevel { get; set; }
        public DevotionLevel NewDevotionLevel { get; set; }
    }
}
