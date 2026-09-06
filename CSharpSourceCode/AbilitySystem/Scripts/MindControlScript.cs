using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Scripts;

public class MindControlScript : CareerAbilityScript
{
    private Agent _caster;
    private Vec3 _targetPosition;

    private bool _init;
    protected override void OnInit()
    {
        base.OnInit();
        _caster = this.CasterAgent;
    }


    protected override void OnBeforeTick(float dt)
    {
        _init = false;
        if (_init)
        {
            return;
        }
        _targetPosition = this.CurrentGlobalPosition;
        _init = true;
    }

    protected override void OnAfterTick(float dt)
    {
        base.OnBeforeTick(dt);

        var tries = GetAmountOfTries();


        var targets = Mission.Current.GetNearbyEnemyAgents(_targetPosition.AsVec2, 5, _caster.Team, new MBList<Agent>());

        var baseChance = this.Ability.Template.ScaleVariable1;


        foreach (var agent in targets.TakeRandom(tries))
        {

            var level = agent.Character.Level - Hero.MainHero.Level;

            var health = agent.Health / agent.HealthLimit;



            var reducedChance = (level * 0.02f) * health;

            if (Hero.MainHero.HasCareerChoice("ByAllMeansKeystone"))
            {
                reducedChance -= 0.1f;
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

    private int GetAmountOfTries()
    {
        var count = 1;
        if (Hero.MainHero.HasCareerChoice("CaelithsWisdomKeystone"))
        {
            count++;

            count += 2;
        }

        if (Hero.MainHero.HasCareerChoice("UnrestrictedMagicKeystone"))
        {
            count++;
        }

        if (Hero.MainHero.HasCareerChoice("ForbiddenScrollsOfSapheryKeystone"))
        {
            count++;
        }

        if (Hero.MainHero.HasCareerChoice("ByAllMeansKeystone"))
        {
            count++;
        }

        if (Hero.MainHero.HasCareerChoice("SoulBindingKeystone"))
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
        if (target == null || target.State != AgentState.Active || target.IsFadingOut()) return;

        target.SetTeam(casterTeam, false);

        if (Hero.MainHero.HasCareerChoice("SoulBindingKeystone"))
        {
            target.Health = target.HealthLimit;
        }


        target.ApplyStatusEffect("fellfang_mark", _caster, 999999f);

        if (Hero.MainHero.HasCareerChoice("SecretOfFellfangKeystone"))
        {
            Hero.MainHero.AddWindsOfMagic(1);
        }
    }

    private void HandleMissed(Agent agent)
    {
        if (Hero.MainHero.HasCareerChoice("UnrestrictedMagicKeystone"))
        {
            var effect = TriggeredEffectManager.CreateNew("apply_fellfang_fire");
            effect.Trigger(agent.Position, Vec3.Up, Agent.Main, Agent.Main.GetCareerAbility().Template, new MBList<Agent>() { agent });
        }
    }


}