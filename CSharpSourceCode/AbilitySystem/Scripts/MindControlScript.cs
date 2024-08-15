using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts;

public class MindControlScript : CareerAbilityScript
{
    private MissionCameraFadeView _cameraView;
    private Agent _controlled;
    private Agent _caster;
    private float _radius;

    private bool _mindControl;

    private bool _sucessfulControl;

    private bool _init;
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

    protected override void OnBeforeTick(float dt)
    {
        base.OnBeforeTick(dt);
        if(_init)return;
        if(IsFading) return;
        var pos = CurrentGlobalPosition;

        var tries = getAmountOfTries();
        
        
        var targets = Mission.Current.GetNearbyAgents(pos.AsVec2, 5, new MBList<Agent>());

    
 
        var baseChance = this.Ability.Template.ScaleVariable1;
        


        foreach (var agent in targets.TakeRandom(tries))
        {

            var level = agent.Character.Level - Hero.MainHero.Level;
     
            var health = agent.Health / agent.HealthLimit;
            
            var reducedChance = (level * 0.02f)*health;
            var chance = baseChance - reducedChance;
            
            TORCommon.Say(chance.ToString());
            if (MBRandom.RandomFloat < chance)
            {
                TORCommon.Say("mindcontrol");
                SetupMindControl(agent);
            }
            else
            {
                HandleMissed(agent);
            }
        }
        _init = true;
    }

    private int getAmountOfTries()
    {
        var count = 1;
        if(Hero.MainHero.HasCareerChoice("CaelithsWisdomKeystone"))
        {
            count++;

            count += 2;
        }
        
        if(Hero.MainHero.HasCareerChoice("SecretOfForestDragonKeystone"))
        {
            count++;
        }

        if (Hero.MainHero.HasCareerChoice("LegendsOfMalokKeystone"))
        {
            count++;
        }

        return count;
    }
    
    private void SetupMindControl(Agent target)
    {
        var casterTeam = _caster.Team;
        target.SetTeam(casterTeam,false);
        
        if (Hero.MainHero.HasCareerChoice("SecretOfForestDragonKeystone"))
        {
            target.Health = target.HealthLimit;
        }
    }

    private void HandleMissed(Agent agent)
    {
        if (Hero.MainHero.HasCareerChoice("SecretOfStarDragonKeystone"))
        {
            var effect = TriggeredEffectManager.CreateNew("apply_fellfang_fire");
            effect.Trigger(agent.Position,Vec3.Up,_caster, this.Ability.Template , new MBList<Agent>(){agent});
        }
        
        
    }

    
    
    protected override void OnBeforeRemoved(int removeReason)
    {
        Mission.Current.OnBeforeAgentRemoved -= AgentRemoved;
        if (_sucessfulControl)
        {
            
            ShiftControllerToCaster();
            
            if (Hero.MainHero.HasCareerChoice("CaelithsWisdomKeystone"))
            {
                var effect = TriggeredEffectManager.CreateNew("boltofaqshy_explosion");
                
                effect.Trigger(_controlled.Position,Vec3.Up,_caster);
            }

        }
    }
    
    private void ShiftControllerToCaster()
    {
        if(_caster==null) return;
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