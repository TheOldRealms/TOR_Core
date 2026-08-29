# CampaignMechanics/RegimentsOfRenown

Named elite "Regiment of Renown" units (a tabletop Warhammer concept: unique, upgraded
unit templates) recruitable from specific settlements.

- **`RORManager`** (static) — loads/indexes ROR templates (`LoadTemplates`, called from
  `SubModule.OnSubModuleLoad`).
- **`RORSettlementTemplate`** — links a settlement to the regiment(s) it can recruit.
- **`RORCampaignBehavior`** (`: CampaignBehaviorBase`) — availability/recruitment logic.
- **`ToRSettlementNameplateVM`** (`: SettlementNameplateVM`) — extends the map settlement
  nameplate VM, presumably to flag settlements offering a Regiment of Renown.
