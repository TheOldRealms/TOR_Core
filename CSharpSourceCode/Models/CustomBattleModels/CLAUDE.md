# Models/CustomBattleModels

Model overrides used specifically in TaleWorlds' Custom Battle mode
(`Game.Current.GameType is CustomGame`), registered in `SubModule.OnGameStart`'s
`else if (Game.Current.GameType is CustomGame ...)` branch instead of the campaign branch.

- **`TORCustomBattleMoraleModel`** (`: CustomBattleMoraleModel`).
- **`TORCustomBattleAgentStatCalculateModel`** (`: CustomBattleAgentStatCalculateModel`).

Both mirror their campaign counterparts (`../TORBattleMoraleModel`,
`../TORAgentStatCalculateModel`) but against the Custom Battle base classes, since Custom
Battle mode has no `Campaign`/`Hero`/career context to draw on.
