# Module Lifecycle — Design

**Date:** 2026-09-09
**Status:** Approved for Step 1. Steps 2–4 are outlines, to be deepened before first use.

Defines the four-step process every module in `CSharpSourceCode/` passes through. Companion
to [`../README.md`](../README.md) (the rules) and
[`../../vertical-slicing-proposal.md`](../../vertical-slicing-proposal.md) (the classification
of which file belongs to which module).

This document specifies **process, not placement**. It never names a module's files. When you
need to know what belongs to `Religion/`, read the proposal's classification table.

## Why four steps and not one pass

The four kinds of work — extracting a module, cleaning its files, applying patterns, exposing
its strings — could in principle be done together in a single visit per module. They are
separated because only the first one is *structural*: until a module is a real boundary, a
cleanup or a localization pass has to guess where the boundary will end up, and gets redone
when it lands somewhere else. Modularizing first makes the other three purely local edits
inside a folder that nothing else reaches into.

The cost of separating them is that a file gets touched more than once over the program's life.
That is accepted: each touch is small, reviewable and independently shippable, which matters
more on a codebase with no automated test suite, where every merge is validated by a build and
a human playtest.

## Step 1 — Modularize

**Entry:** the module has a row in the ledger and nobody else holds it.

**Goal:** the module becomes one folder that registers itself, and no other part of the
codebase names it.

This step is specified in detail because it has been executed once — on `Crafting`, on branch
`feature/moduleCrafting` — and the sub-steps below are that execution generalized.

### 1.1 Inventory

List every file that belongs to this module, wherever it currently lives, using the
proposal's classification table. Expect the module's files to be scattered across several
top-level folders; the scatter is the reason the step exists. `Crafting`'s files were spread
across `CampaignMechanics/Crafting/`, `Models/`, `HarmonyPatches/`, `Extensions/UI/` and
`Items/`.

Categories worth checking explicitly, because each lives in a different top-level folder today:

- campaign behaviors, mission behaviors
- `GameModel` overrides
- Harmony patches that only touch this module's own types
- view-model extensions and Gauntlet screens/states named after this module's UI
- weapon-hit and inventory-use scripts belonging to items this module grants
- quests
- static helpers, constants and data loaders
- any `ModuleData/` XML the module owns
- any `SaveableTypeDefiner`-registered types (flag these — see 1.5 and open question O1)

Record the inventory in the implementation plan. It is the checklist the rest of the step
works through.

### 1.2 Move

Move the inventoried files into `<Module>/`, with models under `<Module>/Models/`. Then:

- update `namespace` declarations and every `using` that referenced the old location;
- update every `<Compile Include="...">` path in `TOR_Core.csproj`. This project is old-style
  (non-SDK) .NET Framework 4.8: **a source file not listed in the csproj silently does not
  build**, so a missed path shows up as a confusing missing-symbol error rather than as a
  missing file;
- move any module-owned `ModuleData/` XML to match, and update the corresponding path in
  `SubModule.xml`.

Do not change behavior in this sub-step. Moves and renames only — that is what makes the diff
reviewable.

### 1.3 Self-register

Add `<Name>Module` in the module's folder, implementing `Framework/ITORModule` and marked
`[TORModule]`. Move the module's `AddBehavior`, `AddModel`, `AddMissionBehavior` and
`ObjectManager.RegisterType` calls out of `SubModule.cs` and into the matching interface
method. Leave the interface methods the module does not need as empty bodies.

`SubModule.cs` keeps one dispatch line per lifecycle hook for the module. It does not shrink to
zero until the registry (open question O2) exists.

Note: `ITORModule` declares five methods with no default implementations, and a module must
provide all five even when most are empty. This is not an oversight to be "fixed" with default
interface methods — the net48 CLR rejects those with `CS8701` regardless of the configured
`LangVersion`. If the empty bodies become a real nuisance, add an abstract base class that
implements the interface with empty virtuals and have modules derive from it.

### 1.4 Cut the cross-module references

The invariant to reach: **the module's folder contains no `using TOR_Core.<OtherModule>`, and
no other module's folder names this one.** Arrows point from modules into `Framework/`, never
between modules.

Two tools, in order of preference:

1. **Module-local helper.** When the module was merely borrowing something from a shared
   helper class, carve out the part it uses into a module-local equivalent. `Crafting` took
   its two `GameModels` accessors out of the shared `GameModelsExtensions` into its own
   `CraftingModelsExtensions`, so `Framework` never has to know the concrete model types.
   Prefer this — it costs one small class and creates no new coupling.

2. **A `Framework/` hook contract.** When two modules genuinely need to influence each other,
   put a contract in `Framework/` that both depend on, so neither names the other.
   `CraftingCareerHooks` is the reference implementation: `Crafting` consumes the hook lists,
   `Careers` populates them at startup, and neither references the other's namespace.

