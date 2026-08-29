# BattleMechanics/AI/CommonAIFunctions

Shared utility-AI primitives used by `CastingAI` (and available to `TeamAI`):

- **`IAgentBehavior`** — interface every scoreable behavior implements (`Execute`,
  `Terminate`, `CalculateUtility`).
- **`BehaviorOption`** — `(Behavior, Target, UtilityValue)` candidate tuple.
- **`Target : Threat`** — a scoring target (enemy `Formation`, `TacticalPosition`, etc.).
- **`Axis` / `AxisExtensions`** — named utility curves combined via `GeometricMean` into one
  score per candidate — the core "utility AI" mechanism.
- **`ScoringFunctions`** — the actual curve functions (distance falloff, density, etc.).
- **`DecisionManager`** — `EvaluateCastingBehaviors`: flattens every behavior's scored
  options and picks the single best via `MaxBy`.
- **`CommonAIDecisionFunctions` / `CommonAIStateFunctions` / `CommonAIFunctions`**
  (`CommonAIFunctions.cs`) — grab-bag of static formation/positioning helpers reused across
  behaviors.
