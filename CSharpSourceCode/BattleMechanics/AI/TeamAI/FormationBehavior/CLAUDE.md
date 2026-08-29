# BattleMechanics/AI/TeamAI/FormationBehavior

TOR subclasses of vanilla `BehaviorComponent`s, registered wherever formations pick their
AI behavior set (culture settings in `../../TORCultureBattleSettings` feed into this):

- **`TORBehaviorBase`** (abstract) — shared base for the overrides below.
- **`TORBehaviorCharge`** (`: BehaviorCharge`), **`TORBehaviorDefend`** (`: BehaviorDefend`),
  **`TORBehaviorRetreat`** (`: BehaviorRetreat`), **`TORBehaviorSkirmish`**
  (`: BehaviorSkirmish`), **`TORBehaviorAggressiveMelee`** (`: BehaviorComponent`) — tuned/
  patched versions of the matching vanilla formation behavior.
- **`TORBehaviorProtectArtillery`** (`: BehaviorComponent`) — new behavior: keeps a
  formation screening friendly artillery pieces.
- **`TORFormationClass`** — formation classification helper used to pick behaviors.
