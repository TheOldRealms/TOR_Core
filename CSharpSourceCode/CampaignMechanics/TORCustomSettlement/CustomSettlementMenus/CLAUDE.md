# CampaignMechanics/TORCustomSettlement/CustomSettlementMenus

Settlement-menu ("wait menu") builders for the custom settlement types in `../Component`.

- **`TORBaseSettlementMenuLogic`** (abstract) — shared menu-registration scaffolding
  (takes a `CampaignGameStarter`).
- **`TrollCaveMenuLogic`**, **`CursedSiteMenuLogic`**, **`OakOfAgesMenuLogic`**,
  **`ShrineMenuLogic`**, **`RaidingSiteMenuLogic`** — one concrete menu per settlement type,
  built with `../TORSettlementMenuHelpers`.
