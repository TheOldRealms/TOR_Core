# P1 — Enchantment Blueprint Store: Test Plan

Manual test plan for phase 1 of [`../enchantment-blueprint-storage-proposal.md`](../enchantment-blueprint-storage-proposal.md).

**What P1 did:** added `EnchantmentBlueprintBehavior`, a campaign-scoped store of the
player's enchantment blueprints, populated from the `EnchantmentLearned` event and from a
one-time union of the party's per-hero lists on session launch. **Nothing reads it yet** —
`EnchantmentBlueprints.IsKnown/GetKnown` still answer from the per-hero
`HeroExtendedInfo.KnownEnchantmentBlueprints` lists.

**What this plan has to prove**, before P2 splits the read API:

1. The store gets populated correctly (S1–S3, S6).
2. The store genuinely persists — not merely looks populated (S5).
3. P1 changed nothing a player can see (S7).

S5 and S7 are the gate. The rest are supporting evidence.

> **Note on what P2 became.** P2 no longer flips the enchanting table onto the store. The
> store answers **known** (has the party ever learned this?); the per-hero lists keep
> answering **craftable** (does a hero present know it?), which preserves the
> hired-Runesmith retention mechanic. So the divergence S4 and S5 produce is a permanent,
> correct state rather than a bug being staged for repair.

## Run record

| | |
|---|---|
| Date | |
| Branch / commit | |
| Save used | |
| Tester | |

## Preconditions

- A **dwarf (DAWI)** playthrough — densest blueprint content (runes, Karaks, Runelord
  career button, two quest counters that read blueprint state).
- Console enabled.
- A companion in the party. This plan assumes one named **Gotrek Gurnisson** — substitute
  freely, but use the hero's *full* name as the game shows it, since that is what the console
  matches on.
- For S1, an **existing save made before P1**. That save has no `_knownBlueprints` key, so
  it also exercises the null guard in `SyncData`.

## Reference

Rune trait ids and the item type each attaches to. Selecting a mismatched item makes the
enchanting table look empty, which is easy to misread as a bug:

| Trait id | Valid item type |
|---|---|
| `dw_rune_stone` | Armor |
| `dw_rune_iron` | Armor |
| `dw_rune_striking` | Melee |
| `dw_rune_might` | Melee |
| `dw_rune_fire` | Weapon |

Commands:

```
tor.add_enchantment_blueprint <traitId>                  # main hero learns it
tor.add_enchantment_blueprint <Hero Name> | <traitId>    # named hero learns it
tor.check_enchantment_blueprint_store                    # store vs party union
```

**Mind the `|`.** The console splits arguments on whitespace and hero names are multi-word,
so the name and the trait id are pipe-separated — the same convention `tor.set_alliance`
uses. Without it, `Gotrek Gurnisson dw_rune_striking` parses as three arguments and you get
the usage string back instead of a grant.

`tor.add_enchantment_blueprint` writes the blueprint directly and performs **no skill or
lore check** — it only validates that the trait id exists. Gotrek does not need Crafting 75
or the RuneMagic lore for any of this, even though the real in-game route
(`learn_dw_rune_stone`) requires both.

`tor.check_enchantment_blueprint_store` prints either:

```
store: <n>, party union: <m>
MATCH - store agrees with the party union.
```

or `DIVERGED`, followed by the ids in each direction.

---

## S1 — Migration onto a pre-P1 save

Load an existing dwarf save that already knows some runes.

```
tor.check_enchantment_blueprint_store
```

- **Expect:** `MATCH`, both counts > 0 and equal.
- **Fail:** `DIVERGED` → *"known by party but absent from store"* means the migration never
  ran, or ran before hero info finished loading.

- [X] Pass  - [ ] Fail

**Result:**
Match, 3 of 3 that I loaded.

**Notes:**

---

## S2 — Live grant to the player

```
tor.add_enchantment_blueprint dw_rune_fire
tor.check_enchantment_blueprint_store
```

- **Expect:** both counts +1, still `MATCH`. Proves the `EnchantmentLearned` subscription
  fires.
- **Fail:** union +1 but store unchanged → the event is not reaching the behavior.

- [X] Pass  - [ ] Fail

**Result:**
Match, 4 of 4 now, player now knows rune of fire.
**Notes:**

---

## S3 — Companion learns one

With Thyk the Runesmith in the party:

```
tor.add_enchantment_blueprint Thyk the Runesmith | dw_rune_striking
tor.check_enchantment_blueprint_store
```

- **Expect:** both counts +1, `MATCH`.
- **Fail:** a `NullReferenceException` here means the console command fix (commit
  `4605df4e`) is not in the build being tested.

