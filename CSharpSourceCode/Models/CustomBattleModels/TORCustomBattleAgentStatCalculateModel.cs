using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TOR_Core.BattleMechanics.Crosshairs;
using TOR_Core.BattleMechanics.StatusEffect;

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
        
        public override void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            base.UpdateAgentStats(agent, agentDrivenProperties);
            UpdateAgentDrivenProperties(agent, agentDrivenProperties);
        }


        public  void UpdateAgentDrivenProperties(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            
            if(!agent.IsHuman) return;
            
            var t = agent.GetComponent<StatusEffectComponent>();
            if (t == null) return;
            if (t.ChangedValue)
            {
                agentDrivenProperties.MaxSpeedMultiplier = Mathf.Max(0, t.value);
            }
        }
    }
}
