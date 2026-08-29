# Extensions

Grab-bag of C# extension methods on vanilla TaleWorlds types, plus two more structured
sub-frameworks: the "extended info" side-data system and the "view-model extension" UI
injection system.

## Root-level extension method classes

One static class per extended type, all in `TOR_Core.Extensions`:
`AgentExtensions`, `AgentDrivenPropertiesExtensions`, `BannerExtensions`,
`CharacterObjectExtensions`, `ClanExtensions`, `CollectionExtensions`,
`ConversationManagerExtensions`, `FormationExtensions`, `GameMenuExtensions`,
`GameModelsExtensions` (shortcuts to `Campaign.Current.Models.GetXyzModel()`),
`HeroExtensions`, `ItemObjectExtensions`, `KingdomExtension`, `MissionExtensions`,
`MobilePartyExtensions`, `SettlementExtensions`, `SkillExtensions`, `TeamExtensions`,
`ViewModelExtensions`. Also **`DebugMethods`** (static) — misc debug/diagnostic helpers.
These are the mod's "everywhere" utility layer — e.g. `AgentExtensions.GetHero()`,
`GetCareer()`, `GetAbility()`, `IsSpellCaster()`, `HasCareer()` used throughout
`AbilitySystem`/`BattleMechanics`/`CharacterDevelopment`.

## `ExtendedInfoSystem/`

TOR needs to attach extra runtime/persisted data to vanilla objects
(`CharacterObject`, `Hero`, `MobileParty`) without subclassing them — see its CLAUDE.md.

## `UI/`

The "view-model extension" pattern: attaches extra bindable properties/commands onto
vanilla Gauntlet `ViewModel`s (party screen, character developer, encyclopedia, crafting,
conversation, etc.) without needing a full custom screen — see its CLAUDE.md.
