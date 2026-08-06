using HarmonyLib;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class MissionCombatMechanicsHelpers
    {
        private const float BruteAiMeleeInitialEnergyMultiplier = 1.2f;

        [ThreadStatic]
        private static Agent _currentCombatStatAttackerAgent;

        [ThreadStatic]
        private static WeaponComponentData _currentCombatStatAttackerUsageItem;

        private static void SetCombatStatContext(Agent attackerAgent, WeaponComponentData attackerUsageItem)
        {
            _currentCombatStatAttackerAgent = attackerAgent;
            _currentCombatStatAttackerUsageItem = attackerUsageItem;
        }

        private static void ClearCombatStatContext()
        {
            _currentCombatStatAttackerAgent = null;
            _currentCombatStatAttackerUsageItem = null;
        }

        private static bool ShouldApplyBruteAiInitialEnergyBonus(Agent attackerAgent, WeaponComponentData attackerUsageItem)
        {
            if (attackerAgent == null || !attackerAgent.IsHuman || !attackerAgent.IsAIControlled)
            {
                return false;
            }

            if (!attackerAgent.HasBrute())
            {
                return false;
            }

            if (attackerUsageItem == null || !attackerUsageItem.IsMeleeWeapon || attackerUsageItem.IsShield)
            {
                return false;
            }

            return true;
        }

        private static float ApplyBruteAiInitialEnergyBonus(float baseMagnitude)
        {
            if (!ShouldApplyBruteAiInitialEnergyBonus(_currentCombatStatAttackerAgent, _currentCombatStatAttackerUsageItem))
            {
                return baseMagnitude;
            }

            return baseMagnitude * BruteAiMeleeInitialEnergyMultiplier;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MissionCombatMechanicsHelper), "CalculateBaseMeleeBlowMagnitude")]
        private static void CalculateBaseMeleeBlowMagnitudePrefix(in AttackInformation attackInformation)
        {
            WeaponComponentData attackerUsageItem = attackInformation.AttackerWeapon.CurrentUsageItem;
            SetCombatStatContext(attackInformation.AttackerAgent, attackerUsageItem);
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(MissionCombatMechanicsHelper), "CalculateBaseMeleeBlowMagnitude")]
        private static Exception CalculateBaseMeleeBlowMagnitudeFinalizer(Exception __exception)
        {
            ClearCombatStatContext();
            return __exception;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MissionCombatMechanicsHelper), "CalculateBaseMeleeBlowMagnitude")]
        private static void CalculateBaseMeleeBlowMagnitudePostfix(ref float __result)
        {
            __result = ApplyBruteAiInitialEnergyBonus(__result);
        }

    }
}