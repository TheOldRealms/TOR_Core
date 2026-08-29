# CampaignMechanics/TORCustomSettlement/CustomSettlementMenus

Settlement-menu ("wait menu") builders for the custom settlement types in `../Component`.

- **`TORBaseSettlementMenuLogic`** (abstract) — shared menu-registration scaffolding
  (takes a `CampaignGameStarter`).
- **`TrollCaveMenuLogic`**, **`CursedSiteMenuLogic`**, **`OakOfAgesMenuLogic`**,
  **`ShrineMenuLogic`**, **`RaidingSiteMenuLogic`** — one concrete menu per settlement type,
  built with `../TORSettlementMenuHelpers`.

## TODO

- **`TrollCaveMenuLogic`** used to track an `_isClearingCave` flag (true when clearing the
  cave, false when a luring attempt goes wrong) but nothing downstream ever read it, so it
  was removed as dead code (see the TODO comment left at its old declaration site). If
  `StartClearCave`/`StartTrollBattle` are meant to diverge on outcome (different
  loot/message/etc.), that distinction still needs implementing.
