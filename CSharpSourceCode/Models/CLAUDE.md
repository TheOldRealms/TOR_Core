# Models

TaleWorlds `GameModel` overrides — the game's standard "swap out the formula" extension
point. Almost every file here is `TORXyzModel : DefaultXyzModel` (or `SandboxXyzModel`),
overriding one or more virtual calculation methods while calling `base.Xyz(...)` for the
rest. All are registered in `SubModule.OnGameStart` via `gameStarterObject.AddModel(new
TORXyzModel())` — that method is the authoritative list of which vanilla model each
replaces. A few (`TORAbilityModel`, `TORFaithModel`, `TORCustomResourceModel`,
`TOREnchantmentCraftingModel`, `TOREnchantmentIngredientsModel`,
`TORHiringCompatibilityModel`, `TORCompanionTrainingModel`, `TORReinforcementRestrictionModel`)
extend `GameModel` directly — new mechanics vanilla has no equivalent formula for.

## By theme

- **Combat/agent stats**: `TORAgentStatCalculateModel` (`: SandboxAgentStatCalculateModel`),
  `TORAgentApplyDamageModel` (`: SandboxAgentApplyDamageModel` — routes through
  `BattleMechanics/DamageSystem/TORDamageHelper`), `TORStrikeMagnitudeModel`
  (`: SandboxStrikeMagnitudeModel`), `TORCombatSimulationModel`, `TORCombatXpModel`,
  `TORBattleMoraleModel` (`: SandboxBattleMoraleModel`), `TORBattleBannerBearersModel`
  (`: SandboxBattleBannerBearersModel`), `TORBattleRewardModel`, `TORDamageParticleModel`.
- **Abilities/magic**: `TORAbilityModel` (`: GameModel` — spell damage/radius/duration
  scaling by skill/perk, called from `BattleMechanics/TriggeredEffect`), `TORFaithModel`
  (`: GameModel` — prayer/religion mechanics).
- **Party**: `TORPartySizeModel`, `TORPartySpeedCalculatingModel`, `TORPartyWageModel`,
  `TORPartyHealingModel`, `TORPartyMoraleModel`, `TORPartyDesertionModel`,
  `TORPartyTrainingModel`, `TORPartyTroopUpgradeModel`, `TORMobilePartyAIModel`,
  `TORMobilePartyFoodConsumptionModel`, `TORReinforcementRestrictionModel` (`: GameModel`).
- **Character/skills**: `TORCharacterDevelopmentModel`, `TORCharacterStatsModel`,
  `TORCompanionTrainingModel` (`: GameModel`), `TORCompanionHiringPriceCalculationModel`,
  `TORPrisonerRecruitmentCalculationModel`, `TORHiringCompatibilityModel` (`: GameModel`).
- **Clan/kingdom/diplomacy**: `TORClanFinanceModel`, `TORClanTierModel`,
  `TORClanPoliticsModel`, `TORDiplomacyModel`, `TORAllianceModel`, `TORTradeAgreementModel`,
  `TORKingdomDecisionPermissionModel` (`: KingdomDecisionPermissionModel`),
  `TORMinorFactionsModel`, `TORMarriageModel`, `TORPersuasionModel`.
- **Settlement/economy**: `TORSettlementFoodModel`, `TORSettlementLoyaltyModel`,
  `TORSettlementMilitiaModel`, `TORSettlementProsperityModel` (sealed),
  `TORVillageProductionCalculatorModel`, `TORTradeItemPriceFactorModel` (sealed),
  `TORTroopSupplierModel`, `TORBuildingEffectModel`, `TORAlleyModel`,
  `TORBanditDensityModel`, `TORInventoryCapacityModel`.
- **Crafting/smithing**: `TORSmithingModel`, `TOREnchantmentCraftingModel` (`: GameModel`),
  `TOREnchantmentIngredientsModel` (`: GameModel`), `TOREquipmentSelectionModel`.
- **Custom resources**: `TORCustomResourceModel` (`: GameModel` — generalized cost scaling,
  see `CampaignMechanics/CustomResources/CustomResource.GetCustomResourceGeneralizedFactor`).
- **Encounters/map/raids**: `TOREncounterModel`, `TOREncounterGameMenuModel`,
  `TORMapVisibilityModel`, `TORRaidModel`, `TORVoiceOverModel`.
- **Siege/tournament**: `TORSiegeEngineCalculationModel`, `TORTournamentModel`.
- **Misc**: `TORVolunteerModel`, `DiplomacyHelpers` (static — shared math for
  `TORDiplomacyModel`/`TORAllianceModel`/`TORTradeAgreementModel`).

## Subfolder

- **`CustomBattleModels/`** — the parallel model set used for the game's Custom Battle mode
  (`Game.Current.GameType is CustomGame`) instead of a real campaign — see its CLAUDE.md.
