using System;
using System.Reflection;
using HarmonyLib;
using NLog;
using SandBox.Missions.MissionLogics;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Problem: Start Battle crashes to desktop in ambush missions. The crash
    /// sits inside vanilla's generals-formation setup: GeneralsAndCaptainsAssignmentLogic
    /// (and its Sandbox subclass) treats the player ambusher as a team
    /// General, calls CreateGeneralFormationForTeam, which calls
    /// Mission.SetFormationPositioningFromDeploymentPlan on a generals
    /// Formation (FormationClass.NumberOfRegularFormations). Our ambush
    /// scene has no deployment-plan entry for that formation class — the
    /// native positioning call derefs invalid data and the game dies.
    ///
    /// Fix: skip the vanilla generals-formation setup entirely when we're
    /// running an ambush mission. Ambush missions don't use the generals
    /// mechanic (no bodyguard, no remote-command) so there's no behavior
    /// to lose. Gate strictly on the presence of AmbushMissionController
    /// so we don't change any other mission type.
    ///
    /// Patch target is looked up via AccessTools.Method, matching the
    /// tracer that found the crash — this is safer than nameof against
    /// method-rename surprises across game updates.
    /// </summary>
    [HarmonyPatch]
    public static class AmbushSkipGeneralsFormationPatch
    {
        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(GeneralsAndCaptainsAssignmentLogic),
                                      "OnDeploymentFinished");
        }

        [HarmonyPrefix]
        static bool Prefix()
        {
            try
            {
                var mission = Mission.Current;
                if (mission != null
                    && mission.GetMissionBehavior<Missions.AmbushMission.AmbushMissionController>() != null)
                {
                    TORCommon.Log(LogLevel.Info,
                        "[GeneralsPatch] skipped GeneralsAndCaptainsAssignmentLogic.OnDeploymentFinished in ambush mission");
                    return false; // skip the vanilla method entirely
                }
            }
            catch (Exception e)
            {
                TORCommon.Log(LogLevel.Error,
                    "[GeneralsPatch] prefix threw, letting vanilla run: " + e);
            }
            return true; // run the vanilla method for non-ambush missions
        }
    }
}
