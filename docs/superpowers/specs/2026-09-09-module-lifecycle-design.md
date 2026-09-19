# Module Lifecycle — Design

**Date:** 2026-09-09
**Amended:** 2026-09-11 — four steps became five epics (Amendment 1); the edge scan's blind spot recorded (Amendment 2).
**Status:** Approved. Epics 1 and 2 specified in detail; 3–5 deepen on first use.

Defines the process every module in `CSharpSourceCode/` passes through. Companion to
[`../README.md`](../README.md) (the rules), [`../CONSTRAINTS.md`](../CONSTRAINTS.md) (the
invariants) and [`../../vertical-slicing-proposal.md`](../../vertical-slicing-proposal.md)
(the classification of which file belongs to which module).

This document specifies **process, not placement**. It never names a module's files.

## Amendment 1 — 2026-09-11: four steps became five epics

The original spec had four steps: Modularize, Refactor, Patternize, Localize. Executing
Crafting exposed two defects.

**Defect 1 — Step 1 held two jobs.** Sub-steps 1.1–1.3 and 1.5 are mechanical: inventory files,
move them, register the module, write the docs. Sub-step 1.4 — cutting the cross-module
references — is none of those things. It is design work, it is where the risk lives, and it is
the part that decides what `Framework/` becomes. Bundled together, the easy four hid the hard
one: Crafting's ledger row read `done` while 1.4 was never finished, and a later plan had to
open with a task to go back and do it. **1.4 is now Epic 2, on its own branch and its own PR.**

**Defect 2 — "Refactor" named a grab bag.** "No file left doing two jobs" described an outcome
without naming the work. **It is now Epic 4, Codesmells**: find bad practices, fix them,
flag what cannot be fixed.

Localization also moved from last to third. It is the lowest-risk epic — text and XML, no
behaviour — and doing it before the code moves means string ids are settled before anything
shuffles them.

| Was | Is now |
|---|---|
| Step 1 Modularize (1.1–1.3, 1.5) | Epic 1 Modularize |
| Step 1 Modularize (1.4) | **Epic 2 Framework** |
| Step 4 Localize | Epic 3 Strings |
| Step 2 Refactor | Epic 4 Codesmells |
| Step 3 Patternize | Epic 5 Pattern |

**One epic is one branch is one PR.** The team cannot review large PRs; that constraint now
shapes the process rather than being absorbed by it.

## Amendment 2 — 2026-09-11: the edge scan is necessary, not sufficient

Found during Crafting's Epic 2. Both of Epic 2's exit criteria are `using`-based greps, and a
`using` is not the only way one module reaches another's types.

An extension method owned by `Framework/` can return a module's type. The caller names neither
the method's owner nor the returned type — it just writes `x.Foo().Bar` — so no `using` is
emitted and the scan reports clean. `CampaignMechanics/Crafting/EnchanterTownBehavior.cs:438,443`
is the found case: `Hero.MainHero.GetCareer().StringId`, where `GetCareer()` is in
`Extensions/HeroExtensions.cs` (Framework) and returns `CareerObject` (Careers).

**Not fixed, deliberately.** The one instance found is a string-id comparison — about the weakest
coupling a reference can be — and inventing a detection pass mid-epic would have been scope the
epic did not ask for. **But it applies to all 30 remaining modules**, and doing it by hand 30
times is the expensive path, so this is a tooling question, not a per-module one.

**Revisit before the third module's Epic 2**, tracked in the ledger's Framework work items. The
likely shape: a Roslyn or reflection pass that resolves each `Framework/` extension method's
return type and flags any that belong to `CampaignMechanics/<Module>`, run once per module as
part of 2.1 rather than written per module. Until it exists, Epic 2 plans should state that their
edge inventory covers `using` directives only.

## Why five epics and not one pass

The five kinds of work — extracting a module, cutting its boundary, exposing its strings,
cleaning its files, naming its patterns — could in principle be done in a single visit per
module. They are separated because each one makes the next one's diff smaller, and because risk
climbs monotonically down the list. Extraction is mechanical; boundary-cutting is design;
patterns are judgement. Mixing them produces a diff no reviewer can hold in their head.

The cost is that a file gets touched more than once over the program's life. That is accepted:
each touch is small, reviewable and independently shippable, which matters more on a codebase
with no automated test suite, where every merge is validated by a build and a human playtest.

## Epic 1 — Modularize

