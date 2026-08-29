# CampaignMechanics/Crafting

TOR's item-enchanting system: a full Gauntlet screen (separate from vanilla smithing) that
applies magical `Items/ItemTrait`s to weapons/armor using ingredients and priest/artisan
services.

- **`EnchantmentHelper`** (static) — `CreateEnchantedItem`: clones an `ItemObject` with a
  fresh generated id and applies the chosen traits/`ItemModifier`, optionally flagged
  player-crafted.
- **`EnchantingScreen`** (`: ScreenBase, IGameStateListener`) + **`EnchantingState`**
  (`: GameState`) — the screen/state pair that opens the enchanting UI.
- **`EnchantingVM`** (root view-model) with supporting VMs: **`EnchantableItemVM`**
  (an item eligible for enchanting), **`EnchantableTraitVM`** (a selectable trait/effect),
  **`EnchantingIngredientVM`** (+ **`EnchantingIngredientWidget`** `: RichTextWidget`,
  internal), **`EnchantingItemTableauVM`** (3D item preview tableau).
- **`EnchanterTownBehavior`** (`: CampaignBehaviorBase`) — the town service entry point
  that opens `EnchantingScreen`.
- **`PriestBehavior`** (`: CampaignBehaviorBase`) — a related town service (priest-blessed
  enchantments/holy traits, ties into `CampaignMechanics/Religion`).
- **`TORArtisanDistrictCampaignBehavior`** (+ `TorItemDuplicationData`,
  `TorItemBeingCraftedData`) — an artisan-district crafting service (queued item
  crafting/duplication over time).
- **`LootCampaignBehavior`** / **`EnchantmentIngredientLootCampaignBehavior`** — adds
  enchanting ingredients and enchanted items to post-battle/dungeon loot tables.

See `Items/TorEnchantingIngredients`, `Items/ItemTrait`, and
`Models/TOREnchantmentCraftingModel`/`TOREnchantmentIngredientsModel` for the underlying
data/cost models this UI drives.
