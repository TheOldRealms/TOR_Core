# Missions

Campaign-facing "how do we open a mission scene and what runs it" layer — factory methods
for launching a mission scene plus small `MissionLogic` controllers for scripted one-off
fights (as opposed to `BattleMechanics/`, which is the in-mission mechanics that run once
inside any mission).

- **`TorMissionManager`** (static, `[MissionManager]`) — `[MissionMethod]`-attributed
  factory methods (the same pattern as vanilla's `MissionManager`/`CampaignMissionManager`)
  that build and open a `Mission` for a given scene: e.g.
  `OpenArcheryContestMission(scene, ArcheryContestTournamentGame, settlement, culture,
  isPlayerParticipating)`. This is the entry point `CampaignMechanics`/`Quests` code calls
  to actually start one of TOR's custom mission types.
- **`TORMissionAgentHandler`** — shared agent-spawning logic reused by several of the
  scripted-fight controllers below.
- **`MissionExperienceBehavior`** — custom XP granting rules for missions.

## Scripted fight controllers (one per special encounter type, all `: MissionLogic`)

- **`QuestFightMissionController(enemyPartyTemplate, enemyCount, onMissionEnd, ...)`** —
  generic "fight this templated enemy party, then run a callback" controller used by quests.
- **`DuelFightMissionController`** — 1v1 honor duel (see
  `CampaignMechanics/CustomDialogs/DuelBehavior`).
- **`BrawlMissionController`** — settlement brawl mini-game (see
  `CampaignMechanics/TORCustomSettlement/GreenskinBrawlBehavior`).
- **`GraveyardFightMissionController`** — graveyard night-watch fight (see
  `CampaignMechanics/RaiseDead`).
- **`TrollCaveMissionController`** — Troll Cave raid mission.
- **`ArcheryContestMissionController`** / **`JoustFightMissionController`** — wrap the
  `BattleMechanics/CustomArenaModes` archery/joust tournament games for campaign use.
