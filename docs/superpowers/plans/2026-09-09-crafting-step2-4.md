> # ⚠️ SUPERSEDED — 2026-09-11
>
> Written against the old four-step spec, before the epic split
> ([spec Amendment 1](../specs/2026-09-09-module-lifecycle-design.md#amendment-1--2026-09-11-four-steps-became-five-epics)).
> **Do not execute as written.** Its content is sound and is being re-cut into four epic plans:
>
> | This file's task | Becomes | Plan |
> |---|---|---|
> | Task 1 — close the Step 1 gaps | **Epic 2 Framework** | `crafting-framework.md` |
> | Task 5 — own string file | Epic 3 Strings *(blocked on `TOR_Tools`)* | `crafting-strings.md` |
> | Tasks 2 + 3 — decompose the three god-files | Epic 4 Codesmells | `crafting-codesmells.md` |
> | Task 4 — town-service NPC pattern | Epic 5 Pattern | `crafting-pattern.md` |
> | Task 6 — close out | Split across all four closeouts | — |
>
> Task 1's finding still stands and is the reason Epic 2 exists: Crafting's ledger row read
> `done` while its cross-module references were never cut.

# Crafting Module — Steps 2–4 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Take `CampaignMechanics/Crafting/` from "Step 1 done" through Step 2 (file-by-file refactor), Step 3 (patternize), and Step 4 (localize), leaving it as the reference module the remaining 25 follow.

**Architecture:** Three of Crafting's 27 files carry 1,423 of its 4,709 lines and each does three or four unrelated jobs. Step 2 extracts collaborator classes out of them *without changing which types are `CampaignBehaviorBase`s* — Bannerlord keys campaign-behavior save data by type name, so splitting a behavior into two behaviors silently loses saved state. Step 3 then names the shape that becomes visible once `EnchanterTownBehavior` and `PriestBehavior` are decomposed: both are the same "town-service NPC" (culture→template map, spawn-if-needed, access predicate, dialog tree), which becomes one abstract base class. Step 4 gives the module its own string file and closes the 26 ids that today exist only as inline C# defaults and are therefore invisible to the environment team.

**Tech Stack:** C# on .NET Framework 4.8, old-style (non-SDK) `TOR_Core.csproj`; Bannerlord `CampaignBehaviorBase` / `GameModel` / Gauntlet; Harmony; localization via `TORTextHelper` + `ModuleData` `Strings` XML nodes.

**Spec:** [`../specs/2026-09-09-module-lifecycle-design.md`](../specs/2026-09-09-module-lifecycle-design.md)

## Global Constraints

Every task's requirements implicitly include this section.

- **Every source file must appear in `TOR_Core.csproj` as `<Compile Include="..." />`.** A file not listed silently does not build, and surfaces as a missing-symbol error elsewhere. Every Create in this plan has a matching csproj edit.
- **Never rename a `CampaignBehaviorBase` subclass, and never change a `dataStore.SyncData` key.** Behaviour save data is keyed by type name and then by the string key. Both are save-breaking.
- **Never renumber a `SaveableTypeDefiner` id.** `TorItemDuplicationData` is id `16` in `SaveGameSystem/SaveableTypeDefiners.cs` and stays there.
- **No default interface methods.** The net48 CLR rejects them with `CS8701` regardless of the configured `LangVersion`. Where "implement only what you need" is wanted, use an abstract base class with empty virtuals.
- **Localization form:** id is `tor_<name>`, the default text carries a `{=str_tor_<name>}` prefix, lookups go through `TORTextHelper` (never `GameTexts.FindText`), and a line break is `{newline}` — not an escape sequence.
- **There is no automated test suite.** The solution holds exactly one project and references no test framework. Verification is: build, then execute the numbered scenarios in a manual test plan under `docs/testplans/`, following the format of `docs/testplans/p1-enchantment-blueprint-store.md` (numbered `S1…Sn` scenarios plus a Run record table). Wherever this plan says "verify", it means run those scenarios in-game and fill in the Run record — not run a test command.
- **Judging a build:** a command-line `dotnet build` on this project emits a large set of unrelated NLog / Harmony / `IsExternalInit` errors. Judge a build by whether that error set is *unchanged from before the branch*, not by whether it is empty. Building in the IDE against the installed game is the reliable check.
- **Do not `git commit` or `git push` unless the user explicitly asks.** The commit steps in this plan describe the commit to make when asked; they are not standing authorization.
- **Behaviour must not change** in Tasks 1–4. These are structural tasks: a player should not be able to tell the branch landed. Task 5 changes only what text is loaded from where.

---

## Findings that shaped this plan

Read this before Task 1; it explains why Task 1 exists at all.

**Crafting is not actually at Step 1 done.** The ledger says `done`; the spec's exit criterion 1 says "no `using TOR_Core.<OtherModule>` inside the module's folder, and no other module's folder names this module." Neither half holds:

*Outbound* — Crafting references two other modules:

| File | Reference | Verdict |
|---|---|---|
| `TORArtisanDistrictCampaignBehavior.cs:13` | `TOR_Core.CampaignMechanics.TORCustomSettlement` → `TORSettlementMenuHelpers.RearrangeTownMenus` | Fixable now, cleanly — Task 1 |
| `PriestBehavior.cs:12` | `TOR_Core.CampaignMechanics.Religion` → `ReligionObject.All` | **Deferred to Religion's own Step 1** — see Task 1 Step 4 |

Its other `TOR_Core` usings (`Extensions`, `Utilities`, `Items`, `Framework`, `Models`, `AbilitySystem.Spells`, and `CharacterDevelopment` for `TORSkills`) all point at Framework-classified code and are legal.

*Inbound* — ten files outside the module reference Crafting types, and all ten are live (no stale usings): `Extensions/HeroExtensions.cs`, `Items/ItemTrait.cs`, `Models/TORFaithModel.cs`, `Ink/InkStory.cs`, `HarmonyPatches/ItemPatches.cs`, `HarmonyPatches/ViewModelPatches.cs`, `Utilities/TORConsoleCommands.cs`, `SaveGameSystem/SaveableTypeDefiners.cs`, plus module code in `Careers/`, `CustomResourceBehavior/`, `TORCustomSettlement/`, `CareerSystem/CareerButton/`, and three `Quests/Careers/` quests.

Untangling all ten is a project in its own right and is **not** in this plan's scope. What this plan does instead is freeze that set as Crafting's declared public surface (Task 1), so Step 2 knows exactly which names it may not rename. That converts an unbounded risk into a checklist.

**A doc comment is wrong.** `Framework/CraftingCareerHooks.cs` states that "`CraftingCareerHookRegistrations` never references `CampaignMechanics/Crafting` either." It does: `CharacterDevelopment/CareerSystem/CraftingCareerHookRegistrations.cs` calls `EnchantmentBlueprints.Learn(...)` on lines 81–94. Task 1 corrects the comment rather than the code — the call is legitimate, it is the claim that is false.

**A save key is misnamed and must stay misnamed.** `PriestBehavior.cs:305` reads `dataStore.SyncData("_learnedEnchantment", ref _learnedBlessings);`. The key says "enchantment", the field is blessings. Correcting the key would orphan the saved value. Leave it; Task 3 adds a comment saying why.

---

## File Structure

**Created**

| File | Responsibility |
|---|---|
| `Framework/TORSettlementMenuHelpers.cs` | Moved from `TORCustomSettlement/`. Generic town-menu reordering; 4 callers across 2 modules. |
| `CampaignMechanics/Crafting/EnchanterCultureMap.cs` | The culture → (enchanter template, enchantment suffixes) data table and the multi-use-culture rules. Data only. |
| `CampaignMechanics/Crafting/EnchanterSpawner.cs` | Creating and locating the enchanter hero for a settlement. |
| `CampaignMechanics/Crafting/EnchanterAccessRules.cs` | The "may the player use this enchanter" predicates. |
| `CampaignMechanics/Crafting/EnchanterDialogs.cs` | The enchanter conversation tree. |
| `CampaignMechanics/Crafting/PriestCultMap.cs` | The priest template/cult data table. |
| `CampaignMechanics/Crafting/PriestSpawner.cs` | Creating and locating priest heroes for a settlement. |
| `CampaignMechanics/Crafting/PriestDialogs.cs` | The priest conversation tree. |
| `CampaignMechanics/Crafting/BattleLootTracker.cs` | Map-event army bookkeeping, extracted from `LootCampaignBehavior`. |
| `CampaignMechanics/Crafting/TownServiceNpcBehavior.cs` | Step 3: the abstract base both town-service behaviors derive from. |
| `ModuleData/Strings/tor_crafting_strings.xml` | The module's own string file. |
| `docs/testplans/crafting-step2-4.md` | Manual test plan; the verification instrument for every task here. |

**Modified**

| File | Change |
|---|---|
| `CampaignMechanics/Crafting/EnchanterTownBehavior.cs` | 757 → roughly 150 lines. Keeps the type name, the `RegisterEvents` wiring and `SyncData`; delegates the rest. |
| `CampaignMechanics/Crafting/PriestBehavior.cs` | 307 → roughly 90 lines, same treatment. |
| `CampaignMechanics/Crafting/LootCampaignBehavior.cs` | 359 → roughly 200 lines. |
| `CampaignMechanics/Crafting/TORArtisanDistrictCampaignBehavior.cs` | One `using` swap. |
| `CampaignMechanics/Crafting/MODULE.md` | Gains a Public Surface section; refreshed at the end of each task. |
| `CampaignMechanics/Crafting/CLAUDE.md` | Refreshed at the end of Tasks 2, 3, 4, 5. |
| `Framework/CraftingCareerHooks.cs` | Doc-comment correction. |
| `CampaignMechanics/TORCustomSettlement/*.cs` (3 files) | `using` updates after the helper moves to Framework. |
| `ModuleData/tor_strings.xml` | 38 Crafting ids removed (3 shared ones stay). |
| `SubModule.xml` | One new `Strings` XML node. |
| `TOR_Core.csproj` | `<Compile Include>` for every created/moved file. |
| `docs/superpowers/module-ledger.md` | Crafting's row, at the end of each step. |

---

## Task 1: Close the Step 1 gaps that Steps 2–4 depend on

Behaviour-neutral. Produces the frozen public surface every later task checks against.

**Files:**
- Create: `CSharpSourceCode/Framework/TORSettlementMenuHelpers.cs` (moved)
- Delete: `CSharpSourceCode/CampaignMechanics/TORCustomSettlement/TORSettlementMenuHelpers.cs`
- Modify: `CSharpSourceCode/CampaignMechanics/Crafting/TORArtisanDistrictCampaignBehavior.cs:13`
- Modify: `CSharpSourceCode/CampaignMechanics/TORCustomSettlement/GoblinRecruitmentBehavior.cs`, `GreenskinBrawlBehavior.cs`, `TORCustomSettlementCampaignBehavior.cs` (usings)
- Modify: `CSharpSourceCode/Framework/CraftingCareerHooks.cs` (doc comment)
- Modify: `CSharpSourceCode/CampaignMechanics/Crafting/MODULE.md`
- Modify: `CSharpSourceCode/TOR_Core.csproj`
- Modify: `docs/superpowers/module-ledger.md`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: `TOR_Core.Framework.TORSettlementMenuHelpers.RearrangeTownMenus(GameMenu menu, string entryId, string targetEntryId, bool above = false)` — same signature, new namespace. Also produces the **Public Surface** list in `MODULE.md`, which Tasks 2–4 treat as un-renameable.

- [ ] **Step 1: Move `TORSettlementMenuHelpers` into Framework**

The file is 50 lines with a single public method and no `using TOR_Core.*` dependencies of its own — it is generic town-menu plumbing that two modules already share, so it meets the spec's promote-to-Framework test.

```bash
cd "CSharpSourceCode"
git mv CampaignMechanics/TORCustomSettlement/TORSettlementMenuHelpers.cs Framework/TORSettlementMenuHelpers.cs
```

Change its namespace declaration:

```csharp
// was: namespace TOR_Core.CampaignMechanics.TORCustomSettlement
namespace TOR_Core.Framework
```

- [ ] **Step 2: Update the four call sites**

In each of these files, replace `using TOR_Core.CampaignMechanics.TORCustomSettlement;` with `using TOR_Core.Framework;` — but only remove the old `using` if nothing else in the file needs it (the three `TORCustomSettlement/` files are in that namespace already and simply drop the line; `TORArtisanDistrictCampaignBehavior.cs` genuinely swaps it):

- `CampaignMechanics/Crafting/TORArtisanDistrictCampaignBehavior.cs:13` — swap to `using TOR_Core.Framework;`
- `CampaignMechanics/TORCustomSettlement/GoblinRecruitmentBehavior.cs` — add `using TOR_Core.Framework;`
- `CampaignMechanics/TORCustomSettlement/GreenskinBrawlBehavior.cs` — add `using TOR_Core.Framework;`
- `CampaignMechanics/TORCustomSettlement/TORCustomSettlementCampaignBehavior.cs` — add `using TOR_Core.Framework;`

- [ ] **Step 3: Update the csproj**

In `TOR_Core.csproj`, change the existing entry:

```xml
<!-- was -->
<Compile Include="CampaignMechanics\TORCustomSettlement\TORSettlementMenuHelpers.cs" />
<!-- becomes -->
<Compile Include="Framework\TORSettlementMenuHelpers.cs" />
```

- [ ] **Step 4: Record the Religion edge as deferred**

Do **not** cut `PriestBehavior.cs:12`'s dependency on `TOR_Core.CampaignMechanics.Religion` in this plan. `Religion/` has not been through Step 1, so any cut made now is a guess at where Religion's boundary will land — exactly what the spec's "modularize first" rule exists to prevent. The two candidate resolutions, for whoever does Religion's Step 1:

1. Promote `ReligionObject` and its hero extension methods to `Framework/`, leaving Religion's behaviors as the module — mirroring how `LoreObject` is Framework while spell content is not.
2. Move `PriestBehavior` out of Crafting into Religion, on the grounds that a priest NPC granting blessings is Religion content that happens to open an enchanting screen.

Add to `docs/superpowers/module-ledger.md`, on the `Religion` row's Notes cell:

```
Must resolve `PriestBehavior.cs` → `ReligionObject` during its Step 1: either promote `ReligionObject` + hero religion extensions to `Framework/`, or move `PriestBehavior` from Crafting into Religion. See `plans/2026-09-09-crafting-step2-4.md` Task 1 Step 4.
```

And on the `Crafting` row's Notes cell, append:

```
Step 1 has one known outbound gap left (`PriestBehavior` → `Religion`), deferred to Religion's Step 1.
```

- [ ] **Step 5: Correct the false claim in `CraftingCareerHooks`**

In `Framework/CraftingCareerHooks.cs`, the class doc comment ends with a claim that is not true. Replace this sentence:

```
/// and CraftingCareerHookRegistrations never references CampaignMechanics/Crafting either.
```

with:

```
/// CraftingCareerHookRegistrations does still reference CampaignMechanics/Crafting in one
/// direction - it calls EnchantmentBlueprints.Learn(...) to grant beginner blueprints - which
/// is the allowed direction: a module may call into another module's content through a
/// Framework hook it is populating. What matters is that Crafting never names a Career type.
```

- [ ] **Step 6: Add the Public Surface section to `MODULE.md`**

Append to `CampaignMechanics/Crafting/MODULE.md`, above the `## See also` section. This is the list Tasks 2–4 must not rename; it is derived from the ten external consumers listed in Findings.

```markdown
## Public surface — do not rename without updating every consumer

Ten files outside this module reference Crafting types. Until those are decoupled, the
following names are frozen; anything not on this list is free to be renamed or restructured
by a refactor pass.

| Name | Consumed by |
|---|---|
| `EnchantmentBlueprints.Learn` / `.IsKnown` / `.GetKnown` | `Ink/InkStory.cs`, `Utilities/TORConsoleCommands.cs`, `CharacterDevelopment/CareerSystem/CraftingCareerHookRegistrations.cs`, `CareerSystem/CareerButton/RunelordCareerButtonBehavior.cs`, `Quests/Careers/OrcShamanQuest2.cs`, `RunelordQuest.cs`, `RunesmithQuest.cs` |
| `EnchantmentBlueprintBehavior` | `Utilities/TORConsoleCommands.cs` |
| `EnchantmentHelper.GetBlueprintRequirements` / `.GetUnmetRequirement` | `Utilities/TORConsoleCommands.cs`, `CampaignMechanics/CustomResourceBehavior/OathGoldBehavior.cs`, `CampaignMechanics/TORCustomSettlement/TORCustomSettlementCampaignBehavior.cs` |
| `TorEnchantingIngredients` (+ `TorTradeGoodType`) | `Items/ItemTrait.cs`, `Models/TORFaithModel.cs`, `CampaignMechanics/Careers/TORCareerPerkCampaignBehavior.cs`, `CustomResourceBehavior/OathGoldBehavior.cs`, `CareerSystem/CareerButton/RunelordCareerButtonBehavior.cs` |
| `TORArtisanDistrictCampaignBehavior.Instance` | `HarmonyPatches/ItemPatches.cs` |
| `RefinementVMExtension` | `HarmonyPatches/ViewModelPatches.cs` |
| `EnchanterTownBehavior` (type name) | `Extensions/HeroExtensions.cs`; also frozen by save-data keying |
| `EnchantmentIngredientLootCampaignBehavior` | `CampaignMechanics/TORCustomSettlement/TORCustomSettlementCampaignBehavior.cs` |
| `TorItemDuplicationData` | `SaveGameSystem/SaveableTypeDefiners.cs` (save id 16) |
```

- [ ] **Step 7: Build and verify**

Build the solution. Expected: the pre-existing unrelated error set, unchanged; no new errors. The only behavioural surface touched is a namespace, so no in-game verification is required for this task beyond launching to the main menu without a load-time crash.

- [ ] **Step 8: Commit (only if the user has asked for commits)**

```bash
git add CSharpSourceCode/Framework/TORSettlementMenuHelpers.cs CSharpSourceCode/CampaignMechanics/TORCustomSettlement/ CSharpSourceCode/CampaignMechanics/Crafting/ CSharpSourceCode/Framework/CraftingCareerHooks.cs CSharpSourceCode/TOR_Core.csproj docs/superpowers/module-ledger.md
git commit -m "Crafting Step 1 cleanup: promote TORSettlementMenuHelpers to Framework, freeze public surface"
```

---

## Task 2: Step 2 — decompose `EnchanterTownBehavior`

757 lines doing four jobs. The type stays a `CampaignBehaviorBase` with its name and `SyncData` untouched; four collaborators come out from behind it.

**Files:**
- Create: `CSharpSourceCode/CampaignMechanics/Crafting/EnchanterCultureMap.cs`
- Create: `CSharpSourceCode/CampaignMechanics/Crafting/EnchanterSpawner.cs`
- Create: `CSharpSourceCode/CampaignMechanics/Crafting/EnchanterAccessRules.cs`
- Create: `CSharpSourceCode/CampaignMechanics/Crafting/EnchanterDialogs.cs`
- Modify: `CSharpSourceCode/CampaignMechanics/Crafting/EnchanterTownBehavior.cs`
- Modify: `CSharpSourceCode/TOR_Core.csproj`
- Create: `docs/testplans/crafting-step2-4.md`

**Interfaces:**
- Consumes: the Public Surface list from Task 1 — `EnchanterTownBehavior` is on it, so the class name and file name do not change.
- Produces:
  - `internal static class EnchanterCultureMap` with `IReadOnlyDictionary<string, (string EnchanterTemplate, List<string> EnchantmentSuffixes)> CultureToTemplate`, `IReadOnlyList<string> MultiUseCultures`, and `bool SharesVcEnchanterBranch(string requestedCulture, string actualCulture)`.
  - `internal sealed class EnchanterSpawner` constructed as `new EnchanterSpawner(Dictionary<string, string> settlementToEnchanterMap)`, exposing `void SpawnIfNeeded()`, `void EnsureMappedForSettlement(Settlement settlement)`, `void Spawn(Settlement settlement, bool forceSpawn = false)`, `void Create(Settlement settlement)`, `Hero GetForTown(Settlement settlement)`, `bool IsTrainerInCollege(Settlement settlement)`.
  - `internal sealed class EnchanterAccessRules` constructed as `new EnchanterAccessRules(EnchanterSpawner spawner)`, exposing `bool IsEnchanter(Hero hero)`, `bool HasAccess(string culture)`, `bool IsDirectlyBlocked(string culture)`, `bool SettlementMatchesCulture(string culture)`, `bool HasMatchingCompanion(Func<Hero, bool> predicate)`.
  - `internal sealed class EnchanterDialogs` constructed as `new EnchanterDialogs(EnchanterSpawner spawner, EnchanterAccessRules access)`, exposing `void Register(CampaignGameStarter starter)`.

- [ ] **Step 1: Write the test plan first**

There is no automated test framework, so the manual plan is the only regression net and it must exist *before* the refactor, not after. Create `docs/testplans/crafting-step2-4.md`:

```markdown
# Crafting Steps 2–4: Test Plan

Manual test plan for [`../superpowers/plans/2026-09-09-crafting-step2-4.md`](../superpowers/plans/2026-09-09-crafting-step2-4.md).

Tasks 1–4 are structural and must be player-invisible; Task 5 changes only where text is
loaded from. So every scenario below is a *no-change* assertion except S9 and S10.

## Run record

| Scenario | Task | Run on | Result | Notes |
|---|---|---|---|---|
| S1 | 2 | | | |
| S2 | 2 | | | |
| S3 | 2 | | | |
| S4 | 3 | | | |
| S5 | 3 | | | |
| S6 | 3 | | | |
| S7 | 4 | | | |
| S8 | 4 | | | |
| S9 | 5 | | | |
| S10 | 5 | | | |
| S11 | all | | | |

## Scenarios

**S1 — Enchanter spawns and is reachable.** Load a campaign, enter an Empire town, open the
artisan district, visit the enchanter. Expected: the enchanter NPC exists, the conversation
opens, and the enchanting screen opens from it.

**S2 — Enchanter access rules still discriminate.** Visit a town of a culture your character
has no access to, and one where access comes only via a companion. Expected: the same
allow/deny outcome as before the branch, including the Vampire-Count shared-branch case.

**S3 — Enchanter save round-trip.** Save inside a town with a spawned enchanter, quit to main
menu, reload. Expected: the same enchanter in the same town — this proves
`_settlementToEnchanterMap` still deserializes into `EnchanterTownBehavior`.

**S4 — Priest spawns and blesses.** Enter a town with a Shallya or Sigmar priest, run the
blessing conversation to completion. Expected: unchanged behaviour and text.

**S5 — Priest save round-trip.** Save, reload, confirm `_settlementToPriestMap` survives and
the previously-learned-blessings flag is still set.

**S6 — Battle loot.** Fight a battle that drops magical items and enchanting ingredients.
Expected: the same drops as before the branch; no duplicate or missing ingredient entries.

**S7 — Both town services after the base-class extraction.** Repeat S1 and S4 back to back in
one session. Expected: unchanged, and no cross-talk between the two services.

**S8 — Second save round-trip after Task 4.** Load the save made in S3 (created *before* the
base class existed). Expected: it still loads and the enchanter map is intact — this is the
scenario that catches an accidental type-name or SyncData-key change.

**S9 — Strings resolve from the new file.** After Task 5, visit the enchanter, the priest, and
the artisan district. Expected: every line of text is identical to before, with no raw
`{=str_tor_...}` markers or blank labels on screen.

**S10 — Newly exposed strings are editable.** Pick three ids from the 26 that Task 5 adds to
`tor_crafting_strings.xml`, change their text in the XML, relaunch. Expected: the changed text
appears in game — proving the environment team can now reach strings that were previously
C#-only.

**S11 — Clean load.** Launch to the main menu and start a new campaign after every task.
Expected: no load-time exception, nothing in `Logs/`.
```

- [ ] **Step 2: Run the baseline**

Before changing any code, run S1, S2, S3, S6 and S11 on the current build and fill in their Run record rows with "baseline". A no-change assertion is worthless without a recorded before.

- [ ] **Step 3: Extract `EnchanterCultureMap`**

Move the data table and its one query out of the behavior. Source: `EnchanterTownBehavior.cs` lines 31–89 (`_cultureToTemplateMap`, `_multiUseCultures`, `SharesVcEnchanterBranch`). Copy the dictionary contents across verbatim — this is a data move, and a typo in a culture id is a silent content bug, not a compile error.

```csharp
using System.Collections.Generic;

namespace TOR_Core.CampaignMechanics.Crafting;

/// <summary>
/// Which enchanter NPC template and enchantment suffixes each culture uses, plus the
/// cultures that share an enchanter branch. Data only - no campaign state, no side effects.
/// </summary>
internal static class EnchanterCultureMap
{
    /// <summary>Culture string id -> the enchanter character template and the enchantment
    /// suffixes that enchanter offers.</summary>
    public static IReadOnlyDictionary<string, (string EnchanterTemplate, List<string> EnchantmentSuffixes)> CultureToTemplate { get; }
        = new Dictionary<string, (string, List<string>)>
        {
            // Copy every entry from EnchanterTownBehavior._cultureToTemplateMap verbatim.
        };

    /// <summary>Cultures whose enchanter serves more than one enchantment branch.</summary>
    public static IReadOnlyList<string> MultiUseCultures { get; }
        = new List<string>
        {
            // Copy every entry from EnchanterTownBehavior._multiUseCultures verbatim.
        };

    /// <summary>True when the requested and actual cultures share the Vampire Count
    /// enchanter branch, so a VC-aligned player may use either.</summary>
    public static bool SharesVcEnchanterBranch(string requestedCulture, string actualCulture)
    {
        // Move the body of EnchanterTownBehavior.SharesVcEnchanterBranch unchanged.
    }
}
```

- [ ] **Step 4: Extract `EnchanterSpawner`**

Move lines 113–236 (`SpawnEnchanterIfNeeded`, `EnsureEnchanterMappedForSettlement`, `SpawnEnchanter`, `IsTrainerInCollege`, `GetEnchanterForTown`, `CreateEnchanter`). The `_settlementToEnchanterMap` dictionary itself **stays a field of `EnchanterTownBehavior`**, because it is what `SyncData` persists; the spawner receives a reference to it.

```csharp
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.Crafting;

/// <summary>
/// Creates and locates the enchanter hero for a settlement. Holds a reference to
/// EnchanterTownBehavior's settlement->enchanter map rather than owning it: that dictionary
/// is persisted by the behavior's SyncData and must stay a field of the behavior.
/// </summary>
internal sealed class EnchanterSpawner
{
    private readonly Dictionary<string, string> _settlementToEnchanterMap;

    public EnchanterSpawner(Dictionary<string, string> settlementToEnchanterMap)
    {
        _settlementToEnchanterMap = settlementToEnchanterMap;
    }

    public void SpawnIfNeeded() { /* body of SpawnEnchanterIfNeeded */ }
    public void EnsureMappedForSettlement(Settlement settlement) { /* body of EnsureEnchanterMappedForSettlement */ }
    public void Spawn(Settlement settlement, bool forceSpawn = false) { /* body of SpawnEnchanter */ }
    public void Create(Settlement settlement) { /* body of CreateEnchanter */ }
    public Hero GetForTown(Settlement settlement) { /* body of GetEnchanterForTown */ }
    public bool IsTrainerInCollege(Settlement settlement) { /* body of IsTrainerInCollege */ }
}
```

References to `_cultureToTemplateMap` inside these bodies become `EnchanterCultureMap.CultureToTemplate`.

- [ ] **Step 5: Extract `EnchanterAccessRules`**

Move lines 237–360 (`IsEnchanter`, `SettlementMatchesEnchanterCulture`, `HasMatchingEnchanterCompanion`, `HasEnchanterAccess`, `IsDirectEnchanterBlocked`).

```csharp
using System;
using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Crafting;

/// <summary>
/// Answers "may the player use this enchanter" - culture match, companion match, career and
/// cross-branch grants. Pure predicates over campaign state; no spawning, no dialog.
/// </summary>
internal sealed class EnchanterAccessRules
{
    private readonly EnchanterSpawner _spawner;

    public EnchanterAccessRules(EnchanterSpawner spawner)
    {
        _spawner = spawner;
    }

    public bool IsEnchanter(Hero hero) { /* body of IsEnchanter */ }
    public bool HasAccess(string culture) { /* body of HasEnchanterAccess */ }
    public bool IsDirectlyBlocked(string culture) { /* body of IsDirectEnchanterBlocked */ }
    public bool SettlementMatchesCulture(string culture) { /* body of SettlementMatchesEnchanterCulture */ }
    public bool HasMatchingCompanion(Func<Hero, bool> predicate) { /* body of HasMatchingEnchanterCompanion */ }
}
```

`HasEnchanterAccess` calls into `CraftingCareerHooks.EnchanterAccessGrants` — keep that call exactly as it is. It is the Framework hook that keeps Careers decoupled, and rerouting it is out of scope.

- [ ] **Step 6: Extract `EnchanterDialogs`**

Move lines 361–751 — the single ~390-line `GenericEnchantmentDialogs` method. Do not attempt to restructure the dialog tree in this step; move it whole, then split it into private methods along the conversation's own hub boundaries (`intro`, `hub`, `blueprints`, `donate items`, `open enchanter`, `quit`), which the existing `tor_enchanter_hub_*` string ids already name for you.

```csharp
using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Crafting;

/// <summary>
/// The enchanter conversation tree. Split into one private method per hub branch, matching
/// the tor_enchanter_hub_* string ids.
/// </summary>
internal sealed class EnchanterDialogs
{
    private readonly EnchanterSpawner _spawner;
    private readonly EnchanterAccessRules _access;

    public EnchanterDialogs(EnchanterSpawner spawner, EnchanterAccessRules access)
    {
        _spawner = spawner;
        _access = access;
    }

    public void Register(CampaignGameStarter starter)
    {
        AddIntroLines(starter);
        AddHubLines(starter);
        AddBlueprintLines(starter);
        AddDonateItemLines(starter);
        AddOpenEnchanterLines(starter);
        AddQuitLines(starter);
    }

    private void AddIntroLines(CampaignGameStarter starter) { /* ... */ }
    private void AddHubLines(CampaignGameStarter starter) { /* ... */ }
    private void AddBlueprintLines(CampaignGameStarter starter) { /* ... */ }
    private void AddDonateItemLines(CampaignGameStarter starter) { /* ... */ }
    private void AddOpenEnchanterLines(CampaignGameStarter starter) { /* ... */ }
    private void AddQuitLines(CampaignGameStarter starter) { /* ... */ }
}
```

The `_learnedEnchantment` and `_spokeToEnchanter` flags are read and written by this tree but persisted by the behavior. Pass them as a small mutable holder or expose them via the behavior — do **not** duplicate them into `EnchanterDialogs` as its own fields, or the saved values stop tracking the dialog state.

- [ ] **Step 7: Reduce `EnchanterTownBehavior` to wiring**

What remains: the two persisted flags, `_settlementToEnchanterMap`, the collaborators, `RegisterEvents`, the four event handlers, and `SyncData` — byte-for-byte unchanged.

```csharp
public class EnchanterTownBehavior : CampaignBehaviorBase
{
    private bool _learnedEnchantment;
    private bool _spokeToEnchanter;
    private Dictionary<string, string> _settlementToEnchanterMap = new();

    private readonly EnchanterSpawner _spawner;
    private readonly EnchanterAccessRules _access;
    private readonly EnchanterDialogs _dialogs;

    public EnchanterTownBehavior()
    {
        _spawner = new EnchanterSpawner(_settlementToEnchanterMap);
        _access = new EnchanterAccessRules(_spawner);
        _dialogs = new EnchanterDialogs(_spawner, _access);
    }

    public override void RegisterEvents() { /* unchanged */ }

    private void OnSessionLaunched(CampaignGameStarter obj) => _dialogs.Register(obj);
    private void OnNewGameCreated(CampaignGameStarter obj) { /* delegates to _spawner */ }
    private void OnBeforeMissionStart() => _spawner.SpawnIfNeeded();
    private void OnGameMenuOpened(MenuCallbackArgs obj) => _spawner.SpawnIfNeeded();

    // DO NOT change these keys - they are how existing saves find this data.
    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_spokeToEnchanter", ref _spokeToEnchanter);
        dataStore.SyncData("_learnedEnchantment", ref _learnedEnchantment);
        dataStore.SyncData("_settlementToEnchanterMap", ref _settlementToEnchanterMap);
    }
}
```

**Watch the `_settlementToEnchanterMap` reference.** `SyncData` may *replace* the dictionary instance on load rather than fill the existing one. If it does, the reference captured in the constructor goes stale and the spawner silently reads an empty map. S3 is the scenario that catches this; if it fails, change `EnchanterSpawner` to take a `Func<Dictionary<string, string>>` accessor instead of the dictionary itself.

- [ ] **Step 8: Add the four files to the csproj**

```xml
<Compile Include="CampaignMechanics\Crafting\EnchanterCultureMap.cs" />
<Compile Include="CampaignMechanics\Crafting\EnchanterSpawner.cs" />
<Compile Include="CampaignMechanics\Crafting\EnchanterAccessRules.cs" />
<Compile Include="CampaignMechanics\Crafting\EnchanterDialogs.cs" />
```

- [ ] **Step 9: Build and verify**

Build, then run S1, S2, S3 and S11 and fill in the Run record. S3 is the gate — it is the one that proves the save wiring survived. Do not proceed to Task 3 with S3 unfilled or failing.

- [ ] **Step 10: Refresh the docs and commit (only if the user has asked for commits)**

Update `MODULE.md` with the four new classes and `CLAUDE.md` with how they now fit together.

```bash
git add CSharpSourceCode/CampaignMechanics/Crafting/ CSharpSourceCode/TOR_Core.csproj docs/testplans/crafting-step2-4.md
git commit -m "Crafting Step 2: decompose EnchanterTownBehavior into map/spawner/access/dialogs"
```

---

## Task 3: Step 2 — decompose `PriestBehavior` and `LootCampaignBehavior`

Same treatment, two smaller files. Split into its own task because it is independently reviewable and independently revertable.

**Files:**
- Create: `CSharpSourceCode/CampaignMechanics/Crafting/PriestCultMap.cs`
- Create: `CSharpSourceCode/CampaignMechanics/Crafting/PriestSpawner.cs`
- Create: `CSharpSourceCode/CampaignMechanics/Crafting/PriestDialogs.cs`
- Create: `CSharpSourceCode/CampaignMechanics/Crafting/BattleLootTracker.cs`
- Modify: `CSharpSourceCode/CampaignMechanics/Crafting/PriestBehavior.cs`
- Modify: `CSharpSourceCode/CampaignMechanics/Crafting/LootCampaignBehavior.cs`
- Modify: `CSharpSourceCode/TOR_Core.csproj`
- Modify: `docs/testplans/crafting-step2-4.md` (Run record)

**Interfaces:**
- Consumes: the shape established in Task 2 — mirror it exactly, so Task 4 can lift a common base out of the two.
- Produces:
  - `internal static class PriestCultMap` with `IReadOnlyDictionary<string, (string Town, string CultId, string EnchantmentSuffix)> TemplateSpawns`.
  - `internal sealed class PriestSpawner` constructed as `new PriestSpawner(Dictionary<string, List<string>> settlementToPriestMap)`, exposing `void SpawnIfNeeded()`, `void Spawn(Settlement settlement, bool forceSpawn)`, `void Create(Settlement settlement, string templateId, string religionId)`, `Hero GetForTown(Settlement settlement)`, `bool IsInTown(Settlement settlement)`.
  - `internal sealed class PriestDialogs` constructed as `new PriestDialogs(PriestSpawner spawner)`, exposing `void Register(CampaignGameStarter starter)`.
  - `internal sealed class BattleLootTracker` exposing `void StoreFromMapEventStart(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)`, `void StoreFromMission(IMission mission)`, `IReadOnlyDictionary<CharacterObject, int> InitialEnemyArmy`, `void Clear()`.

- [ ] **Step 1: Extract `PriestCultMap`**

Source: `PriestBehavior.cs` lines 26–33 (`_priestTemplateSpawns`). Copy entries verbatim.

```csharp
using System.Collections.Generic;

namespace TOR_Core.CampaignMechanics.Crafting;

/// <summary>Which priest template spawns in which town, for which cult, offering which
/// blessing suffix. Data only.</summary>
internal static class PriestCultMap
{
    public static IReadOnlyDictionary<string, (string Town, string CultId, string EnchantmentSuffix)> TemplateSpawns { get; }
        = new Dictionary<string, (string, string, string)>
        {
            // Copy every entry from PriestBehavior._priestTemplateSpawns verbatim.
        };
}
```

- [ ] **Step 2: Extract `PriestSpawner`**

Source: lines 63–150 (`CreatePriests`, `SpawnPriestIfNeeded`, `SpawnPriest`, `IsPriestInTown`, `GetPriestForTown`). As in Task 2, `_settlementToPriestMap` stays a field of `PriestBehavior` because `SyncData` persists it; the spawner takes a reference.

```csharp
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.Crafting;

/// <summary>Creates and locates priest heroes for a settlement. The settlement->priest map
/// belongs to PriestBehavior (it is persisted there); this class only reads and mutates it.</summary>
internal sealed class PriestSpawner
{
    private readonly Dictionary<string, List<string>> _settlementToPriestMap;

    public PriestSpawner(Dictionary<string, List<string>> settlementToPriestMap)
    {
        _settlementToPriestMap = settlementToPriestMap;
    }

    public void SpawnIfNeeded() { /* body of SpawnPriestIfNeeded */ }
    public void Spawn(Settlement settlement, bool forceSpawn) { /* body of SpawnPriest */ }
    public void Create(Settlement settlement, string templateId, string religionId) { /* body of CreatePriests */ }
    public Hero GetForTown(Settlement settlement) { /* body of GetPriestForTown */ }
    public bool IsInTown(Settlement settlement) { /* body of IsPriestInTown */ }
}
```

- [ ] **Step 3: Extract `PriestDialogs`**

Source: lines 156–302 (`AddDialogs`, ~146 lines), including the local functions `PriestCondition`, `PlayerMeetsRequirements` and `IsEnemyOfCult`. Split into private methods along the same hub boundaries the `tor_priest_hub_*` ids name.

The `using TOR_Core.CampaignMechanics.Religion;` moves to this file with the `ReligionObject.All` lookups it serves. That keeps the module's one remaining outbound violation confined to a single file, which makes Religion's Step 1 cheaper — it will have exactly one file to fix rather than a scattered set.

- [ ] **Step 4: Reduce `PriestBehavior` to wiring**

Keep the type name, the fields, `RegisterEvents`, the handlers, and `SyncData`. Add the comment explaining the misnamed key:

```csharp
    // DO NOT change these keys. "_learnedEnchantment" is misnamed - it stores _learnedBlessings -
    // but the key is how existing saves find the value, so correcting it would orphan the data.
    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_settlementToPriestMap", ref _settlementToPriestMap);
        dataStore.SyncData("_learnedEnchantment", ref _learnedBlessings);
    }
```

- [ ] **Step 5: Extract `BattleLootTracker` from `LootCampaignBehavior`**

`LootCampaignBehavior` (359 lines) does two jobs: bookkeeping of the enemy army at map-event start, and deciding which magical items enter or leave the loot pool. Move the first out. Source: lines 24–106 (`_initialEnemyArmy`, `StoreInitialArmyFromMapEventStart`, `StoreInitialArmy`, `OnTrackedMapEventEnded`).

```csharp
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TOR_Core.CampaignMechanics.Crafting;

/// <summary>
/// Remembers the enemy army composition at the start of a map event so post-battle loot can
/// be scaled against what was actually fought. Nothing here is persisted - LootCampaignBehavior
/// has an empty SyncData, and this state is intentionally per-session.
/// </summary>
internal sealed class BattleLootTracker
{
    private readonly Dictionary<CharacterObject, int> _initialEnemyArmy = new();

    public IReadOnlyDictionary<CharacterObject, int> InitialEnemyArmy => _initialEnemyArmy;

    public void StoreFromMapEventStart(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty) { /* ... */ }
    public void StoreFromMission(IMission mission) { /* body of StoreInitialArmy */ }
    public void Clear() => _initialEnemyArmy.Clear();
}
```

`LootCampaignBehavior` keeps `RemoveUnrelatedRuntimeMagicalLootItems`, `RemovedUnusedLootItems` and `AddMagicalItemsFromBattle`, and reads the army through `_tracker.InitialEnemyArmy`.

- [ ] **Step 6: Add the four files to the csproj**

```xml
<Compile Include="CampaignMechanics\Crafting\PriestCultMap.cs" />
<Compile Include="CampaignMechanics\Crafting\PriestSpawner.cs" />
<Compile Include="CampaignMechanics\Crafting\PriestDialogs.cs" />
<Compile Include="CampaignMechanics\Crafting\BattleLootTracker.cs" />
```

- [ ] **Step 7: Build and verify**

Build, then run S4, S5, S6 and S11. S5 is the gate for the priest save wiring; S6 for loot.

- [ ] **Step 8: Refresh the docs and commit (only if the user has asked for commits)**

```bash
git add CSharpSourceCode/CampaignMechanics/Crafting/ CSharpSourceCode/TOR_Core.csproj docs/testplans/crafting-step2-4.md
git commit -m "Crafting Step 2: decompose PriestBehavior and LootCampaignBehavior"
```

---

## Task 4: Step 3 — extract the town-service NPC pattern

Only now is the pattern safe to name. Tasks 2 and 3 produced two behaviors with the same five-part shape; this task lifts the shape into one abstract base rather than inventing an abstraction up front.

**Files:**
- Create: `CSharpSourceCode/CampaignMechanics/Crafting/TownServiceNpcBehavior.cs`
- Modify: `CSharpSourceCode/CampaignMechanics/Crafting/EnchanterTownBehavior.cs`
- Modify: `CSharpSourceCode/CampaignMechanics/Crafting/PriestBehavior.cs`
- Modify: `CSharpSourceCode/TOR_Core.csproj`
- Modify: `CSharpSourceCode/CampaignMechanics/Crafting/MODULE.md`, `CLAUDE.md`
- Modify: `docs/superpowers/module-ledger.md`

**Interfaces:**
- Consumes: `EnchanterSpawner`, `EnchanterDialogs`, `PriestSpawner`, `PriestDialogs` from Tasks 2 and 3.
- Produces: `public abstract class TownServiceNpcBehavior : CampaignBehaviorBase` with `protected abstract void SpawnIfNeeded();`, `protected abstract void RegisterDialogs(CampaignGameStarter starter);`, `protected virtual void OnNewGame(CampaignGameStarter starter) { }`, and a sealed `public override void RegisterEvents()`.

- [ ] **Step 1: Confirm the pattern is real before writing it**

The spec's rule for Step 3 is that a pattern must be earning its place, so check the claim rather than assume it. After Tasks 2 and 3, `EnchanterTownBehavior` and `PriestBehavior` should both consist of: persisted fields, a spawner, a dialogs object, `RegisterEvents` subscribing to the same four campaign events, four one-line handlers, and `SyncData`. Diff the two files. If the `RegisterEvents` bodies and the handler set are not in fact the same, **stop and do not create the base class** — record in `MODULE.md` that the shape did not generalize, and skip to Task 5. A pattern extracted from two things that only look alike is worse than no pattern.

- [ ] **Step 2: Write the base class**

An abstract base, not a default-interface-method interface — net48 rejects those with `CS8701`.

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;

namespace TOR_Core.CampaignMechanics.Crafting;

/// <summary>
/// Shared shape of a town-service NPC behavior: an NPC that must exist in a settlement before
/// the player can reach it, and that owns a conversation tree registered at session launch.
/// Both EnchanterTownBehavior and PriestBehavior spawn on the same three triggers (new game,
/// before mission start, game menu opened) and register dialogs on the fourth; this class owns
/// that wiring so a subclass only supplies the two things that actually differ.
///
/// Subclasses stay concrete CampaignBehaviorBase types with their own SyncData - behavior save
/// data is keyed by type name, so this base must never take over persistence.
/// </summary>
public abstract class TownServiceNpcBehavior : CampaignBehaviorBase
{
    /// <summary>Create the service NPC in any settlement that should have one but does not.</summary>
    protected abstract void SpawnIfNeeded();

    /// <summary>Register this service's conversation tree.</summary>
    protected abstract void RegisterDialogs(CampaignGameStarter starter);

    /// <summary>Optional one-time setup on a brand-new campaign.</summary>
    protected virtual void OnNewGame(CampaignGameStarter starter) { }

    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
        CampaignEvents.OnBeforeMissionStartEvent.AddNonSerializedListener(this, OnBeforeMissionStart);
        CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
    }

    private void OnSessionLaunched(CampaignGameStarter starter) => RegisterDialogs(starter);
    private void OnNewGameCreated(CampaignGameStarter starter) => OnNewGame(starter);
    private void OnBeforeMissionStart() => SpawnIfNeeded();
    private void OnGameMenuOpened(MenuCallbackArgs args) => SpawnIfNeeded();
}
```

**Check the event names against the current `EnchanterTownBehavior.RegisterEvents` before compiling** — the four listed above are the expected set, but the signature and exact event names must be copied from the working code, not from this plan.

- [ ] **Step 3: Derive both behaviors from it**

```csharp
public class EnchanterTownBehavior : TownServiceNpcBehavior
{
    // fields, constructor and SyncData unchanged
    protected override void SpawnIfNeeded() => _spawner.SpawnIfNeeded();
    protected override void RegisterDialogs(CampaignGameStarter starter) => _dialogs.Register(starter);
    protected override void OnNewGame(CampaignGameStarter starter) { /* existing OnNewGameCreated body */ }
}
```

```csharp
public class PriestBehavior : TownServiceNpcBehavior
{
    // fields, constructor and SyncData unchanged
    protected override void SpawnIfNeeded() => _spawner.SpawnIfNeeded();
    protected override void RegisterDialogs(CampaignGameStarter starter) => _dialogs.Register(starter);
    protected override void OnNewGame(CampaignGameStarter starter) { /* existing OnNewGameCreated body */ }
}
```

Both classes stay `public` and keep their names — `EnchanterTownBehavior` is on the frozen public surface, and both are keyed by type name in saves.

- [ ] **Step 4: Add to the csproj**

```xml
<Compile Include="CampaignMechanics\Crafting\TownServiceNpcBehavior.cs" />
```

- [ ] **Step 5: Build and verify**

Build, then run S7, S8 and S11. **S8 is the gate**: it loads a save created back in S3, before the base class existed, and is the scenario that catches an accidental type-name change or a `RegisterEvents` listener that silently stopped being subscribed.

- [ ] **Step 6: Record the pattern**

Add to `MODULE.md`, so the next module's Step 3 has a vocabulary entry rather than starting from nothing:

```markdown
## Patterns

| Pattern | Where | What it bought |
|---|---|---|
| Town-service NPC base class (`TownServiceNpcBehavior`) | `EnchanterTownBehavior`, `PriestBehavior` | Removed a duplicated four-event `RegisterEvents` block and four identical handlers from each. Subclasses supply only spawn and dialog registration. Implemented as an abstract base, not an interface with default methods — net48 rejects those (`CS8701`). |
```

Then mark Crafting's Step 3 `done` in `docs/superpowers/module-ledger.md`, and Step 2 `done` as well if it is not already.

- [ ] **Step 7: Commit (only if the user has asked for commits)**

```bash
git add CSharpSourceCode/CampaignMechanics/Crafting/ CSharpSourceCode/TOR_Core.csproj docs/superpowers/module-ledger.md docs/testplans/crafting-step2-4.md
git commit -m "Crafting Step 3: extract TownServiceNpcBehavior base from enchanter and priest"
```

---

## Task 5: Step 4 — give Crafting its own string file

The module uses 64 localization ids through `TORTextHelper`. 38 are declared in the shared `ModuleData/tor_strings.xml`; **26 exist only as inline C# default text**, which means the environment team cannot see or edit them at all. Three (`tor_inquiry_accept_text`, `tor_inquiry_cancel_text`, `tor_inquiry_ok_text`) are used by 6, 7 and 1 files elsewhere respectively and are genuinely cross-cutting — they stay central.

**Files:**
- Create: `ModuleData/Strings/tor_crafting_strings.xml`
- Modify: `ModuleData/tor_strings.xml` (remove 38 ids, minus the 3 shared → 35 removed)
- Modify: `SubModule.xml`
- Modify: `CampaignMechanics/Crafting/EnchantmentShopHelper.cs:88`
- Modify: `CampaignMechanics/Crafting/EnchantmentBlueprintScript.cs:130`
- Modify: `CampaignMechanics/Crafting/MODULE.md`, `CLAUDE.md`
- Modify: `docs/superpowers/module-ledger.md`

**Interfaces:**
- Consumes: nothing from earlier tasks — this task is independent of Tasks 2–4 and could be done first if desired.
- Produces: `ModuleData/Strings/tor_crafting_strings.xml` as the module's string file, and the precedent for the other 25 modules.

- [ ] **Step 1: Regenerate the id inventory**

Do not trust the counts in this plan — Tasks 2–4 moved code between files. Regenerate:

```bash
cd "CSharpSourceCode/CampaignMechanics/Crafting"
grep -rhoE 'TORTextHelper\.[A-Za-z]+\("(tor_[a-z0-9_]+)"' *.cs Models/*.cs \
  | grep -oE 'tor_[a-z0-9_]+' | sort -u > /tmp/crafting-ids.txt
wc -l /tmp/crafting-ids.txt
```

A plain `grep 'tor_[a-z_]*'` over the folder is **wrong** and will return roughly 115 ids — it catches `CraftingTemplate` object ids (`tor_axe_template`, `tor_orc_mace_template`), dialog token ids (`tor_enchanter_dialog_start`), and `ItemObject` ids (`tor_tradegood_gemstone`), none of which are localization strings. Only strings passed to a `TORTextHelper` call are.

- [ ] **Step 2: Split the inventory three ways**

```bash
cd "Modules/TOR_Core"
# Which are declared centrally today, and which are code-only?
while read id; do
  grep -q "id=\"$id\"" ModuleData/tor_strings.xml && echo "DECLARED $id" || echo "CODEONLY $id"
done < /tmp/crafting-ids.txt

# Which are also used outside the module? Those stay central.
while read id; do
  n=$(grep -rl "\"$id\"" --include=*.cs CSharpSourceCode | grep -v "CampaignMechanics/Crafting/" | wc -l)
  [ "$n" -gt 0 ] && echo "SHARED($n) $id"
done < /tmp/crafting-ids.txt
```

Expected: about 38 DECLARED, about 26 CODEONLY, exactly 3 SHARED (all three `tor_inquiry_*`).

- [ ] **Step 3: Create the module string file**

For each DECLARED-and-not-SHARED id, move its `<string>` element across verbatim — id, `{=str_...}` key and text unchanged. For each CODEONLY id, write a new element whose text is the default string currently passed as `TORTextHelper`'s second argument, copied exactly.

```xml
<?xml version="1.0" encoding="utf-8"?>
<strings>

  <!-- Artisan district menu -->
  <string id="tor_artisan_district_title_text" text="{=str_tor_artisan_district_title_text}Artisan District" />
  <string id="tor_artisan_district_menu_option_text" text="{=str_tor_artisan_district_menu_option_text}Go to the artisan district" />
  <string id="tor_artisan_weaponsmith_option_text" text="{=str_tor_artisan_weaponsmith_option_text}Visit the weaponsmith" />
  <string id="tor_artisan_enchanter_option_text" text="{=str_tor_artisan_enchanter_option_text}Visit the enchanter" />
  <string id="tor_artisan_leave_option_text" text="{=str_tor_artisan_leave_option_text}Leave" />

  <!-- Enchanter conversation - previously C#-only, not editable outside code -->
  <string id="tor_enchanter_hub_intro" text="{=str_tor_enchanter_hub_intro}...copy the default from EnchanterDialogs..." />
  <!-- ...one element per remaining id, grouped by the comment banners below... -->

  <!-- Enchanting screen -->
  <!-- Priest conversation -->
  <!-- Refinement -->
  <!-- Blueprints and shop -->

</strings>
```

The exact text for every id comes from the codebase, not from this plan: for a DECLARED id, from `tor_strings.xml`; for a CODEONLY id, from the C# default argument. Copying either by hand risks a silent text change, so copy mechanically and diff.

- [ ] **Step 4: Register the file in `SubModule.xml`**

Add alongside the existing `<XmlNode>` entries:

```xml
<XmlNode>
  <XmlName id="Strings" path="Strings/tor_crafting_strings" />
</XmlNode>
```

- [ ] **Step 5: Remove the migrated ids from `tor_strings.xml`**

Delete only the DECLARED-and-not-SHARED elements. Leave the three `tor_inquiry_*` entries in place. A duplicate id across two loaded string files has undefined precedence — leaving a stale copy behind is the failure mode to avoid here, so verify each deletion against the list from Step 2.

- [ ] **Step 6: Convert the two remaining raw literals**

`EnchantmentShopHelper.cs:88` builds a `TextObject` from a raw format string:

```csharp
// was
: new TextObject("{TRAIT_EFFECT}\n\n{REQUIREMENT_TEXT}\n\n{COMPLETE_COST}");
// becomes
: TORTextHelper.GetTextObject("tor_enchantmentshop_effect_layout",
    "{TRAIT_EFFECT}{newline}{newline}{REQUIREMENT_TEXT}{newline}{newline}{COMPLETE_COST}");
```

Note both changes: it goes through `TORTextHelper`, **and** the `\n` escapes become `{newline}`, which is what this project's string pipeline understands.

`EnchantmentBlueprintScript.cs:130` passes an English literal as an inquiry title:

```csharp
// was
var inquirydata = new MultiSelectionInquiryData("Choose hero to learn new enchantment",
// becomes
var inquirydata = new MultiSelectionInquiryData(
    TORTextHelper.GetText("tor_enchanting_choose_hero_title", "Choose hero to learn new enchantment"),
```

Add both new ids to `tor_crafting_strings.xml`.

- [ ] **Step 7: Build and verify**

Build, then run S9 and S10. S9 catches a missed migration (text renders as a raw `{=str_tor_...}` marker or an empty label). **S10 is the gate for this whole task** — it is the only scenario that actually proves the environment team gained the ability they were meant to gain.

- [ ] **Step 8: Record and commit (only if the user has asked for commits)**

Add a Localization section to `MODULE.md` naming `ModuleData/Strings/tor_crafting_strings.xml` as the module's string file and noting that the three `tor_inquiry_*` ids remain central because they are shared. Mark Crafting's Step 4 `done` in the ledger, and update the ledger's Framework work-items row for `ModuleData/Strings/` from "not started" to "established by Crafting".

Then answer, in the spec's Step 4 open-questions list, the question this task settled: `tor_strings.xml` is **not** drained to empty — it keeps ids used by more than one module.

```bash
git add ModuleData/ SubModule.xml CSharpSourceCode/CampaignMechanics/Crafting/ docs/superpowers/ docs/testplans/
git commit -m "Crafting Step 4: per-module string file, expose 26 previously code-only strings"
```

---

## Task 6: Close out the module

**Files:**
- Modify: `docs/superpowers/module-ledger.md`
- Modify: `docs/superpowers/specs/2026-09-09-module-lifecycle-design.md`
- Modify: `CSharpSourceCode/CampaignMechanics/Crafting/CLAUDE.md`, `MODULE.md`
- Modify: `docs/testplans/crafting-step2-4.md`

- [ ] **Step 1: Full regression pass**

Run every scenario S1–S11 against the final build in one session, and fill in the Run record completely. Individual tasks verified their own slice; this proves the four changes compose.

- [ ] **Step 2: Verify the exit criteria honestly**

Check each one and write the result into the ledger's Notes rather than assuming:

```bash
cd "CSharpSourceCode"
# Should return only PriestDialogs.cs (the known deferred Religion edge)
grep -rn "using TOR_Core.CampaignMechanics\.\|using TOR_Core.CharacterDevelopment" CampaignMechanics/Crafting/*.cs CampaignMechanics/Crafting/Models/*.cs

# Every Crafting file must appear in the csproj
for f in CampaignMechanics/Crafting/*.cs CampaignMechanics/Crafting/Models/*.cs; do
  w=$(echo "$f" | tr '/' '\\')
  grep -q "$w" TOR_Core.csproj || echo "MISSING FROM CSPROJ: $f"
done
```

- [ ] **Step 3: Feed what was learned back into the spec**

Steps 2–4 were written as outlines pending first use. This was the first use, so answer their open questions from evidence rather than leaving them open for the next module:

- **Step 2's "what triggers a split"** — the trigger that actually fired here was not line count but *number of distinct jobs in one type*. `EnchanterTownBehavior` was split at 757 lines and `LootCampaignBehavior` at 359; a 275-line `EnchantmentHelper` was left alone because it does one thing. Write that up.
- **Step 2's "how is dead code established"** — record whatever method was actually used, and note the reflection/Harmony/XML-script hazard.
- **Step 3's "fixed vocabulary or grown per module"** — this module grew one entry (`TownServiceNpcBehavior`). Record which it turned out to be.
- **Step 4's "is `tor_strings.xml` drained"** — answered: no. Shared ids stay.
- **Add a new constraint the spec does not currently carry:** behaviour save data is keyed by type name, so a Step 2 split must extract collaborators *behind* an existing `CampaignBehaviorBase` rather than split it into several behaviors. This is the single most important thing this module taught, and every remaining module's Step 2 needs it.

- [ ] **Step 4: Update the ledger**

Crafting's row: Steps 1–4 `done`, Notes recording the deferred Religion edge and pointing at the test plan.

- [ ] **Step 5: Commit (only if the user has asked for commits)**

```bash
git add docs/ CSharpSourceCode/CampaignMechanics/Crafting/
git commit -m "Crafting: close Steps 2-4, feed findings back into the lifecycle spec"
```

---

## Notes on execution order

Task 5 (localization) has no dependency on Tasks 2–4 and can be pulled forward if the environment team is waiting on strings — it touches XML and two C# lines, none of them in the files Tasks 2–4 restructure. Tasks 2, 3 and 4 are strictly ordered: Task 4 cannot judge whether the pattern is real until Tasks 2 and 3 have exposed the shape.
