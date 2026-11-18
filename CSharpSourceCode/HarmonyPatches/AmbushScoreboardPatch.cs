using HarmonyLib;
using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TOR_Core.CampaignMechanics.Ambush;
using TOR_Core.Missions.AmbushMission;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Problem: the scoreboard's OnBattleOver can fire mid-ambush when the defender team momentarily hits zero active humans between reinforcement waves, latching a stale Defeat panel.
    /// Fix: prefix-skip OnBattleOver when the ambush resolution is not terminal. Non-ambush missions pass through.
    /// </summary>
    [HarmonyPatch(typeof(SPScoreboardVM), nameof(SPScoreboardVM.OnBattleOver))]
    public static class AmbushScoreboardPatch
    {
        static bool Prefix()
        {
            var mission = Mission.Current;
            if (mission == null) return true;
            bool isAmbush = mission.GetMissionBehavior<AmbushEndLogic>() != null
                            || mission.GetMissionBehavior<AmbushedEndLogic>() != null;
            if (!isAmbush) return true;

            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior == null) return false; // no behavior, treat as mid-mission

            var resolution = behavior.LastResolution;
            bool terminal =
                resolution == AmbushResolution.AmbusherTotalVictory
                || (resolution == AmbushResolution.AmbusherDefeated
                    && !behavior.LastAmbusherLeaderDown)
                || (resolution == AmbushResolution.VictimDefeated
                    && !behavior.LastVictimLeaderDown);
            return terminal;
        }
    }
}
