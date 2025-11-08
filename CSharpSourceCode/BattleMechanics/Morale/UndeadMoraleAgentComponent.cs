using System;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.Morale
{
    public class UndeadMoraleAgentComponent : AgentComponent
    {
        private float _crumbleThreshold = 15f;
        private float _timeElapsed = 0;

        private CommonAIComponent _moraleComponent;

        public UndeadMoraleAgentComponent(Agent agent) : base(agent) { }

        public override void Initialize()
        {
            base.Initialize();
            _moraleComponent = Agent.GetComponent<CommonAIComponent>();
        }

        public override void OnTick(float dt)
        {
            if (_moraleComponent != null) // Main agent has UndeadMoralComponent somehow
            {
                if (Agent == null)
                {
                    TORCommon.Log("UndeadMoraleAgentComponent : agent is null.", NLog.LogLevel.Warn);
                }
                _timeElapsed += dt;
                if (_timeElapsed >= 0.5)
                {
                    _timeElapsed = 0;
                    try
                    {
                        if (Agent.IsActive() || Agent.IsRetreating())
                        {
                            if (_moraleComponent.Morale < _crumbleThreshold)
                            {
                                ApplyCrumble();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TORCommon.Log("UndeadMoraleAgentComponent : Attempted to apply crumbling to agent. Error: " + ex.Message, NLog.LogLevel.Error);
                    }
                }
            }
        }

        private void ApplyCrumble()
        {
            Agent.ApplyStatusEffect("crumble", Agent, 600, false);
        }
    }
}
