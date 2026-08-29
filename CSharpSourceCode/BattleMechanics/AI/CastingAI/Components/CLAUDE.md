# BattleMechanics/AI/CastingAI/Components

**`WizardAIComponent`** (`: HumanAIComponent`) — replaces a spellcasting agent's stock
`HumanAIComponent` (removes it via `agent.RemoveComponent` in its ctor). Every ~3 seconds
(jittered per-agent) it re-evaluates `AvailableCastingBehaviors` through
`DecisionManager.EvaluateCastingBehaviors` and adopts the best-scoring
`AbstractAgentCastingBehavior`; every tick (unless the formation is on Hold Fire) it runs
that behavior's `TacticalBehavior` then the behavior itself. See parent `../CLAUDE.md`.
