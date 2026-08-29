# BattleMechanics/AI/CastingAI/AgentCastingBehavior

One `AbstractAgentCastingBehavior` (`: IAgentBehavior`) subclass per spell-casting "shape".
Each wraps cooldown/range/line-of-sight checks and a utility score
(`CalculateUtility` = geometric mean of registered `Axis`es, see
`../../CommonAIFunctions`), and on `Execute()` selects the ability and calls
`Agent.TryCastCurrentAbility`.

- **`AbstractAgentCastingBehavior`** — shared base (see class doc in parent CLAUDE.md).
- **`MissileCastingBehavior`** — base for aimed single-projectile spells.
  - **`AoETargetedCastingBehavior`** — aims at a scored ground/formation target.
    - **`SelectSingleTargetCastingBehavior`**, **`SelectMultiTargetCastingBehavior`** —
      single- vs. multi-target variants.
- **`AoEAdjacentCastingBehavior`** / **`AoEDirectionalCastingBehavior`** — self-centered or
  directional area spells (cones/lines), paired with the matching `AgentTacticalBehavior`.
- **`SummoningCastingBehavior`** — casts summon-type abilities.
- **`ArtilleryPlacementCastingBehavior`** — places artillery/item-placement abilities;
  cooperates with `WizardAIComponent.UpdateArtilleryTargetPosition`.
- **`PreserveWindsCastingBehavior`** — a "do nothing, conserve Winds of Magic" option so the
  utility system can prefer not casting when nothing scores well.
- **`TacticalTeleportCastingBehavior`** — casts reposition/teleport-type abilities.
