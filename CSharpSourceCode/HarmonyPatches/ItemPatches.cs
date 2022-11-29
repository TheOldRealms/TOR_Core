using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class ItemPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WeaponComponentData), "GetRelevantSkillFromWeaponClass")]
        public static bool AddGunpowderRelevantSkill(ref SkillObject __result, WeaponClass weaponClass)
        {
            if (weaponClass == WeaponClass.Cartridge || weaponClass == WeaponClass.Musket || weaponClass == WeaponClass.Pistol)
            {
                __result = TORSkills.GunPowder;
                return false;
            }
            
            //This might be too much, Throwing stones are confirmed not a big thing in warhammer lore (beside maybe fanatical rats, so faith?
            // The point is, it feels better in the menu when the new magical trinkets are shown as a generic weapon instead of throwing items. 
            // The effect of the stone so in this case it would be Faith, will never been triggered since the projectile is always removed
            /*if (weaponClass == WeaponClass.Stone)     
            {
                __result = TORSkills.Faith;
                return false;
            }*/
            
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorkshopsCampaignBehavior), "IsItemPreferredForTown")]
        public static void OnlyProduceTorItems(ref bool __result, ItemObject item, Town townComponent)
        {
            if (__result && item.Culture == townComponent.Culture) __result = item.IsTorItem();
        }
    }
}
