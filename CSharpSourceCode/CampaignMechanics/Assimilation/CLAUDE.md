# CampaignMechanics/Assimilation

Handles what happens to a settlement's population/culture when it changes hands between
very different factions (e.g. Greenskins sacking an Empire town) — Warhammer factions don't
peacefully "convert" the way vanilla Calradia cultures do.

- **`AssimilationCampaignBehavior`** (`: CampaignBehaviorBase`) — the assimilation
  mechanic itself (loyalty/culture drift, or replacement, after conquest).
- **`RaceFixCampaignBehavior`** — corrects/normalizes character race data that can drift
  or mismatch after culture/faction changes (also see `HarmonyPatches/RaceFixPatches.cs`).
