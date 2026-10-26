using HarmonyLib;
using TaleWorlds.Core;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class ArtilleryPatches
    {
        /// <summary>
        /// Ballistic solutions don't account for engine projectile drag. This removes the effect until the system is rethought.
        /// </summary>
        /// <remarks>
        /// Comment source is \Modules\Native\ModuleData\managed_core_parameters.xml
        /// <!-- missile related (AirFrictionArrow and HeavyAttackMomentumMultiplier have twins in native_core_paramters)-->
        /// <!-- 		speed -= air_friction_constant * (speed * speed) * MISSILE_SIMULATION_DT;	//add drag -->
        /// <!-- AirFriction = surfaceArea * FrictionCoefficient / mass-->
        /// 
        /// Calculations for artillery firing angles can account for friction directly rather than patching the friction constant.
        /// Patching all occurences of WeaponClass.Boulder means that native siege equipment may have induced inaccuracy from missing air friction already accounted for in its calculations. (Agent thrown boulders have negligible deviations.
        /// </remarks>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemObject), "GetAirFrictionConstant")]
        public static void OverrideAirFrictionForCannonBall(ref float __result, WeaponClass weaponClass)
        {
            if (weaponClass == WeaponClass.Boulder) __result = 0;
        }
    }
}