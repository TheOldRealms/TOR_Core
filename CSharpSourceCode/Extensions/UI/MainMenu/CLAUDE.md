# Extensions/UI/MainMenu

Main-menu screen additions/warnings, shown before a campaign is loaded.

- **`TORWelcomePopupVM`** — a first-run/update welcome popup.
- **`TORRecommendedSettingsWarningVM`** (+ **`TORRecommendedSettingsService`**, internal
  static) — warns the player if their graphics/game settings don't match TOR's recommended
  configuration (this mod adds heavy shader/particle content).
- **`TORShaderCacheWarning`** (internal static) — warns about/handles shader-cache-related
  first-launch stutter (pairs with `Utilities/ShaderSourceManager` and
  `SubModule`'s shader-compilation tracking in `OnApplicationTick`).
- **`TORMainMenuLinksVM`** (+ **`TORMainMenuLinkLauncher`**, internal static) — extra
  main-menu links (e.g. Discord/wiki/mod page) opened via the launcher helper.