**Entry:** the module has a ledger row and nobody else holds it.
**Branch:** `feature/[module]Modularize`
**Goal:** the module becomes one folder that registers itself, with its models pulled in.

Specified in detail because it has been executed once — on `Crafting`, on branch
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


### 1.4 Verify and document

- Build; confirm the module's own files produce no new errors (see `CONSTRAINTS.md` on judging
  a build).
- Smoke playtest the module's mechanic in a real session.
- **Load a save made before the move.** The highest-risk part of the epic, not a formality —
  see O1.
- Write or refresh `CLAUDE.md` (narrative: how the pieces fit together) and `MODULE.md` (flat
  per-class lookup table), within the word caps. `CampaignMechanics/Crafting/MODULE.md` is the
  format to copy.
- Write the test plan, the PR notes, and update the ledger row.

### Exit criteria

1. `SubModule.cs` contains no line naming this module's types except the `ITORModule` dispatch.
2. Every moved file appears at its new path in `TOR_Core.csproj`.
3. The project builds with no new errors attributable to this module.
4. A save created before the branch loads, and the module's mechanic still works.
5. `CLAUDE.md` and `MODULE.md` exist, are within caps, and describe the folder as it now is.
6. Test plan scenarios pass; PR notes written; ledger row updated.

**Not an exit criterion any more:** cross-module references. That is Epic 2.

## Epic 2 — Framework

**Entry:** Epic 1 merged.
**Branch:** `feature/[module]Framework`
**Goal:** the module's folder contains no `using TOR_Core.<OtherModule>`, and no other module's
folder names this one. Arrows point from modules into `Framework/`, never between modules.

This epic is where `Framework/` is actually designed. Every module passing through it either
adds to `Framework/` or proves it did not need to.

### 2.1 Inventory the edges

Two lists, both written into the plan before anything moves:

- **Outbound** — every `using TOR_Core.<OtherModule>` inside the module's folder, with the
  member actually used and the file and line.
- **Inbound** — every file outside the module that names one of its types. Each one is either
  cut, or frozen into the module's declared **public surface**.

Untangling every inbound edge is often a project in its own right. When it is, freeze the set
instead: record it in `MODULE.md` as the public surface, and later epics treat those names as
un-renameable. That converts an unbounded risk into a checklist.

**The `using` scan has a blind spot — see Amendment 2.** A module can reach a *type* belonging to
another module without ever naming its namespace, by going through an extension method that
`Framework/` owns. `Crafting` does exactly this: `EnchanterTownBehavior` calls
`Hero.MainHero.GetCareer().StringId`, and `GetCareer()` lives in `Extensions/HeroExtensions.cs`
(Framework) but returns a `CareerObject` (Careers). No `using` appears, so both exit criteria
below report clean. Treat a clean scan as necessary, not sufficient.

### 2.2 Cut the edges

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


### Exit criteria

1. No `using TOR_Core.<OtherModule>` inside the module's folder — or each survivor is recorded
   in the ledger Notes with the module that will remove it and when.
2. No other module's folder names this module, except through a `Framework/` contract.
   Criteria 1 and 2 are `using` scans and miss types reached through Framework extension methods
   — see Amendment 2. A clean scan is necessary, not sufficient.
3. `MODULE.md` carries a **Public Surface** section listing every externally-referenced name.
4. The `Framework/` hook-contract count in the ledger is updated.
5. Build clean, save loads, test plan scenarios pass, PR notes written, ledger updated.

## Epic 3 — Strings

**Entry:** Epic 2 merged.
**Branch:** `feature/[module]Strings`
**Goal:** every player-visible string in the module resolves through an id, and every id is
registered in `ModuleData/tor_strings.xml`.

**Amended 2026-09-11.** This epic originally created one string file per module
(`ModuleData/Strings/tor_<module>_strings.xml`). That is cancelled: **there is one strings file,
`ModuleData/tor_strings.xml`, and it stays that way.** Grouping is by `category` / `subcategory`
/ tags *inside* the file — which is what the TOR_Tools UI and the environment team filter on —
not by splitting it. No `SubModule.xml` XML nodes are added, and nothing moves between files.

The work, per module:

1. Find every hardcoded player-visible literal in the module and convert it to a `TextObject`
   with an id.
2. Register each new id in `tor_strings.xml` **through the `tortools` MCP server**, with the
   category and subcategory that place it with its neighbours.
3. Audit the module's existing ids: right category, no orphans, no id referenced from code but
   absent from the file.

