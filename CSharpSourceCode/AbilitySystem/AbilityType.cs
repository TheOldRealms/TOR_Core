namespace TOR_Core.AbilitySystem
{
    public enum AbilityType
    {
        Innate,
        Spell,
        Prayer,
        ItemBound,
        CareerAbility
    }

    public enum AbilityEffectType
    {
        Projectile,
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
        TimeWarpEffect,
        CareerAbilityEffect
    }

    public enum AbilityTargetType
    {
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
        Instant,
        WindUp,
        Channel
    }

    public enum TriggerType
    {
        EveryTick,
        OnCollision,
        TickOnce,
        OnStop,
        None
    }

    //This is for triggeredeffects.
    public enum TargetType
    {
        Friendly,
        Enemy,
        All,
        FriendlyHero,
        EnemyHero,
        Self
    }
}