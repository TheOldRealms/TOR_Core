﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.Artillery;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class ArtilleryPatches
    {
        /// <summary>
        /// Ballistic solutions don't account for engine projectile drag. This removes the effect until the system is rethought.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemObject), "GetAirFrictionConstant")]
        public static void OverrideAirFrictionForCannonBall(ref float __result, WeaponClass weaponClass)
        {
            if (weaponClass == WeaponClass.Boulder) __result = 0;
        }
    }
}