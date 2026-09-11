# `Crafting` Framework — Test Plan

Manual test plan for [`../superpowers/plans/crafting-framework.md`](../superpowers/plans/crafting-framework.md).

**What this epic changed:** two types moved namespace into `Framework/` —
`TORSettlementMenuHelpers` (town-menu ordering, out of `TORCustomSettlement/`) and
`TorEnchantingIngredients` + `TorTradeGoodType` (the six ingredient `ItemObject`s, out of
`Crafting/`). No member was renamed and no logic was touched.

**What this plan must prove:** a player cannot tell this landed.

## Decisions taken

| # | Decision | Alternatives considered | Why this one |
|---|---|---|---|
| D1 | `TorEnchantingIngredients` promoted to `Framework/` rather than kept in `Crafting/`. | Leave it and let `Careers` keep its `using`; or move it to `Items/` (where `vertical-slicing-proposal.md` wrongly listed it). | It has no `using TOR_Core.*` of its own, and two *Framework* files (`Items/ItemTrait`, `Models/TORFaithModel`) already reached into the module for it — a Framework→module arrow, which is the thing this epic exists to remove. |
| D2 | `LoadIngredients()` is still called from `TORArtisanDistrictCampaignBehavior`, not from Framework. | Move the call into a Framework initializer. | Framework owns the type; Crafting still owns *when* the catalogue loads. Moving the call would change load order — behaviour risk for no boundary gain. |
| D3 | The remaining inbound references (`EnchantmentBlueprints`, `EnchantmentHelper`, `EnchantmentIngredientLootCampaignBehavior`) are **frozen as public surface**, not cut. | Build a second `Framework/` hook contract for blueprint queries. | The spec's ratchet: hook contract #2 for a case this shapeless is how `Framework/` becomes a god object. Freezing converts unbounded risk into a checklist; a real contract can be designed when a second module needs the same thing. |
| D4 | `PriestBehavior` → `Religion` left in place. | Cut it now by promoting `ReligionObject`. | `Religion/` has not been modularized; the cut would guess where its boundary lands. Recorded on both ledger rows. |
| D5 | The extension-method blind spot found in `EnchanterTownBehavior.cs:438,443` (`Hero.MainHero.GetCareer().StringId` — `GetCareer()` is Framework, `CareerObject` is Careers) is recorded, not fixed. | Chase it now across Crafting; or build a detection pass mid-epic. | The coupling is a string-id read, the weakest kind, and a detection pass is tooling the epic did not ask for. It affects all 30 remaining modules, so it belongs in the ledger as a Framework work item — spec Amendment 2 — not in this diff. |

## Run record

| | |
|---|---|
| Date | |
| Branch / commit | `feature/FrameworkCrafting` |
| Save used | |
| Tester | |
| Result | |

## Preconditions

- A **dwarf (DAWI)** campaign — the same one used for `p2-p4-single-store-migration.md` is ideal,
  since it already has blueprints learned and a Karak to walk into. Any culture works for S1/S2;
  DAWI is only needed to exercise the artisan district and rune table together.
- An existing save is fine. This epic changes no save data, no type name and no `SyncData` key —
  if an old save fails to load, that is a real failure, not an expected migration.
- Console enabled.

## Scenarios

### S1 — The game loads at all — **gate**

| | |
|---|---|
| Setup | Launch the game to the main menu. |
| Action | Load the existing DAWI save. |
| Expect | No load-time crash, no missing-type exception in the log. A namespace change that missed a consumer shows up here, not later. |
| Result | ☐ pass ☐ fail |

### S2 — Ingredients still resolve — **gate**

| | |
|---|---|
| Setup | Enter a Karak or any town with an enchanter. |
| Action | Open the enchanting table and look at the ingredient list. |
| Expect | All six ingredients (Arcane Scroll, Blessed Water, Dragon Blood, Amber Crystal, Warpstone Dust, Gem Stone) are named with their real names and your real stock counts. |
| Fail | Blank names, zeroes across the board, or an empty list — `LoadIngredients()` did not run, or ran against a catalogue nothing populated. |
| Result | ☐ pass ☐ fail |

### S3 — Town menu order — **gate**

| | |
|---|---|
| Setup | Enter a town that has the artisan district. |
| Action | Read the town menu top to bottom. |
| Expect | The enchanting/artisan entries sit where they always did, and the vanilla smithy entry is still gone. `TORSettlementMenuHelpers.RearrangeTownMenus` is the only caller of that ordering; if it silently no-ops the entries land at the bottom of the menu. |
| Result | ☐ pass ☐ fail |

### S4 — The other two menu callers

| | |
|---|---|
| Setup | Visit a Greenskin settlement (brawl) and a settlement offering goblin recruitment. |
| Action | Open each menu. |
| Expect | The brawl and goblin-recruitment entries are in their usual positions. These three files were the other consumers of the moved helper. |
| Result | ☐ pass ☐ fail |

### S5 — Enchant an item end to end

| | |
|---|---|
| Setup | `tor.check_enchantment_blueprints` to pick a craftable blueprint. |
| Action | Enchant a valid item with it at the table. |
| Expect | Ingredient cost is charged, the item is produced, and the ingredient counts drop by the right amount. This exercises `TOREnchantmentIngredientsModel` and `TOREnchantmentCraftingModel`, both of which read the promoted catalogue. |
| Result | ☐ pass ☐ fail |

### S6 — Ingredient loot still drops

| | |
|---|---|
| Setup | Note your ingredient counts. |
| Action | Fight and win a field battle, then collect loot. |
| Expect | Ingredients appear in the loot at the usual rate. `EnchantmentIngredientLootCampaignBehavior` and `OathGoldBehavior` both resolve ingredients through the moved type. |
| Result | ☐ pass ☐ fail |

### S7 — Career perk path

| | |
|---|---|
| Setup | A hero with a career perk that grants or converts an enchanting trade good. |
| Action | Trigger it (perk selection, or the relevant campaign event). |
| Expect | Works unchanged. `TORCareerPerkCampaignBehavior` was the one file whose `using TOR_Core.CampaignMechanics.Crafting` was deleted outright — if anything else in it needed Crafting, it fails at build, not here, but exercise the path anyway. |
| Result | ☐ pass ☐ fail |

### S8 — Runelord button

| | |
|---|---|
| Setup | At a Karak with runes known. |
| Action | Open the unit-rune career button. |
| Expect | Rune list, ingredient icons and requirement text all render. This file consumes both the promoted catalogue and the frozen `EnchantmentHelper` surface. |
| Result | ☐ pass ☐ fail |

## Commands added by this epic

None. Every scenario is reachable with existing commands; `tor.check_enchantment_blueprints`
(added by the Centralize branch below this one) is the only one used.

## Summary

| Scenario | Result | Notes |
|---|---|---|
| **S1 Save loads (gate)** | | |
| **S2 Ingredients resolve (gate)** | | |
| **S3 Town menu order (gate)** | | |
| S4 Brawl / goblin menus | | |
| S5 Enchant end to end | | |
| S6 Ingredient loot | | |
| S7 Career perk path | | |
| S8 Runelord button | | |

**Sign-off**

- [ ] Yes — S1, S2 and S3 all pass
- [ ] No — blocked by:
