using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TOR_Core.CharacterDevelopment
{
    public class TORPerkHandlerCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.PerkOpenedEvent.AddNonSerializedListener(this, OnPerkPicked);
        }

        private void OnPerkPicked(Hero hero, PerkObject perk)
        {
            if(hero != null && perk != null)
            {
                if(perk == TORPerks.GunPowder.FiringDrills)
                {
                    hero.HeroDeveloper.AddAttribute(TORAttributes.Discipline, 1, false);
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
}
