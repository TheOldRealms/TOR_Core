using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    /// <summary>Mission-side tuning knobs for the ambush: reinforcement cadence, battle-size split, deploy advantage, retreat zones.</summary>
    public class TORMissionAmbushModel : GameModel
    {
        // Deploy advantage.

        /// <summary>Seconds the enemy AI stands still at mission start so the player gets the alpha strike.</summary>
        public virtual float AiActivationDelay => 8f;

        // Battle-size balance.

        /// <summary>Portion of battle-size budget allocated to the ambush target. Vanilla sally-out uses 0.25.</summary>
        public virtual float TargetTotalTroopRatio => 0.25f;

        /// <summary>Base cap on troops the ambusher side spawns upfront, before the Tactics bonus.</summary>
        public virtual int AmbusherInMissionBaseCap => 25;

        /// <summary>Base cap on troops the victim side spawns upfront, before the Tactics bonus.</summary>
        public virtual int VictimInMissionBaseCap => 40;

        /// <summary>Per-point-of-Tactics bonus to the ambusher cap.</summary>
        public virtual float AmbusherInMissionPerTacticsBonus => 0.1f;

        /// <summary>Per-point-of-Tactics bonus to the victim cap.</summary>
        public virtual float VictimInMissionPerTacticsBonus => 0.2f;

        /// <summary>Resolves the ambusher-side cap from leader Tactics. Leaderless returns the flat base.</summary>
        public virtual int ComputeAmbusherInMissionCap(Hero leader)
        {
            if (leader == null) return AmbusherInMissionBaseCap;
            int tactics = leader.GetSkillValue(DefaultSkills.Tactics);
            int cap = AmbusherInMissionBaseCap + (int)(tactics * AmbusherInMissionPerTacticsBonus);
            // Wolf Pack perk: flat slot bonus on top of Tactics.
            if (leader.GetPerkValue(TORPerks.Roguery.WolfPack))
            {
                cap += (int)TORPerks.Roguery.WolfPack.PrimaryBonus;
            }
            return cap;
        }

        /// <summary>Resolves the victim-side cap from leader Tactics. Leaderless parties return the flat base.</summary>
        public virtual int ComputeVictimInMissionCap(Hero leader)
        {
            if (leader == null) return VictimInMissionBaseCap;
            int tactics = leader.GetSkillValue(DefaultSkills.Tactics);
            return VictimInMissionBaseCap + (int)(tactics * VictimInMissionPerTacticsBonus);
        }

        // Reinforcement cadence.

        /// <summary>Enemy reinforcement wave intervals in seconds. Past the end of this array the schedule plateaus at <see cref="SteadyWaveInterval"/>.</summary>
        public virtual float[] InitialWaveIntervals => new[] { 45f, 30f, 15f, 15f };

        /// <summary>Enemy reinforcement cadence once past the initial ramp.</summary>
        public virtual float SteadyWaveInterval => 15f;

        /// <summary>Fraction of the ambushed side's roster sent per wave.</summary>
        public virtual float ReinforcementBatchFraction => 0.2f;

        /// <summary>Minimum troops per reinforcement wave.</summary>
        public virtual int ReinforcementBatchFloor => 25;

        /// <summary>Per-point-of-Tactics speed-up on the ambushed side's reinforcement intervals.</summary>
        public virtual float ReinforcementIntervalTacticsPerPointReduction => 0.002f;

        /// <summary>Floor on the Tactics interval multiplier.</summary>
        public virtual float MinReinforcementIntervalMultiplier => 0.3f;

        /// <summary>Resolves the interval multiplier for the ambushed side's reinforcement waves from the leader's Tactics.</summary>
        public virtual float GetReinforcementIntervalMultiplier(Hero leader)
        {
            if (leader == null) return 1f;
            int tactics = leader.GetSkillValue(DefaultSkills.Tactics);
            float mult = 1f - tactics * ReinforcementIntervalTacticsPerPointReduction;
            if (mult < MinReinforcementIntervalMultiplier) mult = MinReinforcementIntervalMultiplier;
            return mult;
        }

        /// <summary>Player side reinforcements per wave. Default 0 (one-shot commitment).</summary>
        public virtual float PlayerReinforcementBatchFraction => 0f;

        /// <summary>Fractional bonus to the current reinforcement timer when the player clears the initial patrol early. One-shot per mission.</summary>
        public virtual float InitialPatrolClearedWaveDelayBonus => 0.3f;

        // Retreat and extraction.

        /// <summary>Continuous seconds inside a retreat zone needed to fill the extraction bar.</summary>
        public virtual float ExtractionBarSeconds => 10f;

        /// <summary>Progress threshold (0..1) at which the troops-left-behind warning fires once per mission.</summary>
        public virtual float LeftBehindWarningThreshold => 0.5f;

        /// <summary>Radius of the procedural fallback retreat zone when the scene has no authored one.</summary>
        public virtual float ProceduralFallbackRadius => 15f;

        // Stalemate.

        /// <summary>Real-time minutes before an in-progress ambush where the initial patrol is still alive is force-ended as Stalemate.</summary>
        public virtual float StalemateMinutes => 10f;

        // Tactics skill scaling.

        /// <summary>Per-skill-point bonus to the player's initial troop cap in an ambush.</summary>
        public virtual float TacticsPerAmbushTroopBonus => 0.1f;

        /// <summary>Per-skill-point reduction of the player's reinforcement wave interval when ambushed.</summary>
        public virtual float TacticsReinforcementSpeedup => 0.002f;

        /// <summary>Minimum fraction of the base wave interval.</summary>
        public virtual float TacticsReinforcementSpeedupFloor => 0.25f;

        // Loudness.

        /// <summary>How many player-side loudness events (gunpowder + non-Shadow magic) tip a mission from quiet to loud.</summary>
        public virtual int LoudnessEventThreshold => 1;

        /// <summary>Extra hours added to the base cooldown when a mission ends classified as loud.</summary>
        public virtual float LoudnessPenaltyHours => 6f;

        // Phase: player gets ambushed.

        /// <summary>Seconds the player must stay outside the soft mission boundary before resolving as AmbushedFled.</summary>
        public virtual float AmbushedFledCountdownSeconds => 10f;

        // Formation auto-chunking.

        /// <summary>Troop-count threshold above which a single-class formation gets split in player mode.</summary>
        public virtual int PlayerChunkThreshold => 25;

        /// <summary>Maximum formations per FormationClass in player mode.</summary>
        public virtual int PlayerMaxChunksPerClass => 2;

        /// <summary>Ranged class floor for AI-side chunking.</summary>
        public virtual int MinFormationSizeRanged => 5;

        /// <summary>Ranged class ceiling for AI-side chunking.</summary>
        public virtual int MaxFormationSizeRanged => 20;

        /// <summary>Melee class floor for AI-side chunking.</summary>
        public virtual int MinFormationSizeMelee => 15;

        /// <summary>Melee class ceiling for AI-side chunking.</summary>
        public virtual int MaxFormationSizeMelee => 40;

        /// <summary>Resolves the min-formation-size floor for a given formation class.</summary>
        public virtual int GetMinFormationSize(FormationClass cls)
            => IsRangedClass(cls) ? MinFormationSizeRanged : MinFormationSizeMelee;

        /// <summary>Resolves the max-formation-size ceiling for a given formation class.</summary>
        public virtual int GetMaxFormationSize(FormationClass cls)
            => IsRangedClass(cls) ? MaxFormationSizeRanged : MaxFormationSizeMelee;

        private static bool IsRangedClass(FormationClass cls)
            => cls == FormationClass.Ranged
            || cls == FormationClass.HorseArcher
            || cls == FormationClass.Skirmisher;
    }
}
