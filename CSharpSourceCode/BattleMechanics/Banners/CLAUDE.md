# BattleMechanics/Banners

Custom faction/unit banners usable in missions (beyond the game's per-clan banner system).

- **`CustomBannerManager`** (static) — loads banner definitions from XML
  (`LoadXML`, called in `SubModule.OnSubModuleLoad`); **`FactionBannerOverride`** —
  per-faction override entry (lets a Warhammer faction/culture use a specific custom
  banner texture instead of the generated vanilla one).
- **`CustomBannerMissionLogic`** (`: MissionLogic`) — applies the overrides to banner-bearer
  agents/props during a mission; added in `SubModule.OnMissionBehaviorInitialize`.

Also see `Models/TORBattleBannerBearersModel` (which agents get banners) and
`HarmonyPatches/FactionBannerPatches.cs`.
