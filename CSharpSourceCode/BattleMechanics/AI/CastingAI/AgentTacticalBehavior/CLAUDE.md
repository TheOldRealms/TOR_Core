# BattleMechanics/AI/CastingAI/AgentTacticalBehavior

Positioning/safety behaviors run every tick alongside a caster's chosen
`AgentCastingBehavior` (see `WizardAIComponent.OnTick`), all `: AbstractAgentTacticalBehavior`
(`IAgentBehavior`).

- **`AbstractAgentTacticalBehavior`** — shared base.
- **`KeepSafeTacticalBehavior`** — kite away from nearby danger/melee threats.
- **`AoEAdjacentTacticalBehavior`** / **`AoEDirectionalTacticalBehavior`** — reposition to
  land the caster's adjacent/directional AoE shape on its target.
