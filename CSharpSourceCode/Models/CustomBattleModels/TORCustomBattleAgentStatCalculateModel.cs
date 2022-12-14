using TaleWorlds.MountAndBlade;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TOR_Core.BattleMechanics.Crosshairs;

namespace TOR_Core.Models.CustomBattleModels
{
    public class TORCustomBattleAgentStatCalculateModel : CustomBattleAgentStatCalculateModel
    {
        private CustomCrosshairMissionBehavior _crosshairBehavior;

        public override float GetMaxCameraZoom(Agent agent)
        {
            if (_crosshairBehavior == null)
            {
                _crosshairBehavior = Mission.Current.GetMissionBehavior<CustomCrosshairMissionBehavior>();
            }

            if (_crosshairBehavior != null && _crosshairBehavior.CurrentCrosshair is SniperScope && _crosshairBehavior.CurrentCrosshair.IsVisible)
            {
                return 3;
            }
            return base.GetMaxCameraZoom(agent);
        }
    }
}
