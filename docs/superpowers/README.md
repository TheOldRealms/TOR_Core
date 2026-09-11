# TOR_Core — Module Lifecycle Program

Turns `CSharpSourceCode/` from one flat codebase into a `Framework/` layer plus self-contained
modules. Every module passes through **five epics, in order**.

> **One epic = one branch = one PR.** The team cannot review large PRs. A module never sits
> half-way through an epic across a merge.

## The five epics

| # | Epic | Branch | Done when |
|---|---|---|---|
| 1 | **Modularize** | `feature/[module]Modularize` | One folder, self-registers via `ITORModule`, its models pulled in, builds, plays |
| 2 | **Framework** | `feature/[module]Framework` | No `using TOR_Core.<OtherModule>` in either direction; genuinely shared code has moved to `Framework/` |
| 3 | **Strings** | `feature/[module]Strings` | Every player-visible string resolves through `ModuleData/Strings/tor_<module>_strings.xml` |
| 4 | **Codesmells** | `feature/[module]Codesmells` | Bad practices found and fixed; no file left doing two jobs |
| 5 | **Pattern** | `feature/[module]Pattern` | Recurring seams named; a pattern applied only where it earns its place |

## Why this order

Each epic makes the next one's diff smaller, and risk climbs as you go down the list.

- **Modularize before Framework** — you cannot see which references cross a boundary until the
  boundary exists.
- **Framework before Strings and Codesmells** — cleaning or localizing a file that is about to
  move means doing it twice.
- **Strings before Codesmells** — pure text and XML, zero behaviour risk. Settle the text while
  the code is still still, so a later code shuffle never drags string ids with it.
- **Pattern last** — a pattern extracted before the other four is a guess. After them, the
  repetition is visible and the claim is checkable.

## What every epic delivers

The same five artifacts, every time:

1. **Code changes** — scoped to this epic only.
2. **Test plan** — `docs/testplans/<module>-<epic>.md`, from
   [`templates/epic-testplan.md`](./templates/epic-testplan.md). Numbered scenarios, with
   console commands wherever a scenario can be driven by one. *If a scenario cannot be reached
   without a new console command, write the command.*
3. **Documentation cleanup** — `CLAUDE.md` and `MODULE.md` refreshed and **trimmed**, never
   merely appended to.
4. **Code-check agent pass** — an independent review of the epic's diff before PR notes.
5. **PR notes** — `docs/superpowers/pr-notes/<module>-<epic>.md`, from
   [`templates/pr-notes.md`](./templates/pr-notes.md), containing a copy-paste block. **You open
   the PR**, not the agent.

## Standing rules

1. **Findings are amended automatically.** An epic runs start to finish without checking in.
2. **Harmony or any behaviour overwrite → STOP AND ASK.** This is the one gate that is not a
   judgement call. Alongside destructive operations and pushes to shared branches, it is the
   only reason to interrupt an epic.
3. **Design-style concerns are not escalated mid-run.** Pick the best option, then record the
   choice *and the alternatives considered* at the top of the test plan. Review them at PR time.
4. **All XML handling goes through `TOR_Tools`.** See Dependencies below.
5. **Docs stay short.** `CLAUDE.md` ≤ 150 words, `MODULE.md` ≤ 400 words, test plan ≤ 2 pages.
   These are hard caps, not targets. The repo average today is 121 words — hold that line.
6. **An epic ends with the ledger updated.** An untouched ledger row means the epic is not done.

## What lives where

| Path | What it is |
|---|---|
| `module-ledger.md` | The living record. One row per module, one column per epic. |
| `specs/` | Design docs. Amended in place, never rewritten. |
| `plans/` | One implementation plan per epic per module. |
| `templates/` | The three templates every epic fills in. |
| `pr-notes/` | Copy-paste PR bodies, one per completed epic. |
| [`../architecture-overview.md`](../architecture-overview.md) | What the codebase is today. |
| [`../vertical-slicing-proposal.md`](../vertical-slicing-proposal.md) | **The classification** — which folder is Framework, which is a Module. When it disagrees with this directory about a file's home, the proposal wins and gets amended in place. |

## Dependencies

| Blocker | Blocks | State |
|---|---|---|
| `TOR_Tools` XML handling | Epic 3 (Strings), every module | Being designed. No Strings epic starts until it exists. |
| Save-namespace safety probe (O1) | Epic 1, any module moving a `SaveableTypeDefiner` type | Not run. Cheap, standalone, must precede the first such move. |

## Starting an epic

1. Open `module-ledger.md`; confirm the module's previous epic is `done` and merged.
2. Read that epic's section in [`specs/2026-09-09-module-lifecycle-design.md`](./specs/2026-09-09-module-lifecycle-design.md).
3. Write the plan into `plans/` from [`templates/epic-plan.md`](./templates/epic-plan.md).
4. Branch `feature/[module][Epic]`, execute, build, playtest.
5. Write PR notes; hand them over. **You** open the PR.
6. Update the ledger.
