# `<Module>` <Epic> — PR Notes

> Copy to `pr-notes/<module>-<epic>.md`. The block below is what goes in the PR body —
> everything outside it is for you, not the reviewer.

**Branch:** `feature/<module><Epic>` → `development`
**Commits:** `<base7>..<head7>`
**Test plan:** `docs/testplans/<module>-<epic>.md` — <passed / not yet run>

## For you to check before opening

- Decisions taken during the epic: see the test plan's Decisions table. <n> of them.
- Flagged, not fixed: <anything deliberately left, with why>
- Harmony / behaviour overwrites: <none | asked and approved on DATE>

---

## Copy-paste block

```markdown
## <Module> — <Epic>

**What this does**
<Two sentences. What changed, and what it unblocks.>

**Why now**
<One sentence: which epic this is, what comes next.>

**Changes**
- `path/to/file.cs` — what and why
- `path/to/other.cs` — what and why

**Behaviour**
<"No player-visible change." — or exactly what changed.>

**Decisions worth a look**
- <D1: what was chosen over what, in one line. Delete the section if there were none.>

**How this was tested**
Build clean against the installed game. `docs/testplans/<module>-<epic>.md`, scenarios S1–Sn,
all passing on <save/culture>.

**Not in this PR**
- <deferred item — which epic picks it up>
```
