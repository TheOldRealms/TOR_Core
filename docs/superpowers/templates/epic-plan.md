# `<Module>` — <Epic> Plan

> Copy to `plans/<module>-<epic>.md`. Delete every instruction line as you fill it in.
> Keep it under two pages. Detail belongs in the code, not here.

**Epic:** <1 Modularize | 2 Framework | 3 Strings | 4 Codesmells | 5 Pattern>
**Branch:** `feature/<module><Epic>`
**Entry:** previous epic `done` and merged.
**Goal:** one sentence.

## Constraints

Project-wide constraints live in [`../CONSTRAINTS.md`](../CONSTRAINTS.md) and bind every task
here. List below *only* what is specific to this epic.

-

## Scope

| Doing | Not doing |
|---|---|
|  | |

Anything in the right column that someone will expect in the left needs one line saying why not.

## Tasks

Each task is independently reviewable and independently revertable.

### Task N: <name>

**Files:** create / modify / delete — full paths.
**Produces:** the exact signatures later tasks consume. Names, not descriptions.
**Consumes:** what earlier tasks produced.

- [ ] Step 1: ...
- [ ] Step 2: ...

## Verification

Test plan: `docs/testplans/<module>-<epic>.md`. There is no automated suite — a build plus the
numbered scenarios is the whole net.

## Exit criteria

Each must be checkable by a command or a named scenario, not by assertion.

1.
