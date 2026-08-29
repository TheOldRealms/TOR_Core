# CampaignMechanics/TORCustomSettlement

TOR's bespoke settlement/world-object types — special map locations beyond
towns/castles/villages, each with its own component, menu, and (usually) a
`CampaignBehaviorBase`. Registered as engine object types in `SubModule.BeginGameStart`
(`game.ObjectManager.RegisterType<...Component>(...)`).

- **`TORCustomSettlementCampaignBehavior`** — umbrella behavior wiring the custom
  settlement types into the campaign (spawning, menus, persistence).
- **`TrollCaveCampaignBehavior`** — Troll Cave lair: spawns defending trolls
  (`TrollCaveDefenderPartyComponent`), raidable by the player.
- **`GreenskinBrawlBehavior`** (+ `BrawlMissionResult`) — Greenskin settlement brawl
  mini-game (scored mission result feeding back into the campaign).
- **`GoblinRecruitmentBehavior`** — goblin troop recruitment from custom settlements.
- **`TORSettlementMenuHelpers`** (static) — shared menu-building helpers for the
  `CustomSettlementMenus/` logic classes.

## Subfolders

- **`Component/`** — `SettlementComponent` subclasses, one per settlement type (see its
  CLAUDE.md): `TORBaseSettlementComponent` (abstract base: owner clan, `ReligionObject`,
  active flag) → `BaseRaiderSpawnerComponent` (abstract, spawns raiders periodically) →
  `ChaosPortalComponent`, `HerdStoneComponent`, `SlaverCampComponent`, `TrollCaveComponent`;
  plus `CursedSiteComponent`, `OakOfAgesComponent`, `ShrineComponent`, `WorldRootsComponent`.
- **`CustomSettlementMenus/`** — `TORBaseSettlementMenuLogic` (abstract) and its concrete
  per-settlement-type menu builders (Troll Cave, Cursed Site, Oak of Ages, Shrine,
  Raiding Site).
