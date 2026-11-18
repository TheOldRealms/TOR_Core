using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>Thin subclass of BattleEndLogic that declines MissionEnded so AmbushEndLogic owns the outcome ladder.</summary>
    public class AmbushBattleEndLogic : BattleEndLogic
    {
        public override bool MissionEnded(ref MissionResult missionResult)
        {
            return false;
        }
    }
}
