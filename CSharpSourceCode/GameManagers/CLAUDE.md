# GameManagers

Low-level game/campaign bootstrapping that has to run very early, outside the normal
`CampaignBehaviorBase` lifecycle.

- **`TorCampaignGameManager`** (`: SandBoxGameManager`) — TOR's replacement campaign game
  manager. On load finish it pushes `CharacterCreationState` and registers
  `TORCharacterCreationContentHandler` (see `CampaignMechanics/CharacterCreation`) via the
  1.3.1-era `OnCharacterCreationInitializedEvent`, replacing the older direct-constructor
  handler registration pattern.
- **`TORKeyInputManager`** (static) — registers TOR's custom hotkey category
  (`TORGameKeyContext`, "The Old Realms") and its key bindings (e.g. Quick Cast, Quick Cast
  Selection Menu) with `HotKeyManager`. Must use hardcoded text, not
  `Utilities/TORTextHelper`, since it runs before the game text manager exists. Initialized
  in `SubModule.OnSubModuleLoad`.
- **`TORGameKeyContext`** (`: GameKeyContext`) — the custom key-binding category itself,
  including the `TorKeyMap` key indices (`QuickCast`, `QuickCastSelectionMenu`, etc.).
- **`TORShaderGameManager`** — game-manager variant used to drive/track shader compilation
  state (pairs with `Utilities/ShaderSourceManager` and the shader-cache warning in
  `Extensions/UI/MainMenu`).
