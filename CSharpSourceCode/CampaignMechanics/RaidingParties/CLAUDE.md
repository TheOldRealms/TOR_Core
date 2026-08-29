# CampaignMechanics/RaidingParties

Raider-type war parties (Greenskin/Chaos/Beastmen war-bands that raid rather than hold territory).

- **`IRaidingParty`** — marker/contract interface for a raiding party component.
- **`RaidingPartyComponent`** (`: WarPartyComponent, IRaidingParty`) — the party component
  itself: identifies a `MobileParty` as a raider band with raiding-specific AI/lifecycle.
- **`RaidingPartyCampaignBehavior`** (`: CampaignBehaviorBase`) — spawns/manages raiding
  parties campaign-wide (from `../TORCustomSettlement` raider-spawner settlements, etc.).
