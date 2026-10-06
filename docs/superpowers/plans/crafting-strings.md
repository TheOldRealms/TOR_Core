# `Crafting` — Strings Plan

**Epic:** 3 Strings
**Branch:** `feature/StringsCrafting` (stacked: `next_update` → `moduleCrafting` →
`CentralizeCraftingRecipes` → `FrameworkCrafting` → here; nothing below has merged)
**Entry:** Epic 2 code complete, PR open. Merge-first waived, as for Epic 2.
**Goal:** every player-visible string in `CampaignMechanics/Crafting/` resolves through an id that
exists in `ModuleData/tor_strings.xml`, and every Crafting id in that file is used.

## Constraints

Project-wide: [`../CONSTRAINTS.md`](../CONSTRAINTS.md). Epic-specific:

- **`tor_strings.xml` is edited by hand**, against CONSTRAINTS' "through `tortools`". Re-verified
  2026-09-15: one `strings_add` deleted all 326 category comments (719-line diff). Reads and
  `validate` still go through the server — after a restart, since it indexes in memory. Test plan D1.
- Text only. No `CampaignBehaviorBase`, `SyncData` key or Harmony patch touched.
- Exception and log messages stay literal.

## Scope

| Doing | Not doing |
|---|---|
| Convert literals, `\n`, concatenation and `GameTexts.FindText` in the module | Deleting the unreachable `DonationMode(false)` branch — Epic 4. Localized instead (D5). |
| Bind the four literal labels in `GUI/Prefabs/Crafting/Enchanting.xml` to the VM | `tor_priest_train_hub_select_companion` orphan — Skill Trainer's string, not Crafting's |
| Add missing ids, fix mis-named ids, delete Crafting orphans | Fixing `tortools` write-side formatting loss — a TOR_Tools change, other repo |
| Ship the audit as `docs/superpowers/tools/strings-audit.ps1` for every later module | |

## Tasks

### Task 1: Audit

**Files:** create `docs/superpowers/tools/strings-audit.ps1`.
**Produces:** a list of ids referenced but absent, and Crafting-prefixed ids never referenced.

- [x] Missing: `tor_enchant_prompt_disintegrate`, `tor_gained_items_notification_text`,
      `tor_gained_resource_notification_text`, `tor_refine_all_text` (XML had it as `tor_crafting.refine_all`).
- [x] Orphans: 5 `tor_enchantmentshop_requirement_*`, 8 priest variants; `insufficient_*` revived.
- [x] Culture variants of all enchanter dialog ids: 23 per culture, complete.

### Task 2: Code

**Files:** `CampaignMechanics/Crafting/{EnchantmentShopHelper,EnchanterTownBehavior,EnchantingVM,EnchantableTraitVM,LootCampaignBehavior,RefinementVMExtension,CraftingVMExtension,TORArtisanDistrictCampaignBehavior}.cs`,
`GUI/Prefabs/Crafting/Enchanting.xml`.

- [x] 7 `FindText` → `TORTextHelper`, XML text as default.
- [x] Shop hint, unaffordable text, trait tooltip, refine-all count, donation list → id templates.
- [x] Prefab `Items` / `Enchantments` / `Enchant` / `Done` → `@` bindings on `EnchantingVM`.
- [x] `ironingot6_name` → `tor_ironingot6_name`.

### Task 3: XML

**Files:** `ModuleData/tor_strings.xml`.

- [x] +18 ids, −13 orphans, 1 rename in place. Comment count stays 326; file parses.

### Task 4: Close out

- [x] Build (MSBuild, 0 errors); audit clean except native `str_done`.
- [x] Test plan, code-check pass, PR notes, ledger, spec Amendment 3, `MODULE.md`.

## Verification

Test plan: `docs/testplans/crafting-strings.md`.

## Exit criteria

1. `strings-audit.ps1 -Module CampaignMechanics/Crafting` reports no missing id except `str_done`.
2. `grep -rnF -e FindText -e '\n' CSharpSourceCode/CampaignMechanics/Crafting --include=*.cs | grep -v 'TORCommon.Log\|Exception('` returns nothing.
3. `grep -c "<!--" ModuleData/tor_strings.xml` is still 326.
4. Test plan Run record filled in; ledger `Crafting` Epic 3 reads `PR`.
