# `Crafting` Strings — PR Notes

**Branch:** `feature/StringsCrafting` → `feature/FrameworkCrafting` (fifth in the stack; merge after the four below)
**Commits:** `feeb82f..<head>` — fill in once committed
**Test plan:** `docs/testplans/crafting-strings.md` — **not run**
**Code-check pass:** run. One regression found and fixed (shop "Not enough" hint resolved before
its icon variable was set); fallback-text mismatch fixed; a redundant `CopyTextObject` removed.
**Check before opening:** D1 (hand-editing the XML against CONSTRAINTS — CONSTRAINTS, README and
spec now say so), D5 (dead branch localized, not deleted). No Harmony patch touched.

---

## Copy-paste block

```markdown
## Crafting — Epic 3, Strings

Every player-visible string in `CampaignMechanics/Crafting/` and the enchanting prefab now
resolves through an id that exists in `tor_strings.xml`, and every Crafting id in that file is
used. First Strings epic, so it also settles the spec's open questions (Amendment 3).

**Behaviour:** Text only. Visible differences: the blueprint-shop tooltip loses stray spaces and
a trailing comma; the donation "gained" list is no longer run together. Old saves are unaffected —
no behaviour class, `SyncData` key or saved type touched.

**Worth knowing**
- Four ids the code asked for never existed, so English only showed because of defaults
  (`tor_refine_all_text` was in the XML as `tor_crafting.refine_all`). 13 orphans deleted —
  two literally read "PLEASE REPORT WHERE YOU SEE THIS".
- `tor_strings.xml` was edited by hand. `tortools`' `strings_add` was re-tested on this branch:
  one call deleted all 326 comment blocks (a 719-line diff). CONSTRAINTS now says reads via the
  server, writes by hand, until TOR_Tools keeps comments.
- The old shop tooltip glued a `{=id}` description in front of the cost template, so any
  translation of the description would have silently eaten the cost line.
- `docs/superpowers/tools/strings-audit.ps1` is the check every later Strings epic runs.

**Tested:** MSBuild clean (0 errors); audit reports no missing id but native `str_done`;
`tor_strings.xml` parses with its comment count unchanged. In-game scenarios S1–S9 **not yet run**.

**Not in this PR:** deleting `EnchanterTownBehavior`'s unreachable `DonationMode(false)` branch
and its 4 strings — Crafting Epic 4.
```

## For you, not the PR

- **Restart the `tortools` MCP server before using it.** Its in-memory index predates these edits
  and still holds the reverted test string; a write tool run now would overwrite the file from
  that stale model.
- The WIP committed to `feature/expectedChanges` (`7a254476`) adds `tor_hideout_ingredients_found_text`
  right after `tor_enchantmentshop_future_requirement`. New ids here were placed a few lines
  higher so the two merge without conflict.
- `tor_priest_train_hub_select_companion` is an orphan too, but it sits in the Skill Trainer block.
