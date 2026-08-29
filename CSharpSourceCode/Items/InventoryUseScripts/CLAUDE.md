# Items/InventoryUseScripts

Right-click "use this item" effects on the campaign map, referenced by name from
`Items/ItemTrait.OnInventoryUseScript` and dispatched by
`Items/InventoryUseScriptsCampaignBehavior`.

- **`IInventoryUseScript`** — `OnUse(userParty, item)`, `OnHourlyTick`/`OnDailyTick`
  (for items with a recurring effect while carried, not just a one-shot use).
- **`BaseInventoryUseScript(string[] arguments)`** (`IEquatable`, `[SaveableField]`
  arguments — persisted with the save) — default base; `UseScript` wraps `OnUse` and fires
  `Utilities/TORCampaignEvents.Instance.OnUseInventoryUseScriptObject` for other systems to
  react to. Equality is by type + arguments, so save/load can match instances back up.
  Also defines **`TORUseScriptArgumentException`** and **`ScriptUseData`** (a small
  saveable record of who used what item, when, and how many times).
- **`StartInkStoryScript`** — using the item launches an `Ink/` narrative story.
- **`SkillBookScript`** — grants skill XP/unlocks when read.
- **`CustomResourceContainerScript`** — using the item grants/converts a
  `CampaignMechanics/CustomResources` resource.
- **`EnchantmentBlueprintScript`** — teaches/unlocks an enchantment recipe for
  `CampaignMechanics/Crafting`.
