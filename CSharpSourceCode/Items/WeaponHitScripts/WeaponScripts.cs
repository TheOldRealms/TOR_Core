using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
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
        if(receiverAgent == null) return;
        
        
        var statusEffect = _arguments[0];
        
        if (!int.TryParse(_arguments[2], out var maxStackCount))
        {
            return;
        }
        if (!int.TryParse(_arguments[3], out var duration))
        {
            return;
        }

        var component = receiverAgent.GetComponent<StatusEffectComponent>();
        if(component == null)return;
        
        var attributes  =  component.GetTemporaryAttributes(true).ToListQ();

        var count = attributes.CountQ();

        if (count > maxStackCount)
        {
            return;
        }
        
        receiverAgent.ApplyStatusEffect(statusEffect,receiverAgent,duration,false,false,true);
        
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
    
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow  blow, MissionWeapon missionWeapon, AttackCollisionData attackCollision)
    {
        if(attackingAgent == null) return;
        
        if(attackedAgent == null) return;

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
            effect.Trigger(position, Vec3.Up, _triggererAgent, null,new MBList<Agent>(){target} );
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
        if(attackingAgent == null) return;
        
        if(attackedAgent == null) return;
        
        if(blow.InflictedDamage<=0) return;


        var component = attackedAgent.GetComponent<StatusEffectComponent>();
        
        if(component == null)return;



        if (!int.TryParse(_arguments[0], out var percent))
        {
            return;
        }
        
        var damageOverTimeAggregate = component.GetDamageOverTimeAggregate();
        if (damageOverTimeAggregate > 0)
        {
            attackedAgent.ApplyDamage(blow.InflictedDamage*(percent/100),attackedAgent.Position,attackingAgent,true,false,false);
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
        if(attackingAgent == null) return;
        
        if(attackedAgent == null) return;

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
public class BonusDamageOnUndeadEffectScript() : BaseWeaponHitScript
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData attackCollision)
    {
        if(attackingAgent == null) return;
        
        if(attackedAgent == null) return;
        
        if(blow.InflictedDamage<=0) return;

        if (!attackedAgent.IsUndead() && !attackedAgent.IsVampire()) return;
        
        if (!int.TryParse(_arguments[0], out var percent))
        {
            return;
        }
        
        attackedAgent.ApplyDamage(blow.InflictedDamage*(percent/100),attackedAgent.Position,attackingAgent,true,false,false);
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
/// <inheritdoc/>
/// </summary>
/// <inheritdoc/>
public class KnockOutCheckTriggerScript(string[] arguments) : BaseWeaponHitScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        if (attackedAgent.Health <= 0 && attackedAgent.State == AgentState.Unconscious)
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
public class AmmoRechargeOnHit(string[] arguments) : BaseWeaponHitScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        
        if(blow.InflictedDamage<=0)
            return;
        
        WeaponComponentData currentUsageItem = missionWeapon.CurrentUsageItem;
        
        if(currentUsageItem.IsMeleeWeapon)
            return;
        
        
        MissionEquipment equipment = attackingAgent.Equipment;
        for (int i = 0; i < 5; i++)
        {
            EquipmentIndex equipmentIndex = (EquipmentIndex)i;
           
            if ( equipment[equipmentIndex].CurrentUsageItem == missionWeapon.CurrentUsageItem)
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
        var percent = 0f;
        if(int.TryParse(_arguments[0], out var percentValue))
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
            InformationManager.DisplayMessage(new InformationMessage("Stealth Attack!", new TaleWorlds.Library.Color(255, 165, 85)));
            attackedAgent.ApplyDamage((int)(collisionData.InflictedDamage * percent), attackedAgent.Position);
        }
    }
}

public class BeastSlayingScript(string[] arguments) : BaseWeaponHitScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        if (attackedAgent.IsMount || attackedAgent.HasAttribute("Minotaur"))
        {
            var percent = 0f;
            if(int.TryParse(_arguments[0], out var percentValue))
            {
                percent = percentValue / 100f;
            }
            else
            {
                percent = 0.25f;
            }

            
            var newBlow = new Blow(attackingAgent.Index);

            blow.InflictedDamage = (int) percent * blow.InflictedDamage;
            attackedAgent.RegisterBlow(blow,collisionData);     //Works but the display can be broken 
        }
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
        var percent = 0f;
        if(int.TryParse(_arguments[0], out var percentValue))
        {
            percent = percentValue / 100f;
        }
        else
        {
            percent = 0.25f;
        }

            
        var newBlow = new Blow(attackingAgent.Index);

        blow.InflictedDamage = (int) percent * blow.InflictedDamage;
        attackedAgent.RegisterBlow(blow,collisionData);     //Works but the display can be broken 
    }
}

public class HeadShotTriggerScript(string[] arguments) : BaseWeaponHitScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {

        if (collisionData.VictimHitBodyPart == BoneBodyPartType.Head)
        {
            base.OnHit(attackingAgent,attackedAgent,blow, missionWeapon, collisionData);
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
        if (attackedAgent.Health<=0)
        {
            base.OnHit(attackingAgent,attackedAgent,blow, missionWeapon, collisionData);
        }
    }
}


/// <summary>
/// <para>Adds a triggered effect but also damages the wearer.</para>
/// <inheritdoc />
/// </summary>
/// <inheritdoc />
/// <param name="arguments[3]"> damage to wearer</param>
public class BloodLettingTriggerScript(string[] arguments) : WeaponTriggerEffectScript(arguments)
{
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
       base.OnHit(attackingAgent,attackedAgent, blow,missionWeapon, collisionData);

       if (int.TryParse(_arguments[3], out var damageValue))
       {
           attackingAgent.ApplyDamage(damageValue,attackedAgent.Position);
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
            base.OnHit(attackingAgent,attackedAgent, blow,missionWeapon, collisionData);
        }
    }
}



