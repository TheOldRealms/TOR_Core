using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace TOR_Core.Items.WeaponHitScripts;

/// <summary>
/// <para>Adds a buff X to the attacking hero that can stack by Y amount for Z duration. Buff is applied to the weapon wielder, so the attacking agent.</para>
/// </summary>
/// <param name="arguments[0]"> status effect id</param>
/// <param name="arguments[1]"> maximum stack count. status effects are not applied when the maximum stack is reached </param>
/// <param name="arguments[2]"> status effect duration</param>
public class WeaponBuffStackScript(string[] arguments) : BaseWeaponHitScript(arguments)
{
    public override void OnHit(Agent receiverAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        if (receiverAgent == null) return;


        var statusEffect = _arguments[0];

        if (!int.TryParse(_arguments[1], out var maxStackCount))
        {
            return;
        }

        if (!int.TryParse(_arguments[2], out var duration))
        {
            return;
        }

        var component = receiverAgent.GetComponent<StatusEffectComponent>();
        if (component == null) return;

        var count = component.GetActiveEffectCount(statusEffect);
        if (count >= maxStackCount)
        {
            return;
        }

        receiverAgent.ApplyStatusEffect(statusEffect, receiverAgent, duration, false, false, true);

    }
}
/// <summary>
/// Triggers an effect at the position of the target agent ( or on the attacking agent with parameter) 
/// </summary>
/// <param name="arguments[0]"> triggered effect id</param>
/// <param name="arguments[1]"> on attacker: bool to apply the triggered effect instead around attacker ( with trigger radius), can be null</param>
/// <param name="arguments[2]">targeted. The effect gets applied on an individual(attacked agent) instead of an area around a centered target. can be null</param>
public class WeaponTriggerEffectScript(string[] arguments) : BaseWeaponHitScript(arguments)
{
    protected Agent _triggererAgent;

    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData attackCollision)
    {
        if (attackingAgent == null) return;

        if (attackedAgent == null) return;

        if (_triggererAgent == null)
        {
            _triggererAgent = attackingAgent;
        }


        var triggeredEffect = _arguments[0];
        var applyOnAttacker = false;
        if (_arguments.Length >= 2)
        {
            if (!bool.TryParse(_arguments[1], out applyOnAttacker))
            {

            }
        }
        var targeted = false;
        if (_arguments.Length >= 3)
        {
            bool.TryParse(_arguments[2], out targeted);
        }

        var effect = TriggeredEffectManager.CreateNew(triggeredEffect);
        if (!targeted)
        {
            var position = applyOnAttacker ? attackingAgent.Position : attackedAgent.Position;
            effect.Trigger(position, Vec3.Up, _triggererAgent);
        }
        else
        {
            var position = attackedAgent.Position; // doesnt matter
            var target = applyOnAttacker ? attackingAgent : attackedAgent;
            effect.Trigger(position, Vec3.Up, _triggererAgent, null, new MBList<Agent>() { target });
        }
    }
}



/// <summary>
/// Adds an additional damage , if the enemy suffers from DOT effects.
/// </summary>
/// <param name="arguments[0]"> Percent Bonus Damage of already inflicted damage</param>
public class BonusDOTDamageEffectScript(string[] arguments) : BaseWeaponHitScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData attackCollision)
    {
        if (attackingAgent == null) return;

        if (attackedAgent == null) return;

        if (blow.InflictedDamage <= 0) return;


        var component = attackedAgent.GetComponent<StatusEffectComponent>();

        if (component == null) return;



        if (!int.TryParse(_arguments[0], out var percent))
        {
            return;
        }

        var damageOverTimeAggregate = component.GetDamageOverTimeAggregate();
        if (damageOverTimeAggregate > 0)
        {
            ApplyWeaponTraitDamage(attackedAgent, (int)(blow.InflictedDamage * (percent / 100f)), attackedAgent.Position, attackingAgent);
        }

    }
}

/// <summary>
/// Adds an additional damage to undead
/// </summary>
/// <param name="arguments[0]"> Percent Bonus Damage of already inflicted damage</param>
public class UndeadTriggeredEffectScript(string[] arguments) : WeaponTriggerEffectScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData attackCollision)
    {
        if (attackingAgent == null) return;

        if (attackedAgent == null) return;

        if (attackedAgent.IsUndead() || attackedAgent.IsVampire())
        {
            base.OnHit(attackingAgent, attackedAgent, blow, missionWeapon, attackCollision);
        }
    }
}

