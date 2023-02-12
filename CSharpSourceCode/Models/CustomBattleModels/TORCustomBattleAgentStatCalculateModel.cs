using TaleWorlds.Core;
using TaleWorlds.Library;
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
            UpdateDynamicAgentDrivenProperties(agent, agentDrivenProperties);
        }


        private  void UpdateDynamicAgentDrivenProperties(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            var statusEffectComponent = agent.IsMount ? agent.RiderAgent?.GetComponent<StatusEffectComponent>()  : agent.GetComponent<StatusEffectComponent>();
            if(statusEffectComponent==null)
                return;

            if(!statusEffectComponent.AreBaseValuesInitialized()) return;
            var speedModifier = statusEffectComponent.GetMovementSpeedModifier();
            if (speedModifier!=0f)
            {
                var speedMultiplier =  Mathf.Clamp(speedModifier + 1,0,2);      //to set in the right offset, where -100% would actually result in 0% movement speed
                SetDynamicMovementAgentProperties(agent, statusEffectComponent, agentDrivenProperties, speedMultiplier);
            }
            else
            {
                SetDynamicMovementAgentProperties(agent, statusEffectComponent, agentDrivenProperties, 1);
            }
        }

        private void SetDynamicMovementAgentProperties(Agent agent, StatusEffectComponent component, AgentDrivenProperties agentDrivenProperties, float speedMultiplier)
        {
            if (agent.IsMount)
            {
                SetDynamicMountMovementProperties(component,agentDrivenProperties, speedMultiplier);
            }
            else
            {
                SetDynamicHumanoidMovementProperties(component,agentDrivenProperties, speedMultiplier);
            }
        }
        private void SetDynamicHumanoidMovementProperties(StatusEffectComponent statusEffectComponent, AgentDrivenProperties properties, float speedMultiplier)
        {
            properties.MaxSpeedMultiplier =statusEffectComponent.GetBaseValueForDrivenProperty(DrivenProperty.MaxSpeedMultiplier)*speedMultiplier;
        }

        private void SetDynamicMountMovementProperties(StatusEffectComponent statusEffectComponent, AgentDrivenProperties properties, float speedMultiplier)
        {
            properties.MountSpeed = statusEffectComponent.GetBaseValueForDrivenProperty(DrivenProperty.MountSpeed)*speedMultiplier;
            properties.MountDashAccelerationMultiplier=statusEffectComponent.GetBaseValueForDrivenProperty(DrivenProperty.MountDashAccelerationMultiplier)*speedMultiplier;
            properties.MountManeuver = statusEffectComponent.GetBaseValueForDrivenProperty(DrivenProperty.MountManeuver)*speedMultiplier;
        }
    }
}
