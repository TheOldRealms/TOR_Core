using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.WitchHunter.Bounties
{
    /// <summary>
    /// Unified bounty issue for both Minor and Major tiers. The tier is
    /// SaveableField-persisted so the behavior's <c>OnIssueUpdated</c> hook
    /// can route completion to the right counter without reading the template
    /// back from the issue (templates are runtime constants and could be
    /// edited between save/load).
    /// </summary>
    public class BountyIssue : IssueBase
    {
        [SaveableField(0)] private string _templateId;
        [SaveableField(1)] private int _targetCount;
        [SaveableField(2)] private int _rewardGold;
        [SaveableField(3)] private int _rewardPrestige;
        [SaveableField(4)] private BountyTier _tier;   // defaults to Minor on old saves — correct

        public BountyIssue(Hero issueOwner, string templateId, BountyTier tier, int targetCount, int rewardGold, int rewardPrestige)
            : base(issueOwner, CampaignTime.DaysFromNow(30f))
        {
            _templateId = templateId;
            _tier = tier;
            _targetCount = targetCount;
            _rewardGold = rewardGold;
            _rewardPrestige = rewardPrestige;
        }

        public BountyTemplate Template => BountyTemplates.Get(_templateId);
        public BountyTier Tier => _tier;
        public int TargetCount => _targetCount;
        public int RewardPrestige => _rewardPrestige;

        protected override int RewardGold => _rewardGold;

        public override TextObject IssueBriefByIssueGiver
        {
            get
            {
                var t = Template;
                var brief = TORTextHelper.GetTextObject(t.BriefKey, t.BriefFallback);
                brief.SetTextVariable("COUNT", _targetCount);
                brief.SetTextVariable("TARGET", t.TargetLabel);
                return brief;
            }
        }

        public override TextObject IssueAcceptByPlayer => TORTextHelper.GetTextObject(
            "tor_wh_bounty_accept_player", "I will see it done.");

        public override TextObject IssueQuestSolutionExplanationByIssueGiver
        {
            get
            {
                var text = TORTextHelper.GetTextObject(
                    "tor_wh_bounty_quest_explanation",
                    "Deal with {COUNT} {TARGET}. On the deed the Order will pay you {REWARD_GOLD}{GOLD_ICON} and {REWARD_PRESTIGE} prestige.");
                var t = Template;
                text.SetTextVariable("COUNT", _targetCount);
                text.SetTextVariable("TARGET", t.TargetLabel);
                text.SetTextVariable("REWARD_GOLD", _rewardGold);
                text.SetTextVariable("REWARD_PRESTIGE", _rewardPrestige);
                text.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                return text;
            }
        }

        public override TextObject IssueQuestSolutionAcceptByPlayer => TORTextHelper.GetTextObject(
            "tor_wh_bounty_quest_accept_player", "Consider it done.");

        public override bool IsThereAlternativeSolution => false;
        public override bool IsThereLordSolution => false;

        public override TextObject Title
        {
            get
            {
                var t = Template;
                var title = TORTextHelper.GetTextObject(t.TitleKey, t.TitleFallback);
                title.SetTextVariable("COUNT", _targetCount);
                title.SetTextVariable("TARGET", t.TargetLabel);
                return title;
            }
        }

        public override TextObject Description => Title;

        public override IssueFrequency GetFrequency() => IssueFrequency.VeryCommon;

        public override bool IssueStayAliveConditions()
        {
            var owner = IssueOwner;
            var mapFaction = owner?.CurrentSettlement?.MapFaction;
            return mapFaction != null && !mapFaction.IsAtWarWith(Clan.PlayerClan);
        }

        protected override bool CanPlayerTakeQuestConditions(
            Hero issueGiver,
            out PreconditionFlags flag,
            out Hero relationHero,
            out SkillObject skill,
            out int requiredGold)
        {
            flag = PreconditionFlags.None;
            relationHero = issueGiver;
            skill = null;
            requiredGold = 0;

            var faction = issueGiver.MapFaction ?? issueGiver.CurrentSettlement?.MapFaction;
            if (faction != null && faction.IsAtWarWith(Hero.MainHero.MapFaction))
                flag |= PreconditionFlags.AtWar;

            return flag == PreconditionFlags.None;
        }

        protected override void CompleteIssueWithTimedOutConsequences() { }

        protected override QuestBase GenerateIssueQuest(string questId)
        {
            return new BountyQuest(
                questId, IssueOwner, CampaignTime.DaysFromNow(20f),
                _templateId, _tier, _targetCount, _rewardGold, _rewardPrestige);
        }

        protected override void OnGameLoad() { }
        protected override void HourlyTick() { }
    }
}
