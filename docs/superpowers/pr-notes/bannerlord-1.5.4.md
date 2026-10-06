# Bannerlord 1.5.4 — PR notes

Game is now v1.5.4 (changeset 123627, was 117484 at v1.4.7). v1.5.0 folded the War Sails
naval expansion into the base game, which is why `BattleEnvironment` turns up throughout the
perk API and why mission deployment hooks moved.

This branch is `origin/1.5-changes` merged onto current `next_update`, plus the 1.5.3 → 1.5.4
delta. **It reaches the main menu.** Nothing beyond load has been played.

## Why the merge, not a fresh migration

`origin/1.5-changes` (SlyDevil, 1.5.3) had already solved essentially all of this. A parallel
migration was built against 1.5.4 before that branch was discovered; it was discarded as
redundant. Where the two differed, `1.5-changes` was better — notably it threads the real
`agent/party/victim.CurrentBattleEnvironment` into `PerkHelper` calls and into
`SummonedCombatant`, rather than hardcoding one environment value.

Its merge base was ~1 month stale, but the merge was near-clean: only `CraftingPatches.cs`
(moved by the crafting refactor) and the two tracked DLLs conflicted.

TaleWorlds published a modding-changes thread for this release, which is the best reference for
what moved and why:
<https://forums.taleworlds.com/index.php?threads/e1-6-0-v1-5-0-modding-changes.443896/>

## The 1.5.3 → 1.5.4 delta — three signatures

| Moved | Consequence |
|---|---|
| `GetMobilePartyVisibilityAndInspectedState` lost its `out bool isDistanceDependent` | three-out override bound to nothing; compile error |
| `CreateKingdom` gained `MBReadOnlyList<PolicyObject> initialPolicies` at position 5, pushing `formalName` to 7 | a positional `formalName` silently binds to `initialPolicies`. **Compiled clean on 1.5.3.** Now passed by name |
| `DefaultSettlementGarrisonModel.GetMaximumDailyAutoRecruitmentCount` returns `ExplainedNumber` | already handled on `1.5-changes` via `TORSettlementGarrisonModel` rather than a patch |

## Gaps — what is NOT verified

**Harmony is clean.** Verified by reflection over the 1.5.4 assemblies: 272 patch targets
resolve, 68 `__result` declarations match target return types, all 8 constructor patches match
their targets' parameter lists, 106 parameter bindings agree. These fail loudly at
`OnSubModuleLoad`, so the audit is meaningful. See
[`diagnosing-submodule-load-failures.md`](../../diagnosing-submodule-load-failures.md).

**Five transpilers are unverified and cannot be verified statically** —
`CraftingPatches`, `BaseGameDebugPatches`, `EncounterPatches`, `RaceFixPatches` (×2). They match
IL patterns inside vanilla method bodies. A changed body means no match, no exception and no log
line: the feature just stops working. Check smithing town orders, race/body-property handling and
post-battle prisoner capture by hand.

**Semantic drift is unverified.** Members that still exist with identical signatures but changed
behaviour. `GetEffectiveRelationChange` (int) replacing `GetRelationIncreaseFactor` (float factor)
is one already in this diff.

**One feature is switched off, deliberately.** `ItemTraitStatType.HealthRegen` no longer appears
anywhere in the source: the hook it rode on (`PartyHealingModel.GetHeroesEffectedHealingAmount`,
per-hero) was deleted in 1.5, and the surviving `GetDailyHealingHpForHeroes` is per-party, so a
per-hero equipment effect has nowhere to go. Enchanted gear granting health regeneration therefore
does nothing right now. Career `PassiveEffectType.HealthRegeneration` is unaffected.

**ModuleData XML is unverified but not implicated.** ~995 schema-validation errors appear in
`rgl_log`. Validation is advisory — the handler logs and returns, the document loads regardless —
and vanilla's own `spclans.xml` trips the same validator with `label_color`. Roughly 700 are TOR's
own custom attributes that its C# reads. The ~200 `initial_home_settlement` misses span all 56 TOR
kingdoms and predate this work. Pre-existing, not 1.5.4 damage, but not proven harmless either.

**Save compatibility across the engine bump is untested.**

## Not addressed

`SubModule.xml` depends on `TOR_Environment_new`, because the module is installed under that
folder name while declaring `Id="TOR_Environment"`, and Bannerlord resolves by folder. A `FIXME`
marks it. `TORPaths.TOREnvironmentModuleRootPath` still looks up `"TOR_Environment"` and so
silently fails to find `tor_singleplayerbattlescenes.xml`. Renaming the folder fixes both.
`TOR_Armory/SubModule.xml` declares its game-module dependencies as `v1.3.15`, which is TOR's own
version pasted into the wrong field; it lives in another repo.
