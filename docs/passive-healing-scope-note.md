# Passive healing — scope note

Exploratory. Nothing decided, nothing built. Campaign map only; heroes only.

## Scope

| | In | Out |
|---|---|---|
| Who | Heroes — player, companions, lords | Regular troops |
| Where | Campaign map daily healing | In-mission regeneration |
| Shape | Percentage of max HP, leaning; flat as the exception | — |

"Characters" here means `Hero`, matching the Player / Companion / Lord categories in
`TORCharacterStatsModel`. Troops are deliberately excluded for now — see the open question.

## Where it would attach

Two hooks on the hero side, both in `Models/TORPartyHealingModel.cs`:

| Hook | Line | Granularity | Notes |
|---|---|---|---|
| `GetDailyHealingHpForHeroes` | 198 | Per party | Where blessings, career passives and the Asrai harmony debuff already land |
| `GetHeroesEffectedHealingAmount` | 334 | **Per hero** | Where the equipment `HealthRegen` trait lands; already does stochastic rounding of fractional rates |

`GetHeroesEffectedHealingAmount` is the better fit for anything keyed to the individual hero —
it takes the `Hero` directly and already tolerates fractional values, so a percentage needs no
new rounding logic.

## The main-party gate

Every hero is healed on the campaign map, AI lords included — `Hero.HitPoints` is persistent
vanilla state, and TOR already writes it for non-player heroes (cursed-region damage at
`TORCustomSettlementCampaignBehavior.cs:776`, Orion's full-heal at `OrionCampaignBehavior.cs:943`).
The question is only which heroes get the *new* term.

The two hooks are gated differently, and this decides the implementation cost:

- `GetDailyHealingHpForHeroes` returns at line 231 for anything that isn't `MobileParty.MainParty`
  — handing back `base.…`, i.e. vanilla healing, not zero. One exception above it: a vampire
  leader of a non-main lord party gets +20%. A new term here reaches only the main party unless
  it sits above that return.
- `GetHeroesEffectedHealingAmount` is **not** gated. It has no TOR call site, so vanilla invokes
  it for every hero it heals, lords included; only the equipment clause *inside* it is
  main-party-gated (line 338). A new term here reaches all heroes for free — just don't wrap it
  in that same check.

| Category | Gets TOR bonuses today | Should passive healing reach them? |
|---|---|---|
| Player | Yes | Presumably yes |
| Companion (main party) | Yes | Presumably yes |
| Lord (AI, own party) | Only the vampire +20% | **Undecided** — but free to include via the per-hero hook |

Cadence differs and matters for balancing: per the remark at line 331, AI parties heal once per
day (or four times on the quarter-daily tick) while the main party heals hourly. The stochastic
rounding at line 346 is unbiased either way, so per-day totals should match; the variance does not.

## Why percentage rather than flat, for this scope

Daily healing is roughly flat HP/day, so a 550 HP troll-blooded hero takes ~6.5× longer to go from
near-death to full than an 85 HP one. A flat "natural healing" stat shifts every hero by the same
constant and leaves that ratio untouched. A percentage tracks the ~36× max-HP spread the mod
already has.

The objection to percentages raised earlier — the `(int)` truncation at
`StatusEffectComponent.cs:171` silently zeroing sub-1-HP ticks — is an **in-mission** problem only.
It does not apply here.

## Still undecided

1. Key it off the existing `Regeneration` / `Regeneration2` / `Regeneration3` attributes, giving
   them a campaign meaning they currently lack (no new vocabulary, less plumbing), or off a new
   independent stat?
2. Percentage value(s), and whether there's a cap.
3. Does it stack with the Asrai harmony debuff and the Wardancer symbol, or is it applied before
   them?

## Open question — troops

**Should regular troops get the same treatment?** Deliberately out of scope for now.

Two things to weigh when this comes back:

- The troop side has **no per-character hook**. `GetDailyHealingForRegulars` (line 149) computes
  one party-wide number; the only per-troop granularity in it is the Shallya Seal and rune loops,
  which walk the roster accumulating `value × troop.Number` (lines 284 and 307). A per-race or
  per-attribute healing term would be a third loop of that shape.
- The oddity that motivates this is most visible on troops, not heroes: a troll troop with
  `Regeneration3` heals 12 HP/sec in battle and then recovers on the map at the same daily rate as
  a goblin. Fixing heroes alone leaves that untouched.
