using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.Extensions;
using TOR_Core.Items;

namespace TOR_Core.BattleMechanics.TriggeredEffect.Scripts;

public class OathAndSteelScript : ITriggeredScript
{
    public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
    {
        if (Agent.Main == triggeredByAgent)
            if (Hero.MainHero.HasCareerChoice("LegacyOfGrungniKeystone"))
            {
                var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "flaming_weapon");
                if (trait == null) return;
                foreach (var agent in triggeredAgents)
                {
                    var comp = agent.GetComponent<ItemTraitAgentComponent>();
                    if (comp != null) comp.AddTraitToWieldedWeapon(trait, duration);
                }
            }
    }
}

/// <summary>
/// The Runesmith adds in base condition just a singe effect, while with the attribute gained from the career ability extra effects are applied.
/// </summary>
public class HearthAndHome : ITriggeredScript
{
    public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
    {
        var extraEffect = false;
        var empowered = false;
        if (Agent.Main == triggeredByAgent)
        {
            extraEffect = Hero.MainHero.HasAttribute("ForHearthAndHomeKeystone");
            if (triggeredByAgent.HasAttribute("WisdomThungni")) empowered = true;
        }

        if (!empowered && !extraEffect) return;


        foreach (var agent in triggeredAgents)
        {
            if (extraEffect) agent.ApplyStatusEffect("hearth_and_home_buff_ranged_dmg", triggeredByAgent, duration);

            if (empowered) agent.ApplyStatusEffect("hearth_and_home_buff_speed_empowered", triggeredByAgent, duration);
        }
    }
}

public class SpellbreakerRuneScript : ITriggeredScript
{
    public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
    {
        var template = TriggeredEffectManager.GetTemplateWithId("rune_of_spellbreaking");
        if (template == null)
            return;

        var reduction = template.DamageAmount;


        foreach (var agent in triggeredAgents)
        {
            if (agent.Team.IsPlayerAlly)
            {
                var component = agent.GetComponent<StatusEffectComponent>();
                component.RemoveStatusEffect("", StatusEffectComponent.EffectFlag.Enemy);//Sly : why is an empty status effect attempting to be removed? This will do nothing because status effects with empty ids will be filtered out before attempting addition to a component.
            }

            if (!agent.IsSpellCaster()) continue;

            agent.GetHero().AddWindsOfMagic(-reduction);
        }

        if (Agent.Main == triggeredByAgent && Agent.Main.HasAttribute("WisdomThungni"))
        {
            var agents = Mission.Current.AllAgents.WhereQ(x => x.IsSpellCaster() && !x.GetHero().CharacterObject.IsRunesmith());

            foreach (var agent in agents)
            {
                var component = agent.GetComponent<AbilityComponent>();
                if (component == null) continue;

                foreach (var ability in component.KnownAbilitySystem) ability.SetCoolDown(ability.Template.CoolDown);
            }
        }
    }
}

public class WrathAndRuinScript : ITriggeredScript
{
    public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
    {
        var extraEffect = false;
        var empowered = false;
        if (Agent.Main == triggeredByAgent)
            if (triggeredByAgent.HasAttribute("WisdomThungni"))
                empowered = true;

        if (!empowered) return;


        foreach (var agent in triggeredAgents)
        {
            agent.ApplyStatusEffect("wrath_and_ruin_dot", triggeredByAgent, duration);
            agent.FallDown();
        }
    }
}