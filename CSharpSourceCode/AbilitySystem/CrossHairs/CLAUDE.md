# AbilitySystem/CrossHairs

Player-facing aiming reticles shown while an ability is armed, one shape per
`CrosshairType` (set on `AbilityTemplate.CrosshairType`).

- **`ICrosshair`** — `Show()/Hide()/Tick()/IsVisible`.
- **`Crosshair`** — plain default implementation of `ICrosshair`.
- **`AbilityCrosshair`** (abstract, `IDisposable, ICrosshair`) — base for ability-aware
  crosshairs; exposes `Frame`/`Position` used by `Ability.CalculatePlayerCastMatrixFrame`
  to place the spawned effect entity.
  - `SelfCrosshair` — no aiming, centers on caster.
  - `Pointer` — simple ground/world pointer (ability placement, e.g. Augment/Heal-on-ground).
  - `MissileCrosshair` — aiming reticle for projectile/missile-type abilities.
    - `SingleTargetCrosshair(AbilityTemplate)` — locks onto a single valid agent under the
      reticle (`CachedTarget`), used for SingleAlly/SingleEnemy target types.
  - `TargetedAOECrosshair` — ground-targeted area indicator.
  - `WindCrosshair` — directional cone/line indicator for Wind-type abilities.
- **`ProjectileCrosshair_VM`** — view-model backing the crosshair widget's Gauntlet UI.
- **`CrosshairType`** enum — the switch key `AbilityTemplate`/`Ability` use to pick which
  of the above to instantiate.

Consumed by `Ability.GetSpawnFrame` (via `Ability.Crosshair`) and set up by whatever mission
UI arms the ability (`AbilityManagerMissionLogic` / `AbilityHUD_VM`).
