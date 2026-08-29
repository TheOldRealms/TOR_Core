# BattleMechanics/TriggeredEffect

The data-driven "payload" system for what happens when a spell/weapon/trap fires: damage,
healing, status-effect application, unit summoning, prefab spawning, or running a named
custom script. Both `AbilitySystem` (via `Ability.Template.TriggeredEffects`) and item
weapon-hit scripts (`Items/WeaponHitScripts`) resolve their actual effects through this.

- **`TriggeredEffectTemplate`** (`ITemplate`) — XML data: `DamageType`/`DamageAmount`/
  `DamageVariance`, `Radius`/`HasShockWave`, `TargetType` (Self/Enemy/Friendly/All),
  `ImbuedStatusEffects` (→ resolved to `StatusEffectTemplate`s), particle/sound feedback,
  and optional `ScriptNameToTrigger` (reflection-instantiated `ITriggeredScript`),
  `SpawnPrefabName`, or `TroopIdToSummon`/`NumberToSummon` for summon effects.
- **`TriggeredEffectManager`** (static) — loads/indexes templates
  (`LoadTemplates`, from `SubModule.OnSubModuleLoad`), `GetTemplateWithId`/
  `GetTemplatesWithIds` used everywhere a `TriggeredEffects` string list needs resolving.
- **`TriggeredEffect`** (`IDisposable`) — runtime `Trigger(...)` call: resolves targets by
  `TargetType` + radius, applies the `TORAbilityModel`'s skill/perk scaling
  (`GetSkillEffectivenessForAbilityDamage`, radius/duration scaling) when in campaign,
  routes damage/heal through `TORMissionHelper.DamageAgents`/`HealAgents`, queues status
  effects (batches through `AbilityManagerMissionLogic` when present, for correct
  visual/sound sequencing and spell-session bookkeeping), spawns burst
  particles/sound, and finally reflection-invokes the configured `ITriggeredScript`.
  Uses a small time-delayed disposal queue (`ProcessPendingDisposals`) to avoid
  disposing sound handles mid-tick.
- **`AnimationTrigger`/`AnimationTriggerManager`/`AnimationTriggerMissionLogic`** —
  separate, simpler system: named triggers fired from animation events (e.g. a weapon
  swing keyframe) rather than ability/weapon-hit code; `AnimationTriggerTuple` pairs a
  trigger name with its handler.

## Subfolder

- **`Scripts/`** — `ITriggeredScript` implementations invoked via `ScriptNameToTrigger`
  (see its CLAUDE.md).
