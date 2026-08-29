# CampaignMechanics/CustomDialogs/ConversationTags

`ConversationTag` implementations (`IsApplicableTo(CharacterObject character)`) used as
dialog line conditions/filters, so a conversation line can be written once and
automatically apply only to characters of the right race/culture/background.

- **`CommonTags.cs`** — `PlayerIsRenownedTag` (player clan tier > 2).
- **`HumanTags.cs`** — `IsBretonnianTag`/`IsEmpireTag` and their `PlayerIs...` variants,
  `IsWarriorPriestTag`/`PlayerIsWarriorPriestTag`, `PlayerIsGrailKnightTag`.
- **`DwarfTags.cs`** — `IsDwarfTag`, `PlayerIsDwarfTag`.
- **`AsraiTag.cs`** — `AsraiTag`, `ElfMaleTag`, `PlayerIsElfTag`, `PlayerIsAsraiTag`.
- **`EonirTag.cs`** — `EonirTag`, `PlayerIsEonirTag`.
- **`GreenskinTags.cs`** — `IsOrcTag`, `IsGoblinTag`, `PlayerIsOrcTag`.
- **`VampireTags.cs`** — `VampireMaleTag`, `VampireFemaleTag`.
- **`BloodDragonTag.cs`** / **`MousillonTag.cs`** — specific vampire bloodline/culture tags.

Referenced from dialog registration code in `../CustomDialogCampaignBehavior` and other
`CampaignBehaviorBase`s that add conversation lines via `CampaignGameStarter.AddDialogLine`.
