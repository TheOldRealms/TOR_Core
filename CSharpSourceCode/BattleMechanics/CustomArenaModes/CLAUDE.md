# BattleMechanics/CustomArenaModes

New arena/tournament game modes beyond vanilla melee tournaments.

- **`ArcheryContestTournamentGame`** (`: FightTournamentGame`) +
  **`ArcheryContestTournamentBehavior`** (`: TournamentBehavior`, in
  `CustomTournamentBehaviors.cs`) + **`ArcheryContestAgentController`** (`: AgentController`)
  — a ranged-only archery contest mode. Driven in the campaign layer by
  `Missions/ArcheryContestMissionController`.
- **`JoustTournamentGame`** (`: FightTournamentGame`) +
  **`JoustTournamentBehavior`** (`: TournamentBehavior`, in `CustomTournamentBehaviors.cs`)
  + **`JoustLaneEndVolumeBox`** (`: MissionObject`) — a mounted joust mode with dedicated
  lanes; the volume box marks/triggers lane-end turnaround. Driven by
  `Missions/JoustFightMissionController`.

See `Missions/` for the campaign-facing controllers that set these modes up, and
`CharacterCreation`/settlement menus for how a player enters them.
