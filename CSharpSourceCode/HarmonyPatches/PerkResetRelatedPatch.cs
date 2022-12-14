using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class PerkResetRelatedPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PerkResetCampaignBehavior), "ClearPermanentBonusesIfExists")]
        public static void ResetExtraPerks(Hero hero, PerkObject perk)
        {
            if(perk == TORPerks.GunPowder.FiringDrills)
            {
                if (!hero.GetPerkValue(perk))
                {
                    return;
                }
                hero.HeroDeveloper.RemoveAttribute(TORAttributes.Discipline, 1);
            }
        }
    }
}
