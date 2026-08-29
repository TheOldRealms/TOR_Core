# BattleMechanics/Artillery

Field (non-siege-only) artillery pieces usable in open battles, extending TaleWorlds'
`RangedSiegeWeapon`/`StandingPoint` machinery. AI crew logic lives separately in
`BattleMechanics/AI/ArtilleryAI`; team-level protection behavior in
`BattleMechanics/AI/TeamAI` (`TORBehaviorProtectArtillery`, `TORTacticPositionalArtillery`).

- **`BaseFieldSiegeWeapon`** (abstract `: RangedSiegeWeapon`) — shared base for field
  artillery: ammo, aiming, firing.
  - **`FieldTrebuchet`** — concrete field trebuchet usable outside sieges.
  - **`ArtilleryRangedSiegeWeapon`** — general field artillery piece (cannon-type).
- **`ArtilleryStandingPoint`** / **`TrebuchetStandingPoint`** (`: StandingPoint`) and
  **`AmmoPickUpStandingPoint`** (`: StandingPointWithWeaponRequirement`) — the usable
  positions troops interact with to crew/reload the weapon.
- **`CannonBallPile`** (`: SiegeMachineStonePile`) — ammo pile prop for cannon-type weapons.
- **`Ballistics`** (static) — trajectory/aiming math shared by the weapons above.

Placement in battle is driven by `AbilitySystem`'s `ArtilleryPlacementScript`/
`ArtilleryPlacementCastingBehavior` for player/AI-called artillery abilities.