/// <summary>
/// Adds an additional damage to undead
/// </summary>
/// <param name="arguments[0]"> Percent Bonus Damage of already inflicted damage</param>
public class BonusDamageOnUndeadEffectScript(string[] arguments) : BaseWeaponHitScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData attackCollision)
    {
        if (attackingAgent == null) return;

        if (attackedAgent == null) return;

        if (blow.InflictedDamage <= 0) return;

        if (!attackedAgent.IsUndead() && !attackedAgent.IsVampire()) return;

        if (!int.TryParse(_arguments[0], out var percent))
        {
            return;
        }

        ApplyWeaponTraitDamage(attackedAgent, (int)(blow.InflictedDamage * (percent / 100f)), attackedAgent.Position, attackingAgent);
    }
}



/// <summary>
/// <inheritdoc/>
/// </summary>
/// <inheritdoc/>
public class BuffStackOnKill(string[] arguments) : WeaponBuffStackScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData attackCollision)
    {
        if (attackedAgent.Health <= 0)
        {
            base.OnHit(attackingAgent, attackedAgent, blow, missionWeapon, attackCollision);
        }
    }
}

/// <summary>
/// <para>Triggers effect when eliminating an enemy, with probability based on knockout chance.</para>
/// <para>Uses the game's AgentDecideKilledOrUnconsciousModel to determine trigger probability.</para>
/// </summary>
/// <remarks>
/// <para>Weapon scripts run during OnHit, before the agent state (Killed/Unconscious) is determined.</para>
/// <para>Hooking into OnAgentRemoved to check AgentState.Unconscious would require a new implementation.</para>
/// <para>Instead, we query the same probability model the game uses, giving an equivalent result.</para>
/// </remarks>
/// <inheritdoc/>
public class KnockOutCheckTriggerScript(string[] arguments) : WeaponTriggerEffectScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        // Only trigger when eliminating an enemy
        if (attackedAgent.Health > 0)
            return;

        // Get weapon flags for the knockout probability calculation
        var weaponFlags = WeaponFlags.MeleeWeapon;
        if (!missionWeapon.IsEmpty && missionWeapon.CurrentUsageItem != null)
        {
            weaponFlags = missionWeapon.CurrentUsageItem.WeaponFlags;
        }

        // Get the kill probability from the game's model (same model used by Mission.GetAgentState)
        var model = MissionGameModels.Current?.AgentDecideKilledOrUnconsciousModel;
        if (model == null)
            return;

        float killProbability = model.GetAgentStateProbability(
            attackingAgent,
            attackedAgent,
            blow.DamageType,
            weaponFlags,
            out _);

        // Roll against knockout probability (1 - killProbability)
        // If random < killProbability, this would have been a knockout
        float roll = MBRandom.RandomFloat;
        if (roll < killProbability)
        {
            base.OnHit(attackingAgent, attackedAgent, blow, missionWeapon, collisionData);
        }
    }
}

/// <summary>
/// <para>Adds ammo on kill</para>
/// <inheritdoc/>
/// </summary>
/// <inheritdoc/>
public class AmmoRechargeOnHit : BaseWeaponHitScript
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        
        if (blow.InflictedDamage <= 0)
            return;

        WeaponComponentData currentUsageItem = missionWeapon.CurrentUsageItem;

        if (currentUsageItem.IsMeleeWeapon)
            return;


        MissionEquipment equipment = attackingAgent.Equipment;
        for (int i = 0; i < 5; i++)
        {
            EquipmentIndex equipmentIndex = (EquipmentIndex)i;

            if (equipment[equipmentIndex].CurrentUsageItem == missionWeapon.CurrentUsageItem)
            {
                var amount = missionWeapon.Amount;
                amount++;

                if (amount != missionWeapon.Amount)
                {
                    equipment.SetAmountOfSlot(equipmentIndex, amount, true);
                }
            }

        }
    }
}


public class StealthAttackScript(string[] arguments) : BaseWeaponHitScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow inflictedDamge, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        if (attackedAgent == null) return;

        var percent = 0f;
        if (int.TryParse(_arguments[0], out var percentValue))
        {
            percent = percentValue / 100f;
        }
        else
        {
            percent = 0.25f;
        }

        var agentDirection = attackedAgent.LookDirection;
        var attackerDirection = collisionData.WeaponBlowDir.NormalizedCopy();
        var isStealthAttack = false;
        if (agentDirection.Length != 0 && attackerDirection.Length != 0)
        {
            var degree = Vec3.AngleBetweenTwoVectors(agentDirection, attackerDirection).ToDegrees();
            isStealthAttack = degree < 90;
        }

        if (isStealthAttack || !attackedAgent.AIStateFlags.HasFlag(Agent.AIStateFlag.Alarmed))
        {
            InformationManager.DisplayMessage(new InformationMessage(TORTextHelper.GetText("tor_stealth_attack_text", "Stealth Attack!"), new TaleWorlds.Library.Color(255, 165, 85)));
            ApplyWeaponTraitDamage(attackedAgent, (int)(collisionData.InflictedDamage * percent), attackedAgent.Position, originatesFromAbility: true);
        }
    }
}

