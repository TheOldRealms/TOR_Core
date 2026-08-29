# BattleMechanics/SFX

Small, reusable `ScriptComponentBehavior`s for scene-prop dressing, usable from the scene
editor as well as spawned at runtime (e.g. by `AbilitySystem`/`StatusEffect` visuals):

- **`TORSpinner`** — continuously rotates its entity (`RotationSpeed`); used by
  `StatusEffectComponent` for effects flagged `Template.Rotation`.
- **`TORFaceArmy`** (in `TORFaceEnemy.cs`) — orients an entity to face a target/enemy.
- **`TORLightDampener`** — attenuates a light source's intensity over time/conditions.
- **`TORSimpleObjectAnimator`** — plays a simple keyframe/transform animation on an entity.
- **`PlayerFlyableObjectScript`** — lets the player fly/ride an otherwise-static object
  (used for spectacle set-pieces, e.g. mounts summoned by abilities).
