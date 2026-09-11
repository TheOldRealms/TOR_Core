# Single Blueprint Store (P2–P4): Test Plan

One consolidated pass over the collapse to a single campaign-wide blueprint store, described in
[`../enchantment-blueprint-storage-proposal.md`](../enchantment-blueprint-storage-proposal.md).
Supersedes the P2-only plan, which was never run.
[`p1-enchantment-blueprint-store.md`](./p1-enchantment-blueprint-store.md) is kept as the
historical record of the P1 pass.

**Requires a new campaign** - see Preconditions.

## What changed, in one table

| Before | Now |
|---|---|
| Blueprints stored per hero (`HeroExtendedInfo.KnownEnchantmentBlueprints`) | One campaign-wide store (`EnchantmentBlueprintBehavior`) |
| Table unioned the *current* party — a departing hero took their blueprints away | Blueprints are permanent; nobody takes anything with them |
| Skill checked at purchase (three places, three ways), never at the table | Skill + lore/attribute checked **at the table**, nowhere else |
| Under-skilled row greyed out in the shop, unbuyable | Row is buyable; the *table* greys it with the reason |
| Career cost reduction stacked once per knowing hero | Main hero only |
| Runelord button and quest counters read `Hero.MainHero`'s list | Both read the store (party-wide) |

**The big one:** the hired-Runesmith **retention** mechanic is gone by design. You still need a
qualifying hero to *learn* a rune and to *craft* with it, but dismissing them no longer erases
the knowledge. If dismissing a Runesmith still removes runes from the table, that is now a
**bug**, the exact inverse of what P1/S7 tested.

## Run record

| | |
|---|---|
| Date | 2026-09-11 |
| Branch / commit | `feature/CentralizeCraftingRecipes` at `5f647591` |
| Save used | New DAWI campaign (per Preconditions) |
| Tester | randychihuahua |

## Preconditions

- **A new dwarf (DAWI) campaign.** Saves from before this change are not supported — the
  per-hero `[SaveableField(10)]` is deleted, so old saves lose their blueprints and may fail to
  load at all. Do not spend time diagnosing an old save that misbehaves; start fresh.
- Console enabled.
- A companion you can dismiss and re-hire.

## Reference

| Trait id | Valid item type |
|---|---|
| `dw_rune_stone` | Armor |
| `dw_rune_iron` | Armor |
| `dw_rune_striking` | Melee |
| `dw_rune_might` | Melee |
| `dw_rune_fire` | Weapon |

Commands:

```
tor.add_enchantment_blueprint <traitId>                  # learn it
tor.check_enchantment_blueprints                         # NEW - what is known, and what blocks each
```

The `[Hero Name] | [TraitId]` form is **gone**. There is no hero to add a blueprint to any
more, so the command takes a trait id and nothing else, and rejects a second argument.

`tor.check_enchantment_blueprint` and `tor.check_enchantment_blueprint_store` are **gone**; both
compared the store against a party union that no longer exists. The replacement,
`tor.check_enchantment_blueprints`, takes no arguments and prints:

```
known: 5 (craftable now: 4, blocked: 1)
  craftable:
    dw_rune_fire
    ...
  known but not craftable right now:
    dw_rune_striking - Requires Smithing 150.
```

Since knowledge is now permanent and unconditional, "what is blocking this" is the only
interesting question left — which is exactly what the command answers.

The learned event still carries a hero, but no handler reads it any more, so the command
attributes every grant to the main hero.

---

## S1 — The store persists — **gate**

On a new campaign, learn two or three blueprints (console is fine):

```
tor.add_enchantment_blueprint dw_rune_fire
tor.add_enchantment_blueprint dw_rune_stone
tor.check_enchantment_blueprints
```

- **Expect:** `known: 2`, both listed.

Then **save, quit to the main menu, exit the game entirely, relaunch and reload**, and re-run
the check.

- **Expect:** still `known: 2`.
- **Fail — count is 0 after reload:** `SyncData` is not persisting `_knownBlueprints`, or
  `RebuildIndex` is not running on load. Nothing rebuilds the store from hero data any more, so
  unlike in P1 a broken `SyncData` cannot be papered over — it shows up immediately.

This replaces P1's orphaned-entry trick. That test existed only because a session-launch seed
could mask a broken save; with the seed gone, a plain save/reload is now a genuine proof.

- [X] Pass  - [ ] Fail

**Result:**

**Notes:**

---

## S2 — Blueprints survive a departure — **gate**

The headline behaviour change, and the inverse of P1/S7.

1. Confirm a rune is craftable and note which hero justifies it.
2. Dismiss that hero from the party screen.
3. `tor.check_enchantment_blueprints`
4. Open the enchanting table at a Karak and select a matching item.

