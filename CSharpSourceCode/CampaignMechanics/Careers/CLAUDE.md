# CampaignMechanics/Careers

Campaign-map glue for the Career system (the core data model — `CareerObject`,
`CareerChoiceObject`, perks — lives in `CharacterDevelopment/CareerSystem`; this folder is
the campaign-behavior/dialog layer on top of it).

- **`TORCareerPerkCampaignBehavior`** (`: CampaignBehaviorBase`) — applies/reacts to
  career perk effects at the campaign level (companion in
  `BattleMechanics/CareerPerkMissionBehavior` for missions).
- **`CareerDialogOptionsCampaignBehavior`** — registers career-specific dialog lines/options.
- **`CareerButtonDialogs`** (static) — dialog text/logic backing the in-mission "career
  button" special actions (see `CharacterDevelopment/CareerSystem/CareerButton`).
- **`GrailDamselEnvoyOfTheLadyPerkDialog`** — a specific perk-triggered dialog for the
  Grail Damsel career's "Envoy of the Lady" perk.
