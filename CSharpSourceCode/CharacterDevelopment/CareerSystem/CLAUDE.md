# CharacterDevelopment/CareerSystem

The Career data model — Warhammer "prestige class" progression (Grail Knight, Witch
Hunter, Runelord, Slayer, Orc Boss, etc.), each with a signature `CareerAbility`
(see `AbilitySystem/CareerAbility` + `AbilitySystem/Scripts/CareerAbilityScript`), a perk
tree, and often a special right-click "career button" action.

## Core model

- **`CareerObject`** (`: PropertyObject`, XML-defined via `MBObjectManager`) — one career:
  `AbilityTemplateID`/`AbilityScriptType` (its signature ability), `ChargeType`
  (`CooldownOnly` vs `Custom`, the latter driven by a `ChargeFunction` computing charge from
  combat hits — see `CareerAbilityChargeSupplier`), `RootNode`/`ChoiceGroups` (the perk tree),
  an eligibility `Predicate<Hero>`. `MutateAbility`/`MutateTriggeredEffect`/
  `MutateStatusEffect` let unlocked choices numerically tweak the career's ability/effects
  at cast time (called from `AbilitySystem/Scripts/CareerAbilityScript`,
  `BattleMechanics/StatusEffect/StatusEffectComponent`).
- **`CareerChoiceObject`** (`: PropertyObject`, + nested `MutationObject`, `PassiveEffect`)
  — one perk-tree node: a `PassiveEffect` (flat/percentage stat bonus,
  `PassiveEffectType`-keyed, consumed by `CareerHelper.ApplyBasicCareerPassives`/
  `AddCareerPassivesForDamageValues`) and/or a `MutationObject` (numeric tweak applied to
  the career's ability/triggered-effect/status-effect).
- **`CareerChoiceGroupObject`** (`: PropertyObject`) — groups/tiers of `CareerChoiceObject`s
  forming the tree structure.
- **`CareerHelper`** (static) — the query/application layer used everywhere else in the
  codebase: `ApplyBasicCareerPassives`, `AddCareerPassivesForDamageValues` (feeds
  `BattleMechanics/DamageSystem/TORDamageHelper`), `IsValidCareerMissionInteractionBetweenAgents`,
  `PrayerCooldownIsNotShared`.
- **`CareerObjectVM`/`CareerChoiceObjectVM`/`CareerChoiceGroupObjectVM`** — view-models for
  the tree UI; **`CareerAbilityEffectVM`** — shows the ability's current numeric effect.
- **`CareerScreen`** (`: ScreenBase, IGameStateListener`) + **`CareerScreenGameState`**
  (`: GameState`) + **`CareerScreenVM`** — the dedicated Career screen (pick/respec choices).

## Subfolders

- **`Choices/`** — one `TORCareerChoicesBase` subclass per career, registering that
  career's actual `CareerChoiceObject`/`PassiveEffect`/`MutationObject` data
  (see its CLAUDE.md).
- **`CareerButton/`** — right-click "special action" buttons some careers add to the
  party/prisoner screen (see its CLAUDE.md).
