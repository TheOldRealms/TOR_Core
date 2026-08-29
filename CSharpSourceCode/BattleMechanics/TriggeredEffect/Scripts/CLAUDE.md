# BattleMechanics/TriggeredEffect/Scripts

`ITriggeredScript` (`OnTrigger(position, triggerer, targets, duration)`) implementations,
reflection-instantiated by `TriggeredEffect.TriggerScript` via
`TriggeredEffectTemplate.ScriptNameToTrigger`. One-off bespoke behaviors too specific to
generalize into template fields.

- **`DynamicItemTraitScripts.cs`** — largest file; one class per weapon/item enchantment
  proc effect (Lore-of-Magic flavored): `ApplySwiftShiverTrait`, `ApplyHagbaneTrait`,
  `ApplyStarFireTrait`, `ApplyFlamingItemTraitScript`/`ApplyLesserFlamingItemTraitScript`,
  `ApplyLightItemTraitScript`/`ApplyLesserLightItemTraitScript`,
  `ApplyHeavensItemTraitScript`/`ApplyLesserHeavensItemTraitScript`/`ApplyGreaterHeavensItemTraitScript`/`ApplyMightyHeavensItemTraitScript`,
  `ApplyDeathDamageItemTraitScript`, `ApplyMetalItemTraitScript`,
  `ApplyQuickSilverWeaponItemTraitScript`, `ApplyHolyItemTraitScript`,
  `ApplyAzyrForesightScript`, `EnchantWeaponScript`, `ApplyTranquillityCadaiTrait`,
  `SpiritLeech` — each applies a specific `Items/ItemTrait` / status effect on hit.
- **`RuneMagicScripts.cs`** — Dwarf Rune Magic procs: `OathAndSteelScript`,
  `HearthAndHome`, `SpellbreakerRuneScript`, `WrathAndRuinScript`.
- **`AnvilOfDoomSpawnerScript`** — spawns the Anvil of Doom prop (Runelord/Runesmith
  ability prerequisite, see `AbilitySystem/Ability.IsDisabled`).
- **`PrefabSpawnerScript`** — generic "spawn this prefab" trigger.
- **`SummonScript`** — spawns `TroopIdToSummon` × `NumberToSummon` (Summoning abilities).
- **`KnockDownScript`** — knocks affected agents down.
- **`TraitHelper`** (static) — shared helpers for the trait scripts above.
- **`ITriggeredScript`** — the interface itself (also implemented by
  `AbilitySystem/Scripts/TeleportScript.TeleportTriggeredScript`).
