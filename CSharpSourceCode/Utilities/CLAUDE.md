# Utilities

Cross-cutting static helpers used from every other folder. No campaign-behavior/mission
logic of its own — pure infrastructure.

- **`TORConstants`** — every magic number/id string in one place, including the important
  **`Cultures`** struct mapping Warhammer culture names to the underlying vanilla
  `CultureObject.StringId` they're built on/reuse: `EMPIRE="empire"`,
  `BRETONNIA="vlandia"`, `SYLVANIA="khuzait"`, `MOUSILLON="mousillon"`,
  `ASRAI="battania"`, `DRUCHII="druchii"`, `BEASTMEN="steppe_bandits"`,
  `CHAOS="chaos_culture"`, `EONIR="eonir"`, `DAWI="sturgia"` (Dwarfs), `GREENSKIN="aserai"`,
  plus bandit/deserter variant cultures. `Cultures.All` is the 8 "main" playable cultures.
  Also has a **`Factions`** struct (Empire provinces: Reikland, Middenland, Ostland,
  Ostermark, Stirland, Hochland, Averland, Wissenland, Talabecland, Nordland, Moot,
  Wasteland, ...) and religion/devotion/voice-index constants.
- **`TORCommon`** (static) — grab-bag: `Say`/`Log` (message + NLog wrappers),
  `GetRandomDirection`/`GetRandomOrientation`, `FindNearestSettlement`/
  `FindSettlementsAroundPosition`/`FindPartiesAroundPosition` (spatial queries via
  `Locatable` search), `CopyEquipmentToClipBoard` (debug helper).
- **`TORConfig`** — loads `TORConfiguration` from an XML config file (`ReadConfig`, called
  from `SubModule.OnSubModuleLoad`) exposing tunable campaign constants (kingdom war
  cadence, bandit party caps, career perk point cap, declare-war score weights,
  `UseAlternativeVoiceManager`, `DisableMinstrelEvent`) plus a password-gated
  `EnableFreeRaceSelection` debug toggle.
- **`TORPaths`** — module root/data/log path resolution (`TORCoreModuleRootPath`,
  `TORArmoryModuleRootPath`, `TORLogPath`, etc.) — everything else that reads XML/assets
  goes through this rather than hardcoding relative paths.
- **`TORCampaignEvents`** — TOR's own custom `CampaignEvent` definitions (beyond vanilla
  `CampaignEvents`), e.g. `OnUseInventoryUseScriptObject` (see `Items/InventoryUseScripts`).
- **`TORConsoleCommands`** — debug console commands.
- **`TORTests`** — in-game smoke tests, likely surfaced via console commands.
- **`CTBlog`** — a debug/telemetry logging helper (separate from NLog, per its name — check
  before assuming it's dead code).
- **`TORNotificationHelper`** — builds/shows `CampaignMechanics/MapNotifications`.
- **`TORTextHelper`** / **`TextObjectExtension`** / **`StringExtensions`** /
  **`DictionaryExtensions`** — localization text lookup wrappers and small generic
  extensions.
- **`TORDamageDisplay`** — floating combat-text style damage number display.
- **`TORSpellBlowHelper`** — classifies whether a `Blow`/`KillingBlow` originated from a
  spell (used by `BattleMechanics/DamageSystem/TORDamageHelper.DetermineMask`).
- **`TORSummonHelper`** — mission agent-limit checks for summon-type abilities
  (`AbilitySystem/Ability.IsDisabled` calls `CanSummon()`).
- **`TORMissionHelper`** — mission-side damage/heal/status-effect
  application entry points used by `BattleMechanics/TriggeredEffect`
  (`DamageAgents`/`HealAgents`/`ApplyStatusEffectToAgent`).
- **`TORHireHelper`** — hiring-cost/eligibility helpers.
- **`TOREquipmentHelper`** — equipment set resolution helpers (character creation, troops).
- **`TORExtendedInfoHelper`** — query helpers over `Extensions/ExtendedInfoSystem` data.
- **`TORGameStarterHelper`** — startup wiring helpers (`CleanCampaignStarter`,
  `AddVerifiedIssueBehaviors`, called from `SubModule.InitializeGameStarter`).
- **`TORGameMenuBackgroundSwitcher`** — swaps settlement menu background art by context.
- **`TORMassMaterialSwitcher`** — bulk material/shader swapping on entities (visual variants).
- **`TOREntityRotator`** — simple entity rotation helper (scene/prop tool).
- **`TORParticleSystem`** (+ `ParticleIntensity` enum) — attaches/pools particle effects
  on agents/entities; used heavily by `BattleMechanics/StatusEffect/StatusEffectComponent`.
- **`TORAnimationLogger`** — debug mission behavior logging animation events (added only
  when a debugger is attached, see `SubModule.OnMissionBehaviorInitialize`).
- **`ShaderSourceManager`** — copies TOR_Armory shader sources into the game folder before
  shader compilation (`SubModule.OnSubModuleLoad`, gated by `ENABLECOPYSHADERS`).
