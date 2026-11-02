using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Thin subclass of vanilla <see cref="BattleEndLogic"/> for ambush
    /// missions. Keeps everything the base class does on every tick
    /// (retreat detection, victory-reaction trigger on AgentVictoryLogic,
    /// battle-won / lost messages, enemy-defender pullback tracking,
    /// the private depletion / retreat flags that vanilla's post-battle
    /// flow reads downstream) and only overrides MissionEnded to decline.
    ///
    /// Why decline: AmbushMissionController's shared-supplier drain in
    /// AfterStart empties the troop queue for both sides so vanilla's
    /// (0, 0, 0) spawn path never fires. That makes
    /// DefaultBattleMissionAgentSpawnLogic.IsSideDepleted return true
    /// from frame one — 25 defenders still swinging on the field.
    /// Vanilla's MissionEnded would see "enemy side depleted" and fire
    /// CreateSuccessful immediately, declaring an instant Player Victory
    /// before anyone attacks anyone. Returning false here keeps
    /// <see cref="AmbushEndLogic"/>'s outcome ladder (HeroWounded,
    /// Defeated, TotalVictory on actual field-clear, Extracted, Stalemate)
    /// as the sole decider while the rest of the base class still runs
    /// so the post-mission stealth-menu / loot-flow handoff stays intact.
    /// </summary>
    public class AmbushBattleEndLogic : BattleEndLogic
    {
        public override bool MissionEnded(ref MissionResult missionResult)
        {
            return false;
        }
    }
}
