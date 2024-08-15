using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Scripts;

public class MindControlScript : CareerAbilityScript
{
    private MissionCameraFadeView _cameraView;
    private Agent _controlled;
    private Agent _caster;
    private float _radius;

    private bool _mindControl;
    protected override void OnInit()
    {
        base.OnInit();
        var radius = 0f;
        _caster = this.CasterAgent;
        _cameraView = Mission.Current.GetMissionBehavior<MissionCameraFadeView>();
        Mission.Current.OnBeforeAgentRemoved += AgentRemoved;
        
    }

    private void AgentRemoved(Agent affectedagent, Agent affectoragent, AgentState agentstate, KillingBlow killingblow)
    {
        if (affectedagent == _controlled) Stop();
    }

    protected override void OnAfterTick(float dt)
    {
        base.OnAfterTick(dt);
        if (!_mindControl&& !IsFading)
        {
            _mindControl = true;
            var target =   ExplicitTargetAgents[0];
            var casterTeam = _caster.Team;
            _caster.Controller = Agent.ControllerType.None;
            target.Controller = Agent.ControllerType.Player;
            target.SetTeam(casterTeam,true);
            _controlled = target;
        }
    }
    
    protected override void OnBeforeRemoved(int removeReason)
    {
        Mission.Current.OnBeforeAgentRemoved -= AgentRemoved;
        if (_mindControl) ShiftControllerToCaster();
    }
    
    private void ShiftControllerToCaster()
    {
        _caster.ClearTargetFrame();

        if (_caster.Health > 0)
        {
            _caster.Controller = Agent.ControllerType.Player;
            _controlled.Controller = Agent.ControllerType.AI;
        }

        _cameraView.BeginFadeOutAndIn(0.1f, 0.1f, 0.5f);
        CasterAgent.WieldInitialWeapons();
        _mindControl = false;
    }
}