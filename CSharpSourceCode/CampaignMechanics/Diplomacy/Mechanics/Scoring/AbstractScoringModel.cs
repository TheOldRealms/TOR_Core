using System;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring
{
    internal abstract partial class AbstractScoringModel<T> where T : AbstractScoringModel<T>, new()
    {
        public static T Instance { get; } = new T();

        public virtual float ScoreThreshold { get; } = 100.0f;

        protected IDiplomacyScores Scores { get; }

        protected AbstractScoringModel(IDiplomacyScores scores) => Scores = scores;

        public virtual ExplainedNumber GetScore(Kingdom ourKingdom, Kingdom otherKingdom, bool includeDesc = false)
        {
            var explainedNum = new ExplainedNumber(Scores.Base, includeDesc);

            TextObject CreateTextWithKingdom(string text, Kingdom kingdom) => includeDesc
                ? new TextObject(text).SetTextVariable("KINGDOM", kingdom.Name)
                : null;

            // Weak Kingdom (Us)

            if (!ourKingdom.IsStrong())
                explainedNum.Add(Scores.BelowMedianStrength, _TWeakKingdom);

            // Common Enemies

            var commonEnemies = FactionManager.GetEnemyKingdoms(ourKingdom).Intersect(FactionManager.GetEnemyKingdoms(otherKingdom));

            foreach (var commonEnemy in commonEnemies)
                explainedNum.Add(Scores.HasCommonEnemy, CreateTextWithKingdom(SCommonEnemy, commonEnemy));

            // Their Alliances with Enemies

            var alliedEnemies = KingdomExtensions.AllActiveKingdoms
                .Where(k => k != ourKingdom
                         && k != otherKingdom
                         && FactionManager.IsAlliedWithFaction(otherKingdom, k)
                         && FactionManager.IsAtWarAgainstFaction(ourKingdom, k));

            foreach (var alliedEnemy in alliedEnemies)
                explainedNum.Add(Scores.ExistingAllianceWithEnemy, CreateTextWithKingdom(SAlliedToEnemy, alliedEnemy));

            // Their Alliances with Neutrals

            var alliedNeutrals = KingdomExtensions.AllActiveKingdoms
                .Where(k => k != ourKingdom
                         && k != otherKingdom
                         && FactionManager.IsAlliedWithFaction(otherKingdom, k)
                         && !FactionManager.IsAtWarAgainstFaction(ourKingdom, k));

            // FIXME: alliedNeutrals also includes common allies as it's coded... Should they be scored differently? Probable answer: YES!

            foreach (var alliedNeutral in alliedNeutrals)
                explainedNum.Add(Scores.ExistingAllianceWithNeutral, CreateTextWithKingdom(SAlliedToNeutral, alliedNeutral));

            // Relationship
            float relationMult;
            //FIXME: Should introduce a proper logic for such situations across the mod. It should only be a temporary condition, but can mess things up for good.
            if (ourKingdom.Leader is null || otherKingdom.Leader is null)
                relationMult = 0;
            else
                relationMult = MBMath.ClampFloat((float) Math.Log((ourKingdom.Leader.GetRelation(otherKingdom.Leader) + 100f) / 100f, 1.5), -1f, +1f);
            explainedNum.Add(Scores.Relationship * relationMult, _TRelationship);

            // Expansionism (Them)
            var expansionismPenalty = otherKingdom.GetExpansionismDiplomaticPenalty();

            if (expansionismPenalty < 0)
                explainedNum.Add(expansionismPenalty, _TExpansionism);

            // Tendency
            explainedNum.Add(Scores.Tendency, _TTendency);

            return explainedNum;
        }

        public virtual bool ShouldFormBidirectional(Kingdom ourKingdom, Kingdom otherKingdom)
            => ShouldForm(ourKingdom, otherKingdom) && ShouldForm(otherKingdom, ourKingdom);

        public virtual bool ShouldForm(Kingdom ourKingdom, Kingdom otherKingdom)
            => GetScore(ourKingdom, otherKingdom).ResultNumber >= ScoreThreshold;

        private static readonly TextObject _TWeakKingdom = new TextObject("{=!}Weak Kingdom");
        private static readonly TextObject _TRelationship = new TextObject("{=!}Relationship");
        private static readonly TextObject _TExpansionism = new TextObject("{=!}Expansionism");
        private static readonly TextObject _TTendency = new TextObject("{=!}Action Tendency");

        private const string SWarWithKingdom = "{=!}War with {KINGDOM}";
        private const string SAllianceWithKingdom = "{=!}Alliance with {KINGDOM}";
        private const string SPactWithKingdom = "{=!}Non-Aggression Pact with {KINGDOM}";
        private const string SMarriageWithLeaderClan = "{=!}Marriage with leading clan of {KINGDOM}";

        private const string SCommonEnemy = SWarWithKingdom;
        private const string SAlliedToEnemy = SAllianceWithKingdom;
        private const string SAlliedToNeutral = SAllianceWithKingdom;

        private const string SPactWithEnemy = SPactWithKingdom;
        private const string SPactWithNeutral = SPactWithKingdom;
    }
}