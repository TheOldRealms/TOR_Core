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
    private Agent _caster;
    private float _radius;

    private bool _mindControl;

    private bool _sucessfulControl;

    private bool _init;
    protected override void OnInit()
    {
        base.OnInit();
        _caster = this.CasterAgent;
    }
    

    protected override void OnBeforeTick(float dt)
    {
        base.OnBeforeTick(dt);
        _init = false;
        var pos = CurrentGlobalPosition;

        var tries = getAmountOfTries();
        
        
        var targets = Mission.Current.GetNearbyAgents(pos.AsVec2, 5, new MBList<Agent>());

    
 
        var baseChance = this.Ability.Template.ScaleVariable1;
        
        
        
        
        foreach (var agent in targets.TakeRandom(tries))
        {

            var level = agent.Character.Level - Hero.MainHero.Level;
     
            var health = agent.Health / agent.HealthLimit;

            
            
            var reducedChance = (level * 0.02f)*health;
            
            if (Hero.MainHero.HasCareerChoice("ByAllMeansKeystone"))
            {
                reducedChance-=0.1f;
            }
            var chance = baseChance - reducedChance;
            
            if (MBRandom.RandomFloat < chance)
            {
                SetupMindControl(agent);
            }
            else
            {
                HandleMissed(agent);
            }
        }

        _init = true;

        Stop();
    }

    private int getAmountOfTries()
    {
        var count = 1;
        if(Hero.MainHero.HasCareerChoice("CaelithsWisdomKeystone"))
        {
            count++;

            count += 2;
        }
        
        if(Hero.MainHero.HasCareerChoice("UnrestrictedMagicKeystone"))
        {
            count++;
        }
        
        if(Hero.MainHero.HasCareerChoice("ForbiddenScrollsOfSapheryKeystone"))
        {
            count++;
        }
        
        if(Hero.MainHero.HasCareerChoice("ByAllMeansKeystone"))
        {
            count++;
        }
        
        if(Hero.MainHero.HasCareerChoice("SecretOfVenomDragonKeystone"))
        {
            count++;
        }
        
        if (Hero.MainHero.HasCareerChoice("LegendsOfMalokKeystone"))
        {
            count++;
        }

        if (Hero.MainHero.HasCareerChoice("SecretOfFellfangKeystone"))
        {
            count++;
        }

        return count;
    }
    
    private void SetupMindControl(Agent target)
    {
        var casterTeam = _caster.Team;
        target.SetTeam(casterTeam,false);
        
        if (Hero.MainHero.HasCareerChoice("SecretOfVenomDragonKeystone"))
        {
            target.Health = target.HealthLimit;
        }

        
        target.ApplyStatusEffect("fellfang_mark",_caster,999999f);
    }

    private void HandleMissed(Agent agent)
    {
        if (Hero.MainHero.HasCareerChoice("UnrestrictedMagicKeystone"))
        {
            var effect = TriggeredEffectManager.CreateNew("apply_fellfang_fire");
            effect.Trigger(agent.Position,Vec3.Up,Agent.Main, Agent.Main.GetCareerAbility().Template,new MBList<Agent>(){agent});
        }
    }
    
    
}