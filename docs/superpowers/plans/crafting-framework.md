# `Crafting` — Framework Plan

**Epic:** 2 Framework
**Branch:** `feature/FrameworkCrafting` (stacked: `next_update` → `feature/moduleCrafting` →
`feature/CentralizeCraftingRecipes` → here — Epic 1 and the Centralize work are both awaiting
review, so this epic builds on them rather than waiting)
**Entry:** Epic 1 code complete, PR open. The "previous epic merged" rule is waived deliberately;
see Branch above.
**Goal:** Crafting stops naming other modules, and other modules stop naming Crafting except
through names Crafting has declared frozen.

## Constraints

Project-wide constraints: [`../CONSTRAINTS.md`](../CONSTRAINTS.md). Epic-specific:

- Promotion to `Framework/` is a namespace change only. No member renamed, no signature changed,
  no behaviour changed. A player cannot tell this branch landed.
- `Framework/` hook-contract count stays at 1 (`CraftingCareerHooks`). This epic adds none — both
  cuts are promotions, not contracts.
- Every `CampaignBehaviorBase` keeps its type name and `SyncData` keys. Nothing here touches a
  `SaveableTypeDefiner`, so the O1 save-namespace probe does not gate this epic.

## Scope

| Doing | Not doing |
|---|---|
| Promote `TORSettlementMenuHelpers` to `Framework/` | Cutting `PriestBehavior` → `Religion`. `Religion/` has not been modularized; any cut now guesses where its boundary lands. Deferred to Religion's Epic 1, recorded on both ledger rows. |
| Promote `TorEnchantingIngredients` + `TorTradeGoodType` to `Framework/` | Cutting `EnchantmentBlueprints` / `EnchantmentHelper` / `EnchantmentIngredientLootCampaignBehavior` out of Careers, CustomResources and TORCustomSettlement. That is real module state, not generic plumbing; a Framework contract for it is a design project, not a PR. Frozen as public surface instead. |
| Freeze the surviving inbound set into `MODULE.md` | Splitting `EnchantmentHelper` (275 lines; its item-construction half is arguably generic). That is Epic 4 Codesmells. |
| Correct the false claim in `Framework/CraftingCareerHooks.cs` | |

## Edge inventory

**Outbound** — `using TOR_Core.<OtherModule>` inside `CampaignMechanics/Crafting/`:

| File:line | Reference | Disposition |
|---|---|---|
| `TORArtisanDistrictCampaignBehavior.cs:13` | `TORCustomSettlement` → `TORSettlementMenuHelpers.RearrangeTownMenus` | Cut — Task 1 |
| `PriestBehavior.cs:12` | `Religion` → `ReligionObject.All` | Deferred — Task 3 |

**Inbound** — 17 files name `TOR_Core.CampaignMechanics.Crafting`. Nine are Framework-classified
(`Extensions/`, `Items/`, `Models/`, `Ink/`, `HarmonyPatches/` ×2, `SaveGameSystem/`,
`Utilities/`, `SubModule.cs`) — legal, except that two of them reach in only for
`TorTradeGoodType`, which Task 2 fixes. Eight are other modules:

| File | Uses | Disposition |
|---|---|---|
| `Careers/TORCareerPerkCampaignBehavior.cs` | `TorTradeGoodType`, `TorEnchantingIngredients` | Cut by Task 2 |
| `CustomResourceBehavior/OathGoldBehavior.cs` | `TorEnchantingIngredients.GemStone`, `EnchantmentHelper.CreateEnchantedItem` | Partly cut by Task 2; remainder frozen |
| `TORCustomSettlement/TORCustomSettlementCampaignBehavior.cs` | `EnchantmentHelper`, `EnchantmentIngredientLootCampaignBehavior` | Frozen |
| `CareerSystem/CareerButton/RunelordCareerButtonBehavior.cs` | `EnchantmentHelper`, `EnchantmentBlueprints`, `TorEnchantingIngredients` | Partly cut by Task 2; remainder frozen |
| `CareerSystem/CraftingCareerHookRegistrations.cs` | `EnchantmentBlueprints.Learn` | Allowed — it populates the Framework hook |
| `Quests/Careers/OrcShamanQuest2.cs`, `RunelordQuest.cs`, `RunesmithQuest.cs` | `EnchantmentBlueprints.GetKnown` | Frozen |

