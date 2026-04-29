using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.AI.TeamAI.FormationBehavior
{
    /// <summary>
    /// Culture-aware retreat behavior. Applies culture-specific retreat resistance.
    /// Higher retreat resistance = lower retreat weight = less likely to retreat.
    /// </summary>
    public class TORBehaviorRetreat : BehaviorRetreat
    {
        public TORBehaviorRetreat(Formation formation) : base(formation)
        {
        }

        protected override float GetAiWeight()
        {
            float baseWeight = base.GetAiWeight();

            if (baseWeight <= 0f)
                return baseWeight;

            var culture = TORCultureBattleSettings.GetTeamCulture(Formation.Team);
            var personality = TORCultureBattleSettings.GetPersonality(culture);

            // Divide by retreat resistance - higher resistance = lower weight = less retreat
            return baseWeight / personality.RetreatResistance;
        }
    }
}
