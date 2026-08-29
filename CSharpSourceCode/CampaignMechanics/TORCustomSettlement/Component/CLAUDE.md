# CampaignMechanics/TORCustomSettlement/Component

`SettlementComponent` subclasses, one per bespoke settlement/world-object type registered
in `SubModule.BeginGameStart`.

- **`TORBaseSettlementComponent`** (abstract `: SettlementComponent`) — shared base: parses
  common XML attrs (`background_mesh`, `wait_mesh`, `background_crop_position`, `religion`),
  resolves `Religion` (`CampaignMechanics/Religion`), tracks `OwnerClan`/`IsActive`.
  - **`BaseRaiderSpawnerComponent`** (abstract) — adds periodic raider-troop spawning; base
    for the "hostile lair" settlement types:
    - **`ChaosPortalComponent`** — Chaos incursion portal.
    - **`HerdStoneComponent`** — Beastmen herdstone.
    - **`SlaverCampComponent`** — Dark Elf/Norscan-style slaver camp.
    - **`TrollCaveComponent`** — troll lair (pairs with `../TrollCaveCampaignBehavior`).
  - **`CursedSiteComponent`** (`IDisposable`) — a cursed/haunted site (Vampire-flavored).
  - **`OakOfAgesComponent`** — Wood Elf sacred site.
  - **`ShrineComponent`** — a religious shrine tied to a `ReligionObject`.
  - **`WorldRootsComponent`** — Wood Elf world-roots network site.