- **Expect:** the rune is **still known** and **still offered by the table**, assuming someone
  present still clears its requirements. If nobody does, it must be listed as *blocked* with a
  reason — never silently missing.
- **Fail — the rune vanishes from the table:** something still consults per-hero data.
- **Fail — the rune disappears from `known`:** the store is being rebuilt from party state
  somewhere.

- [X] Pass  - [ ] Fail

**Result:**

**Notes:**

---

## S3 — Crafting-time gating: skill — **gate**

The replacement for the retention mechanic, so it has to actually bite.

Find or arrange a known blueprint whose skill requirement nobody in the party meets (the
`blocked` list from S1/S2 is the quickest way to spot one).

- **Expect at the table:** the enchantment is **listed and greyed**, and its hover tooltip ends
  with `Requires <Skill> <value>.` Clicking it does nothing.
- **Expect it is never hidden.** Hiding reads as "you lost it", which is the confusion this
  whole migration exists to remove.
- Then raise the skill above the threshold (`campaign.set_skill_value` or levelling) and reopen
  the table — it must become selectable.

- [X] Greyed, not hidden
- [X] Tooltip states the requirement
- [X] Clicking a greyed entry does nothing
- [X] Becomes selectable once the skill is met

**Result:**

**Notes:**

---

## S4 — Crafting-time gating: lore / attribute

Same as S3 but for the restriction half. A rune requires `RuneMagic`, which in practice only a
Runesmith companion has.

With a rune known but **no** qualifying hero present:

- **Expect:** listed and greyed, tooltip reading `Requires a character who knows the Lore of
  <lore>.` (or `Requires a character with <attribute>.` for attribute-gated ones).
- Re-hire a qualifying hero and reopen — it becomes selectable.

This is where the Runesmith fantasy now lives: they are required to *use* the rune, not to
*remember* it.

- [X] Pass  - [ ] Fail

**Result:**

**Notes:**

---

## S5 — Both checks must land on one hero

A deliberate design choice worth confirming rather than discovering later.

Arrange a party where hero A clears the lore restriction but is under the skill threshold, and
hero B clears the skill but not the lore.

- **Expect:** the enchantment is **still blocked**. Requirements do not pool across heroes.
- **Fail — it is selectable:** `GetUnmetRequirement` is checking the two halves independently.

If this is awkward to stage, skip it and say so — S3 and S4 are the load-bearing ones.

- [X] Pass  - [ ] Fail  - [ ] Skipped

**Result:**
This one was a bit more involved. I had my main hero who had the knowledge (runesmithing), and I gave a thane a higher smithing level, and it is compliant
Groin the thane had 75 smithing, but didn't know the appropriate lore.
**Notes:**

---

## S6 — Purchase is no longer skill-gated

At an enchanter/Karak shop, find a blueprint whose skill requirement you do **not** meet.

- **Expect:** the row is **enabled and buyable**, with a hint along the lines of "You can learn
  this now, but cannot enchant with it yet. Requires <Skill> <value>."
- **Expect:** rows are still disabled when you cannot **afford** them — that is the only
  remaining reason to grey a shop row.
- Buy it, then check it appears in `tor.check_enchantment_blueprints` under `blocked`.

- [X] Under-skilled row is buyable
- [X] Hint explains the future requirement
- [X] Unaffordable rows still disabled
- [X] Purchased blueprint shows as known-but-blocked

**Result:**

**Notes:**

---

## S7 — Purchase always lands

The old `SelectRecipientHero` returned null when the main hero was not among 2+ eligible heroes,
silently turning a paid-for blueprint into an inventory item.

With **two or more** non-main-hero companions eligible for the same blueprint, buy it.

- **Expect:** it is learned, with a notification naming one of them. It must **not** land in the
  inventory as an item.

- [X] Pass  - [ ] Fail

**Result:**

**Notes:**

---

## S8 — Manuscripts

Use a blueprint manuscript item from the inventory.

- **Expect:** the hero-choice inquiry lists everyone who clears the **lore/attribute**
  restriction, regardless of their skill — skill is no longer a filter here.
- **Expect:** using a manuscript for something already known says "You have already learned this
  enchantment." rather than offering an empty or pointless hero list.

- [X] Skill no longer filters the hero list
- [X] Already-known manuscript is refused cleanly

**Result:**
Still asking for which character should learn it. Not too much of a fan of that. We should remove that and have it display a straight message
**Notes:**

---

## S9 — Quest counters (P3b)

Start or continue a Runelord / Runesmith / Orc Shaman quest with a rune-counting task.

- **Expect:** the counter's starting value equals the `known:` count from
  `tor.check_enchantment_blueprints` (for the rune quests, which count all known blueprints).
