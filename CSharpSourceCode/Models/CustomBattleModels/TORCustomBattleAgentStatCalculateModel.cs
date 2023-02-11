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
            UpdateAgentDrivenProperties(agent, agentDrivenProperties);
        }


        public  void UpdateAgentDrivenProperties(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            if (!agent.IsMount&&agent.GetComponent<StatusEffectComponent>() == null)
                return;
            switch (agent.IsMount)
            {
                case true when agent.RiderAgent == null:
                case true when agent.RiderAgent.GetComponent<StatusEffectComponent>() == null:
                    return;
            }


            StatusEffectComponent t;

            t = agent.IsMount ? agent.RiderAgent.GetComponent<StatusEffectComponent>() : agent.GetComponent<StatusEffectComponent>();
            
            /*var t = agent.GetComponent<StatusEffectComponent>();
            if (t == null) return;*/
            var speedModifier = t.GetMovementSpeedModifier();
            if (speedModifier.Item2!=0f)
            {
                var speedmultiplier =  Mathf.Clamp(speedModifier.Item2 + 1,0,2);

                if (agent.IsMount)
                {
                    
                    agentDrivenProperties.MountSpeed=Mathf.Max(0, t.value);
                    //agentDrivenProperties.TopSpeedReachDuration=Mathf.Max(0, t.value);
                    agentDrivenProperties.MountSpeed = Mathf.Max(0, t.value);
                    agentDrivenProperties.MountDashAccelerationMultiplier=Mathf.Max(0, t.value);
                    agentDrivenProperties.MountManeuver = Mathf.Max(0, t.value);
                    agent.SetActionChannel(0, ActionIndexCache.Create("act_horse_stand_1"));
                    agent.SetAgentFlags(AgentFlag.MoveForwardOnly);
                    agent.SetCurrentActionSpeed(0,0f);
                    

                    //agent.MountAgent.AgentDrivenProperties.SetStat(DrivenProperty.ArmorEncumbrance,Mathf.Max(10000000000000000000000000f, t.value));
                }
                else
                {
                    
                    agentDrivenProperties.MaxSpeedMultiplier =speedModifier.Item1*speedmultiplier;
                }
            }
            else
            {
                if (agent.IsMount)
                {
                }
                else
                {
                    if(speedModifier.Item1<=0) return;
                    agentDrivenProperties.MaxSpeedMultiplier = Mathf.Max(0, speedModifier.Item1);
                }
            }
        }
    }
}