public class BeastSlayingScript(string[] arguments) : BaseWeaponHitScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        if (!(attackedAgent.IsMount || attackedAgent.HasAttribute("Minotaur")))
        {
            return;
        }

        if (blow.InflictedDamage <= 0)
        {
            return;
        }

        var percent = 0f;
        if (int.TryParse(_arguments[0], out var percentValue))
        {
            percent = percentValue / 100f;
        }
        else
        {
            percent = 0.25f;
        }

        var bonusDamage = (int)(blow.InflictedDamage * percent);
        if (bonusDamage <= 0)
        {
            return;
        }

        ApplyWeaponTraitDamage(attackedAgent, bonusDamage, attackedAgent.Position, attackingAgent);
    }
}

public class ExtraHeadshotDamageScript(string[] arguments) : BaseWeaponHitScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        if (collisionData.VictimHitBodyPart != BoneBodyPartType.Head)
        {
            return;
        }

        if (blow.InflictedDamage <= 0)
        {
            return;
        }

        var percent = 0f;
        if (int.TryParse(_arguments[0], out var percentValue))
        {
            percent = percentValue / 100f;
        }
        else
        {
            percent = 0.25f;
        }

        var bonusDamage = (int)(blow.InflictedDamage * percent);
        if (bonusDamage <= 0)
        {
            return;
        }

        ApplyWeaponTraitDamage(attackedAgent, bonusDamage, attackedAgent.Position, attackingAgent);
    }
}

public class HeadShotTriggerScript(string[] arguments) : BaseWeaponHitScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {

        if (collisionData.VictimHitBodyPart == BoneBodyPartType.Head)
        {
            base.OnHit(attackingAgent, attackedAgent, blow, missionWeapon, collisionData);
        }
    }
}
/// <summary>
/// <para>Triggers only if the attacked was killed by attack</para>
/// <inheritdoc />
/// </summary>
/// 
///<inheritdoc />
public class TriggerOnKillScript(string[] arguments) : WeaponTriggerEffectScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        if (attackedAgent.Health <= 0)
        {
            base.OnHit(attackingAgent, attackedAgent, blow, missionWeapon, collisionData);
        }
    }
}


/// <summary>
/// <para>Adds a triggered effect but also damages the wearer.</para>
/// <inheritdoc />
/// </summary>
/// <inheritdoc />
/// <param name="arguments[5]"> damage to wearer</param>
public class BloodLettingTriggerScript(string[] arguments) : WeaponTriggerEffectScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        base.OnHit(attackingAgent, attackedAgent, blow, missionWeapon, collisionData);

        if (int.TryParse(_arguments[5], out var damageValue))
        {
            ApplyWeaponTraitDamage(attackingAgent, damageValue, attackedAgent.Position, originatesFromAbility: true);
        }

    }
}

/// <summary>
/// <para>Adds .</para>
/// </summary>
/// 
/// <param name="arguments[0]">Amount of gained Health</param>
/// <param name="arguments[1]">Minimum Health to trigger</param>
public class BloodLeechingScript(string[] arguments) : BaseWeaponHitScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        if (!int.TryParse(_arguments[0], out var amount))
        {
            return;
        }

        if (!int.TryParse(_arguments[1], out var minimumHealth))
        {
            return;
        }

        if (attackingAgent.Health < minimumHealth)
        {
            attackingAgent.Health += amount;
        }
    }
}

/// <summary>
/// <para>Adds a triggered effect only for a certain race</para>
/// <inheritdoc/>
/// </summary>
/// <inheritdoc/>
/// <param name="arguments[3]"> race id</param>
public class RaceTriggerScript(string[] arguments) : WeaponTriggerEffectScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {

        var raceIndex = FaceGen.GetRaceOrDefault(_arguments[3]);

        if (attackedAgent.Character.Race == raceIndex)
        {
            base.OnHit(attackingAgent, attackedAgent, blow, missionWeapon, collisionData);
        }
    }
}