using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Null-object stub given to vanilla's spawn-logic
    /// <c>SetCustomReinforcementSpawnTimer</c> so its CustomTimer branch
    /// has something non-null to call. We don't want vanilla driving
    /// reinforcement waves at all (those are owned end-to-end by
    /// <see cref="AmbushReinforcementMissionLogic"/>), but the vanilla
    /// <c>SetReinforcementsSpawnEnabled</c> path calls
    /// <c>_customReinforcementSpawnTimer.ResetTimer(...)</c> without a
    /// null-check, so leaving the field null NPEs on FinishDeployment.
    /// <see cref="Check"/> always returns false, so vanilla's
    /// <c>CheckCustomReinforcementBatch</c> never spawns anything.
    /// </summary>
    internal sealed class NoOpReinforcementSpawnTimer : ICustomReinforcementSpawnTimer
    {
        public bool Check(BattleSideEnum side) => false;
        public void ResetTimer(BattleSideEnum side) { }
    }
}
