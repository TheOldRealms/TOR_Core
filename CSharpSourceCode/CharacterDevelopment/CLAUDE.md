# CharacterDevelopment

Character progression data: skills, attributes, perks, traits, and the Career system
(Warhammer-flavored "prestige classes" like Grail Knight, Witch Hunter, Runelord, Slayer,
Orc Boss). Most root-level files are static-ish registries initialized once at startup
(`SubModule.BeginGameStart`/game object registration) mirroring vanilla's
`DefaultSkills`/`DefaultTraits` pattern.

## Root-level registries

- **`TORSkills`** — TOR's additional skill definitions.
- **`TORSkillEffects`** — skill-driven passive effects.
- **`TORAttributes`** — additional character attributes.
- **`TORCharacterTraits`** — additional personality/character traits.
- **`TORPerks`** (+ nested static `Spellcraft`, `GunPowder`, `Faith` groups) — TOR's extra
  perks, grouped by theme (e.g. `TORPerks.Spellcraft.ArcaneLink`, referenced by
  `BattleMechanics/TriggeredEffect/TriggeredEffect`).
- **`TORPerkHandlerCampaignBehavior`** (`: CampaignBehaviorBase`) — applies perk effects
  campaign-side.
- **`TORCareers`** — registry of every `CareerObject` (Blood Knight, Grail Damsel, Grail
  Knight, Mercenary, Minor Vampire, Necromancer, Warrior Priest [+ Ulric variant], Witch
  Hunter, Black Grail Knight, Necrarch, Imperial Magister, Waywatcher, Spellsinger, Warden,
  Grey Lord, Knight of the Old World, Ironbreaker, Slayer, Runelord, Orc Boss, Orc Shaman).
- **`TORCareerChoices`** (+ `CareerHasNoChoicesException`) — registry of all
  `CareerChoiceObject`s (the perk-tree nodes within a career).
- **`TORCareerChoiceGroups`** — registry of `CareerChoiceGroupObject`s (tiers/branches
  grouping choices within a career tree).
- **`CareerAbilityChargeSupplier`** (static) — supplies charge-gain hooks for careers whose
  signature ability builds "charge" from combat actions rather than a plain cooldown
  (`ChargeType.Custom` on `CareerObject`).

## Subfolder

- **`CareerSystem/`** — the Career data model itself (`CareerObject`, `CareerChoiceObject`,
  `CareerChoiceGroupObject`), its screen/VMs, and the perk-tree "special button" +
  per-career choice-effect implementations (see its CLAUDE.md).
