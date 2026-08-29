# CampaignMechanics/CustomResources

TOR's per-culture "second currency" system — each major culture has its own thematic
resource tracked alongside gold/influence.

- **`CustomResource`** — definition: `StringId`, localized name/description
  (`tor_custom_resource_name/description` game texts), icon, which culture(s) it belongs to,
  and an optional custom tooltip function. `GetCustomResourceIconAsText` renders it as an
  inline `<img>` tag for use in UI text.
- **`CustomResourceManager`** (singleton) — registers all resources on `Initialize()`
  (called from `SubModule.InitializeGameStarter`):
  - Empire → **Prestige**
  - Bretonnia → **Chivalry** (`ChivalryHelper`)
  - Sylvania/Mousillon (Vampire factions) → **DarkEnergy**
  - Asrai (Wood Elves) → **ForestHarmony** (`ForestHarmonyHelper`)
  - Eonir (Wood Elves) → **CouncilFavor** (`FavorHelper`)
  - Dawi (Dwarfs) → **OathGold** (`OathGoldHelper`)
  - Greenskin → **Teef**/**Waaagh** (see `TeefHelper`, `WaaaghMeter/`)

  Also tracks a `_massBudget`/`_resourceChanges` and hooks the party screen
  (`ScreenManager.OnPushScreen/OnPopScreen`) to show resource changes in the party UI.
- **`ChivalryHelper` / `ForestHarmonyHelper` / `FavorHelper` / `OathGoldHelper` / `TeefHelper`**
  (static) — per-resource gain/spend rules and tooltip info providers.

Actual gain/spend triggers live in the sibling `CustomResourceBehavior/` folder;
`Models/TORCustomResourceModel` governs generalized cost scaling
(`CustomResource.GetCustomResourceGeneralizedFactor`).

## Subfolder

- **`WaaaghMeter/`** — Greenskin-specific Waaagh resource UI (map view + VM) and behavior.
