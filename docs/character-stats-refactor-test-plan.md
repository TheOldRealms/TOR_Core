# Test plan — TORCharacterStatsModel health refactor

Manual, in-game. Automated coverage is not available for game models in this repo.

## Decisions

| Decision | Choice | Why |
|---|---|---|
| Baseline | Capture values with the committed `bin/Win64_Shipping_Client/TOR_Core.dll` **before** overwriting it | Every scenario except 3b and 15 asserts "unchanged", which needs a before |
| Save to use | One existing campaign, not a fresh start | Exercises the save-compat path and gives real companions/lords to inspect |
| Second save | One Asrai (wood elf) campaign | Scenarios 7, 8, 13, 14 are unreachable otherwise |
| Numbers vs tooltips | Both recorded | Numbers must be identical; tooltip rows are the intended change |
| Not verified | Exact float equality on `AddFactor` chains | Displayed HP is rounded; match to the integer shown |
| Not verified | Treeman +1100 | Confirmed as intended at +1000 from troop data; no change made |

## Run record

| # | Scenario | Baseline | After | Pass/fail | Notes |
|---|---|---|---|---|---|
| 1 | Troop tier steps | | | | |
| 2 | Undead troop penalty | | | | |
| 3a | Champion, scaling present | | | | |
| 3b | Champion, scaling missing | | | | |
| 4 | Race troops | | | | |
| 5 | Monster races | | | | |
| 6 | Player vampire exclusion | | | | |
| 7 | Asrai harmony debuff | | | | |
| 8 | Tree symbols | | | | |
| 9 | Companion career passives | | | | |
| 10 | Holy Crusader scaling | | | | |
| 11 | For Hearth and Home scaling | | | | |
| 12 | Lord attribute bonuses | | | | |
| 13 | Clan lord in main party | | | | |
| 14 | Oak of Ages | | | | |
| 15 | Campaign start HP | | | | |
| 16 | Tooltip strings | | | | |
| 17 | Save compatibility | | | | |

## Scenarios

### Troop path

**1. Troop tier steps.** Field one tier 0, one tier 4 and one tier 6+ troop. Max HP must match
baseline exactly. The tier contribution now shows as a "Troop tier" row where the breakdown is
displayed; the total must not move.

**2. Undead troop penalty.** A non-hero undead troop without `NecromancerChampion` keeps its −25.
An undead *hero* must be unaffected by this line — it is troop-only and always was.

**3a. Necromancer champion, scaling present.** Play a Necromancer, raise a champion in a mission
with the career ability available. Champion HP must equal baseline.

**3b. Necromancer champion, scaling missing.** *This is the bug fix.* Reach the fallback — easiest
is a champion present after the player agent is gone, or a champion spawned without the
Necromancer career (cheat-spawn). Expected: champion has **1 HP** and dies to anything. Before this
change it had roughly double its normal health. Confirm the log line "Champion being set to 1 hp"
appears in `Logs/` and that the outcome now matches it.

**4. Race troops.** One dwarf, one goblin, one orc troop: +20 / −20 / +40 against baseline. The
same three values must also still apply to *heroes* of those races — that block was duplicated and
is now shared, so check one dwarf hero too.

**5. Monster races.** Minotaur +350, troll +450, treeman +1000, dryad +100. Treeman must be
**+1000, not +1100** — it carries the `TreeSpirit` attribute as well as the monster race, and the
`else if` that keeps the two from stacking is intentional.

### Player

**6. Player vampire exclusion.** A vampire player character must **not** receive the +100 "Vampire
body" bonus; a vampire companion or lord must. The exclusion still uses `IsHumanPlayerCharacter`.

**7. Asrai harmony debuff.** Asrai save. At Unbound, health factor −35%; at Bound, −15%; at
Harmony, none. Check on the player **and** on a companion in the main party — both read the
player's harmony level. With the Wanderer symbol active, no debuff at all.

**8. Tree symbols.** Wardancer symbol: +25% to the player and to main-party members. Durthu
symbol: +10% to the **player only**, and it now carries a "Durthu Symbol" tooltip row where it
previously showed an unlabelled factor.

### Companion

**9. Companion career passives.** With the relevant player career choices unlocked, confirm each
still lands on a companion: `GuiltyByAssociationPassive3`, `CommanderPassive4`,
`EnvoyOfTheLadyPassive2` (Bretonnian knight only, applied as a factor), `BestofDaBestPassive4`
(Big Boss only), `GorkAnMorkAreWatchinPassive4` (Shaman Boss only). None may apply to a
non-companion hero.

**10. Holy Crusader scaling.** With `HolyCrusaderPassive2`, a Bretonnian knight companion in the
main party gains `knight count × passive value`. Add a second knight and confirm the bonus grows by
exactly one step. The counting was rewritten from `Where().Count()` to `CountQ` — same result
expected.

**11. For Hearth and Home scaling.** With `ForHearthAndHomePassive2`, the bonus equals
`traited item count × passive value`. Swap one enchanted item in and out and confirm one step of
change. Rewritten from `WhereQ().Sum()` to `CountQ() ×`.

### Lord

**12. Lord attribute bonuses.** An AI lord with `Everchosen` gets +2000, Orion +3000, `Tough` +100
— unchanged, but each now shows a labelled tooltip row.

**13. Clan lord in the main party.** *Highest-risk regression.* Put a lord of your own clan (not a
wanderer companion — they are not `IsPlayerCompanion`) into the main party in the Asrai save. They
must still receive the forest harmony debuff and the Oak of Ages factor, exactly as before. They
must **not** receive the companion-only career passives from scenario 9.

### Cross-cutting

**14. Oak of Ages.** With two or more `WEHealthUpgrade` upgrades unlocked, the health factor is
`0.1 × count` as a **single** tooltip row. Previously it was one row per upgrade with the same
total. Verify the total is unchanged and only the row count differs.

**15. Campaign start HP.** Start a new campaign and confirm heroes begin at full computed max HP.

**16. Tooltip strings.** Every new row renders readable text, not a raw id or a blank: Troop tier,
Undead body, Necromantic empowerment, Everchosen, Orion, Tough, Dwarf bonus, Goblin frailty, Orc
bonus, Gift of Nurgle, Oak of Ages, Perks, Durthu Symbol.

**17. Save compatibility.** Load a campaign saved with the previous build. No saved fields were
added or renamed, so this should be clean — confirm no load error and no HP shift on existing
heroes.
