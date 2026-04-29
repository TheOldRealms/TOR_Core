using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.AI.TeamAI.FormationBehavior
{
    /// <summary>
    /// Culture-aware skirmish behavior. Applies culture-specific skirmish weight multipliers.
    /// </summary>
    public class TORBehaviorSkirmish : BehaviorSkirmish
    {
        public TORBehaviorSkirmish(Formation formation) : base(formation)
        {
        }

        protected override float GetAiWeight()
        {
            float baseWeight = base.GetAiWeight();

            if (baseWeight <= 0f)
                return baseWeight;

            var culture = TORCultureBattleSettings.GetTeamCulture(Formation.Team);
            var personality = TORCultureBattleSettings.GetPersonality(culture);

            return baseWeight * personality.SkirmishWeightMultiplier;
        }
    }
}
