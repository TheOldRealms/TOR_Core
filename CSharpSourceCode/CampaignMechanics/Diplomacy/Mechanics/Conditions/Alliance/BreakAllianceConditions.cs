using System.Collections.Generic;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.Alliance
{
    class BreakAllianceConditions : AbstractConditionEvaluator<BreakAllianceConditions>
    {
        private static readonly List<IDiplomacyCondition> Conditions = new List<IDiplomacyCondition>()
        {
            new TimeElapsedSinceAllianceFormedCondition()
        };
        public BreakAllianceConditions() : base(Conditions) { }
    }
}