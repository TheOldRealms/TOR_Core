# CampaignMechanics

The largest folder: every `CampaignBehaviorBase` (registered in
`SubModule.InitializeGameStarter`) plus its supporting helpers/VMs/screens. This is where
Warhammer's setting gets expressed on the campaign map — factions, careers, religion,
custom resources (Waaagh, Teef, Oath Gold, Chivalry, Forest Harmony, Favor), diplomacy,
crafting/enchanting, and unique named characters/settlements.

Most files follow one of these shapes:
- **`XyzCampaignBehavior` / `XyzTownBehavior`** (`: CampaignBehaviorBase`) — registers game
  events (`AddBehaviors`) and reacts to them; one per mechanic, added individually in
  `SubModule.cs`.
- **`XyzHelper`** (static) — pure logic/queries a behavior or UI delegates to.
- **`XyzVM` / `XyzScreen`** — Gauntlet view-model/screen pairs for a dedicated UI (crafting,
  spellbook-adjacent, religion encyclopedia, etc.).

## Root-level files

- **`CampaignEventHelpers`** (static) — shared helpers for wiring up `CampaignEvents`.
- **`TorRecruitmentHelpers`/`TORAIRecruitmentCampaignBehavior`** — custom troop recruitment
  rules (including AI lord recruitment).
- **`TORPartyUpgraderCampaignBehavior`** — custom troop-upgrade-path logic per party.
- **`TORCaptivityCampaignBehavior`** — custom prisoner/captivity handling.
- **`TORSpecialSettlementBehavior`** — hooks for TOR's special settlement types.
- **`TORFactionDiscontinuationCampaignBehavior`** — handles a faction ceasing to exist
  (Warhammer factions can be wiped out/disabled, unlike vanilla's always-present kingdoms).
- **`TORStartupBehavior`** — first-run/new-campaign setup.
- **`GreenskinAICampaignBehavior`** — Waaagh-flavored AI behavior for Greenskin lords.
- **`SkillTrainerBehavior`** (+ `HeroTrainingData`) — companion/hero skill training over time.
- **`TorMapBarSpriteWidget`** (`: IconBrushWidget`) — custom map-bar icon widget.
- **`TORCampaignMusicHandler`** (`: IMusicHandler`) — custom campaign music selection.

## Subfolders (one mechanic each — see each folder's own CLAUDE.md)

- **`Assimilation/`** — race/culture "fixing" and assimilation of captured settlements.
- **`BountyMaster/`** — bounty-hunting contracts.
- **`Careers/`** — campaign-side glue for the Career system (dialogs, perks) — the core
  Career data model itself lives in `CharacterDevelopment/CareerSystem`.
- **`Chaos/`** — the Chaos faction/invasion mechanic.
- **`CharacterCreation/`** — TOR's custom character-creation flow (race/culture specializations).
- **`Companions/`** — companion recruitment/AI companion behavior.
- **`Crafting/`** — weapon/armor enchanting system (screen, VMs, ingredients, loot).
- **`CustomDialogs/`** — extra conversation lines/behaviors + culture-specific
  `ConversationTags/`.
- **`CustomEvents/`** — generic scripted campaign events framework.
- **`CustomResourceBehavior/`** — town/campaign behaviors that grant/spend custom resources.
- **`CustomResources/`** — the custom resource system itself (Waaagh, Teef, Oath Gold,
  Chivalry, Forest Harmony, Favor) + `WaaaghMeter/` UI.
- **`Diplomacy/`** — alliances, trade agreements, kingdom decisions.
- **`MapNotifications/`** — custom map notification popups.
- **`MasterEngineer/`** — Dwarf Master Engineer town service.
- **`PostBattleLoot/`** — post-battle looted-troop bookkeeping.
- **`RaidingParties/`** — raider war-party AI/component.
- **`RaiseDead/`** — Vampire Counts/Necromancy raise-dead mechanics, graveyards, tree spirits.
- **`RegimentsOfRenown/`** — named elite unit regiments recruitable at settlements.
- **`Religion/`** — the religion/deity system and its encyclopedia page.
- **`ServeAsAHireling/`** — player-as-hireling activity.
- **`SpellTrainers/`** — trainers who teach spells/lores in town.
- **`TORCustomSettlement/`** — TOR's bespoke settlement types (Troll Cave, Chaos Portal,
  Shrine, Herdstone, Cursed Site, Oak of Ages, World Roots, Slaver Camp) + their menus/components.
- **`UniqueSpawns/`** — named unique heroes/monsters spawned into the world (e.g. Orion).
- **`Villages/`** — custom village types.
