# BattleMechanics/AI

Custom battlefield AI, layered on top of TaleWorlds' `HumanAIComponent`/`BehaviorComponent`/
`TeamAIGeneral` framework. Three broad concerns:

1. **Spellcaster ("wizard") AI** — `CastingAI/` — utility-scored decisions about when/what
   to cast and how to stay safe while doing it.
2. **Formation/team AI overrides** — `TeamAI/` — TOR replacements for vanilla formation
   behaviors (charge/retreat/skirmish/defend/aggressive-melee) and the team general.
3. **Misc battlefield AI** — artillery crew AI (`ArtilleryAI/`), non-combatant/civilian
   mission AI (`CivilianMissionAI/`), and shared scoring/utility helpers (`CommonAIFunctions/`).

Root file: **`TORCultureBattleSettings`** (static) — per-culture tuning knobs for battle AI
(aggression, formation spacing, etc. — used to give e.g. Greenskins vs. Empire different
"feel"). **`AIKickAgentComponent`** — lets AI-controlled agents throw kick attacks.

## `CommonAIFunctions/` — shared utility-AI building blocks

Used by both `CastingAI` and (indirectly) `TeamAI`:

- **`IAgentBehavior`** — interface every scoreable per-agent behavior implements
  (`Execute`, `Terminate`, `CalculateUtility`).
- **`BehaviorOption`** — a `(Behavior, Target, UtilityValue)` tuple candidate.
- **`Target` : `Threat`** — a scoring target: an enemy `Formation`, a `TacticalPosition`, etc.
- **`Axis`/`AxisExtensions`** — named utility curves ("axes") that get combined via
  `GeometricMean` into one utility score per candidate (classic utility-AI pattern).
- **`ScoringFunctions`** — the actual curve functions (distance falloff, density, etc.)
  that `Axis`es wrap.
- **`DecisionManager`** — `EvaluateCastingBehaviors`: flattens all behaviors' scored
  options and picks the single best (`MaxBy` utility).
- **`CommonAIFunctions`** (`CommonAIDecisionFunctions`/`CommonAIStateFunctions`/
  `CommonAIFunctions`) — grab-bag of static helpers reused across behaviors (formation
  queries, positioning math).

## `CastingAI/` — spellcasters

- **`Components/WizardAIComponent`** (`: HumanAIComponent`) — replaces the agent's stock
  `HumanAIComponent` entirely. Every ~3s (`EvalInterval`, jittered) it re-evaluates
  `AvailableCastingBehaviors` via `DecisionManager` and picks the best-scoring
  `AbstractAgentCastingBehavior`; every tick it runs that behavior's `TacticalBehavior`
  (positioning/safety) then the casting behavior itself, unless the formation is on
  Hold Fire.
- **`AgentCastingBehaviorConfiguration`** (static) — registers, per ability-effect-type,
  which `Axis`es feed its utility score (`UtilityByType`) and builds the list of available
  behaviors for an agent from its known abilities (`PrepareCastingBehaviors`).
- **`AgentCastingBehavior/`** — one class per casting "shape", all `: AbstractAgentCastingBehavior`
  (`IAgentBehavior`): `MissileCastingBehavior` (base for aimed-projectile spells),
  `AoETargetedCastingBehavior` → `SelectSingleTargetCastingBehavior`/`SelectMultiTargetCastingBehavior`,
  `AoEAdjacentCastingBehavior`, `AoEDirectionalCastingBehavior`, `SummoningCastingBehavior`,
  `ArtilleryPlacementCastingBehavior`, `PreserveWindsCastingBehavior` (chooses not to cast,
  conserving Winds of Magic), `TacticalTeleportCastingBehavior`. `AbstractAgentCastingBehavior`
  itself owns cooldown/range/line-of-sight checks, target updating, and the
  utility-score calc (`CalculateUtility` = geometric mean of its `Axis` list + target
  hysteresis to avoid target-flip-flopping).
- **`AgentTacticalBehavior/`** — `AbstractAgentTacticalBehavior` (`IAgentBehavior`) and
  concrete positioning behaviors run alongside casting: `KeepSafeTacticalBehavior`
  (kite away from danger), `AoEAdjacentTacticalBehavior`/`AoEDirectionalTacticalBehavior`
  (reposition to land AoE shapes).
- **`SupportMissionLogic/`** — `QuerySystemExtensions`/`QuerySystemExtensionsMissionLogic`,
  extends TaleWorlds' formation `QuerySystem` (tactical position queries) for the above.

## `TeamAI/`

- **`FormationBehavior/`** — TOR subclasses of vanilla `BehaviorComponent`s:
  `TORBehaviorBase` (abstract shared base), `TORBehaviorCharge`, `TORBehaviorDefend`,
  `TORBehaviorRetreat`, `TORBehaviorSkirmish`, `TORBehaviorAggressiveMelee`,
  `TORBehaviorProtectArtillery` (new), `TORFormationClass`.
- **`TeamBehavior/TORTeamAIGeneral`** (`: TeamAIGeneral`) — top-level team AI override.
  - **`TeamBehavior/Tactics/TORTacticPositionalArtillery`** (`: TacticDefensiveLine`) —
    a tactic that keeps a defensive line while positioning artillery.
- **`TORMissionCombatantsLogic`** (`: MissionCombatantsLogic`) — replaces the vanilla
  combatants logic (see `SubModule.OnBeforeMissionBehaviorInitialize`,
  `CreateFromInstance`).

## `ArtilleryAI/`

- **`FieldSiegeWeaponAI`** (`: UsableMachineAIBase`) — AI crew behavior for the field
  siege weapons in `BattleMechanics/Artillery` (aim/reload/fire loop).

## `CivilianMissionAI/`

Non-combat town/village mission AI (townsfolk going about their day, reacting to trouble):
`TORDailyBehaviorGroup`/`TORAlarmedBehaviorGroup` (`AgentBehaviorGroup`),
`TORWalkingBehavior`/`TORFightBehavior` (`AgentBehavior`).
