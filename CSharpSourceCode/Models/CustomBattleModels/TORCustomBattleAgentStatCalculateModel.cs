using TaleWorlds.Core;
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
            if (t.ChangedValue)
            {
                

                if (agent.IsMount)
                {
                    agentDrivenProperties.MountSpeed=Mathf.Max(0, t.value);
                    //agentDrivenProperties.TopSpeedReachDuration=Mathf.Max(0, t.value);
                    agentDrivenProperties.MountSpeed = Mathf.Max(0, t.value);
                    agentDrivenProperties.MountDashAccelerationMultiplier=Mathf.Max(0, t.value);
     
                    //agent.MountAgent.AgentDrivenProperties.SetStat(DrivenProperty.ArmorEncumbrance,Mathf.Max(10000000000000000000000000f, t.value));
                }
                else
                {
                    agentDrivenProperties.MaxSpeedMultiplier = Mathf.Max(0, t.value);
                }
                //agentDrivenProperties.MountSpeed = Mathf.Max(0, t.value);
                if (agent.HasMount)
                {
                   
                  //  agent.MountAgent.AgentDrivenProperties.MountManeuver=Mathf.Max(0, t.value);
                    //agent.MountAgent.AgentDrivenProperties.TopSpeedReachDuration=Mathf.Max(0, t.value);
                    //agent.MountAgent.AgentDrivenProperties.MountDashAccelerationMultiplier=Mathf.Max(0, t.value);
                    //agent.MountAgent.SetAgentDrivenPropertyValueFromConsole();
                    //agent.MountAgent.AgentDrivenProperties.MountManeuver=Mathf.Max(0, t.value);
                   // agent.MountAgent.AgentDrivenProperties.MaxSpeedMultiplier = Mathf.Max(0, t.value);
                }
                
                /*if (agent.HasMount)
                {
                   
                    agent.MountAgent.UpdateAgentProperties();
                }*/
                   

            }
        }
    }
}
