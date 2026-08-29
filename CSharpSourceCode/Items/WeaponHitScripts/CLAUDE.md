# Items/WeaponHitScripts

On-hit weapon proc effects, referenced by name from `Items/ItemTrait.OnWeaponHitScript`
and instantiated per hit.

- **`IWeaponHitScript`** — `OnHit(attackingAgent, attackedAgent, blow, missionWeapon,
  collisionData)`.
- **`BaseWeaponHitScript(string[] arguments)`** — default base implementing the interface;
  `ApplyWeaponTraitDamage` routes secondary/bonus damage through
  `AbilitySystem/AbilityManagerMissionLogic.QueueOnHitSecondaryDamage` when available
  (deferred outside the original hit callback to avoid mutating agent state mid-resolution),
  else falls back to `Extensions/AgentExtensions.ApplyDamage` directly.
- **`WeaponScripts.cs`** — the bulk of concrete on-hit procs (elemental/magical weapon
  effects triggered on a successful hit).
- **`DefenseScripts.cs`** — on-hit scripts that trigger from the defender's side (e.g.
  reactive/retaliation effects, damage reduction procs).
- **`KnightlyStrikeHitScript`** — the on-hit effect tied to the Grail Knight/Knight career
  "Knightly Strike" ability (see `AbilitySystem/Scripts/KnightlyStrikeScript`).

Dispatched via **`WeaponHitScriptsMissionLogic`** (root `Items/` folder,
`: MissionLogic`), added in `SubModule.OnMissionBehaviorInitialize`, which listens for hit
events and looks up the attacker's weapon's `ItemTrait.OnWeaponHitScript` to invoke.
