using HarmonyLib;
using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TOR_Core.CampaignMechanics.Ambush;
using TOR_Core.Missions.AmbushMission;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Problem: vanilla's scoreboard fires <c>OnBattleOver</c> twice for
    /// an ambush mission — once mid-mission the moment the defender team
    /// briefly hits 0 active humans between reinforcement waves (the
    /// tick path checks <c>BattleEndLogic.IsEnemySideRetreating</c>,
    /// which flips true on 0-enemies + retreat-interval elapsed), and
    /// once at real mission end. The first call reads
    /// <c>PlayerEncounter.WinningSide = None</c> (no SetPlayerVictorious
    /// yet), latches <c>IsOver=true</c> + <c>BattleResult="Defeat"</c>,
    /// and drives the whole "battle is over" scoreboard animation.
    /// AmbushScoreboardVMExtension's IsAmbushResultVisible gate hides
    /// the title but the IsOver-driven panel transitions still play,
    /// and the stale Defeat stays bound because OnBattleOver early-
    /// exits on IsOver=true the second time.
    ///
    /// Fix: prefix-skip OnBattleOver whenever the mission is an ambush
    /// and the resolution isn't terminal. Terminal matches
    /// AmbushScoreboardVMExtension's definition (AmbusherTotalVictory,
    /// non-hero-down AmbusherDefeated, non-hero-down VictimDefeated).
    /// Non-ambush missions pass through unchanged.
    ///
    /// Scoped to the one method, one condition. No reflection, no
    /// private-field writes. Keeps this single UI-adjacent patch below
    /// the "patch sprawl = design smell" threshold.
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
            if (behavior == null) return false; // no behavior → treat as mid-mission, skip.

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
