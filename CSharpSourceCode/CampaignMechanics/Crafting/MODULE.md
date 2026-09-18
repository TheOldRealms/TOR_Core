# Crafting — Module Reference

Quick per-class index of `CampaignMechanics/Crafting/`. For the narrative version (how the
pieces fit together) see [`CLAUDE.md`](./CLAUDE.md) in this folder; this file is a flat
lookup table instead.

## Registration

| Class | File | What it does |
|---|---|---|
| `CraftingModule` | `CraftingModule.cs` | `[TORModule] : ITORModule` — registers this module's campaign behaviors (`EnchanterTownBehavior`, `TORArtisanDistrictCampaignBehavior`, `PriestBehavior`, `EnchantmentIngredientLootCampaignBehavior`, `LootCampaignBehavior`) and models (`TORSmithingModel`, `TOREnchantmentIngredientsModel`, `TOREnchantmentCraftingModel`) with `SubModule`. |

## Campaign behaviors

| Class | File | What it does |
|---|---|---|
| `EnchanterTownBehavior` | `EnchanterTownBehavior.cs` | Town-service entry point: maps culture → enchanter NPC template/dialogs and opens `EnchantingScreen`. |
| `TORArtisanDistrictCampaignBehavior` (+ `TorItemDuplicationData`, `TorItemBeingCraftedData`) | `TORArtisanDistrictCampaignBehavior.cs` | Artisan-district service: tracks duplicated/enchanted items (via `TORCampaignEvents.ItemDuplicated`) and queued item-crafting state; removes the vanilla smithy menu and rearranges town menus in its place. |
| `PriestBehavior` | `PriestBehavior.cs` | Town-service entry point for priest-blessed enchantments: maps culture/cult → priest NPC template and opens the blessing-flavored enchanting shop. |
| `EnchantmentIngredientLootCampaignBehavior` | `EnchantmentIngredientLootCampaignBehavior.cs` | Adds enchanting ingredients to post-battle loot (`OnCollectLootsItemsEvent`) by summing per-enemy drop factors from `TOREnchantmentIngredientsModel`; also clears any leftover ingredient stock from settlements daily. |
| `LootCampaignBehavior` | `LootCampaignBehavior.cs` | Adds/removes magical (item-trait) loot to/from the lootable pool, including pruning items no longer used by the player or companions. |

## Models

| Class | File | What it does |
|---|---|---|
| `TORSmithingModel` (`: DefaultSmithingModel`) | `Models/TORSmithingModel.cs` | Vanilla-smithing overrides: hides crafting templates with no accessible pieces (`ValidateHiddenCraftingTemplates`), filters NPC smithing orders by culture-appropriate weapon category, and applies career/religion energy-cost and refining-formula modifiers via `CraftingCareerHooks`. |
| `TOREnchantmentIngredientsModel` | `Models/TOREnchantmentIngredientsModel.cs` | Enchanting-ingredient economy: per-ingredient custom-resource cost, drop factor per defeated character/context, and the random+career-bonus roll that turns a battle's drop score into a loot amount. |
| `TOREnchantmentCraftingModel` | `Models/TOREnchantmentCraftingModel.cs` | Enchanting-session limits: max simultaneous enchantments/blessings a party of heroes can apply (career/perk/culture-driven), and the effective ingredient cost of a trait after career cost-reduction hooks. |

## Static helpers

