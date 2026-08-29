# AbilitySystem/Spells

Winds-of-Magic spell layer on top of the generic `Ability`/`AbilityTemplate` system.

- **`Spell`** (`: Ability`) — a castable Winds-of-Magic spell instance.
- **`SpellCastingLevel`** enum — spell tier labels used in UI (mirrors `AbilityTemplate.SpellTier`).
- **`LoreObject`** — one "Lore" of magic (school), e.g. `LoreOfFire`, `LoreOfLife`,
  `HighMagic`, `DarkMagic`, `Necromancy`, `RuneMagic`, `BigWaaagh`, `MinorMagic`.
  Hardcoded static list (`GetAll`/`GetLore`) encodes lore-vs-culture eligibility
  (`DisabledForCultures`) and `IsRestrictedToVampires` — this is where "which races can
  learn which magic" is defined (see `Utilities/TORConstants.Cultures` for the culture IDs:
  Empire, Bretonnia, Mousillon, Sylvania, Asrai, Eonir, Dawi/Dwarfs, Greenskin...).
  Note the in-code caveat: Spellsingers (Asrai) get a special-cased bypass of the
  High/Dark Magic culture lock via `CampaignMechanics/SpellTrainers/SpellTrainerInTownBehavior`.

## Subfolders

- **`SpellBook/`** — the campaign-map spellbook screen for learning/viewing spells:
  `SpellBookScreen`/`SpellBookState` (game state + screen), `SpellBookVM` (root VM),
  `AbilityItemVM` (abstract row VM) → `SpellItemVM`, `LoreObjectVM`,
  `SpellBookMapIconVM`, `StatItemVM` (stat-line rows shown via `AbilityTemplate.GetStats`).
- **`Prayers/`** — the equivalent in-mission book for battle Prayers:
  `BattlePrayerScreen`/`BattlePrayerBookState`, `BattlePrayersVM`,
  `PrayerItemVM` (`: AbilityItemVM`), `PrayerLoreObjectVM`.
