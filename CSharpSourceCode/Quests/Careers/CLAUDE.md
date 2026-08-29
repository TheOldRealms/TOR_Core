# Quests/Careers

One `QuestBase` subclass per Career-specific storyline quest (multi-stage, `JournalLog`
tasks saved via `[SaveableField]`), launched through `../TORQuestHelper.StartCareerQuest`
with a `questPath` like `"Quests.Careers.RunesmithQuest"`.

- **`RunesmithQuest`** / **`RunelordQuest`** — Dwarf Runelord career questline (crafting
  runes, tied to `CampaignMechanics/Menagery`/`Crafting`).
- **`OrcBossQuest1`** / **`OrcBossQuest2`** — Orc Boss career questline (sequential parts).
- **`OrcShamanQuest1`** / **`OrcShamanQuest2`** — Orc Shaman career questline.