**Never hand-edit `tor_strings.xml`.** It is ~5,900 lines; a dropped or malformed id is
invisible until a player sees a raw `{=str_tor_...}` in-game. The server indexes the file and
validates writes — that is the whole reason Epic 3 waited for it.

The relevant tools: `strings_query` and `strings_search` to find, `strings_list_categories` and
`strings_by_category` to place, `strings_add` / `strings_update` / `strings_delete` to write.
`strings_add` generates the `{=str_...}` key itself — do not hand-write one.

Conventions are in [`../CONSTRAINTS.md`](../CONSTRAINTS.md). Culture variants use a `.<culture>`
suffix on the id (`tor_enchantmentshop_title.empire`).

**To settle on first use:**

- What counts as player-visible? Debug and log strings stay hardcoded; the boundary cases are
  inquiry titles, tooltips and console feedback.
- What is the category convention for a module's strings — one category per module, or per
  feature within it? The existing file is organised by feature ("Skill perks", "Career Choice",
  "UI"), not by code module, so a module's strings will usually span several categories. Decide
  whether that is fine or whether a subcategory should carry the module name.
- Is there a check that catches a newly added hardcoded literal in an already-done module, or is
  it review discipline only?
- Does `ModuleData/Languages/` need per-language files now? Only `VoicedLines` exists there and
  no translation file has ever been produced, so the first Strings epic may produce text nothing
  yet consumes. `TranslationTools` in the MCP server is the thing that would consume it.

## Epic 4 — Codesmells

**Entry:** Epic 3 merged (or skipped with a ledger note while `TOR_Tools` is pending).
**Branch:** `feature/[module]Codesmells`
**Goal:** bad practices found and fixed; no file left doing two jobs.

Read every file in the module in turn. Split files carrying two unrelated responsibilities.
Delete dead code. Normalize naming to the module's own convention. Reduce each class's surface
to what is actually consumed.

Because Epic 2 removed the cross-module references, this epic should never need to change a
signature another module depends on. If it does, Epic 2 was incomplete — go back and finish it
rather than working around it.

**Fix automatically; flag what cannot be fixed.** A smell that cannot be fixed inside this epic
goes in the test plan's Decisions table with what it would take, not into a conversation.

**To settle on first use:**

- What triggers "this file needs splitting" — a line count, a count of responsibilities, or
  reviewer judgement?
- How is "dead" established on a codebase where much is reached by reflection, by Harmony, or by
  script names declared in XML? A plain no-references search is **not** sufficient evidence, and
  deleting on that basis is how a mechanic silently stops firing.

## Epic 5 — Pattern

**Entry:** Epic 4 merged.
**Branch:** `feature/[module]Pattern`
**Goal:** the module's recurring seams named, and a pattern applied only where it earns its
place.

Explicitly **not** "apply patterns to everything". A pattern introduced where the code had no
repetition adds indirection and subtracts nothing. The deliverable is a short list — often one
or two entries, sometimes zero — of seams that genuinely recur, each with the pattern chosen and
a sentence on what it bought.

**The kill switch.** Before writing an abstraction, diff the things it would unify. If they are
not in fact the same shape, **do not write it** — record in `MODULE.md` that the shape did not
generalize, and close the epic. A pattern extracted from two things that only look alike is
worse than no pattern. An epic that ends here has still succeeded.

Two shapes already recur across the codebase and are the starting vocabulary:

- **XML template + static factory/manager.** Implemented at least five times
  (`AbilityTemplate`, `TriggeredEffectTemplate`, `StatusEffectTemplate`, `ItemTrait`, and the
  `MBObjectManager`-registered types). A module adding a sixth should conform rather than
  invent a variation.
- **`[Attribute]` + reflection scan for registration.** Implemented twice —
  `[ViewModelExtension]`, and `[TORModule]` once its registry exists.

**To settle on first use:**

- Is the vocabulary fixed program-wide up front, or grown module by module?
- Does this epic get to change a module's public surface, or is it internal-only?
- Where does the vocabulary live — a section here, or its own document?

## Deferred: Ink localization

Out of scope for the five epics, and explicitly **not** part of any module's Strings epic.

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
saves?** *Blocking; must be answered before the first module that moves such a type. Applies to Epic 1.*
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

**O3 — Module ordering for Epic 1.** The ledger's current order follows the proposal's phased
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
before the module that would claim it starts Epic 1. `Greenskins` may not survive as a module
at all.
