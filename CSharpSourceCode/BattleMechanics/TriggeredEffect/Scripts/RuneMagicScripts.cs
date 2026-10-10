using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Items;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.BattleMechanics.TriggeredEffect.Scripts;

/// <summary>
/// The physical resistance buff comes from the triggered effect. The script adds the player's extras: extra physical damage
/// while empowered by Wisdom of Thungni, and a flaming weapon with the Legacy of Grungni keystone.
/// </summary>
public class OathAndSteelScript : ITriggeredScript
{
    public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
    {
        if (Agent.Main != triggeredByAgent) return;

        var empowered = triggeredByAgent.HasAttribute(CharacterAttributes.WISDOM_THUNGNI);
        var flamingWeapon = Hero.MainHero.HasCareerChoice("LegacyOfGrungniKeystone") ? ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "flaming_weapon") : null;

        if (!empowered && flamingWeapon == null) return;

        foreach (var agent in triggeredAgents)
        {
            if (empowered) agent.ApplyStatusEffect("oath_and_steel_buff_dmg", triggeredByAgent, duration);

            if (flamingWeapon != null) agent.GetComponent<ItemTraitAgentComponent>()?.AddTraitToWieldedWeapon(flamingWeapon, duration);
        }
    }
}

/// <summary>
/// The movement and reload speed buffs come from the triggered effect. The script adds the player's extras: ranged damage
/// with the For Hearth and Home keystone, and attack speed while empowered by Wisdom of Thungni.
/// </summary>
public class HearthAndHome : ITriggeredScript
{
    public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
    {
        if (Agent.Main != triggeredByAgent) return;

        var rangedDamage = Hero.MainHero.HasCareerChoice("ForHearthAndHomeKeystone");
        var empowered = triggeredByAgent.HasAttribute(CharacterAttributes.WISDOM_THUNGNI);

        if (!empowered && !rangedDamage) return;

        foreach (var agent in triggeredAgents)
        {
            if (rangedDamage) agent.ApplyStatusEffect("hearth_and_home_buff_ranged_dmg", triggeredByAgent, duration);

            if (empowered) agent.ApplyStatusEffect("hearth_and_home_buff_ats", triggeredByAgent, duration);
        }
    }
}

/// <summary>
/// The triggered effect collects every agent in range. Friendly agents of the caster are cleansed, enemy spellcasters are drained.
/// </summary>
public class SpellbreakerRuneScript : ITriggeredScript
{
    private const float WindsDrain = 25;

    public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
    {
        var casterTeam = triggeredByAgent?.Team;
        if (casterTeam == null) return;

        var isPlayerCast = Agent.Main == triggeredByAgent;

        foreach (var agent in triggeredAgents)
        {
            if (agent.Team == null) continue;

            if (agent.Team.IsFriendOf(casterTeam))
            {
                // An empty id matches every effect, the Enemy flag limits the removal to harmful ones.
                agent.GetComponent<StatusEffectComponent>()?.RemoveStatusEffect("", StatusEffectComponent.EffectFlag.Enemy);
            }
            else if (agent.Team.IsEnemyOf(casterTeam) && agent.IsSpellCaster())
            {
                agent.GetHero()?.AddWindsOfMagic(-WindsDrain);
            }
        }

        if (!isPlayerCast || !triggeredByAgent.HasAttribute(CharacterAttributes.WISDOM_THUNGNI)) return;

        // Empowered: every enemy spellcaster on the field, Runesmiths excepted, has all abilities put on full cooldown.
        var enemyCasters = Mission.Current.Agents.WhereQ(x => x.Team != null && x.Team.IsEnemyOf(casterTeam) && x.IsSpellCaster() && x.GetHero()?.CharacterObject.IsRunesmith() != true);
        foreach (var agent in enemyCasters)
        {
            var component = agent.GetComponent<AbilityComponent>();
            if (component == null) continue;

            foreach (var ability in component.KnownAbilitySystem) ability.SetCoolDown(ability.Template.CoolDown);
        }
    }
}

/// <summary>
/// The damage and the Sundered debuff come from the triggered effect. When empowered by Wisdom of Thungni, the script also
/// knocks the enemies down and sets them on fire. At most, runeofwrathandruin (At 300 spellcraft, empowered and with all perks and career perks) 
/// will hit for 101 dmg to include its DOT. This puts it just below a mid tier master spell, appropriate for the class </summary>
public class WrathAndRuinScript : ITriggeredScript
{
    public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
    {
        if (Agent.Main != triggeredByAgent || !triggeredByAgent.HasAttribute(CharacterAttributes.WISDOM_THUNGNI)) return;

        foreach (var agent in triggeredAgents)
        {
            agent.ApplyStatusEffect("wrath_and_ruin_dot", triggeredByAgent, duration);
            agent.FallDown();
        }
    }
}