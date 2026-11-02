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
    /// Problem: the vanilla "Reset Deployment" button on the OoB screen routes
    /// through <see cref="MissionOrderDeploymentControllerVM.ExecuteAutoDeploy"/>,
    /// which calls <c>BattleDeploymentHandler.AutoDeployTeamUsingDeploymentPlan</c>
    /// and <c>HandleGeneralsDeploymentFrames</c>. Both assume a populated
    /// deployment plan + a General agent; ambush missions have neither (we
    /// use custom spawn via <see cref="Missions.AmbushMission.AmbushMissionController"/>)
    /// so the vanilla path hits <c>Mission.GetFormationSpawnFrame</c> on missing
    /// data and the game dies.
    ///
    /// Fix: in ambush missions, hand the Reset action off to the ambush
    /// controller's <see cref="Missions.AmbushMission.AmbushMissionController.ResetAssignments"/>
    /// method, which restores each chunk to its initial computed spot
    /// (preferred-class-matched first) and re-lays-out the formations.
    /// Non-ambush missions keep the vanilla flow untouched.
    ///
    /// Why Harmony here rather than subclass/VM-extension:
    /// <list type="bullet">
    /// <item><c>ExecuteAutoDeploy</c> is a non-virtual method on a vanilla
    /// VM instantiated by Sandbox code we don't control; shadowing with
    /// <c>new</c> wouldn't catch the hotkey path (the handler calls it on
    /// the typed base reference).</item>
    /// <item>There is no event / observer surface on the deployment
    /// controller VM for this action.</item>
    /// <item>The TOR VM extension framework can add properties but not
    /// override an existing method.</item>
    /// </list>
    /// Patch target is looked up via <c>AccessTools.Method</c> to survive
    /// method renames across game updates.
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
                if (controller == null) return true; // not ambush: let vanilla run
                controller.ResetAssignments();
                return false; // skip vanilla's deployment-plan path
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
