# AbilitySystem/Scripts

`AbilityScript` (`ScriptComponentBehavior`) and its subclasses — the mission-side entity
that actually flies/sits/pulses in the world while an `Ability` is active, and decides
when to trigger the ability's `TriggeredEffect`s (damage/heal/status/etc., from
`BattleMechanics/TriggeredEffect`).

## Base class: `AbilityScript.cs`

Owns the entity lifetime: ticking, movement (`ShouldMove`/`GetNextGlobalFrame`), lifetime
expiry vs `Template.Duration`, collision detection (`OnPhysicsCollision`/`CollidedWithAgent`),
sound looping, and `TriggerType`-driven firing (`OnCollision`, `EveryTick`, `TickOnce`,
`OnStop`). `TriggerEffects` opens/uses an `AbilityManagerMissionLogic` spell session
(`_castId`) so damage from this cast can be attributed for XP/kill-credit. Subclasses
override `GetNextGlobalFrame`, `ShouldMove`, `OnBeforeTick`/`OnAfterTick`,
`GetEffectsToTrigger`, `HandleCollision`.

## Effect-type scripts (one per `AbilityEffectType`, picked by `Ability.AddBehaviour`)

`ProjectileScript`, `MissileScript` (also serves SeekerMissile via `SeekerController`),
`WindScript`, `HealScript`, `AugmentScript` (also TacticalReposition), `SummoningScript`,
`HexScript`, `VortexScript`, `BlastScript`, `BombardmentScript`, `ArtilleryPlacementScript`
(also ItemPlacement), `TimeWarpScript`.

## Career signature abilities

**`CareerAbilityScript`** — base for `CareerAbility`'s effect: re-clones its
`TriggeredEffectTemplate` per cast and lets the caster's `CareerObject.MutateTriggeredEffect`
tweak numbers (perk-driven scaling), then follows the caster instead of flying free.
One subclass per Career ability (`CharacterDevelopment/CareerSystem/CareerObject`
wires these up by StringID):

- `KnightlyChargeScript` / `KnightlyStrikeScript` (Grail Knight/Knight of the Old World)
- `AccusationScript`, `MindControlScript`, `ShadowStepScript` (Witch Hunter / Necrarch-adjacent)
- `TeleportScript` (+ `DamselTeleportScript`, `TeleportTriggeredScript`) (Grail Damsel)
- `ArcaneConduit`, `WisdomOfThungniScript` (Runelord/Runesmith — Dwarf rune magic)
- `ArmedToDaTeef`, `CallOfDaGreen`, `RedFuryScript`, `WrathOfTheWoodScript` (Greenskin careers)
- `AxeOfUlricScript`, `HawkEyeScript`, `LethalShotScript`, `DoomSeekingScript`,
  `SummonChampionScript`, `ImpenetrableScript` (various human/elf careers)
- **`CareerAbilityMissleScript`** — missile variant base, subclassed by
  `ArrowOfKurnousScript` (Waywatcher) and `BlastOfAgonyScript`.

## Other

- **`ITriggeredScript`** — separate lightweight interface (see
  `BattleMechanics/TriggeredEffect/Scripts`) implemented by `TeleportTriggeredScript` here.
