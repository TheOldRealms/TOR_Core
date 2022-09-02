using HarmonyLib;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;

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
            else return true;
        }
    }
}
