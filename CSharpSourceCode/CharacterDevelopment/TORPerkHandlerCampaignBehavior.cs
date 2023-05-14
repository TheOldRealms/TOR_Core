using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

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
                if(perk == TORPerks.Faith.DivineMission)
                {
                    if (hero.HeroDeveloper.CanAddFocusToSkill(DefaultSkills.Medicine))
                    {
                        hero.HeroDeveloper.AddFocus(DefaultSkills.Medicine, 1);
                    }
                    else hero.HeroDeveloper.UnspentFocusPoints += 1;
                }
                if (perk == TORPerks.Faith.ForeSight) hero.HeroDeveloper.UnspentAttributePoints += 1;
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
}
