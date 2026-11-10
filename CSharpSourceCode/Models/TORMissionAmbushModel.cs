using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace TOR_Core.Models
{
    /// <summary>
    /// Mission-side tuning knobs for the ambush — reinforcement cadence, battle-
    /// size split, deploy-advantage window, retreat-zone timing. Separate from
    /// <see cref="TORAmbushModel"/> which handles the campaign-side success
    /// calculator + detection rolls. All <c>virtual</c> so mod subclasses can
    /// rebalance individual knobs without touching the mission code.
    /// </summary>
    public class TORMissionAmbushModel : GameModel
    {
        // ---- Deploy advantage ----

        /// <summary>Seconds the enemy AI stands still at mission start so the
        /// player gets the alpha strike. Lifted from vanilla sally-out's
        /// BesiegerActivationDuration.</summary>
        public virtual float AiActivationDelay => 8f;

        // ---- Battle-size balance ----

        /// <summary>Portion of battle-size budget allocated to the ambush
        /// target. Vanilla sally-out: 0.25 (25/75 split). Smaller means a tighter
        /// ambush — fewer bodies on screen at once.</summary>
        public virtual float TargetTotalTroopRatio => 0.25f;

        /// <summary>Base cap on troops that spawn upfront per side,
        /// before the Tactics bonus. See <see cref="ComputeInitialOnFieldCap"/>
        /// for the full formula.</summary>
        public virtual int InitialOnFieldBaseCap => 25;

        /// <summary>Per-point-of-Tactics bonus to the initial on-field
        /// cap. 0.2 → every 5 Tactics adds one troop. At 300 Tactics
        /// the cap resolves to 85.</summary>
        public virtual float InitialOnFieldPerTacticsBonus => 0.2f;

        /// <summary>Resolves the initial on-field cap for a given
        /// leader hero. Reads the hero's Tactics skill and adds
        /// <see cref="InitialOnFieldPerTacticsBonus"/> per point to
        /// <see cref="InitialOnFieldBaseCap"/>. Leaderless parties
        /// (caravans, some patrols) get the flat base cap — no hero
        /// to pull a Tactics value from. Used by both the ambusher-
        /// side spawn (how many troops lie in wait) and the
        /// ambushed-side spawn (how many troops are on the column
        /// when the strike lands). Rest of each side's roster:
        /// ambusher's excess is simply not deployed (one-shot
        /// commitment, post-MVP may add waves); ambushed's excess
        /// feeds the reinforcement pool.</summary>
        public virtual int ComputeInitialOnFieldCap(Hero leader)
        {
            if (leader == null) return InitialOnFieldBaseCap;
            int tactics = leader.GetSkillValue(DefaultSkills.Tactics);
            return InitialOnFieldBaseCap + (int)(tactics * InitialOnFieldPerTacticsBonus);
        }

        // ---- Reinforcement cadence ----

        /// <summary>Enemy reinforcement wave intervals (seconds between
        /// successive waves). Index 0 = time until first wave. Once past the
        /// end of this array the schedule plateaus at <see cref="SteadyWaveInterval"/>.
        /// Default: 45s / 30s / 15s / 15s then 15s steady → waves at
        /// 0:45 / 1:15 / 1:30 / 1:45 / 2:00 / 2:15 / ...</summary>
        public virtual float[] InitialWaveIntervals => new[] { 45f, 30f, 15f, 15f };

        /// <summary>Enemy reinforcement cadence once past the initial ramp.</summary>
        public virtual float SteadyWaveInterval => 15f;

        /// <summary>Each enemy wave is this fraction of their total roster.
        /// 0.1 → 10% per wave: 15 troops/wave against a 150-strong lord party,
        /// 30 against a 300-strong army. Scales naturally with target size.
        /// Combined with <see cref="ReinforcementBatchFloor"/> so very small
        /// parties still send meaningful waves instead of trickling in a
        /// single troop at a time.</summary>
        public virtual float ReinforcementBatchFraction => 0.1f;

        /// <summary>Minimum troops per reinforcement wave. If the
        /// fractional computation would yield fewer, we bump up to
        /// this floor. If fewer than this many troops remain in the
        /// pool, the final wave sends whatever's left.</summary>
        public virtual int ReinforcementBatchFloor => 5;

        /// <summary>Per-point-of-Tactics speed-up on the ambushed
        /// side's reinforcement intervals. 0.002 → 0.2% faster per
        /// point, so 300 Tactics would push the multiplier down to
        /// 0.4 (60% faster waves). Clamped by
        /// <see cref="MinReinforcementIntervalMultiplier"/>.</summary>
        public virtual float ReinforcementIntervalTacticsPerPointReduction => 0.002f;

        /// <summary>Floor on the Tactics interval multiplier — even
        /// a max-Tactics leader can't make waves come faster than
        /// this fraction of the base interval. Prevents degenerate
        /// "waves every 2 seconds" scenarios at extreme skill
        /// values.</summary>
        public virtual float MinReinforcementIntervalMultiplier => 0.3f;

        /// <summary>Resolves the interval multiplier for the
        /// ambushed side's reinforcement waves from the side leader's
        /// Tactics. Applied once at
        /// <c>AmbushReinforcementMissionLogic.AfterStart</c> to each
        /// of <see cref="InitialWaveIntervals"/> and
        /// <see cref="SteadyWaveInterval"/>. Leaderless parties
        /// return 1.0 (base intervals, no bonus).</summary>
        public virtual float GetReinforcementIntervalMultiplier(Hero leader)
        {
            if (leader == null) return 1f;
            int tactics = leader.GetSkillValue(DefaultSkills.Tactics);
            float mult = 1f - tactics * ReinforcementIntervalTacticsPerPointReduction;
            if (mult < MinReinforcementIntervalMultiplier) mult = MinReinforcementIntervalMultiplier;
            return mult;
        }

        /// <summary>Player side reinforcements per wave. Default 0 — the ambush
        /// is a one-shot commitment of the force the player strikes with.</summary>
        public virtual float PlayerReinforcementBatchFraction => 0f;

        /// <summary>
        /// Fractional bonus to the current reinforcement timer when the player
        /// kills the entire initial patrol while the next-wave timer is still
        /// ticking. 0.3 → "next wave was 20s away, now it's 26s away." One-shot
        /// per mission. Rewards fast Phase 1 clears with a regroup window.
        /// </summary>
        public virtual float InitialPatrolClearedWaveDelayBonus => 0.3f;

        // ---- Retreat / extraction ----

        /// <summary>Seconds the hero must stay inside a retreat zone to fill
        /// the extraction bar. Design calls for 10s continuous.</summary>
        public virtual float ExtractionBarSeconds => 10f;

        /// <summary>Progress threshold (0..1) at which the "troops left behind
        /// will be wounded" warning fires once per mission.</summary>
        public virtual float LeftBehindWarningThreshold => 0.5f;

        /// <summary>Radius of the procedural fallback zone (used when the scene
        /// has no scener-authored <c>AmbushRetreatZone</c>). Slightly generous
        /// vs the authored default because there's no visual aura.</summary>
        public virtual float ProceduralFallbackRadius => 15f;

        // ---- Stalemate ----

        /// <summary>Real-time minutes before an in-progress ambush where the
        /// initial patrol is still alive is force-ended as Stalemate (defeat).
        /// Safety net for navmesh / geometry bugs that leave the enemy unable
        /// to reach the player.</summary>
        public virtual float StalemateMinutes => 10f;

        // ---- Tactics skill scaling ----

        /// <summary>
        /// Per-skill-point bonus to the player's initial troop cap in an
        /// ambush. 0.1 → +30 troops at Tactics 300. Applied on top of the
        /// battle-size budget split.
        /// </summary>
        public virtual float TacticsPerAmbushTroopBonus => 0.1f;

        /// <summary>
        /// Per-skill-point reduction of the player's reinforcement wave
        /// interval when the player is being ambushed (Phase F). Multiplicative
        /// shrink: 0.002 × 300 Tactics = 60% reduction → 40% of base. Floored
        /// by <see cref="TacticsReinforcementSpeedupFloor"/>.
        /// </summary>
        public virtual float TacticsReinforcementSpeedup => 0.002f;

        /// <summary>
        /// Minimum fraction of the base wave interval. Even an infinite Tactics
        /// skill can't shrink intervals below this fraction.
        /// </summary>
        public virtual float TacticsReinforcementSpeedupFloor => 0.25f;

        // ---- Loudness ----

        /// <summary>
        /// How many player-side loudness events (gunpowder shots + non-Shadow
        /// magic casts) tip a mission from "quiet" to "loud" at mission end.
        /// Loud missions get the full extended cooldown; quiet TotalVictory
        /// skips the cooldown entirely. See campaign-stealth-design.md § 6b.
        /// </summary>
        public virtual int LoudnessEventThreshold => 1;

        /// <summary>
        /// Extra hours added to <see cref="AmbushCampaignBehavior.CooldownHours"/>
        /// when a mission ends classified as loud. Default +6h → effective
        /// cooldown doubles when gunpowder / magic was involved.
        /// </summary>
        public virtual float LoudnessPenaltyHours => 6f;

        // ---- Phase F (player gets ambushed) ----

        /// <summary>
        /// How long the player must stay outside the soft mission boundary
        /// before the Phase F mission resolves as <c>AmbushedFled</c>. The
        /// countdown resets to 0 the moment the player re-enters the boundary.
        /// See ai-ambushes-player-design.md § 3 row 4.
        /// </summary>
        public virtual float AmbushedFledCountdownSeconds => 10f;

        // ---- Formation auto-chunking ----

        /// <summary>
        /// Troop-count threshold above which a single-class formation gets
        /// split in player mode. 60 infantry crosses the threshold and
        /// becomes two 30-troop formations; 20 infantry stays as one.
        /// </summary>
        public virtual int PlayerChunkThreshold => 25;

        /// <summary>
        /// Maximum formations per <c>FormationClass</c> in player mode. Keeps
        /// the deployment sidebar legible: a 240-infantry roster becomes two
        /// 120-troop formations, not ten 24-troop formations.
        /// </summary>
        public virtual int PlayerMaxChunksPerClass => 2;

        /// <summary>
        /// Floor size per auto-chunked formation in AI mode. Prevents the
        /// spread-to-fill logic from producing tiny formations on small AI
        /// rosters: 20 archers across 2 archer spots stays as one formation
        /// of 20, not two of 10.
        /// </summary>
        public virtual int MinAiFormationSize => 15;

        /// <summary>Weight for the Concentrated AI distribution pattern
        /// (all troops of a class in one spot, other matching spots empty).
        /// Relative to <see cref="AiDistributionEvenWeight"/> and
        /// <see cref="AiDistributionAsymmetricWeight"/>.</summary>
        public virtual float AiDistributionConcentratedWeight => 0.3f;

        /// <summary>Weight for the Even AI distribution pattern (equal
        /// split across matching spots).</summary>
        public virtual float AiDistributionEvenWeight => 0.5f;

        /// <summary>Weight for the Asymmetric AI distribution pattern
        /// (skewed split like 25/75 across matching spots).</summary>
        public virtual float AiDistributionAsymmetricWeight => 0.2f;
    }
}
