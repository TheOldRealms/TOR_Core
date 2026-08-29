# BattleMechanics/StatusEffect

The buff/debuff system: DOTs/HOTs, damage amplification/resistance, movement/attack/reload
speed mods, lance steadiness, temporary character attributes — applied by spells, prayers,
items, and terrain.

- **`StatusEffectTemplate`** (`ITemplate, IEquatable`) — XML-defined effect data:
  `EffectType` (`DamageOverTime`, `HealthOverTime`, `WindsOverTime`, `DamageAmplification`,
  `Resistance`, `MovementManipulation`, `AttackSpeedManipulation`, `ReloadSpeedManipulation`,
  `LanceSteadiness`, `TemporaryAttributeOnly`), `BaseEffectValue`, target `DamageType`/
  `AttackTypeMask` (for amplification/resistance types), particle/visual config
  (`ParticleId`, `ParticleIntensity`, `ApplyToRootBoneOnly`, `Rotation`).
- **`StatusEffectManager`** (static) — loads/indexes all templates
  (`LoadStatusEffects`, called from `SubModule.OnSubModuleLoad`) and creates runtime
  `StatusEffect` instances (`CreateNewStatusEffect`, handles cloning for career-mutated effects).
- **`StatusEffect`** (`IDisposable, IEquatable`) — one active instance on an agent:
  template + applier + remaining duration + `CastId` (links back to the
  `AbilitySystem/SpellCasting/SpellCastSession` that applied it, for kill/XP credit).
- **`StatusEffectComponent`** (`: AgentComponent, IDisposable`) — attached per-agent
  (via `AddAgentComponentsMissionLogic`); the real engine of the system:
  - Ticks every ~1s (`OnElapsed`), decrementing durations and removing expired effects.
  - Aggregates all active effects into one `EffectAggregate` per tick (sum of DoT/HoT/Winds-
    over-time/speed mods/resistances/amplifications across `AttackTypeMask`×`DamageType`),
    exposed via `GetAmplifiers`/`GetResistances`/`GetMovementSpeedModifier`/etc. for other
    systems (damage models, agent driven properties) to query.
  - Owns a pooled particle-visual system (`StatusParticleVisualPoolEntry`) so repeated
    stacking/refreshing of the same effect on the same agent reuses particle systems
    instead of recreating them, with a dormant-pool cleanup timer.
  - Restores/resynchronizes `AgentDrivenProperties` (speed, swing/reload speed, mount stats)
    when effects apply or expire (`SynchronizeBaseValues`/`RefreshStatusStateAfterRemoval`).
- **`StatusEffectMissionLogic`** (`: MissionLogic`) — mission-level driver that ticks every
  agent's `StatusEffectComponent` (added in `SubModule.OnMissionBehaviorInitialize`).

Consumed by `AbilitySystem` (spell/prayer effects), `BattleMechanics/TriggeredEffect`
(`AssociatedStatusEffects`), and item enchantments (`Items/ItemTrait`).
