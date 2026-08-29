# Items

Item-level extensions: magical weapon/armor traits (enchantments), custom item metadata,
and inventory-item "use" scripts (right-click a campaign item to trigger an effect).

- **`ItemTrait`** (`IEquatable<ItemTrait>`, XML-defined) — a magical trait/enchantment
  definition: `ResistanceTuple`/`AmplifierTuple`/`AdditionalDamageTuple`
  (from `Extensions/ExtendedInfoSystem/CharacterExtendedInfo`'s tuple types, reused here for
  items), an `OnWeaponHitScript`/`OnInventoryUseScript` reference (by name, resolved to a
  script class), `ImbuedStatusEffectId`/`ImbuedEffectChance`, crafting cost
  (`IsCraftable`, `IngredientItem`/`IngredientAmount`), valid item type, and a `StatsTuple`
  for flat stat bonuses. This is the data half of `CampaignMechanics/Crafting`'s enchanting
  system and of pre-enchanted unique items.
- **`ItemTraitManager`** (static) — loads/indexes all `ItemTrait`s (`LoadItemTraits`,
  called from `SubModule.OnSubModuleLoad`).
- **`ItemTraitAgentComponent`** (`: AgentComponent`) — tracks which traits are active on an
  agent's equipped gear during a mission.
- **`ExtendedItemObjectManager`** / **`ExtendedItemObjectProperties`** — extra per-`ItemObject`
  metadata beyond what `ItemTrait` covers (loaded via `ExtendedItemObjectManager.LoadXML`
  in `SubModule.OnSubModuleLoad`).
- **`TorEnchantingIngredients`** — ingredient item definitions used by the crafting UI.
- **`TorItemMenuVM`** / **`TorItemTraitVM`** / **`TorInventoryItemTupleWidget`** /
  **`TorImageIdentifierWidget`** — inventory/tooltip UI showing an item's traits.
- **`InventoryUseScriptsCampaignBehavior`** (`: CampaignBehaviorBase`) — dispatches
  "use this item" actions from the inventory screen to the matching
  `InventoryUseScripts/` script.

## Subfolders

- **`WeaponHitScripts/`** — `IWeaponHitScript` implementations: on-hit weapon procs
  (see its CLAUDE.md).
- **`InventoryUseScripts/`** — `IInventoryUseScript` implementations: right-click item-use
  effects on the campaign map (see its CLAUDE.md).
