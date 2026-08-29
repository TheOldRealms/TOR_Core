# AbilitySystem

The "magic" layer of TOR: Warhammer's Winds of Magic spells, priestly Prayers, and
per-Career signature abilities, all built on one shared runtime.

## Core model

- **`Ability`** (abstract, `Ability.cs`) — runtime instance wrapping an `AbilityTemplate`.
  Handles cooldowns, cast validation (`CanCast`/`IsDisabled`), cast-frame calculation for
  player/AI/quick-cast, spawning the effect `GameEntity`, and wiring up the right
  `AbilityScript` subclass based on `AbilityEffectType` (Missile, Wind, Heal, Augment,
  Summoning, Hex, Vortex, Blast, Bombardment, ArtilleryPlacement, TimeWarpEffect, etc.).
  Three concrete subclasses:
  - **`Spells/Spell.cs`** — Winds-of-Magic spells (costs Winds, has miscast chance, `SpellTier`, `BelongsToLoreID`).
  - **`Prayer.cs`** — priest prayers (separate cooldown pool, `PrayerLevel` tier).
  - **`CareerAbility.cs`** — each Career's unique active ability (see `CharacterDevelopment/CareerSystem`).
  - **`ItemBoundAbility.cs`** — abilities granted by wielding a specific item.
- **`AbilityTemplate`** (`ITemplate`) — XML-serializable data definition (damage via
  `TriggeredEffects`, visuals, cast type/time, crosshair type, target type, distances...).
  Loaded/cloned via `AbilityFactory` from `AbilityTemplates.xml`-style data.
- **`AbilityFactory`** — loads and indexes all `AbilityTemplate`s at startup (`SubModule.OnSubModuleLoad`).
- **`AbilityComponent`** (`AgentComponent`) — attached to agents that can cast; tracks
  known abilities (`KnownAbilitySystem`), Anvil-of-Doom state for Rune magic, prayer cooldown, quick-cast state.
- **`AbilityManagerMissionLogic`** — mission-level `MissionLogic` driving input → cast
  requests, ticking cooldowns/wind-up casts, and mediating with the HUD.
- **`AbilityHUD_VM`** / **`CareerAbilityHUD_VM`** / **`AbilityRadialSelection_VM`** +
  `AbilityRadialSelectionItemWidget` — in-mission UI (hotbar + radial ability picker).
- **`AbilityType`** / **`AbilityEffectType`** / **`AbilityTargetType`** / `CastType` /
  `TriggerType` enums (in `AbilityType.cs`) drive most of the switch logic above.
- **`SeekerController`/`SeekerParameters`** — homing-missile behavior for seeker spells.
- **`SummonedAgentOrigin`/`SummonedCombatant`** — lets summoned troops (from Summoning abilities)
  count as a proper battle combatant/origin for kill credit, banners, etc.

## Subfolders

- **`CrossHairs/`** — player aiming reticles per ability shape (see its CLAUDE.md).
- **`Scripts/`** — `AbilityScript` (`ScriptComponentBehavior`) implementations: the actual
  mission-side visual/physics/timing logic for each `AbilityEffectType`, plus one
  `CareerAbilityScript` subclass per Career signature ability (see its CLAUDE.md).
- **`SpellCasting/`** — `SpellCastSession`, tracks an in-progress multi-step cast (used for
  Winds-of-Magic miscast rolls / windup UX outside the basic Ability flow).
- **`Spells/`** — `Spell`, `LoreObject` (a school/"Lore" of magic, e.g. Lore of Fire),
  `SpellCastingLevel`; plus `SpellBook/` (in-mission spellbook screen: `SpellBookScreen`,
  `SpellBookVM`, item/lore view-models) and `Prayers/` (battle prayer book equivalent:
  `BattlePrayerScreen`, `BattlePrayersVM`, `PrayerItemVM`).

## Notes

- Loaded once at `SubModule.OnSubModuleLoad` via `AbilityFactory.LoadTemplates()`.
- AI casting decisions live outside this folder in
  `BattleMechanics/AI/CastingAI` (`WizardAIComponent`, `AgentCastingBehavior`,
  `AgentTacticalBehavior`) — this folder is the mechanism, that folder is the brain.
