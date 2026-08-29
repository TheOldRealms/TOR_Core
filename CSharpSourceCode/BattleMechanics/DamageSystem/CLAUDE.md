# BattleMechanics/DamageSystem

The shared damage-type/resistance math used by every damage source in the mod (melee,
ranged, spells).

- **`DamageType`** enum — TOR's extended damage typing (beyond vanilla Cut/Pierce/Blunt),
  e.g. Fire/Physical/etc. (see `TriggeredEffectTemplate.DamageType`,
  `StatusEffectTemplate.DamageType`), plus an `All` sentinel used for array sizing.
- **`TORDamageHelper`** (static) — the actual formulas:
  - `CalculateDamageWithProportions` — splits one base damage value across multiple
    `DamageType`s by proportion, applies per-type amplification minus resistance
    (used for melee/ranged where `TORAgentApplyDamageModel` blends types).
  - `CalculateSingleTypeDamage` — single-`DamageType` version (used for spells,
    `TORAbilityModel`).
  - `ApplyCareerPassives` — folds `CharacterDevelopment/CareerSystem` `CareerHelper`
    passive bonuses into the attack/defense percentage arrays when the main party is
    involved in the hit.
  - `DetermineMask(Blow/KillingBlow)` — classifies a hit as Spell/Ranged/Melee
    (`AttackTypeMask`), checking `Utilities/TORSpellBlowHelper` first so spell damage isn't
    misclassified as a normal ranged/melee hit.

Consumed by `Models/TORAgentApplyDamageModel`, `Models/TORAbilityModel`,
`BattleMechanics/StatusEffect` (amplification/resistance aggregation), and
`BattleMechanics/TriggeredEffect`.
