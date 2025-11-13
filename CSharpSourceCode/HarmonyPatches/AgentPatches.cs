using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    [HarmonyPatchCategory("LatePatches")]
    public static class AgentPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Agent), "GetBattleImportance")]
        public static void BattleImportancePatch(ref float __result, Agent __instance)
        {
            if (__instance.IsExpendable())
            {
                __result = 0;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Agent), "CombatActionsEnabled", MethodType.Getter)]
        public static void CombatActionsEnabledPatch(ref bool __result, Agent __instance)
        {
            if (__instance.IsMainAgent && __result)
            {
                var logic = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
                if (logic != null && logic.ShouldSuppressCombatActions) __result = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Formation), "GetMedianAgent")]
        public static void MedianAgentPatch(ref Agent __result, Formation __instance)
        {
            if (__result == null)
            {
                List<Agent> units = [];
                foreach (var unit in __instance.Arrangement.GetAllUnits())
                {
                    if (unit is Agent agent && agent.IsActive())
                    {
                        units.Add(agent);
                    }

                }
                __result = units.FirstOrDefault();
            }
        }
    }
}