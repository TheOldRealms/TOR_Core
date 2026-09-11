# `Crafting` Framework — PR Notes

**Branch:** `feature/FrameworkCrafting` → `feature/CentralizeCraftingRecipes` (**not** `next_update` —
this is the fourth branch in a stack; merge it after the two below it land)
**Commits:** `5f64759..<head>` — fill in once committed
**Test plan:** `docs/testplans/crafting-framework.md` — **not run**
**Code-check pass:** run, findings folded in — see "For you" below.
**Check before opening:** D1–D4 in the test plan, chiefly **D3** (freeze rather than cut the
remaining inbound edges). Reviewer disagreement on D3 changes the shape of every later module's
Epic 2, so settle it here. No Harmony patch touched; no approval needed.

---

## Copy-paste block

```markdown
## Crafting — Epic 2, Framework

Cuts the arrows between `Crafting` and the modules around it, and turns the ones that survive
into a declared, frozen list instead of an unbounded risk. This is the epic that decides what
`Framework/` is for, so the worked example matters more than the diff does.

**Behaviour:** No player-visible change. Both moves are namespace-only — no member renamed, no
signature changed, no `CampaignBehaviorBase` type name or `SyncData` key touched, no
`SaveableTypeDefiner` id involved. Old saves load.

**Worth knowing**
- Two types were promoted to `Framework/`, not one. `TorEnchantingIngredients` was the surprise:
  `Items/ItemTrait` and `Models/TORFaithModel` — both Framework-classified — were already
  reaching into `CampaignMechanics/Crafting/` to get it. That is a Framework→module arrow, which
  is the exact thing this epic exists to delete, so the catalogue was never module content. Three
  inbound `using`s disappeared as a result, one of them Careers'.
- `vertical-slicing-proposal.md` had it filed under `Items/` root and headed for `Crafting/`.
  Both halves were wrong; amended in place, with an Amendments table added to that doc.
- The remaining inbound edges (`EnchantmentBlueprints`, `EnchantmentHelper`,
  `EnchantmentIngredientLootCampaignBehavior`, consumed by Careers, CustomResources,
  TORCustomSettlement and three quests) are **frozen, not cut** — listed as Public Surface in
  `Crafting/MODULE.md` and un-renameable from here. Cutting them means a second `Framework/` hook
  contract, and the spec's own ratchet says that is how `Framework/` becomes a god object. Hook
  count stays at 1.
- `Framework/CraftingCareerHooks.cs` claimed `CraftingCareerHookRegistrations` never references
  `CampaignMechanics/Crafting`. It does — `EnchantmentBlueprints.Learn(...)`, lines 81–94. The
  comment was wrong, not the code; corrected to say why that direction is allowed.

**Tested:** Build only — the command-line error set is unchanged from the branch below (every
unresolved type is Harmony or NLog, the known baseline; no TOR_Core symbol fails to resolve).
The eight scenarios in `docs/testplans/crafting-framework.md` are **not yet run**.

**Not in this PR:** `PriestBehavior` → `Religion.ReligionObject.All`, the one surviving outbound
edge. Cutting it now would guess where Religion's boundary lands before Religion has been
modularized; it is recorded on Religion's ledger row for its Epic 1, with the two candidate
resolutions written out.
```

## For you, not the PR

- `TORCustomSettlement/CLAUDE.md` (211 words) and `Items/CLAUDE.md` (180) are over the 150-word
  cap. Both were already over before this branch; this epic only deleted a stale bullet from each.
  They belong to those modules' own epics — do not let a reviewer expand scope here.
- The stack means this PR's diff will read clean only if the two below it are reviewed first.
- The code-check pass found no code defects: zero non-`using`/`namespace`/comment lines in the
  whole `.cs` diff, csproj correct at 624 `<Compile>` entries, all 15 consumers of the two moved
  types resolving. Its four documentation findings are already fixed here — the corrected
  `CraftingCareerHooks` comment overreached ("Crafting never names a Career type" is false in
  three places; now scoped to career-*specific* content), `EnchantmentHelper.BlueprintRequirement`
  was missing from the Public Surface table, exit criterion 1's grep matched its own folder path,
  and `Framework/CLAUDE.md` needed "in code" to stop contradicting a prose mention.
