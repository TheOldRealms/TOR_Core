# CampaignMechanics/CharacterCreation

TOR's replacement character-creation flow: race/culture-specific narrative stages plus a
custom final "Specialization" stage (pick a starting profession/background per race).

- **`TORCharacterCreationContentHandler`** (`: ICharacterCreationContentHandler`, singleton
  `Instance`) — the central driver: builds `CharacterCreationOption`/`SpecializationOption`
  lists, tracks stage navigation (`LastStageIndex`, per-stage selections e.g. Wood Elf gods
  / Dwarf grudges / profession), applies the final race/equipment/religion/specialization
  choice on finish. Has an in-code TODO noting it needs rework for the 1.3.1 native
  `ICharacterCreationContentHandler` pattern (replaced the older `CharacterCreationContentBase`).
- **`CharacterCreationOption`** / **`SpecializationOption`** — XML-loadable option data for
  the generic stages and the custom specialization stage respectively.
- **`TORSpecializationStage`** (`: CharacterCreationStageBase`) — the custom final stage.
  - **`TORSpecializationStageView`** (`: CharacterCreationStageViewBase`) — its Gauntlet view.
  - **`TORSpecializationStageVM`** (+ `SpecializationOptionVM`,
    `TORSpecializationGainedPropertiesVM`, `SpecializationAttributeGroupVM`,
    `SpecializationAttributeVM`, `SpecializationSkillItemVM`, `FocusIconVM`, all in the same
    file) — the stage's view-model tree, showing attribute/skill/focus gains per option.
- **`FaceGenHelper`** (static) — face-generation helpers per race (pairs with
  `HarmonyPatches/FaceGenPatches.cs`).
- **`TORCharacterCreationException`** (+ `TORCCXmlLoadException`,
  `TORCCSpecializationStageLoadException`, `TORCCReflectionException`,
  `TORCCEquipmentUpdateException`, `TORCCInvalidOptionTypeException`) — a small exception
  hierarchy for reporting specific character-creation data/loading failures.
