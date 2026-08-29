# AbilitySystem/Spells/SpellBook

Campaign-map "Spellbook" screen UI (a `ScreenBase` + `GameState`, Gauntlet-based, not a
mission behavior):

- **`SpellBookScreen`** (`ScreenBase, IGameStateListener`) + **`SpellBookState`** (`GameState`)
  — screen/state pair pushed onto the game state stack to open the spellbook.
- **`SpellBookVM`** — root view-model: lists known/learnable spells grouped by `LoreObject`.
- **`AbilityItemVM`** (abstract `ViewModel`) — shared row VM for a single ability entry;
  base for `SpellItemVM` here and `PrayerItemVM` in `../Prayers`.
- **`LoreObjectVM`** — VM wrapper around a `LoreObject` (lore icon/name/unlock state).
- **`SpellBookMapIconVM`** — the map-bar icon/button that opens the spellbook.
- **`StatItemVM`** — one label/value stat row (cooldown, Winds cost, tier, type), built from
  `AbilityTemplate.GetStats`.
