# CampaignMechanics/CustomResources/WaaaghMeter

Greenskin "Waaagh" meter — a mass-momentum resource that grows as Greenskin factions
win battles/raid, unlocking benefits at thresholds ("Big Waaagh").

- **`WaaaghBehavior`** (`: CampaignBehaviorBase`) — accrues/decays the meter based on
  campaign events; ties into `WaaaghHelper` for the actual math and
  `AbilitySystem/Spells/LoreObject` ("BigWaaagh" lore unlock).
- **`WaaaghHelper`** (static) — gain/threshold calculations.
- **`WaaaghMeterMapView`** (`: MapView`) — the on-map meter widget.
- **`WaaaghMeterVM`** — its view-model.
