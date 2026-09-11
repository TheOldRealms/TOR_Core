# Module Ledger

The living record of where every module is. **Update this at the end of every epic** — an epic
with an untouched row is not finished.

Epic definitions: [`README.md`](./README.md) · [`specs/2026-09-09-module-lifecycle-design.md`](./specs/2026-09-09-module-lifecycle-design.md).
Which files belong to which module: [`../vertical-slicing-proposal.md`](../vertical-slicing-proposal.md).

## Legend

| Mark | Meaning |
|---|---|
| `—` | Not started. |
| `WIP` | In progress; see the Branch column. |
| `PR` | Code complete, PR open, not yet merged. |
| `done` | Exit criteria met **and merged to `development`**. |
| `n/a` | Does not apply (justify in Notes). |

Branch names are `feature/[module][Epic]` — e.g. `feature/craftingFramework`.

## Ledger

Ordered by suggested Modularize sequence: proven-small first, split-heavy in the middle,
`Careers` last because it spans five current top-level folders.

| # | Module | 1 Modularize | 2 Framework | 3 Strings | 4 Codesmells | 5 Pattern | Notes |
|---|---|---|---|---|---|---|---|
| 0 | `Crafting` | **PR** | next | blocked | — | — | The worked example. Code done on `feature/moduleCrafting`, **unmerged** — needs its own PR under the new one-epic-one-PR rule. Owns `CraftingCareerHooks` in `Framework/`. Strings blocked on `TOR_Tools`. |
| 1 | `BountyMaster` | — | — | — | — | — | Small, few cross-references. Good second module. |
| 2 | `PostBattleLoot` | — | — | — | — | — | Small, few cross-references. |
| 3 | `Villages` | — | — | — | — | — | Pulls in `TORVillageProductionCalculatorModel`; possibly `PlaguedVillageQuestCampaignBehavior` (*verify*). |
| 4 | `Assimilation` | — | — | — | — | — | |
| 5 | `Companions` | — | — | — | — | — | Pulls in `TORCompanionHiringPriceCalculationModel`, `TORCompanionTrainingModel`. |
| 6 | `ServeAsAHireling` | — | — | — | — | — | Pulls in `TORHiringCompatibilityModel` (*verify*). |
| 7 | `SpellTrainers` | — | — | — | — | — | Pulls in `Quests/SpecializeLoreQuest`. |
| 8 | `MasterEngineer` | — | — | — | — | — | Pulls in `Quests/EngineerQuest`. |
| 9 | `RaidingParties` | — | — | — | — | — | |
| 10 | `UniqueSpawns` | — | — | — | — | — | |
| 11 | `RegimentsOfRenown` | — | — | — | — | — | |
| 12 | `RaiseDead` | — | — | — | — | — | |
| 13 | `Banners` | — | — | — | — | — | From `BattleMechanics/`. |
| 14 | `Dismemberment` | — | — | — | — | — | From `BattleMechanics/`. |
| 15 | `Artillery` | — | — | — | — | — | Depends on Framework `AI/ArtilleryAI`. Pulls in `ArtilleryPatches` — **Harmony, ask first**. |
| 16 | `Firearms` | — | — | — | — | — | From `BattleMechanics/`; absorbs `SniperScope/`. |
| 17 | `Tournaments` | — | — | — | — | — | New module: `CustomArenaModes/` + `ArcheryContestMissionController` + `JoustFightMissionController` + `DuelBehavior` + `TORTournamentModel` + `TournamentPatches` + `ArenaPracticePatch` — **Harmony, ask first**. |
| 18 | `Chaos` | — | — | — | — | — | Possibly `HuntCultistsQuestCampaignBehavior` (*verify*). |
| 19 | `Greenskins` | — | — | — | — | — | Home of `GreenskinAICampaignBehavior` unresolved — may fold into `CustomResources` (Waaagh). Decide before starting. |
| 20 | `CharacterCreation` | — | — | — | — | — | |
| 21 | `Diplomacy` | — | — | — | — | — | Pulls in four models; already contributes its own `SaveableTypeDefiner`s — **O1 applies**. |
| 22 | `Religion` | — | — | — | — | — | Pulls in `TORFaithModel`. Owns the `ReligionObject.All` edge `Crafting` currently reaches across for. |
| 23 | `CustomResources` | — | — | — | — | — | `CustomResourceBehavior/` + `CustomResources/` + `WaaaghMeter/` + `TORCustomResourceModel` + `CustomResourcePatches` — **Harmony, ask first**. |
| 24 | `TORCustomSettlement` | — | — | — | — | — | Also absorbs `TORSpecialSettlementBehavior`, `TORMonsterSiegeLogic`, `SiegeEarlyVictoryMissionLogic`. |
| 25 | `Careers` | — | — | — | — | — | **Last.** Spans `CampaignMechanics/Careers/`, `CharacterDevelopment/CareerSystem/`, `CharacterDevelopment` root career types, `Quests/Careers/`, `AbilitySystem/Scripts/` career scripts, `CareerPerkMissionBehavior`, `SimpleCareerQuestBehavior`. |

Module count is a target, not a contract. When a placement changes, amend
`vertical-slicing-proposal.md` and this table together.

## Framework work items

Not modules, but module work depends on them.

| Item | State | Notes |
|---|---|---|
| `ITORModule` / `TORModuleAttribute` | done | In `Framework/`. Proven by `CraftingModule`. |
| `TOR_Tools` XML handling | **being designed — blocks every Strings epic** | All XML handling routes through it. Nothing in Epic 3 starts until it exists. |
| Save-namespace safety probe | **not run — blocking** | Open question O1. Must precede the first module that moves a `SaveableTypeDefiner`-registered type. Cheap and standalone; run it alone so a failure is unambiguous. |
| `TORModuleRegistry` (reflection discovery) | not built | Open question O2. `SubModule.cs` still calls `new XModule().Register...()` explicitly. Fine up to ~10 modules. |
| `Framework/` hook-contract count | 1 (`CraftingCareerHooks`) | At the third or fourth narrow pairwise hook class, stop and build a generic registry instead of adding a fifth. |
| `ModuleData/Strings/` per-module files | not started | First one lands with Crafting's Strings epic, behind `TOR_Tools`. |
