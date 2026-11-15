using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;

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

        /// <summary>Base cap on troops the ambusher side spawns upfront,
        /// before the Tactics bonus. A tighter strike team: fewer bodies
        /// in the opening wave than the column they hit.</summary>
        public virtual int AmbusherInMissionBaseCap => 25;

        /// <summary>Base cap on troops the victim side spawns upfront,
        /// before the Tactics bonus. Larger than the ambusher cap so the
        /// column visibly outnumbers the strike team at first contact;
        /// the ambusher advantage is tempo and positioning, not raw count.</summary>
        public virtual int VictimInMissionBaseCap => 40;

        /// <summary>Per-point-of-Tactics bonus to the ambusher cap.
        /// 0.1 → every 10 Tactics adds one troop. At 300 Tactics that
        /// is +30 troops on top of <see cref="AmbusherInMissionBaseCap"/>.
        /// Smaller than the victim's bonus — an ambusher is constrained
        /// by the terrain they chose and the need to stay hidden; extra
        /// Tactics tips the stealth setup, not the on-field count.</summary>
        public virtual float AmbusherInMissionPerTacticsBonus => 0.1f;

        /// <summary>Per-point-of-Tactics bonus to the victim cap.
        /// 0.2 → every 5 Tactics adds one troop. At 300 Tactics that
        /// is +60 troops on top of <see cref="VictimInMissionBaseCap"/>.
        /// Larger than the ambusher's bonus: a well-drilled column
        /// leader rallies the march formation on contact and gets
        /// more bodies into the fight, so Tactics on the defender
        /// side pays in sheer response size.</summary>
        public virtual float VictimInMissionPerTacticsBonus => 0.2f;

        /// <summary>Resolves the ambusher-side cap. Adds
        /// <see cref="AmbusherInMissionPerTacticsBonus"/> per Tactics
        /// point to <see cref="AmbusherInMissionBaseCap"/>. Leaderless
        /// (rare on the striker side: typically a player or lord party)
        /// returns the flat base. Ambusher excess beyond the cap is
        /// held in reserve and does not deploy — one-shot commitment.</summary>
        public virtual int ComputeAmbusherInMissionCap(Hero leader)
        {
            if (leader == null) return AmbusherInMissionBaseCap;
            int tactics = leader.GetSkillValue(DefaultSkills.Tactics);
            int cap = AmbusherInMissionBaseCap + (int)(tactics * AmbusherInMissionPerTacticsBonus);
            // Wolf Pack primary: party-leader roguery perk adds a flat
            // slot count on top of the Tactics bonus (PrimaryBonus = 5).
            if (leader.GetPerkValue(TORPerks.Roguery.WolfPack))
            {
                cap += (int)TORPerks.Roguery.WolfPack.PrimaryBonus;
            }
            return cap;
        }

        /// <summary>Resolves the victim-side cap. Adds
        /// <see cref="VictimInMissionPerTacticsBonus"/> per Tactics
        /// point to <see cref="VictimInMissionBaseCap"/>. Leaderless
        /// parties (caravans, patrols) get the flat base. Victim
        /// excess beyond the cap feeds the reinforcement wave pool
        /// owned by <c>AmbushReinforcementMissionLogic</c>.</summary>
        public virtual int ComputeVictimInMissionCap(Hero leader)
        {
            if (leader == null) return VictimInMissionBaseCap;
            int tactics = leader.GetSkillValue(DefaultSkills.Tactics);
            return VictimInMissionBaseCap + (int)(tactics * VictimInMissionPerTacticsBonus);
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
        /// 0.2 → 20% per wave: 30 troops/wave against a 150-strong lord
        /// party, 60 against a 300-strong army. Scales naturally with
        /// target size. Combined with <see cref="ReinforcementBatchFloor"/>
        /// so very small parties still send meaningful waves instead of
        /// trickling in a single troop at a time.</summary>
        public virtual float ReinforcementBatchFraction => 0.2f;

        /// <summary>Minimum troops per reinforcement wave. If the
        /// fractional computation would yield fewer, we bump up to
        /// this floor. If fewer than this many troops remain in the
        /// pool, the final wave sends whatever's left. At the current
        /// 20% fraction this takes over for rosters ≤ 125.</summary>
        public virtual int ReinforcementBatchFloor => 25;

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

        /// <summary>Ranged class floor for AI-side chunking: formations
        /// with this count or fewer stay as a single unit. Ranged splits
        /// aggressively because concentrated bowfire is punishing; small
        /// clumps that would fall below this size never get broken up.</summary>
        public virtual int MinFormationSizeRanged => 5;

        /// <summary>Ranged class ceiling for AI-side chunking: at or
        /// above this count a formation always splits across matching
        /// spots (if more than one is available). Between min and max the
        /// split chance ramps linearly.</summary>
        public virtual int MaxFormationSizeRanged => 20;

        /// <summary>Melee class floor for AI-side chunking: melee prefers
        /// to stay cohesive, so the floor is higher than the ranged one.
        /// Below this, keep the shield-wall together.</summary>
        public virtual int MinFormationSizeMelee => 15;

        /// <summary>Melee class ceiling for AI-side chunking: at or
        /// above this count a formation always splits across matching
        /// spots.</summary>
        public virtual int MaxFormationSizeMelee => 40;

        /// <summary>Resolves the min-formation-size floor for a given
        /// formation class. Ranged classes (bow, horse archer, skirmisher)
        /// use <see cref="MinFormationSizeRanged"/>; everything else uses
        /// <see cref="MinFormationSizeMelee"/>.</summary>
        public virtual int GetMinFormationSize(FormationClass cls)
            => IsRangedClass(cls) ? MinFormationSizeRanged : MinFormationSizeMelee;

        /// <summary>Resolves the max-formation-size ceiling for a given
        /// formation class. Pair with <see cref="GetMinFormationSize"/>
        /// to build the split-probability ramp.</summary>
        public virtual int GetMaxFormationSize(FormationClass cls)
            => IsRangedClass(cls) ? MaxFormationSizeRanged : MaxFormationSizeMelee;

        private static bool IsRangedClass(FormationClass cls)
            => cls == FormationClass.Ranged
            || cls == FormationClass.HorseArcher
            || cls == FormationClass.Skirmisher;
    }
}
