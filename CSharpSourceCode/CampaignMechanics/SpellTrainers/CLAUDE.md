# CampaignMechanics/SpellTrainers

**`SpellTrainerInTownBehavior`** (`: CampaignBehaviorBase`) — a town service where an NPC
trainer teaches the player/companions spells from a `LoreObject` (see
`AbilitySystem/Spells/LoreObject`), gated by culture eligibility — including the special
Spellweaver dialog bypass that lets Asrai Spellsingers learn High/Dark Magic despite the
normal culture lock (see the note in `AbilitySystem/Spells/CLAUDE.md`).
