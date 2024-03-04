using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring.Alliance.Factors
{
    public class PlayerRelationshipWithFactionLeader : IDiplomacyScoreFactor
    {
        protected static readonly TextObject _TPlayerIsLeader = new TextObject("{=!}The player is kingdom leader");
        protected static readonly TextObject _TPlayerToLeader = new TextObject("{=!}Player's relationship to kingdom leader");
        public void ApplyFactor(ref ExplainedNumber result, Kingdom ourKingdom, Kingdom otherKingdom, IDiplomacyScores scores)
        {
            if (Hero.MainHero == ourKingdom.Leader || Hero.MainHero == otherKingdom.Leader)
            {
                result.Add(scores.RelationToLeader, _TPlayerIsLeader);
            }
            else
            {
                if (Hero.MainHero.Clan.Kingdom.Leader != null)
                {
                    var relationMult = MBMath.ClampFloat((float)Math.Log((Hero.MainHero.Clan.Kingdom.Leader.GetRelationWithPlayer() + 100f) / 100f, 1.5), -1f, +1f);
                    result.Add(scores.Relationship * relationMult, _TPlayerToLeader);
                }
            }
        }
    }
}
