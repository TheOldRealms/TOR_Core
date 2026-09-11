# Project Constraints

Binds every epic, every task, every agent. Referenced by plans rather than copied into them.

## Save compatibility — breaking these is unrecoverable for players

- **Never rename a `CampaignBehaviorBase` subclass.** Behaviour save data is keyed by type name.
- **Never change a `dataStore.SyncData` key** — even one that is misspelled or misleading. A
  corrected key orphans the saved value. Leave it and add a comment saying why.
- **Never renumber a `SaveableTypeDefiner` id.** Ids are stable and never reused.
- **Moving a `SaveableTypeDefiner`-registered type to a new namespace is unproven** (open
  question O1). Run the standalone probe before the first module that does it.

## Build

- **Every source file must appear in `TOR_Core.csproj` as `<Compile Include="..." />`.** This is
  an old-style non-SDK net48 project: an unlisted file silently does not build, and surfaces as
  a confusing missing-symbol error elsewhere. Every file created gets a matching csproj edit.
- **No default interface methods.** The net48 CLR rejects them with `CS8701` regardless of the
  configured `LangVersion`. Where "implement only what you need" is wanted, use an abstract base
  class with empty virtuals.
- **Judging a build:** a command-line `dotnet build` emits a large set of unrelated NLog /
  Harmony / `IsExternalInit` errors. Judge by whether that error set is **unchanged from before
  the branch**, not by whether it is empty. Building in the IDE against the installed game is
  the reliable check.
- `bin/Win64_Shipping_Client/TOR_Core.dll` is checked into git. A build overwrites it.

## Localization

- **One file.** `ModuleData/tor_strings.xml` is the only strings file. There are no per-module
  string files — grouping is by `category` / `subcategory` / tags inside the one file, which is
  what the environment team filters on.
- **All `tor_strings.xml` access goes through the `tortools` MCP server** (`D:/TOR_DEV/TOR_Tools`).
  Never hand-edit it: it is ~5,900 lines, and a dropped id is invisible until a player sees a raw
  `{=str_tor_...}` in-game. The server indexes it in memory and validates on write.
- Id is `tor_<name>`; the text carries a `{=str_tor_<name>}` default. Culture variants use a
  `.<culture>` suffix (`tor_enchantmentshop_title.empire`).

- Lookups go through `TORTextHelper`, never `GameTexts.FindText`.
- A line break is `{newline}` — not an escape sequence.

## Verification

- **There is no automated test suite.** The solution holds one project and references no test
  framework. "Verify" always means: build, then run the numbered scenarios in the epic's test
  plan in-game and fill in the Run record. It never means running a test command.
- An agent cannot run the scenarios. An epic's tasks complete as
  `unverified — awaiting playtest`; a human closes them.

## Process

- **Do not `git commit` or `git push` unless explicitly asked.** A plan step describing a commit
  is not standing authorization.
- **Harmony patches and any behaviour overwrite: stop and ask.** Not a judgement call.
- **Design-style choices are not escalated mid-epic.** Choose, then record the choice and its
  alternatives in the test plan's Decisions table.
- **Docs are trimmed, never appended to.** `CLAUDE.md` ≤ 150 words, `MODULE.md` ≤ 400.
