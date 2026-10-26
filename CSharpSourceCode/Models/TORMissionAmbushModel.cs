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
        /// 30 against a 300-strong army. Scales naturally with target size.</summary>
        public virtual float ReinforcementBatchFraction => 0.1f;

        /// <summary>Player side reinforcements per wave. Default 0 — the ambush
        /// is a one-shot commitment of the force the player strikes with.</summary>
        public virtual float PlayerReinforcementBatchFraction => 0f;

        // ---- Retreat zone ----

        /// <summary>Seconds the main agent must stay inside a retreat zone for
        /// the extraction to register. Prevents accidental retreat from the
        /// player running past the zone chasing a kill.</summary>
        public virtual float ZoneHoldDuration => 3f;

        /// <summary>Radius of the procedural fallback zone (used when the scene
        /// has no scener-authored <c>AmbushRetreatZone</c>). Slightly generous
        /// vs the authored default because there's no visual aura.</summary>
        public virtual float ProceduralFallbackRadius => 15f;
    }
}
