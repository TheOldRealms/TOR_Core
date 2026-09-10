# Module Ledger

The living record of where every module is in the four-step lifecycle. **Update this at the
end of every step** — a step with an untouched ledger row is not finished.

Step definitions: [`specs/2026-09-09-module-lifecycle-design.md`](./specs/2026-09-09-module-lifecycle-design.md).
Which files belong to which module: [`../vertical-slicing-proposal.md`](../vertical-slicing-proposal.md).

## Legend

| Mark | Meaning |
|---|---|
| `—` | Not started. |
| `WIP` | In progress; see the Branch column. |
| `done` | Step's exit criteria met and merged. |
| `n/a` | Step does not apply (justify in Notes). |

## Ledger

Ordered by suggested Step 1 sequence: proven-small first, split-heavy in the middle,
`Careers` last because it spans five current top-level folders.

| # | Module | S1 Modularize | S2 Refactor | S3 Patternize | S4 Localize | Branch | Notes |
|---|---|---|---|---|---|---|---|
| 0 | `Crafting` | done | — | — | — | `feature/moduleCrafting` | The worked example the Step 1 process was generalized from. Owns `CraftingCareerHooks` in `Framework/`. **Not yet merged to `development`.** |
| 1 | `BountyMaster` | — | — | — | — | | Small, few cross-references. Good second module. |
| 2 | `PostBattleLoot` | — | — | — | — | | Small, few cross-references. |
| 3 | `Villages` | — | — | — | — | | Pulls in `TORVillageProductionCalculatorModel`; possibly `PlaguedVillageQuestCampaignBehavior` (see proposal, flagged *verify*). |
| 4 | `Assimilation` | — | — | — | — | | |
| 5 | `Companions` | — | — | — | — | | Pulls in `TORCompanionHiringPriceCalculationModel`, `TORCompanionTrainingModel`. |
| 6 | `ServeAsAHireling` | — | — | — | — | | Pulls in `TORHiringCompatibilityModel` (flagged *verify* in the proposal). |
| 7 | `SpellTrainers` | — | — | — | — | | Pulls in `Quests/SpecializeLoreQuest`. |
| 8 | `MasterEngineer` | — | — | — | — | | Pulls in `Quests/EngineerQuest`. |
| 9 | `RaidingParties` | — | — | — | — | | |
| 10 | `UniqueSpawns` | — | — | — | — | | |
| 11 | `RegimentsOfRenown` | — | — | — | — | | |
| 12 | `RaiseDead` | — | — | — | — | | |
| 13 | `Banners` | — | — | — | — | | From `BattleMechanics/`. |
| 14 | `Dismemberment` | — | — | — | — | | From `BattleMechanics/`. |
| 15 | `Artillery` | — | — | — | — | | From `BattleMechanics/`; depends on Framework `AI/ArtilleryAI`. Pulls in `ArtilleryPatches`. |
| 16 | `Firearms` | — | — | — | — | | From `BattleMechanics/`; absorbs `SniperScope/`. |
| 17 | `Tournaments` | — | — | — | — | | New module: `BattleMechanics/CustomArenaModes/` + `Missions/ArcheryContestMissionController` + `JoustFightMissionController` + `CustomDialogs/DuelBehavior` + `TORTournamentModel` + `TournamentPatches` + `ArenaPracticePatch`. |
| 18 | `Chaos` | — | — | — | — | | Possibly `Quests/HuntCultistsQuestCampaignBehavior` (flagged *verify*). |
| 19 | `Greenskins` | — | — | — | — | | Home of `GreenskinAICampaignBehavior` is unresolved — may fold into `CustomResources` (Waaagh) instead of being its own module. Decide before starting. |
| 20 | `CharacterCreation` | — | — | — | — | | |
| 21 | `Diplomacy` | — | — | — | — | | Pulls in four models; already contributes its own `SaveableTypeDefiner`s. |
| 22 | `Religion` | — | — | — | — | | Pulls in `TORFaithModel`. |
| 23 | `CustomResources` | — | — | — | — | | `CustomResourceBehavior/` + `CustomResources/` + `WaaaghMeter/` + `TORCustomResourceModel` + `CustomResourcePatches`. |
| 24 | `TORCustomSettlement` | — | — | — | — | | Also absorbs `TORSpecialSettlementBehavior`, `TORMonsterSiegeLogic`, `SiegeEarlyVictoryMissionLogic`. |
| 25 | `Careers` | — | — | — | — | | **Last.** Spans `CampaignMechanics/Careers/`, `CharacterDevelopment/CareerSystem/`, `CharacterDevelopment` root career types, `Quests/Careers/`, `AbilitySystem/Scripts/` career scripts, `CareerPerkMissionBehavior`, `SimpleCareerQuestBehavior`. |

Module count is a target, not a contract — the proposal flags several placements as *verify*,
and a module may be added, merged or dropped as Step 1 uncovers what a folder actually touches.
When that happens, amend `vertical-slicing-proposal.md` and this table together.

## Framework work items

Not modules, but tracked here because module work depends on them.

| Item | State | Notes |
|---|---|---|
| `ITORModule` / `TORModuleAttribute` | done | In `Framework/`. Proven by `CraftingModule`. |
| `TORModuleRegistry` (reflection discovery) | not built | `SubModule.cs` still calls `new XModule().Register...()` explicitly. Open question O2 in the spec — explicit list is fine up to roughly ten modules. |
| Save-namespace safety probe | **not done — blocking** | Open question O1 in the spec. Must be answered before the first module that moves a `SaveableTypeDefiner`-registered type. |
| `Framework/` hook-contract count | 1 (`CraftingCareerHooks`) | At the third or fourth narrow pairwise hook class, stop and build a generic registry instead of adding a fifth. |
| `ModuleData/Strings/` per-module string files | not started | Needed before the first Step 4. |
