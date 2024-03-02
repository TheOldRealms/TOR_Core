
using System.Collections.Generic;
using TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.GenericConditions;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.Alliance
{
    class FormAllianceConditions : AbstractConditionEvaluator<FormAllianceConditions>
    {
        private static readonly List<IDiplomacyCondition> Conditions = new List<IDiplomacyCondition>()
        {
            new AlliancesEnabledCondition(),
            new AtPeaceCondition(),
            new TimeElapsedSinceLastWarCondition(),
            new NotInAllianceCondition(),
            new HasEnoughInfluenceCondition(),
            new HasEnoughGoldCondition(),
            new HasEnoughScoreCondition(),
            new NotEliminatedCondition(),
            new ReligiouslyAlignedCondition()
        };

        public FormAllianceConditions() : base(Conditions) { }
    }
}