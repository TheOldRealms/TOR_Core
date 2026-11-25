using System;
using System.Reflection;
using HarmonyLib;
using NLog;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Problem: vanilla's Reset Deployment button on the OoB screen assumes a populated deployment plan and a General agent, which ambush missions never have.
    /// Fix: in ambush missions, hand the Reset action to the ambush controller's ResetAssignments method so formations snap back to their initial computed spots.
    /// </summary>
    [HarmonyPatch]
    public static class AmbushResetDeploymentPatch
    {
        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(MissionOrderDeploymentControllerVM),
                                      "ExecuteAutoDeploy");
        }

        [HarmonyPrefix]
        static bool Prefix()
        {
            try
            {
                var mission = Mission.Current;
                var controller = mission?.GetMissionBehavior<Missions.AmbushMission.AmbushMissionController>();
                if (controller == null) return true; // not an ambush, run vanilla
                controller.ResetAssignments();
                return false; // skip vanilla deployment-plan path
            }
            catch (Exception e)
            {
                TORCommon.Log(LogLevel.Error,
                    "[ResetDeploymentPatch] prefix threw, falling back to vanilla: " + e);
                return true;
            }
        }
    }
}
