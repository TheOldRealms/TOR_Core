# CampaignMechanics/Diplomacy

Alliance/war/trade-agreement mechanics beyond vanilla kingdom diplomacy.

- **`HonorAllianceDecision`** (`: KingdomDecision`, + nested `HonorAllianceOutcome`,
  + `HonorAllianceDecisionTypeDefiner : SaveableTypeDefiner`) — a Total-War-style forced
  choice: when an ally is attacked, the kingdom must vote within 24h to either join the war
  or break the alliance — no "do nothing and stay allied" option.
- **`TORAllianceWarBehavior`** (`: CampaignBehaviorBase`, +
  `TORAllianceWarBehaviorTypeDefiner : SaveableTypeDefiner`) — tracks alliances and raises
  `HonorAllianceDecision`s when one is tested; persists alliance state via the type definer.
- **`TORKingdomDecisionsCampaignBehavior`** — registers/manages TOR's custom kingdom
  decisions generally.
- **`TORTradeAgreementAIBehavior`** — AI logic for proposing/accepting trade agreements
  (see `Models/TORTradeAgreementModel`, `Models/DiplomacyHelpers`).

See `Models/TORAllianceModel`, `Models/TORDiplomacyModel`,
`Models/TORKingdomDecisionPermissionModel` for the scoring/permission rules these behaviors
call into.