## Tasks

### Task 1: Promote `TORSettlementMenuHelpers` to `Framework/`

50 lines, one public method, no `using TOR_Core.*` of its own, already shared by two modules.

**Files:** move `CampaignMechanics/TORCustomSettlement/TORSettlementMenuHelpers.cs` →
`Framework/TORSettlementMenuHelpers.cs`; modify
`CampaignMechanics/Crafting/TORArtisanDistrictCampaignBehavior.cs`,
`CampaignMechanics/TORCustomSettlement/{GoblinRecruitmentBehavior,GreenskinBrawlBehavior,TORCustomSettlementCampaignBehavior}.cs`,
`TOR_Core.csproj:623`.
**Produces:** `TOR_Core.Framework.TORSettlementMenuHelpers.RearrangeTownMenus(GameMenu, string, string, bool = false)` — signature unchanged.

- [x] Step 1: `git mv` the file, change its namespace to `TOR_Core.Framework`.
- [x] Step 2: `TORArtisanDistrictCampaignBehavior.cs:13` — swap the using to `using TOR_Core.Framework;`.
- [x] Step 3: the three `TORCustomSettlement/` files resolved it implicitly from their own
      namespace — add `using TOR_Core.Framework;` to each.
- [x] Step 4: csproj line 623 → `<Compile Include="Framework\TORSettlementMenuHelpers.cs" />`.
- [x] Step 5: build. Expect the pre-existing unrelated error set, unchanged.

### Task 2: Promote `TorEnchantingIngredients` + `TorTradeGoodType` to `Framework/`

A 68-line item-object catalogue with zero `using TOR_Core.*`. Two Framework files
(`Items/ItemTrait.cs`, `Models/TORFaithModel.cs`) already reach into the module for it, which is
the proof it was never module content.

**Files:** move `CampaignMechanics/Crafting/TorEnchantingIngredients.cs` →
`Framework/TorEnchantingIngredients.cs`; modify `TOR_Core.csproj:531`, plus every consumer —
`Items/ItemTrait.cs`, `Models/TORFaithModel.cs`,
`CampaignMechanics/Careers/TORCareerPerkCampaignBehavior.cs`,
`CampaignMechanics/CustomResourceBehavior/OathGoldBehavior.cs`,
`CharacterDevelopment/CareerSystem/CareerButton/RunelordCareerButtonBehavior.cs`, and the
in-module users `EnchanterTownBehavior.cs`, `EnchantingIngredientVM.cs`, `EnchantingVM.cs`,
`EnchantmentIngredientLootCampaignBehavior.cs`, `TORArtisanDistrictCampaignBehavior.cs`,
`Models/TOREnchantmentCraftingModel.cs`, `Models/TOREnchantmentIngredientsModel.cs`.
**Produces:** `TOR_Core.Framework.TorEnchantingIngredients` and `TOR_Core.Framework.TorTradeGoodType`, members unchanged.

- [x] Step 1: `git mv` the file, change its namespace to `TOR_Core.Framework`.
- [x] Step 2: csproj line 531 → `<Compile Include="Framework\TorEnchantingIngredients.cs" />`.
- [x] Step 3: add `using TOR_Core.Framework;` to every consumer above that lacks it.
- [x] Step 4: in `CampaignMechanics/Careers/TORCareerPerkCampaignBehavior.cs:12`, **remove**
      `using TOR_Core.CampaignMechanics.Crafting;` — `TorTradeGoodType` and
      `TorEnchantingIngredients` are the only Crafting names it uses. If the build disagrees, put
      it back and move that file into the frozen table instead.
- [x] Step 5: leave `TORArtisanDistrictCampaignBehavior.cs:56`'s `LoadIngredients()` call where it
      is. Crafting still owns *when* the catalogue loads; Framework owns only the type.
- [x] Step 6: build. Same error set as before the branch.

