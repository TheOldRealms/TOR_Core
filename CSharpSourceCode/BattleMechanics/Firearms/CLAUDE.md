# BattleMechanics/Firearms

**`FirearmsMissionLogic`** (`: MissionLogic`) — black-powder weapon mechanics (Empire
handguns/pistols, grenades, the Dwarf "Trollhammer torpedo", cannon-type explosions):
custom explosion damage/radius per weapon type (`_explosionDamage`/`_explosionRadius`,
torpedo variant sized 30% smaller), continuous-fire tracking per agent
(`ContinousFiringData`, `_continousFiringAgents`) for sustained-fire weapons, and managed
gunshot/grenade sound events (validated once, played/cleaned up per shot). Added in
`SubModule.OnMissionBehaviorInitialize`.