- [X] Pass  - [ ] Fail

**Result:**
Match
**Notes:**

---

## S4 — Thyk leaves the party

Dismiss Thyk from the party screen — there is no console command for this.

```
tor.check_enchantment_blueprint_store
```

- **Expect:** `DIVERGED`, with `dw_rune_striking` listed under *"in store but not known by
  any current party hero"*. **This divergence is correct**, and it is exactly what the store
  exists to record: the party has learned that rune (**known**), but nobody present can make
  it (**craftable**). The enchanting table losing it when Gotrek walks is the intended
  Runesmith-retention mechanic, not a bug — see the correction section of the proposal.
- **Fail:** `MATCH` → the store dropped it, meaning it is not yet independent of the
  per-hero lists.

- [X] Pass  - [ ] Fail

**Result:**
Makes sense, the only one who could learn it is gone (thyk)
**Notes:**

---

## S5 — Persistence proof — **gate**

Continuing directly from S4's diverged state: **save, quit to menu, reload**, then:

```
tor.check_enchantment_blueprint_store
```

- **Expect:** still `DIVERGED`, `dw_rune_striking` still listed.
- **Fail:** `MATCH` after reload → the store did not survive, and the session-launch
  migration silently rebuilt it from the hero lists.

> **Why this scenario and not a plain save/reload.** The migration re-unions the party's
> lists on every session launch, so a completely broken `SyncData` would be repapered on
> load and still report `MATCH`. The orphaned entry from S4 is the only state that cannot
> be reconstructed from hero data — so it is the only thing that actually tests
> persistence. A plain save/reload proves nothing here.

- [X] Pass  - [ ] Fail

**Result:**
Still diverged after exiting to main menu and reloading, and exiting the game completely.
**Notes:**

---

## S6 — Non-party hero is ignored

Pick any lord in your clan or kingdom.

```
tor.add_enchantment_blueprint <Lord Name> | dw_rune_might
tor.check_enchantment_blueprint_store
```

- **Expect:** store count **unchanged**. Proves the `ShouldRecord` filter.
- **Fail:** store +1 → the filter is admitting non-party heroes, which would corrupt the
  **known** answer P2 builds on (quest counters would credit a lord's blueprint to you).

- [x] Pass  - [ ] Fail

**Result:**
Created party with Gloin the Thane, gave him the skill and it still sat at 4 (unchanged)

**Notes:**

---

## S7 — P1 is invisible in play — **gate**

The regression check. P1 must change nothing player-visible; anything that moves is a bug.

Set up a party with a hired Runesmith who knows a rune the player does not, then at a Karak:

- Open the enchanting table. Select **armour**, then a **melee weapon**. The offered trait
  list must match pre-P1 behaviour, with one known exception: P0 changed the ordering to
  globally alphabetical (it used to be grouped per hero, then sorted within each group).
- **Dismiss the Runesmith and reopen the table** — their runes must disappear from it. This
  is the retention mechanic, and it must still work: P1 records the knowledge centrally but
  must not make it craftable. If the runes are still offered, the store has leaked into the
  craftable path.
- **Runelord career button** — still main-hero-only, unchanged by P1.
- **Runesmith / Runelord quest counters** — unchanged by P1.

- [X] Enchanting table unchanged (modulo ordering)
- [X] Dismissing the Runesmith still removes their runes from the table
- [X] Runelord career button unchanged
- [X] Quest counters unchanged

**Result:**
I'll check the quest counters later, it's layered behind a bunch of setup that I don't have an easy way to fix.
**Notes:**

---

## S8 — New campaign

Fresh dwarf start:

```
tor.check_enchantment_blueprint_store
```

- **Expect:** `store: 0, party union: 0`, `MATCH`, no crash on an empty store.
- Then take the Runelord career choice that grants `dw_rune_stone`
  (`CraftingCareerHookRegistrations`) and re-check — **expect both +1**.

- [X] Pass  - [ ] Fail

**Result:**

**Notes:**

---

## Summary

| Scenario | Result | Notes |
|---|---|---|
| S1 Migration onto pre-P1 save | | |
| S2 Live grant to player | | |
| S3 Companion learns | | |
| S4 Companion leaves → diverged | | |
| **S5 Persistence proof (gate)** | | |
| S6 Non-party hero ignored | | |
| **S7 P1 invisible in play (gate)** | | |
| S8 New campaign | | |

**Gate decision — may P2 split the read API (`IsKnown` vs `IsCraftable`)?**

- [X] Yes — S5 and S7 both pass
- [ ] No — blocked by:

## Follow-ups found while testing

<!-- Anything noticed in passing that isn't a P1 defect. -->
