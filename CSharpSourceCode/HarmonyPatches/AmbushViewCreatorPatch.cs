using HarmonyLib;
using SandBox.View.Missions;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TOR_Core.Missions.AmbushMission;
using TOR_Core.Missions.AmbushMission.UI;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Swaps vanilla's DeploymentMissionView for our AmbushDeploymentMissionView
    /// on ambush missions. Vanilla instantiates DeploymentMissionView directly
    /// with `new` inside SandBoxMissionViews.OpenBattleMission (bypassing the
    /// override registry), so the [OverrideView] attribute system is a dead end
    /// for this specific view. Postfix finds the vanilla instance in the
    /// returned array and replaces it with ours.
    /// </summary>
    [HarmonyPatch(typeof(SandBoxMissionViews), nameof(SandBoxMissionViews.OpenBattleMission))]
    public static class AmbushViewCreatorPatch
    {
        public static void Postfix(Mission mission, ref MissionView[] __result)
        {
            if (mission?.GetMissionBehavior<AmbushMissionController>() == null) return;
            for (int i = 0; i < __result.Length; i++)
            {
                if (__result[i].GetType() == typeof(DeploymentMissionView))
                {
                    __result[i] = new AmbushDeploymentMissionView();
                    return;
                }
            }
        }
    }
}
