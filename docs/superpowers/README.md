# TOR_Core — Module Lifecycle Program

This directory holds the plans and specs for the long-running effort to turn
`CSharpSourceCode/` from one flat codebase into a `Framework/` layer plus a set of
self-contained vertical-slice modules, and then to clean, patternize and localize each
module in turn.

Future plans and specs go here too — this is the home for anything that describes work
we intend to do, as opposed to work already done.

## What lives where

| File | What it is |
|---|---|
| `README.md` | This file. The four steps, the rules, how to use this directory. |
| `module-ledger.md` | The only living document. One row per module, which step it is on. Update it at the end of every step. |
| `specs/` | Design docs. Written once, amended rather than rewritten. |
| `plans/` | Implementation plans (one per step per module, produced from a spec). |

Two existing documents sit outside this directory and are **not** superseded by it:

- [`../architecture-overview.md`](../architecture-overview.md) — what the codebase is today.
- [`../vertical-slicing-proposal.md`](../vertical-slicing-proposal.md) — **the classification**:
  which folder is Framework, which is a Module, and where each stray file belongs.

The split is deliberate. `vertical-slicing-proposal.md` answers *"where does this file go?"*.
This directory answers *"what do I do to one module, in what order?"*. When the two disagree
about a file's home, the proposal wins and gets amended in place.

## The four steps

Every module passes through the same four steps, in order:

| Step | Name | The module is done with this step when |
|---|---|---|
| 1 | **Modularize** | It is a single folder, registers itself via `ITORModule`, has no cross-module references, builds, and plays. |
| 2 | **Refactor** | A file-by-file cleanup pass is complete; no file is left doing two jobs. |
| 3 | **Patternize** | Design patterns have been applied *where they earn their place*, and the module's recurring seams are named. |
| 4 | **Localize** | Every player-visible string resolves through `ModuleData/Strings/tor_<module>_strings.xml`. |

The full definition of each step — sub-steps, entry and exit criteria, open questions — is in
[`specs/2026-09-09-module-lifecycle-design.md`](./specs/2026-09-09-module-lifecycle-design.md).

## The rules

1. **Step 1 runs across every module before any module starts Step 2.** Modularization is
   the pass that unblocks the other three: once a module is a real boundary, cleaning it,
   patternizing it and localizing it are all local operations. Doing them before the boundary
   exists means doing them twice.
2. **Steps 2, 3 and 4 are picked up per module, in any order across modules.** There is no
   requirement that every module reach Step 2 before any reaches Step 3. Pick a module, take
   it up a step, tick the ledger.
3. **One step is one branch is one PR.** A module never sits half-way through a step across a
   merge. If a step turns out bigger than expected, finish the part you can define an exit
   criterion for and split the remainder into a follow-up — do not merge a partial step.
4. **A step ends with the ledger updated.** If the ledger was not touched, the step is not done.
5. **Steps 2–4 are specified thin on purpose.** They will be deepened into full specs the
   first time a module actually reaches them, using what Step 1 taught us. Do not treat their
   current outlines as complete instructions.

## How to start work on a module

1. Open `module-ledger.md`, find the module, confirm which step it is on and that nobody else
   holds it.
2. Open the spec section for that step.
3. Produce an implementation plan into `plans/` (via the `superpowers:writing-plans` skill).
4. Branch, execute, build, playtest, PR.
5. Update the ledger.

## Known gaps, deliberately deferred

- **Ink localization.** The 30 `.ink` files under `InkStories/` hold roughly 3,371 lines of
  prose with no localization mechanism at all — the text is inline English, unreachable by
  translators. This is out of scope for the four steps and is *not* part of any module's
  Step 4. It is recorded, with the options already considered, in
  [`specs/2026-09-09-module-lifecycle-design.md`](./specs/2026-09-09-module-lifecycle-design.md#deferred-ink-localization)
  so that it is picked up from where the thinking stopped rather than from scratch.
