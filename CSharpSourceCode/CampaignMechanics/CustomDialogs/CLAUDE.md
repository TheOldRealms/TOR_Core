# CampaignMechanics/CustomDialogs

Extra conversation content: new dialog flows/behaviors plus (in `ConversationTags/`) the
`ConversationTag`s that gate culture/race-specific lines in both vanilla and TOR dialogs.

- **`CustomDialogCampaignBehavior`** (`: CampaignBehaviorBase`) — registers TOR's general
  additional dialog lines/flows.
- **`TORCompanionDialogBehavior`** — companion-specific conversation content.
- **`CareerSwitchCampaignBehavior`** — the dialog/flow for changing a hero's Career.
- **`DuelBehavior`** — dialog-triggered honor duels (see also
  `Missions/DuelFightMissionController`).
- **`BloodKissSceneNotificationItem`** (`: SceneNotificationData`) — a scripted scene
  notification for the Vampire "Blood Kiss" (turning a companion into a vampire) narrative
  moment.

## Subfolder

- **`ConversationTags/`** — `ConversationTag` (`IsApplicableTo(CharacterObject)`)
  implementations used as dialog conditions, one file per race/culture family:
  `CommonTags` (`PlayerIsRenownedTag`), `HumanTags` (Bretonnian/Empire/Warrior
  Priest/Grail Knight checks), `DwarfTags`, `AsraiTag` (+ elf/player-is-elf variants),
  `EonirTag`, `GreenskinTags` (orc/goblin), `VampireTags` (male/female), `BloodDragonTag`,
  `MousillonTag`.
