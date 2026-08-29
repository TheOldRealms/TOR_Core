# SaveGameSystem

Single file: **`TORSaveableTypeDefiner`** (`: SaveableTypeDefiner`, base id `771000`) —
registers every custom class/interface that needs to be persisted in save games (party
components, settlement components, quest data, inventory-use-script data, custom-battle
tournament games, notification types, etc.) with a stable numeric id via
`AddClassDefinition`/`AddInterfaceDefinition`.

**Critical constraint (called out in the file's own remarks):** never change an existing
type's id once players have saves using it — old saves will crash on load when the game
scans the save folder. Only ever append new ids; only renumber if you're deliberately
breaking save compatibility and know players will need to clear their saves.

Other `SaveableTypeDefiner`s exist too, colocated with what they define (e.g.
`CampaignMechanics/Diplomacy/TORAllianceWarBehavior.TORAllianceWarBehaviorTypeDefiner`,
`HonorAllianceDecision.HonorAllianceDecisionTypeDefiner`) — check for those before assuming
this file is the only place a type gets registered.
