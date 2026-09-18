# `<Module>` <Epic> — PR Notes

> Copy to `pr-notes/<module>-<epic>.md`. The block below is the PR body; everything outside it
> is for you.

**Branch:** `feature/<module><Epic>` → `development` · **Commits:** `<base7>..<head7>`
**Test plan:** `docs/testplans/<module>-<epic>.md` — <passed / not run>
**Check before opening:** <decisions taken · anything left unfixed · Harmony approvals>

---

## Copy-paste block

**Half a page, hard cap.** The diff shows what changed file by file — do not restate it. This
says only what a reviewer cannot get from reading the diff.

```markdown
## <Module> — <Epic>

<Two sentences: what this does, and why now.>

**Behaviour:** <"No player-visible change." — or exactly what changed, and what a user would notice.>

**Worth knowing**
- <Something surprising a reviewer would otherwise have to discover: a default that flipped, a
  decision taken over an alternative, a bug found on the way. Two or three lines at most.>

**Tested:** <build + which test-plan scenarios, on which save/culture.>

**Not in this PR:** <deferred item — which epic picks it up.>
```