- Have a **companion** learn one → counter increments.
- **Save and reload** → the counter holds. This is the specific bug being closed: the baseline
  used to be recomputed main-hero-only on load, so a companion's increment silently reverted.
- Dismiss that companion → the counter still holds.

- [X] Baseline matches known count
- [X] Companion learning increments it
- [X] Survives a reload
- [X] Survives the companion leaving

**Result:**

**Notes:**

---

## S10 — Runelord career button (P3a)

At a Karak, with a rune learned by a **companion** rather than the player:

- **Expect:** the unit-rune button sees it. Previously it read the main hero's list only, so a
  companion-learned rune was invisible here while the table offered it.
- With **no** blueprints known at all, the button must still show "Hero doesn't know any Runes
  yet" rather than erroring.

- [X] Companion-learned rune is visible to the button
- [X] Empty-store case still handled

**Result:**
The empty store should technically never be reached as we can't get to that without completing some of the quest.

**Notes:**

Assumptions I've made:

    Threshold is derived from the constituent runes (highest of the 3).
    Best-in-party — any hero can clear it, but the SAME hero must also have
    RuneMagic.
    Bug: Rune of Retribution references a trait id that doesn't exist(dw_rune_preservation), so it has never shown up in the list. So that was fixed.


Resulting gates:
  T1  Guarding / Sanctuary / Battle    75
  T2  Strollaz' 100 · Rapid Fire 150 · Retribution 250
  T3  Grimnir 225 · Grungni 250 · Valaya 275
---

## S11 — Cost reduction is main-hero only (P3c)

With a career that grants an enchantment cost reduction:

- **Expect:** the ingredient cost is the same whether one or three heroes who could benefit are
  in the party — the discount no longer stacks per knowing hero.
- **Expect:** a **companion's** career no longer contributes any discount. Only the main hero's
  does.

Note this is the decision flagged as "for now": a hired Runelord can satisfy the lore
restriction but contributes nothing to cost.

- [X] Discount no longer stacks
- [X] Only the main hero's career counts

**Result:**

**Notes:**

---

## S12 — Fresh campaign

Fresh dwarf start:

```
tor.check_enchantment_blueprints
```

- **Expect:** "No enchantment blueprints known.", no crash on an empty store.
- Take the Runelord career choice granting `dw_rune_stone`, re-run → known: 1.
- Open the table with armour selected → the rune is listed, greyed or not depending on whether
  you meet its requirements. **Career grants are now skill-gated like everything else** — that
  is intended, and is the balance change called out in the proposal.

- [X] Pass  - [ ] Fail

**Result:**

**Notes:**

---

## Summary

| Scenario | Result | Notes |
|---|---|---|
| **S1 Store persists across a reload (gate)** | Pass | |
| **S2 Blueprints survive a departure (gate)** | Pass | |
| **S3 Crafting-time skill gate (gate)** | Pass | All four checks |
| S4 Crafting-time lore/attribute gate | Pass | |
| S5 Both checks on one hero | Pass | Staged deliberately: main hero held RuneMagic, Groin the thane held 75 Smithing without the lore — stayed blocked |
| S6 Purchase not skill-gated | Pass | |
| S7 Purchase always lands | Pass | |
| S8 Manuscripts | Pass, with change request | Still prompts for a recipient hero; should print a straight message instead — **open** |
| S9 Quest counters | Pass | |
| S10 Runelord career button | Pass | Empty store unreachable in practice (gated behind quest progress); `dw_rune_preservation` trait id bug found and fixed |
| S11 Cost reduction main-hero only | Pass | |
| S12 Fresh campaign | Pass | |

**Sign-off**

- [X] Yes — S1, S2 and S3 all pass
- [ ] No — blocked by:

## Decisions to revisit after playing it

These are live in the code with `NOTE FOR REVIEW` comments; the test pass is a good moment to
form an opinion on each.

1. **Cost reduction (main hero) and the skill gate (best-in-party) resolve differently.** A
   hired Runelord can unlock a rune but not discount it. Note the "best in party" alternative for
   cost is not currently implementable — companions have no careers to draw a discount from.
2. **Should old saves have been migrated?** They are not: `[SaveableField(10)]` is deleted, so
   pre-change saves lose their blueprints and may not load. Deliberate, on the grounds that a new
   campaign is a fine ask for a mod - but it is the decision most likely to draw complaints.
3. **Career grants are now skill-gated** (S12). Runelord/Imperial Magister blueprints used to
   bypass every check by never passing through the shop; they now meet the table gate like
   anything else. Real balance change on those careers.

## Follow-ups found while testing

<!-- Anything noticed in passing that isn't a defect in this migration. -->
