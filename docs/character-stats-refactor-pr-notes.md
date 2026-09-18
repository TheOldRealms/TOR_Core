# TORCharacterStatsModel — health calculation refactor

`CSharpSourceCode/Models/TORCharacterStatsModel.cs` (+238/−168), `ModuleData/tor_strings.xml` (+12).

## Two real bugs

**Necromancer champion doubled instead of dying.** The fallback taken when the player's career
ability scaling can't be read logged "Champion being set to 1 hp and being left to die", then ran
`value = number.ResultNumber - 1; number.Add(value)` — which yields `2R−1`, roughly double health.
It now replaces the number outright (`new ExplainedNumber(1f)`) and returns. The log message was
right; the arithmetic was missing a sign.

**`(bool)(model?.CampaignStartTime.IsNow)` throws when the model is null** — the `?.` produces a
null `bool?` and the unboxing cast fails, so the null-safe operator made the crash it looked like
it was preventing. Now `model != null && model.CampaignStartTime.IsNow`.

## Why the split is shaped the way it is

Heroes are classified Player / Companion / Lord (`GetHeroKind`), with troops branching earlier.
Forest harmony deliberately does **not** hang off that switch: your own clan's lords can ride in
the main party and are not `IsPlayerCompanion`, so gating the Asrai block on hero kind would have
silently stripped their harmony debuff. It stays keyed on party membership.

Race bonuses were duplicated verbatim in the troop and hero paths (`IsDwarf(Hero)` and
`IsDwarf(CharacterObject)` are byte-identical), so they hoist into `AddRaceHealth` for all four
categories. Reordering is safe because `AddFactor` applies to the accumulated total regardless of
insertion point; the one order-sensitive read was the champion block, which stays last.

I did **not** convert the race checks to a static dictionary keyed on race id.
`FaceGen.GetRaceOrDefault` returns 0 for an unregistered race, so a static initializer running
before races load would throw `TypeInitializationException` on a duplicate key.

The `IsTreeman()` / `else if (IsTreeSpirit())` pairing looks like a bug and isn't. `tor_we_treeman`
carries `race="large_humanoid_monster"` *and* the `TreeSpirit` attribute
(`tor_troopdefinitions.xml:1467`, `tor_extendedunitproperties.xml:1274`), so the `else` is what
keeps treemen on +1000 rather than +1100. Commented in place.

## Behaviour changes a reviewer should weigh

Numbers are unchanged everywhere except the two bugs above. What did change:

- Forest harmony now reads `Hero.MainHero.GetForestHarmonyLevel()` instead of
  `hero.PartyBelongedTo.LeaderHero.GetForestHarmonyLevel()`. Same hero in every reachable case,
  matches how `ForestHarmonyHelper` reads it everywhere else, and removes a null `LeaderHero`
  dereference.
- Oak of Ages was `AddFactor(0.1f)` once per unlocked upgrade, producing N identical tooltip lines.
  Now one line at `0.1f * count` — same factor, one row. `GetCampaignBehavior` is also null-guarded.
- Every bare `Add(…)` gained a description, so the health tooltip now explains Everchosen, Orion,
  Tough, the troop tier step, the undead penalty and the three race bonuses instead of showing an
  unattributed delta. Twelve new `tor_stats_*` strings.

## Verification

There is no build path on the dev machine that resolves this project's PackageReferences
(non-SDK csproj, no Visual Studio MSBuild), so a clean build was not obtainable — `dotnet build`
reports ~2.3k `CS0246`s repo-wide. Roslyn still binds every file, and
`TORCharacterStatsModel.cs`'s only diagnostic is the same line-1 `using NLog;` error 90 other
files get. That is inference, not a green build. Manual scenarios in
`character-stats-refactor-test-plan.md` are the real gate.
