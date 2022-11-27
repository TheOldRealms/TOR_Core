﻿namespace TOR_Core.AbilitySystem
{
    public enum AbilityType
    {
        Innate,
        Spell,
        Prayer,
        ItemBound,
        SpecialMove,
        CareerAbility
    }

    public enum AbilityEffectType
    {
        Missile,
        SeekerMissile,
        Wind,
        Vortex,
        Heal,
        Augment,
        Hex,
        Summoning,
        Bombardment,
        Blast,
        ArtilleryPlacement,
        AgentMoving
    }

    public enum AbilityTargetType
    {
        Invalid,
        Self,
        SingleEnemy,
        SingleAlly,
        EnemiesInAOE,
        AlliesInAOE,
        WorldPosition,
        GroundAtPosition
    }

    public enum CastType
    {
        Invalid,
        Instant,
        WindUp,
        Channel
    }

    public enum TriggerType
    {
        EveryTick,
        OnCollision,
        TickOnce
    }
    
    public enum ChargeType
    {
        Time,
        Kills,
        Damage,
        Invalid,
    }

    //This is for triggeredeffects.
    public enum TargetType
    {
        Friendly,
        Enemy,
        All,
        FriendlyHero,
        EnemyHero,
        Invalid
    }
}