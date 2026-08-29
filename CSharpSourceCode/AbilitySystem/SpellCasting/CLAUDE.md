# AbilitySystem/SpellCasting

Single file: **`SpellCastSession`** — an accounting object for one ability cast, created by
`AbilityManagerMissionLogic.CreateSpellSession` when an `AbilityScript` first triggers and
collected once its effects (including any lingering `StatusEffect` durations) have played
out. Books damage/healing/kills/status-effects dealt by that cast, split into
friendly-fire vs. enemy buckets, and exposes totals (`TotalDamageDealt`,
`AgentsKilledCount`, `HasData`, etc.) used for post-cast feedback (kill credit, XP,
combat log/HUD numbers). Not persisted — purely an in-mission bookkeeping struct keyed by
`CastID`.
