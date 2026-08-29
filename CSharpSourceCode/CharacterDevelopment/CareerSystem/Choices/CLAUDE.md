# CharacterDevelopment/CareerSystem/Choices

One `TORCareerChoicesBase` (abstract, ctor takes the owning `CareerObject`) subclass per
Career, registering that career's `CareerChoiceGroupObject`/`CareerChoiceObject` tree
(passives + ability mutations). `CareerChoicesHelper` (static) holds shared construction
helpers used by all of them.

Subclasses (one per `CharacterDevelopment.TORCareers` entry):
`BlackGrailKnightCareerChoices`, `BloodKnightCareerChoices`, `GrailDamselCareerChoices`,
`GrailKnightCareerChoices`, `GreyLordCareerChoices`, `ImperialMagisterCareerChoices`,
`IronbreakerCareerChoices`, `KnightOldWorldCareerChoices`, `MercenaryCareerChoices`,
`NecrarchCareerChoices`, `NecromancerCareerChoices`, `OrcBossCareerChoices`,
`OrcShamanCareerChoices`, `RunelordCareerChoices`, `SlayerCareerChoices`,
`SpellsingerCareerChoices`, `VampireCountCareerChoices`, `WardenCareerChoices`,
`WarriorPriestCareerChoices`, `WarriorPriestUlricCareerChoices`,
`WaywatcherCareerChoices`, `WitchHunterCareerChoices`.

Consumed by `CharacterDevelopment/TORCareerChoices`/`TORCareerChoiceGroups` at startup and
rendered by `../CareerScreenVM`/`CareerChoiceObjectVM`.
