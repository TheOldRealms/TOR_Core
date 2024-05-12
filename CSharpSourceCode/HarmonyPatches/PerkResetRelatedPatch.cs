using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class PerkResetRelatedPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PerkResetCampaignBehavior), "ClearPermanentBonusesIfExists")]
        public static void ResetExtraPerks(Hero hero, PerkObject perk)
        {
            if (!hero.GetPerkValue(perk)) return;

            if (perk == TORPerks.GunPowder.FiringDrills)
            {   
                hero.HeroDeveloper.RemoveAttribute(TORAttributes.Discipline, 1);
            }
            if(perk == TORPerks.Faith.DivineMission)
            {
                hero.HeroDeveloper.RemoveFocus(DefaultSkills.Medicine, 1);
            }
            if(perk == TORPerks.Faith.ForeSight)
            {
                hero.HeroDeveloper.UnspentAttributePoints -= 1;
            }
            
            if (Hero.MainHero.HasAnyCareer())
            {
                       
                var prayers = CareerHelper.GetBattlePrayerList(Hero.MainHero.GetCareer());

                var rank = 0;

                if (perk == TORPerks.Faith.NovicePrayers)
                {
                    rank = 2;
                }
                        
                else if (perk == TORPerks.Faith.AdeptPrayers)
                {
                    rank = 3;
                }
                        
                else if (perk == TORPerks.Faith.GrandPrayers)
                {
                    rank = 4;
                }
                        
                var prayersForRank = prayers.Where(X => X.Rank == rank);
                var extendedInfo = hero.GetExtendedInfo(); 
                foreach (var prayer in prayersForRank)
                {
                    extendedInfo.RemoveAbility(prayer.PrayerID);
                }
            }
            
        }
    }
}
