# AbilitySystem/Spells/Prayers

In-mission "Prayer book" UI — the Prayer equivalent of `../SpellBook`, but opened during a
battle rather than on the campaign map:

- **`BattlePrayerScreen`** (`ScreenBase, IGameStateListener`) + **`BattlePrayerBookState`**
  (`GameState`) — screen/state pair.
- **`BattlePrayersVM`** — root view-model listing the caster's known `Prayer`s.
- **`PrayerItemVM`** (`: AbilityItemVM`, base defined in `../SpellBook`) — one prayer row.
- **`PrayerLoreObjectVM`** — grouping VM analogous to `LoreObjectVM`, but for prayer
  "lores"/deities rather than Winds-of-Magic lores.
