# Extensions/UI

A reflection-based "view-model extension" framework that bolts extra bindable
properties/commands onto vanilla Gauntlet `ViewModel`s, so TOR can add UI hooks to native
screens (party, character developer, encyclopedia, crafting/refinement, conversation, map
info, SP item tooltip) without subclassing or fully replacing them.

- **`IViewModelExtension`** / **`BaseViewModelExtension`** (abstract, `IDisposable`) — base
  for every extension: wraps a target `ViewModel` (`_vm`), and via reflection forwards
  property/method access (`GetPropertyValue`/`SetPropertyValue`/`ExecuteCommand`/
  `GetViewModelAtPath`) to either the extension's own members (if declared on an
  `IViewModelExtension`-derived type) or the wrapped VM's — letting Gauntlet's XML/Prefab
  data-binding address extension properties as if they were on the original VM. Registers
  itself with the manager on construction.
- **`ViewModelExtensionManager`** (singleton) — `CollectViewModelExtensions()` scans for
  `[ViewModelExtension]`-attributed types at startup
  (`SubModule.OnSubModuleLoad` calls `Initialize()` before Harmony patches, since patches
  reroute vanilla VM construction to also construct/register the matching extension); holds
  a `ConditionalWeakTable<ViewModel, ExtensionHolder>` so extensions are GC'd with their VM.
- **`ViewModelExtensionAttribute`** — marks a `BaseViewModelExtension` subclass and the
  vanilla `ViewModel` type it targets.

## Concrete extensions (one per extended vanilla VM)

`PartyVMExtension` (+ `PendingResourceCostVM`), `PartyCharacterVMExtension`,
`CharacterDeveloperVMExtension`, `CraftingVMExtension`, `RefinementVMExtension`,
`ConversationItemVMExtension`, `MissionConversationVMExtension`,
`HeroEncyclopediaVMExtension`, `UnitEncyclopediaVMExtension`, `SPItemVMExtension`,
`TORMapInfoVMExtension`.

## Other

- **`TORInitialScreen`** (`: ScreenBase, IGameStateListener`) — TOR's replacement initial
  loading/splash screen.
- **`MainMenu/`** — main-menu-specific VMs/services (welcome popup, recommended-settings
  warning, shader-cache warning, extra main-menu links) — see its CLAUDE.md.