### Task 3: Freeze what is left, record what is deferred

**Files:** `CampaignMechanics/Crafting/MODULE.md`, `Framework/CraftingCareerHooks.cs`,
`docs/superpowers/module-ledger.md`.
**Consumes:** the post-Task-2 inbound set.

- [x] Step 1: re-run the inbound scan and write the **surviving** set — not the pre-Task-2 one —
      into a `## Public surface — do not rename without updating every consumer` section of
      `MODULE.md`, one row per frozen name with its consumers.

```bash
grep -rn "using TOR_Core.CampaignMechanics.Crafting" --include=*.cs CSharpSourceCode \
  | grep -v "CampaignMechanics/Crafting/"
```

- [x] Step 2: in `Framework/CraftingCareerHooks.cs`, replace the sentence claiming
      "`CraftingCareerHookRegistrations` never references `CampaignMechanics/Crafting` either."
      It does — `EnchantmentBlueprints.Learn(...)`, lines 81–94. State instead that a module may
      call into another module's content through a Framework hook it is populating, and that what
      matters is that Crafting never names a Career type.
- [x] Step 3: ledger `Religion` row Notes — must resolve `PriestBehavior.cs` → `ReligionObject`
      during its Epic 1, by either promoting `ReligionObject` + hero religion extensions to
      `Framework/`, or moving `PriestBehavior` into Religion.
- [x] Step 4: ledger `Crafting` row Notes — one outbound edge survives (`PriestBehavior` →
      `Religion`), plus the frozen inbound set, with this plan as the reference.

### Task 4: Close the epic out

**Files:** `CampaignMechanics/Crafting/CLAUDE.md`, `CampaignMechanics/Crafting/MODULE.md`,
`Framework/CLAUDE.md` (create if absent), `docs/vertical-slicing-proposal.md`,
`docs/superpowers/module-ledger.md`, `docs/testplans/crafting-framework.md`,
`docs/superpowers/pr-notes/crafting-framework.md`.

- [x] Step 1: amend `vertical-slicing-proposal.md` in place — `TORSettlementMenuHelpers` and
      `TorEnchantingIngredients` are Framework now.
- [x] Step 2: rewrite `Crafting/CLAUDE.md` to shape (≤150 words) and re-trim `MODULE.md`
      (≤400 words) with the Public Surface section included. Rewrite, do not append.
- [x] Step 3: write `docs/testplans/crafting-framework.md` from `templates/epic-testplan.md`.
- [x] Step 4: code-check agent pass over the epic diff.
- [x] Step 5: PR notes from `templates/pr-notes.md`. **The human opens the PR.**
- [x] Step 6: ledger — `Crafting` Epic 2 → `PR`; Framework table hook count still 1.

## Verification

Test plan: `docs/testplans/crafting-framework.md`. No automated suite. Both promotions are
namespace-only, so the net is: the build error set is unchanged from before the branch, and
in-game the two promoted types still resolve — the artisan district town menu is ordered
correctly, and enchanting ingredients are found rather than null (a failed `LoadIngredients`
shows as an empty ingredient list at the table, not as a crash).

## Exit criteria

1. `grep -rn "^using TOR_Core.CampaignMechanics" CSharpSourceCode/CampaignMechanics/Crafting/ | grep -v "TOR_Core.CampaignMechanics.Crafting"`
   returns exactly one line — `PriestBehavior.cs` → `Religion` — and the ledger's `Crafting` Notes
   names it as deferred to Religion's Epic 1.
2. The Task 3 Step 1 scan returns no file that is not either Framework-classified,
   `CraftingCareerHookRegistrations.cs`, or a row in `MODULE.md`'s Public Surface table.
3. Build error set unchanged from `feature/CentralizeCraftingRecipes`.
4. `docs/testplans/crafting-framework.md` exists with its Run record filled in.
5. Ledger `Crafting` row reads `PR` under Epic 2.

Criteria 1 and 2 are `using` scans only. They do not catch a module type reached through a
`Framework/` extension method — see spec Amendment 2 and D5 in the test plan.
