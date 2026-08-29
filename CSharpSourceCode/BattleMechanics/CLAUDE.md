# BattleMechanics

Everything that runs inside a `Mission` (battles, sieges, tournaments, arena fights):
custom AI, status effects, damage/triggered-effect resolution, siege artillery, banners,
dismemberment, firearms, monster-siege support, and misc SFX/voice mission logics. This is
the largest and most mission-behavior-heavy folder in the mod; most classes are either a
`MissionLogic`/`MissionView` registered in `SubModule.OnMissionBehaviorInitialize`, or an
`AgentComponent`/`ScriptComponentBehavior` attached per-agent or per-prop.

## Root-level mission logics

- **`TORBattleAgentLogic`** (`: BattleAgentLogic`) — replaces the vanilla per-agent battle
  logic (kill credit, morale-on-death, etc.) to route through TOR's own systems.
- **`AddAgentComponentsMissionLogic`** — attaches TOR's custom `AgentComponent`s
  (`AbilityComponent`, `StatusEffectComponent`, `AgentVoiceComponent`, AI components) to
  every spawned agent.
- **`CareerPerkMissionBehavior`** — applies/reacts to Career perks during missions.
- **`CinematicCameraMissionView`** — scripted camera moves (cutscene-style shots).
- **`CustomCrosshairMissionBehavior`** — swaps in TOR ability/weapon crosshairs, replacing
  `MissionGauntletCrosshair` (removed in `SubModule`).
- **`SiegeEarlyVictoryMissionLogic`** — lets a siege end early under custom conditions.
- **`TORMonsterSiegeLogic`** — large file; lets giant-monster troops (trolls, etc.) act as
  their own siege "detachment" (ladders/gates), with several internal Harmony patch classes
  (`TORMonsterSiegeLadderQueueConditionsPatch`, `...GateDamagePatch`, etc.) colocated here
  rather than in `HarmonyPatches/`.

## Subfolders

- **`AI/`** — custom battlefield AI: formation behaviors, team AI, spellcaster AI,
  civilian mission AI, artillery AI, and shared decision-making helpers (own CLAUDE.md tree).
- **`Artillery/`** — field siege weapons (trebuchets etc.) beyond the vanilla siege-only ones.
- **`Banners/`** — custom faction banner assets/overrides usable in missions.
- **`CustomArenaModes/`** — archery contest and joust tournament game modes for the arena.
- **`DamageSystem/`** — `DamageType` enum + `TORDamageHelper`, shared damage-type/resistance
  math used by both spell damage (`Models/TORAbilityModel`) and physical damage
  (`Models/TORAgentApplyDamageModel`).
- **`Dismemberment/`** — `DismembermentMissionLogic` (gore/limb-loss on kill).
- **`Firearms/`** — black-powder weapon mechanics (Empire handguns/cannons — reload,
  misfire, continuous-fire tracking).
- **`Morale/`** — `UndeadMoraleAgentComponent` (undead troops ignore/are immune to morale).
- **`SFX/`** — small standalone `ScriptComponentBehavior`s for scene dressing
  (light dampening, object spinning/animating, face-toward-target, flyable objects).
- **`SniperScope/`** — a zoom/scope `ICrosshair` implementation for long-range weapons.
- **`StatusEffect/`** — the buff/debuff (DOT/HOT/stat-mod) system applied by spells, items,
  and prayers.
- **`TriggeredEffect/`** — the data-driven "what happens on hit/cast" system: damage,
  healing, status application, summoning, script triggers — the payload `Ability`/item
  scripts fire off.
- **`Voice/`** — custom battle shouts and per-agent voice-over component.

## Key shared types

- **`TORDamageHelper`** — routes spell/melee/ranged damage through the same
  per-`DamageType` proportion/amplification/resistance math, and folds in Career-passive
  bonuses (`CareerHelper.AddCareerPassivesForDamageValues`) when the main party is involved.
- **`TriggeredEffect`/`TriggeredEffectTemplate`/`TriggeredEffectManager`** — XML-defined
  "effect packages" (damage, heal, status effects, summon, spawn prefab, run a named
  `ITriggeredScript`) resolved by AoE/target-type against nearby agents; used by both
  Ability scripts and item weapon-hit scripts.
