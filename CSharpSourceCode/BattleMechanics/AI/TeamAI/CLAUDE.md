# BattleMechanics/AI/TeamAI

TOR's replacements for vanilla team/formation-level battle AI.

- **`TORMissionCombatantsLogic`** (`: MissionCombatantsLogic`) — swaps in for the vanilla
  combatants logic; installed via `SubModule.OnBeforeMissionBehaviorInitialize`
  (`TORMissionCombatantsLogic.CreateFromInstance`).
- **`FormationBehavior/`** — per-formation behavior overrides (charge/defend/retreat/
  skirmish/aggressive-melee + a new protect-artillery behavior).
- **`TeamBehavior/`** — `TORTeamAIGeneral` (`: TeamAIGeneral`) and, under `Tactics/`,
  `TORTacticPositionalArtillery` (`: TacticDefensiveLine`).
