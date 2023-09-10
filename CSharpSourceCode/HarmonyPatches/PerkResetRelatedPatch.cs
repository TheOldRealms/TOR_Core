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

            if (perk == TORPerks.Faith.NovicePrayers)
            {
                if (hero.HasCareer(TORCareers.WarriorPriest))
                {
                    
                    if (hero.HasAbility("HealingHand")) hero.GetExtendedInfo().RemoveAbility("HealingHand");
                }

                if (hero.HasCareer(TORCareers.GrailDamsel))
                {
                    if (hero.HasAbility("AuraOfTheLady")) hero.GetExtendedInfo().RemoveAbility("AuraOfTheLady");
                }
            }
            
            if (perk == TORPerks.Faith.AdeptPrayers)
            {
                if (hero.HasCareer(TORCareers.WarriorPriest))
                {
                    if (hero.HasAbility("ArmourOfRighteousness")) hero.GetExtendedInfo().RemoveAbility("ArmourOfRighteousness");
                    if (hero.HasAbility("Vanquish")) hero.GetExtendedInfo().RemoveAbility("Vanquish");
                }
                            
                if (hero.HasCareer(TORCareers.GrailDamsel))
                {
                    if (hero.HasAbility("ShieldOfCombat")) hero.GetExtendedInfo().RemoveAbility("ShieldOfCombat");
                    if (hero.HasAbility("LadysFavour")) hero.GetExtendedInfo().RemoveAbility("LadysFavour");
                   
                }
                            
            }
            if (perk == TORPerks.Faith.GrandPrayers)
            {
                if (hero.HasCareer(TORCareers.WarriorPriest))
                {
                    if (hero.HasAbility("CometOfSigmar")) hero.GetExtendedInfo().RemoveAbility("CometOfSigmar");
                }
                if (hero.HasCareer(TORCareers.GrailDamsel))
                {
                    if (hero.HasAbility("AerialShield")) hero.GetExtendedInfo().RemoveAbility("AerialShield");
                }
            }
        }
    }
}
