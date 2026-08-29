# BattleMechanics/AI/CastingAI

Utility-AI for agents that can cast spells/prayers/career abilities in battle. See
`../CLAUDE.md` ("CastingAI") for the full picture. Quick map:

- **`AgentCastingBehaviorConfiguration`** (this folder, root) — registers which `Axis`es
  score each casting-behavior type and builds an agent's `PrepareCastingBehaviors` list
  from its known abilities.
- **`Components/WizardAIComponent`** — the `HumanAIComponent` replacement driving it all.
- **`AgentCastingBehavior/`** — the actual cast decisions (one class per spell "shape").
- **`AgentTacticalBehavior/`** — positioning/safety run alongside casting.
- **`SupportMissionLogic/`** — `QuerySystem` extensions the above rely on for formation queries.
