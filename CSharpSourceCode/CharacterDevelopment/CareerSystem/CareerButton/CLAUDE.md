# CharacterDevelopment/CareerSystem/CareerButton

Right-click "special action" buttons that some careers add to the party-roster/prisoner
screen (e.g. sacrificing a prisoner, converting a troop, using a career resource on a unit)
— distinct from the in-mission `CareerAbility`.

- **`CareerButtonBehaviorBase`** (abstract, ctor takes the owning `CareerObject`) — contract:
  `ButtonClickedEvent`, `ShouldButtonBeVisible`, `ShouldButtonBeActive` (+ disable reason
  text), `CareerButtonIcon`; registers itself with
  `SpecialbuttonEventManagerHandler.Instance` via `Register()`.
- **`SpecialbuttonHandler.cs`** — **`SpecialbuttonEventManagerHandler`** (singleton) — the
  registry/dispatcher all button behaviors register into; routed to from wherever the
  roster screen fires its click event (see `HarmonyPatches`/`Extensions/UI`).
- **`CareerButtons`** — static list/lookup of all registered button behaviors.
- **`GreenskinCareerButton`** (abstract `: CareerButtonBehaviorBase`) — shared base for
  Greenskin careers' buttons:
  - **`OrcBossCareerButton`**, **`OrcShamanCareerButton`**.
- Per-career concrete behaviors: **`BlackGrailKnightCareerButtonBehavior`**,
  **`GrailKnightCareerButtonBehavior`** (+ `KnightPuritySeal` in
  `KnightOldWorldCareerButtonBehavior.cs`... see below), **`ImperialMagisterCareerButtonBehavior`**
  (+ `PowerstoneHelper`/`PowerStone` — a resource-stone mechanic), **`IronbreakerCareerButton`**
  (→ `IronbreakerCareerButtonBehavior`), **`KnightOldWorldCareerButtonBehavior`**
  (+ `KnightPuritySeal`), **`MercenaryCareerButtonBehavior`**, **`NecrarchCareerButtonBehavior`**,
  **`RunelordCareerButtonBehavior`** (+ `UnitRune`), **`SlayerCareerButtonBehavior`**,
  **`WaywatcherCareerButtonBehavior`** (+ `ArrowType`), **`WitchHunterCareerButtonBehavior`**.
- **`CareerButtonHelper`** (static) — shared query/action helpers used across the concrete
  behaviors above.
