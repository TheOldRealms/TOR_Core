# Ink

Integration of Inkle's [Ink](https://www.inklestudios.com/ink/) narrative scripting
language (via `lib/ink-engine-runtime.dll` + `lib/ink_compiler.dll`, referenced in
`TOR_Core.csproj`) for branching story content — used for narrative events/quests too
complex for simple dialog trees.

- **`InkStoryManager`** (singleton) — loads every `*.ink` file from the module's
  `InkStories/` folder into an `InkStory` (`Initialize()`, called from
  `SubModule.OnSubModuleLoad`); `AllStories`/`LastStoryId` track loaded/most-recent story.
- **`InkStory`** — wraps a compiled Ink `Story` runtime instance and bridges it to game
  state: exposes choices/continue-text, and lets Ink script "external functions" reach into
  the campaign (spawning items via `Items`, custom settlements via
  `CampaignMechanics/TORCustomSettlement`, custom events, quests, missions, audio) so a
  story file can trigger real game effects, not just show text.
- **`InkFileHandler`** — file I/O helper for locating/reading `.ink`/compiled story assets.
- **`InkFakeMarketData`** — mock market data used when an Ink story needs to reference
  prices/trade without a real market context.
- **`InkStoryCampaignBehavior`** (`: CampaignBehaviorBase`) — campaign-level hook that
  starts/advances stories (also see `Items/InventoryUseScripts/StartInkStoryScript`, an
  item that launches a story when used).
- **`InkStoryVM`** / **`InkStoryChoiceVM`** — Gauntlet view-models for presenting the
  current story text and its choices to the player.
