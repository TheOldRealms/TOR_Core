# HarmonyPatches

Every Harmony patch in the mod (one static class per patched vanilla system, using
`[HarmonyPatch]` attributes — prefix/postfix/transpiler methods named `...Patch`).
Applied via `SubModule.HarmonyInstance.PatchAllUncategorized()` at `OnSubModuleLoad`, except
classes tagged `[HarmonyPatchCategory("LatePatches")]` (e.g. `AgentPatches`), which are
applied later in `SubModule.OnGameInitializationFinished` — after the campaign's text
manager exists, needed for patches that touch localized strings (see the `SubModule`
remarks on `InitializeGameStarter`). Some monster/siege-specific patches live outside this
folder, colocated in `BattleMechanics/TORMonsterSiegeLogic.cs` instead.

Grouped by what they patch (not exhaustive — see individual files for exact target methods):

- **Agents/combat**: `AgentPatches` (custom voice routing via
  `BattleMechanics/Voice/AgentVoiceComponent`), `MissionCombatMechanicsHelpers`,
  `MissionPatches`, `RetreatPatches` (`TORAutoResolveRetreatPatches`),
  `AnimationSystemDataPatches`, `MountCreationPatches`.
- **Custom battle / arena / tournaments**: `CustomBattlePatches`, `ArenaPracticePatch`,
  `TournamentPatches`, `ArtilleryPatches`.
- **Items/inventory/crafting**: `ItemPatches`, `InventoryPatches`, `InventoryResetPatch`,
  `FastTradeInventoryPatch`, `CraftingPatches`, `VeterinarianPatch`.
- **Characters/creation/race**: `CharacterObjectPatches` (+ townsfolk/villager spawn-rate
  sub-patches), `CharacterCreationPatches` (positive-effect text, narrative stage start,
  gained-attribute population), `RaceFixPatches`, `FaceGenPatches`.
- **Campaign map/world**: `SettlementPatches`, `HideoutPatches`, `MobilePartyPatches`,
  `MapEventJoinRestrictionsPatch` (+ `StartBattleActionJoinRestrictionsPatch`),
  `EncounterPatches` (+ `EncounterGameMenuBehaviorPatches`), `CustomWorldMapPatch`
  (+ `QuestPartyMapTrackerProviderPatches`), `NotablesCampaignBehaviorPatches`,
  `ObjectManagerPatches`, `CaravanVisualPatch`.
- **Diplomacy/factions**: `FactionBannerPatches`.
- **Custom resources**: `CustomResourcePatches`.
- **Models**: `ModelPatches` (+ `ClampTroopsLeftToGarrisonCapacityPatch`).
- **UI/VM**: `ViewModelPatches` (+ `ScoreboardBaseVMPatches`,
  `RefinementVMOnSelectActionPatch`), `ViewModelRefreshPatch`, `PartyScreenPerformancePatches`
  (`PartyVM_TransferAllTroops_PerformancePatch`), `EncyclopediaPatches` (+ unit
  property/tooltip sub-patches), `TableauRenderPatches`, `GameTextPatches`,
  `LogEntryNotificationPatches`, `MainMenuOptionsPatches`, `MainMenuCrashPatch`
  (`MainMenuDeferredClearAllCleanup`, `ToMainMenuClearAllFix`), `LoadingScreenPatches`,
  `GameKeyOptionsCategoryPatch`.
- **Dialogue**: `ConversationPatches`.
- **Perks/skills**: `PerkResetRelatedPatch`.
- **Music**: `MBMusicManagerPatches`.
- **Debug**: `BaseGameDebugPatches`.

Consult `Utilities/TORConfig`/`TORConstants` and the target class's file for the exact
method signatures being patched before modifying one of these.
