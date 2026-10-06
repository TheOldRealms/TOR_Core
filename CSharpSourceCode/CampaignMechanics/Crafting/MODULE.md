# Crafting — Module Reference

Per-class lookup for `CampaignMechanics/Crafting/`.

| Class | File | What it does |
|---|---|---|
| `CraftingModule` | `CraftingModule.cs` | `[TORModule] : ITORModule` — registers behaviors and models. |
| `EnchanterTownBehavior` | `EnchanterTownBehavior.cs` | Town service: culture → enchanter NPC template/dialogs, opens `EnchantingScreen`. |
| `PriestBehavior` | `PriestBehavior.cs` | Town service: culture/cult → priest NPC, opens the blessing shop. `ReligionObject.All` is the one surviving outbound edge. |
| `TORArtisanDistrictCampaignBehavior` (+ `TorItemDuplicationData`, `TorItemBeingCraftedData`) | `TORArtisanDistrictCampaignBehavior.cs` | Artisan district: duplication/queued-crafting state, replaces the vanilla smithy menu. |
| `EnchantmentBlueprintBehavior` / `EnchantmentBlueprints` | `EnchantmentBlueprintBehavior.cs`, `EnchantmentBlueprints.cs` | The campaign-wide blueprint store and its query/learn API. |
| `EnchantmentIngredientLootCampaignBehavior` | same | Ingredient drops in post-battle loot; clears leftover settlement stock. |
| `LootCampaignBehavior` | same | Adds/prunes magical item-trait loot in the lootable pool. |
| `TORSmithingModel`, `TOREnchantmentIngredientsModel`, `TOREnchantmentCraftingModel` | `Models/` | Vanilla-smithing overrides; ingredient economy and drop factors; enchantment limits and cost. |
| `EnchantmentHelper` | `EnchantmentHelper.cs` | Blueprint requirements/eligibility queries, plus `CreateEnchantedItem` / `CreateItemCopy`. |
| `EnchantmentShopHelper` | `EnchantmentShopHelper.cs` | The "learn a blueprint" purchase inquiry on top of `EnchantmentHelper`. |
| `CraftingModelsExtensions` | same | Module-local `GameModels` accessors, so `Framework` never names these models. |
| `EnchantingVM` + `EnchantableItemVM`, `EnchantableTraitVM`, `EnchantingIngredientVM` (+ `EnchantingIngredientWidget`), `EnchantingItemTableauVM` | `*VM.cs` | The enchanting screen's view-models. |
| `CraftingVMExtension` / `RefinementVMExtension` | same | "Refine All" on the vanilla refinement screen: button plus repeat-count logic. |
| `EnchantingScreen` / `EnchantingState` | same | Gauntlet screen + paired `GameState`; entered via `EnchantingScreen.Open()`. |
| `EnchantmentBlueprintScript` | same | Inventory-use script on blueprint items. |
| `CraftingPatches` | same | Filters vanilla crafting categories and smithing orders to TOR templates. |

## Public surface — do not rename without updating every consumer

Frozen by Epic 2; anything absent is free to restructure.

| Name | Consumed by |
|---|---|
| `EnchantmentBlueprints.Learn` / `.IsKnown` / `.GetKnown` | `CraftingCareerHookRegistrations` (via the hook), `RunelordCareerButtonBehavior`, `Quests/Careers/{RunelordQuest,RunesmithQuest,OrcShamanQuest2}`, `InkStory`, `TORConsoleCommands` |
| `EnchantmentBlueprintBehavior` | `TORConsoleCommands` |
| `EnchantmentHelper.GetBlueprintRequirements` / `.GetUnmetRequirement` / `.CreateEnchantedItem` | `RunelordCareerButtonBehavior`, `OathGoldBehavior`, `TORCustomSettlementCampaignBehavior`, `TORConsoleCommands` |
| `EnchantmentHelper.BlueprintRequirement` | `RunelordCareerButtonBehavior` names it explicitly — it is what frozen `GetBlueprintRequirements` returns |
| `EnchantmentIngredientLootCampaignBehavior` | `TORCustomSettlementCampaignBehavior` |
| `EnchanterTownBehavior` (type name) | `HeroExtensions`; also frozen by save-data keying |
| `TORArtisanDistrictCampaignBehavior.Instance` | `ItemPatches` |
| `RefinementVMExtension` | `ViewModelPatches` |
| `TorItemDuplicationData` | `SaveableTypeDefiners` (save id 16) |

## See also

`Framework/TorEnchantingIngredients`, `Items/ItemTrait`, `Framework/CraftingCareerHooks`.
Strings: `tor_strings.xml` — *priest blessings*, *Enchantment Shop*, *Enchanting UI Messages*.
