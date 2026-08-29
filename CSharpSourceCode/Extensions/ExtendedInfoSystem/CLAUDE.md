# Extensions/ExtendedInfoSystem

Side-table data attached to vanilla `CharacterObject`/`Hero`/`MobileParty` instances (keyed
by string id) since those types can't be subclassed with extra fields.

- **`ExtendedInfoManager`** (`: CampaignBehaviorBase`, singleton `Instance`) — owns the
  dictionaries (`_characterInfos`, `_heroInfos`, `_partyInfos`, plus banner-resource and
  settlement-info tables), loads static per-troop data from
  `tor_extendedunitproperties.xml` (TOR_Core's ExtendedData folder), and hooks nearly every
  relevant `CampaignEvents` (session launch, new game, hourly/daily/quarter-daily ticks,
  hero created/killed, party created/destroyed, troop upgrades, battle end) to keep the
  side-tables in sync. For `CustomGame` (non-campaign), `CreateDefaultInstanceAndLoad()` is
  used instead (see `SubModule.InitializeGameStarter`).
- **`CharacterExtendedInfo`** (+ `ResourceCostTuple`, `ResistanceTuple`, `AmplifierTuple`,
  `DamageProportionTuple`, all `IEquatable`) — per-`CharacterObject` extra data: resource
  costs, damage-type resistances/amplifications/proportions (feeds
  `BattleMechanics/DamageSystem/TORDamageHelper`), race/culture-driven combat traits.
- **`HeroExtendedInfo(character)`** — per-`Hero` data (e.g. `CareerID` — see
  `AbilitySystem/Scripts/CareerAbilityScript.GetEffectsToTrigger`, — Winds of Magic pool,
  religion/faith state).
- **`MobilePartyExtendedInfo`** — per-party data.

Accessed via extension methods in `../HeroExtensions`/`../CharacterObjectExtensions`/
`../MobilePartyExtensions` (e.g. `hero.GetExtendedInfo()`) rather than directly.