**The ratchet on tool 2.** Each hook contract is a narrow, pairwise, hand-written class. One is
a good trade. Several is a `Framework/` god object made of special cases. The rule: at the
third or fourth such class, stop adding them and build a generic reflection-based registry
instead — the same discovery pattern `Extensions/UI`'s `ViewModelExtensionManager` already
uses. Track the count in the ledger's Framework table.

If neither tool fits — the dependency is real, one-directional, and the callee is genuinely
generic — the right answer is usually that the callee is not module content at all and should
be promoted into `Framework/`. Amend the proposal's classification when that happens.

### 1.5 Verify and document

- Build, and confirm the module's own files produce no new errors. On this old-style net48
  project a command-line build also emits a large set of unrelated NLog/Harmony/
  `IsExternalInit` errors; judge the result by whether that error set is unchanged from before
  the branch, not by whether it is empty.
- Smoke playtest the module's mechanic in a real session.
- **Load a save made before the move.** This is the highest-risk part of the step, not a
  formality — see O1.
- Write or refresh the folder's `CLAUDE.md` (narrative: how the pieces fit together) and
  `MODULE.md` (flat per-class lookup table). `CampaignMechanics/Crafting/MODULE.md` is the
  format to copy.
- Update the ledger row.

### Exit criteria

1. No `using TOR_Core.<OtherModule>` inside the module's folder, and no other module's folder
   names this module.
2. `SubModule.cs` contains no line naming this module's types except the `ITORModule` dispatch.
3. Every moved file appears at its new path in `TOR_Core.csproj`.
4. The project builds with no new errors attributable to this module.
5. A save created before the branch loads, and the module's mechanic still works.
6. `CLAUDE.md` and `MODULE.md` exist and describe the folder as it now is.
7. The ledger row reads `done` for Step 1.

## Step 2 — Refactor (outline)

**Entry:** Step 1 done and merged.
**Goal:** a file-by-file cleanup pass; no file left doing two jobs.

The work: read every file in the module in turn. Split files that carry two unrelated
responsibilities. Delete dead code and unreferenced members. Normalize naming to the module's
own convention. Reduce the surface each class exposes to what is actually consumed.

Because Step 1 removed every inbound cross-module reference, Step 2 should never need to change
a signature another module depends on. If it does, that is a signal Step 1 was incomplete for
this module — stop and finish Step 1 rather than working around it.

**To settle before the first Step 2:**

- What is the trigger that says "this file needs splitting"? A line count, a count of distinct
  responsibilities, or reviewer judgement.
- Does Step 2 rewrite a module's `MODULE.md`, or is `MODULE.md` regenerated at the end of every
  step regardless?
- How is "dead" established on a codebase where a lot is reached by reflection, by Harmony, or
  by script names declared in XML? A plain "no references" search is not sufficient evidence
  here, and deleting on that basis is how a mechanic silently stops firing.

## Step 3 — Patternize (outline)

**Entry:** Step 2 done.
**Goal:** design patterns applied *where they earn their place*, and the module's recurring
seams named.

This step is explicitly **not** "apply patterns to everything". A pattern introduced where the
code had no repetition adds indirection and subtracts nothing. The deliverable per module is a
short list — often one or two entries, sometimes zero — of seams that genuinely recur, each
with the pattern chosen for it and a sentence on what it bought.

Two shapes already recur across the codebase and are the obvious starting vocabulary:

- **XML template + static factory/manager.** Already implemented at least five separate times
  (`AbilityTemplate`, `TriggeredEffectTemplate`, `StatusEffectTemplate`, `ItemTrait`, and the
  `MBObjectManager`-registered types). A module adding a sixth should conform to a shared shape
  rather than invent a variation.
- **`[Attribute]` + reflection scan for registration.** Implemented twice —
  `[ViewModelExtension]`, and `[TORModule]` once its registry exists.

**To settle before the first Step 3:**

- Is the pattern vocabulary fixed program-wide up front, or grown module by module? Growing it
  risks two modules solving the same seam differently; fixing it up front risks specifying
  patterns for seams that turn out not to exist.
- Does Step 3 get to change a module's public surface, or is it internal-only like Step 2?
- Where does the vocabulary live — a section in this spec, or its own document?

## Step 4 — Localize (outline)

**Entry:** Step 2 done. Step 3 is not a prerequisite.
**Goal:** every player-visible string in the module resolves through its own string file.

The work, per module:

1. Find every hardcoded player-visible literal in the module and convert it to a `TextObject`
   with a localization id.
2. Create `ModuleData/Strings/tor_<module>_strings.xml` and register it as its own
   `<XmlNode><XmlName id="Strings" path="Strings/tor_<module>_strings" /></XmlNode>` in
   `SubModule.xml`.
