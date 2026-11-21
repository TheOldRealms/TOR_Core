using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models.Ambush
{
    /// <summary>
    /// Mission-side tuning knobs for the ambush: reinforcement cadence, battle-size split, deploy advantage, retreat zones.
    /// </summary>
    public class TORMissionAmbushModel : GameModel
    {

        // Tunable knobs. Overridable via subclass for balance mods.
        public virtual float AiActivationDelay => 8f;
        public virtual float TargetTotalTroopRatio => 0.25f;
        public virtual int AmbusherInMissionBaseCap => 25;
        public virtual int VictimInMissionBaseCap => 40;
        public virtual float AmbusherInMissionPerTacticsBonus => 0.1f;
        public virtual float VictimInMissionPerTacticsBonus => 0.2f;
        public virtual float SteadyWaveInterval => 15f;
        public virtual float ReinforcementBatchFraction => 0.2f;
        public virtual int ReinforcementBatchFloor => 25;
        public virtual float ReinforcementIntervalTacticsPerPointReduction => 0.002f;
        public virtual float MinReinforcementIntervalMultiplier => 0.3f;
        public virtual float PlayerReinforcementBatchFraction => 0f;
        public virtual float InitialPatrolClearedWaveDelayBonus => 0.3f;
        public virtual float ExtractionBarSeconds => 10f;
        public virtual float LeftBehindWarningThreshold => 0.5f;
        public virtual float ProceduralFallbackRadius => 15f;
        public virtual float StalemateMinutes => 10f;
        public virtual float TacticsPerAmbushTroopBonus => 0.1f;
        public virtual float TacticsReinforcementSpeedup => 0.002f;
        public virtual float TacticsReinforcementSpeedupFloor => 0.25f;
        public virtual int LoudnessEventThreshold => 1;
        public virtual float LoudnessPenaltyHours => 6f;
        public virtual float AmbushedFledCountdownSeconds => 10f;
        public virtual int PlayerChunkThreshold => 25;
        public virtual int PlayerMaxChunksPerClass => 2;
        public virtual int MinFormationSizeRanged => 5;
        public virtual int MaxFormationSizeRanged => 20;
        public virtual int MinFormationSizeMelee => 15;
        public virtual int MaxFormationSizeMelee => 40;

        /// <summary>
        /// Resolves the ambusher-side cap from leader Tactics. Leaderless returns the flat base.
        /// </summary>
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

        /// <summary>
        /// Resolves the victim-side cap from leader Tactics. Leaderless parties return the flat base.
        /// </summary>
        public virtual int ComputeVictimInMissionCap(Hero leader)
        {
            if (leader == null) return VictimInMissionBaseCap;
            int tactics = leader.GetSkillValue(DefaultSkills.Tactics);
            return VictimInMissionBaseCap + (int)(tactics * VictimInMissionPerTacticsBonus);
        }

        /// <summary>
        /// Enemy reinforcement wave intervals in seconds. Past the end of this array the schedule plateaus at <see cref="SteadyWaveInterval"/>.
        /// </summary>
        public virtual float[] InitialWaveIntervals => new[] { 45f, 30f, 15f, 15f };

        /// <summary>
        /// Resolves the interval multiplier for the ambushed side's reinforcement waves from the leader's Tactics.
        /// </summary>
        public virtual float GetReinforcementIntervalMultiplier(Hero leader)
        {
            if (leader == null) return 1f;
            int tactics = leader.GetSkillValue(DefaultSkills.Tactics);
            float mult = 1f - tactics * ReinforcementIntervalTacticsPerPointReduction;
            if (mult < MinReinforcementIntervalMultiplier) mult = MinReinforcementIntervalMultiplier;
            return mult;
        }

        /// <summary>
        /// Resolves the min-formation-size floor for a given formation class.
        /// </summary>
        public virtual int GetMinFormationSize(FormationClass cls)
            => IsRangedClass(cls) ? MinFormationSizeRanged : MinFormationSizeMelee;

        /// <summary>
        /// Resolves the max-formation-size ceiling for a given formation class.
        /// </summary>
        public virtual int GetMaxFormationSize(FormationClass cls)
            => IsRangedClass(cls) ? MaxFormationSizeRanged : MaxFormationSizeMelee;

        /// <summary>
        /// True when the formation class fires projectiles.
        /// </summary>
        private static bool IsRangedClass(FormationClass cls)
            => cls == FormationClass.Ranged
            || cls == FormationClass.HorseArcher
            || cls == FormationClass.Skirmisher;
    }
}
