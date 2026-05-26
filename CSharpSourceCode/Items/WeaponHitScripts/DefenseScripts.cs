using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.Items.WeaponHitScripts;

/// <summary>
/// Adds a buff X to the attacking hero that can stack by Y amount for Z duration. Buff is applied to the Armor wearer, so the attacked agent
/// </summary>
/// <param name="arguments[0]"> status effect id</param>
/// <param name="arguments[1]"> maximum stack count. status effects are not applied when the maximum stack is reached </param>
/// <param name="arguments[2]"> status effect duration</param>
public class DefenseStackBuffScript(string[] arguments) : WeaponBuffStackScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData attackCollision)
    {
        base.OnHit(receiverAgent: attackedAgent, attackingAgent, blow, missionWeapon, attackCollision);  // note the flip from attacker to attacked
    }
}

/// <summary>
/// <para>NOTE THAT THE ATTACKED AGENT IS THE WEARER OF THE ARMOR WITH TRAIT. </para>
/// <inheritdoc />
/// </summary>
/// 
///<inheritdoc />
public class DefenseTriggerEffectScript(string[] arguments) : WeaponTriggerEffectScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData attackCollision)
    {
        if (attackedAgent == null) return;
        _triggererAgent = attackedAgent;
        base.OnHit(attackingAgent, attackedAgent, blow, missionWeapon, attackCollision);
    }
}


/// <summary>
/// <para>Triggers an effect that exclusive for undead and vampires
/// </para>
/// <inheritdoc />
/// </summary>
/// <inheritdoc />
public class UndeadConditionDefenseTriggerEffectScript(string[] arguments) : DefenseTriggerEffectScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData attackCollision)
    {
        if (attackingAgent == null) return;
        if(attackedAgent == null) return;

        if (attackingAgent.IsUndead() || attackingAgent.IsVampire())
        {
            base.OnHit(attackingAgent, attackedAgent, blow, missionWeapon, attackCollision);
        }


    }
}
/// <summary>
/// <para>Triggers an effect when it suits the attack type</para>
/// <inheritdoc />
/// </summary>
/// <inheritdoc />
/// n<param name="arguments[3]"> Attack type : Spell, Melee, Range. All is not considered</param>
public class AttackTypeDefenseTriggerScript(string[] arguments) : DefenseTriggerEffectScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData attackCollisionData)
    {
        if (attackingAgent == null) return;

        if (missionWeapon.IsEmpty) return;

        var argument = _arguments[3];


        var attackType = AttackTypeMask.All;
        switch (argument)
        {
            case "melee":
                attackType = AttackTypeMask.Melee;
                break;
            case "ranged":
                attackType = AttackTypeMask.Ranged;
                break;
            case "spell":
                attackType = AttackTypeMask.Spell;
                break;
            default:
                return;
        }

        if (attackType == AttackTypeMask.Spell)
        {
            if (!TORSpellBlowHelper.IsSpellBlow(blow)) return;
        }

        if (attackType == AttackTypeMask.Ranged && missionWeapon.GetWeaponComponentDataForUsage(missionWeapon.CurrentUsageIndex).IsRangedWeapon)
        {
            base.OnHit(attackingAgent, attackedAgent, blow, missionWeapon, attackCollisionData);
        }

        if (attackType == AttackTypeMask.Melee && missionWeapon.GetWeaponComponentDataForUsage(missionWeapon.CurrentUsageIndex).IsMeleeWeapon)
        {
            base.OnHit(attackingAgent, attackedAgent, blow, missionWeapon, attackCollisionData);
        }
    }
}


/// <summary>
/// <para>Only applies on Undead</para>
/// <inheritdoc />
/// </summary>
/// <inheritdoc />
public class UndeadConditionDefenseStackBuffScript(string[] arguments) : DefenseStackBuffScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        if (attackingAgent == null) return;
        if (attackedAgent == null) return;

        if (attackingAgent.IsUndead() || attackedAgent.IsVampire())
        {
            base.OnHit(attackingAgent, attackingAgent, blow, missionWeapon, collisionData);
        }
    }
}

public class ShieldScript : BaseWeaponHitScript
{
}

public class TimeCoolDownReductionShieldScript() : BaseWeaponHitScript()
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData attackCollisionData)
    {
        if (attackingAgent == null) return;
        if (attackedAgent == null) return;

        var hero = attackedAgent.GetHero();

        if (hero == null) return;

        if (hero.HasAnyCareer())
        {
            var component = attackedAgent.GetComponent<AbilityComponent>();
            if (component.CareerAbility.IsOnCooldown())
            {
                var coolDown = component.CareerAbility.GetCoolDownLeft();
                coolDown--;
                attackedAgent.GetComponent<AbilityComponent>().CareerAbility.SetCoolDown(coolDown);
            }
        }
    }
}


public class ReviveScript() : BaseWeaponHitScript()
{
    private static Mission _trackedMission;
    private static readonly System.Collections.Generic.HashSet<int> _revivedAgentIndices = new();

    private static void ResetReviveStateIfMissionChanged()
    {
        if (_trackedMission == Mission.Current)
        {
            return;
        }

        _trackedMission = Mission.Current;
        _revivedAgentIndices.Clear();
    }

    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData attackCollisionData)
    {
        if (attackedAgent == null) return;

        ResetReviveStateIfMissionChanged();

        if (_revivedAgentIndices.Contains(attackedAgent.Index))
        {
            return;
        }

        if (attackedAgent.Health > 0f)
        {
            return;
        }

        _revivedAgentIndices.Add(attackedAgent.Index);  //TODO move revived weapon indice to mission logic
        attackedAgent.Heal(attackedAgent.HealthLimit * 0.5f);
    }
}

public class DefenseWindsDrainScript(string[] arguments) : BaseWeaponHitScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        int.TryParse(_arguments[0], out var amount);

        if (attackingAgent.IsSpellCaster())
        {
            if (TORSpellBlowHelper.IsSpellBlow(blow))
            {
                attackingAgent.GetHero().AddWindsOfMagic(-amount);
            }
        }

    }
}

public class ShieldBreakScript(string[] arguments) : DefenseTriggerEffectScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        if (collisionData.IsShieldBroken)
        {
            base.OnHit(attackingAgent, attackedAgent, blow, missionWeapon, collisionData);
        }
    }
}