3. Move the module's existing ids out of the 5,864-line `ModuleData/tor_strings.xml` into that
   file.

Conventions already in force on this project and unchanged by this step: the string id is
`tor_<module>_<name>`, the text carries a `{=str_tor_<module>_<name>}` default, lookups go
through `TORTextHelper` rather than `GameTexts.FindText`, and a line break is `{newline}`, not
an escape sequence.

The point of the per-module file is that the environment team edits one small file scoped to
one feature instead of scrolling a single shared one, and that a module's text moves with the
module.

**To settle before the first Step 4:**

- Is `tor_strings.xml` drained module by module until it is empty, or does it stay as the home
  for genuinely cross-cutting strings? If the latter, what qualifies as cross-cutting.
- What counts as player-visible? Debug and log strings stay hardcoded; the boundary cases are
  inquiry titles, tooltips, and console feedback.
- Is there a check that catches a newly added hardcoded literal in an already-localized module,
  or is it review discipline only?
- Does `ModuleData/Languages/` need per-language files created now? Only `VoicedLines` exists
  there today and no translation file has ever been produced, so the first Step 4 may be
  producing text nothing yet consumes.

## Deferred: Ink localization

Out of scope for the four steps, and explicitly **not** part of any module's Step 4.

`InkStories/` holds 30 `.ink` files, roughly 3,371 lines, with prose written inline as English.
There is no localization mechanism of any kind: a translator cannot reach the text. `Ink/` is
also classified as `Framework`, so the work does not belong to a module in the first place.

Three options were considered and none chosen:

1. **Extraction tool; writers keep plain prose.** Writers keep authoring readable English in
   `.ink`. A dev-time tool harvests each display line into a strings file with generated stable
   ids, and `InkStoryManager` resolves lines through `TORTextHelper` at display time, falling
   back to the `.ink` text. Keeps the files authorable and diffable. Costs an extractor plus an
   id-stability rule so re-running it does not churn ids.
2. **Authors write ids inline.** No tooling, explicit and stable ids — at the cost of making
   the `.ink` files unreadable as narrative and the writing workflow substantially worse.
3. **Runtime auto-id by content hash.** Cheapest to build, no source changes. Ids break on
   every typo fix, and translators have no source file to work from unless a dump tool is
   written anyway.

Option 1 was the leading candidate. Recorded here so this is resumed rather than re-derived.

## Open questions

**O1 — Does moving a `SaveableTypeDefiner`-registered type to a new namespace break existing
saves?** *Blocking; must be answered before the first module that moves such a type.*
The `Crafting` pass did not answer it: `TorItemDuplicationData` is registered as id `16` in
`SaveGameSystem/SaveableTypeDefiners.cs`, but it already lived in `CampaignMechanics/Crafting/`
and so did not change namespace. Most later modules will move saved types. The probe is cheap —
move one saved type to a new namespace, keep its id, load a save made before the move — and it
should be run standalone, before it is run as part of a module's step, so that a failure is
unambiguous. The `SaveGameSystem` rule that ids are stable and never reused holds regardless of
the answer.

**O2 — Reflection-based `TORModuleRegistry`, or keep the explicit list in `SubModule.cs`?**
The proposal's target shape is a reflection scan for `[TORModule]` types, mirroring
`ViewModelExtensionManager`. `Crafting` proved the interface but the registry is unbuilt, and
`SubModule.cs` still calls `new CraftingModule().Register...()` explicitly. The explicit list is
readable, debuggable and has no startup cost; it is fine up to roughly ten modules. Revisit when
the count approaches that, and confirm the reflection scan's startup cost against Bannerlord's
load profile before committing to it.

**O3 — Module ordering for Step 1.** The ledger's current order follows the proposal's phased
suggestion: proven-small modules first (`BountyMaster`, `PostBattleLoot`, `Villages`),
split-heavy ones in the middle, and `Careers` last because it spans five current top-level
folders. Confirm or reorder before starting module #1.

**O4 — Does `SaveGameSystem`'s central definer get split per module?** Today ids are tracked in
one place, which is what makes collisions reviewable, but a couple of behaviors already define
their own definers inline. The likely answer is: existing ids stay where they are (renumbering
breaks saves), the central file remains the id ledger, and new types are registered by their
owning module. Confirm before a module needs to add a saved type.

**O5 — Placements flagged *verify* in the proposal.** `GreenskinAICampaignBehavior`,
`TORHiringCompatibilityModel`, `HuntCultistsQuestCampaignBehavior` and
`PlaguedVillageQuestCampaignBehavior` do not have a settled module. Each needs a source read
before the module that would claim it starts Step 1. `Greenskins` may not survive as a module
at all.
