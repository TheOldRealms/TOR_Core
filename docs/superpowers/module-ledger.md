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
| `done` | Exit criteria met **and merged to `next_update`** — the working master for this program. |
| `n/a` | Does not apply (justify in Notes). |

Branch names are `feature/[Epic][Module]` — e.g. `feature/moduleCrafting`, `feature/FrameworkCrafting`.
Epics stack on the branch below them when the one below is still in review rather than waiting for a merge.

## Ledger

Ordered by suggested Modularize sequence: proven-small first, split-heavy in the middle,
`Careers` last because it spans five current top-level folders.

| # | Module | 1 Modularize | 2 Framework | 3 Strings | 4 Codesmells | 5 Pattern | Notes |
|---|---|---|---|---|---|---|---|
| 0 | `Crafting` | **PR** | **PR** | next | — | — | The worked example. Epic 1 on `feature/moduleCrafting`, Epic 2 on `feature/FrameworkCrafting` — both unmerged, stacked (`next_update` → `moduleCrafting` → `CentralizeCraftingRecipes` → `FrameworkCrafting`). Epic 2 promoted `TORSettlementMenuHelpers` and `TorEnchantingIngredients` to `Framework/` and froze the inbound set as Public Surface in `MODULE.md`. One outbound edge survives — `PriestBehavior` → `Religion.ReligionObject.All` — deferred to Religion's Epic 1. Strings unblocked: `tortools` MCP server is live. |
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
| 22 | `Religion` | — | — | — | — | — | Pulls in `TORFaithModel`. Must resolve `Crafting/PriestBehavior.cs:12` → `ReligionObject.All` during its Epic 1: either promote `ReligionObject` + hero religion extensions to `Framework/`, or move `PriestBehavior` out of Crafting into Religion. See `plans/crafting-framework.md`. |
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
| Extension-method edge audit | **deferred — revisit before the third module's Epic 2** | Epic 2's exit criteria are `using` scans; a Framework extension method returning a module type (`Hero.GetCareer()` → `CareerObject`) passes them invisibly. Found in Crafting Epic 2, spec Amendment 2. Tooling question, not a per-module one — doing it by hand across 30 modules is the expensive path. |
| `TORModuleRegistry` (reflection discovery) | not built | Open question O2. `SubModule.cs` still calls `new XModule().Register...()` explicitly. Fine up to ~10 modules. |
| `Framework/` hook-contract count | 1 (`CraftingCareerHooks`) — unchanged by Crafting Epic 2, which promoted types rather than adding contracts | At the third or fourth narrow pairwise hook class, stop and build a generic registry instead of adding a fifth. |
| One `tor_strings.xml` (no per-module files) | Epic 3, every module | **Settled 2026-09-11.** Grouping is by category/subcategory inside the single file. All access via the `tortools` MCP server. |
