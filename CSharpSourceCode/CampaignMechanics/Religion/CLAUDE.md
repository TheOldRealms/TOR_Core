# CampaignMechanics/Religion

Warhammer deity/pantheon system layered onto heroes and settlements.

- **`ReligionObject`** (`: MBObjectBase`, XML-defined via `MBObjectManager`) — a deity/faith:
  name, deity name, lore/blessing text, associated `CultureObject`, `Pantheon` enum,
  `HostileReligions` (feeds `GetHostilityFactor`, used by diplomacy/relations math),
  religious troops/elite units/artifacts, `InitialClans`. `FillAll()` loads the full list
  from `Religions.xml` (`SubModule.BeginGameStart`). Has an encyclopedia link.
- **`ReligionObjectHelper`** (static) — queries/helpers (e.g. a hero's dominant religion,
  compatibility scoring between religions/pantheons).
- **`ReligionCampaignBehavior`** (`: CampaignBehaviorBase, IDisposable`) — the runtime
  mechanic: heroes gain/lose faith, blessings apply, hostility affects relations.
- **`ReligionEncyclopediaPage`** (`: EncyclopediaPage, IPublicEncyclopediaPage`) +
  **`TorEncyclopediaModel`** (`: OverrideEncyclopediaModel`) +
  **`TorEncyclopediaListItemNameComparer`** — adds a Religion category/page to the game's
  encyclopedia.
- **`EncyclopediaReligionObjectVM`** (`: EncyclopediaContentPageVM`) — the page's view-model.

Settlements can be tied to a religion via `ReligionObject` on
`CampaignMechanics/TORCustomSettlement/Component/TORBaseSettlementComponent` (shrines, etc.).
Also see `Models/TORFaithModel`.