| Class | File | What it does |
|---|---|---|
| `EnchantmentHelper` | `EnchantmentHelper.cs` | Blueprint data/eligibility queries (who can learn a blueprint, is it already known/in inventory) and `CreateEnchantedItem`/`CreateItemCopy` — clones an `ItemObject` with a fresh id and applies chosen traits/`ItemModifier`. |
| `EnchantmentShopHelper` | `EnchantmentShopHelper.cs` | Builds and shows the "learn a blueprint" purchase inquiry (eligible heroes, gold/custom-resource cost, requirement text) on top of `EnchantmentHelper`'s data. |
| `CraftingModelsExtensions` | `CraftingModelsExtensions.cs` | `GameModels` extension methods (`GetSmithingModel()`, `GetEnchantmentIngredientModel()`) — this module's equivalent of the shared `GameModelsExtensions`, kept local so `Framework` doesn't need to know these concrete model types. |
| `TorEnchantingIngredients` (+ `TorTradeGoodType` enum) | `TorEnchantingIngredients.cs` | Static lookup of the six enchanting-ingredient `ItemObject`s (Arcane Scroll, Blessed Water, Dragon Blood, Amber Crystal, Warpstone Dust, Gem Stone), loaded once per campaign via `LoadIngredients()`. |

## View-models / UI

| Class | File | What it does |
|---|---|---|
| `EnchantingVM` | `EnchantingVM.cs` | Root view-model for the enchanting screen: item list, trait list/selection, ingredient list, preview tableau. |
| `EnchantableItemVM` | `EnchantableItemVM.cs` | One equippable item eligible for enchanting, shown in `EnchantingVM`'s item list. |
| `EnchantableTraitVM` | `EnchantableTraitVM.cs` | One selectable enchantment trait/effect, with name/icon/description/tooltip and a selection callback. |
| `EnchantingIngredientVM` | `EnchantingIngredientVM.cs` | One ingredient row: current vs. pending amount, name/icon, tooltip. |
| `EnchantingIngredientWidget` (internal) | `EnchantingIngredientWidget.cs` | `: RichTextWidget` — renders an ingredient amount in red when it goes negative (insufficient stock). |
| `EnchantingItemTableauVM` | `EnchantingItemTableauVM.cs` | 3D item-preview tableau data (item id, banner code, item-modifier id) for the equipment being enchanted. |
| `CraftingVMExtension` | `CraftingVMExtension.cs` | `[ViewModelExtension(typeof(CraftingVM))]` — adds a "Refine All" button (repeats refinement until stamina/materials run out) to the vanilla smithing-refinement screen. |
| `RefinementVMExtension` | `RefinementVMExtension.cs` | `[ViewModelExtension(typeof(RefinementVM))]` — backs `CraftingVMExtension`'s "Refine All": computes the max repeatable-refinement count from stamina and material stock and executes them in a loop. |

## Screen / state

| Class | File | What it does |
|---|---|---|
| `EnchantingScreen` (`: ScreenBase, IGameStateListener`) | `EnchantingScreen.cs` | Gauntlet screen that hosts `EnchantingVM`; opened via `EnchantingScreen.Open()`. |
| `EnchantingState` (`: GameState`) | `EnchantingState.cs` | Minimal `GameState` paired with `EnchantingScreen` via `[GameStateScreen]`. |

## Item scripts

| Class | File | What it does |
|---|---|---|
| `EnchantmentBlueprintScript` (`: BaseInventoryUseScript`) | `EnchantmentBlueprintScript.cs` | Inventory-use script attached to blueprint items: on use, offers eligible party heroes (by skill/attribute/lore requirement) the chance to learn the referenced enchantment blueprint. |

## Harmony patches

| Class | File | What it does |
|---|---|---|
| `CraftingPatches` | `CraftingPatches.cs` | Filters the vanilla crafting-category popup and daily NPC smithing-order generation down to TOR's valid/culture-appropriate `CraftingTemplate`s; re-initializes saved crafted items before the game strips non-ready objects. |

## See also

- `Items/TorEnchantingIngredients` usage and `Items/ItemTrait` — the underlying enchantment
  data model this UI drives.
- `CraftingCareerHooks` (in `Framework/`) — the career/religion cost-reduction and loot-bonus
  hooks `TORSmithingModel`/`TOREnchantmentCraftingModel`/`TOREnchantmentIngredientsModel`
  call into, kept out of this module so Crafting doesn't need to know Career/Religion types.
