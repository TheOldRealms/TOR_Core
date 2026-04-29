using System;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.AI.TeamAI.FormationBehavior
{
    /// <summary>
    /// Culture-aware charge behavior. Applies culture-specific charge weight multipliers and minimum floor.
    /// </summary>
    public class TORBehaviorCharge : BehaviorCharge
    {
        public TORBehaviorCharge(Formation formation) : base(formation)
        {
        }

        protected override float GetAiWeight()
        {
            float baseWeight = base.GetAiWeight();

            var culture = TORCultureBattleSettings.GetTeamCulture(Formation.Team);
            var personality = TORCultureBattleSettings.GetPersonality(culture);

            // Apply multiplier to base weight
            float modifiedWeight = baseWeight * personality.ChargeWeightMultiplier;

            // Ensure minimum floor for aggressive cultures (if there's an enemy)
            if (Formation.CachedClosestEnemyFormation != null && personality.ChargeWeightMinimum > 0f)
            {
                modifiedWeight = Math.Max(modifiedWeight, personality.ChargeWeightMinimum);
            }

            return modifiedWeight;
        }
    }
}
