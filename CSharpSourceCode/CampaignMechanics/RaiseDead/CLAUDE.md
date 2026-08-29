# CampaignMechanics/RaiseDead

Vampire Counts / Mousillon (undead) and Asrai (tree spirit) resurrection-flavored mechanics.

- **`RaiseDeadInTownBehavior`** (`: CampaignBehaviorBase`) — a town service letting an
  undead-aligned player raise fallen troops as undead reinforcements.
- **`PostBattleCampaignBehavior`** — post-battle hook that ties into raising the dead after
  a fight (corpses available to raise scale with the battle just fought).
- **`GraveyardNightWatchPartyComponent`** (`: PartyComponent`) — a party that guards a
  graveyard at night (tied to `BattleMechanics` graveyard fight content in `Missions/`).
- **`TreeSpiritHelpers`** (static) — Wood Elf tree-spirit summoning/raising helpers
  (parallel mechanic for Asrai, thematically similar to undead raising).
