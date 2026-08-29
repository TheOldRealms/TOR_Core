# Quests

`QuestBase`-derived campaign quests, plus generic quest infrastructure. (Career-flavor
one-off scripted events use the lighter `CampaignMechanics/CustomEvents` framework instead
of a full quest class.)

- **`TORQuestHelper`** (static) — `StartCareerQuest(questPath)`: generic quest-launcher
  used by Career content — resolves a `TOR_Core.<questPath>` type name via reflection and
  starts it if not already ongoing (guards against duplicate quest instances by `StringId`).
- **`QuestPartyComponent`** — party component marking a `MobileParty` as belonging to an
  active quest (e.g. an escort or hunted target).
- **`EngineerQuest`** — a Dwarf-engineering-flavored quest (pairs with
  `CampaignMechanics/MasterEngineer`).
- **`SpecializeLoreQuest`** — quest for specializing into a Winds-of-Magic Lore
  (`AbilitySystem/Spells/LoreObject`) beyond the basic trainer interaction.
- **`HuntCultistsQuestCampaignBehavior`** / **`PlaguedVillageQuestCampaignBehavior`**
  (`: CampaignBehaviorBase`) — self-contained quest-line behaviors (issue, track, and
  resolve their quest without needing a separate `QuestBase` subclass registered elsewhere).

## Subfolder

- **`Careers/`** — one `QuestBase` subclass per Career-specific storyline quest, launched
  via `TORQuestHelper.StartCareerQuest` (see its CLAUDE.md).
