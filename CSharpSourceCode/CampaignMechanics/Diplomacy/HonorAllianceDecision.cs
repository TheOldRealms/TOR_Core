using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.Models;

namespace TOR_Core.CampaignMechanics.Diplomacy
{
    /// <summary>
    /// Total War-style alliance decision: When an ally is attacked, you must choose to
    /// either join the war or break the alliance. There is no option to refuse and keep the alliance.
    /// </summary>
    public class HonorAllianceDecision : KingdomDecision
    {
        [SaveableField(101)]
        public readonly Kingdom AttackedAlly;

        [SaveableField(102)]
        public readonly Kingdom Attacker;

        public HonorAllianceDecision(Clan proposerClan, Kingdom attackedAlly, Kingdom attacker)
            : base(proposerClan)
        {
            AttackedAlly = attackedAlly;
            Attacker = attacker;
        }

        // Immediate decision - no waiting
        protected override int HoursToWait => 0;

        public override bool IsAllowed()
        {
            // Check if the alliance still exists and the war is still ongoing
            return Kingdom.IsAllyWith(AttackedAlly) &&
                   AttackedAlly.IsAtWarWith(Attacker) &&
                   !Kingdom.IsAtWarWith(Attacker) &&
                   !AttackedAlly.IsEliminated &&
                   !Attacker.IsEliminated;
        }

        // No influence cost - this is an automatic obligation
        public override int GetProposalInfluenceCost() => 0;

        public override TextObject GetGeneralTitle()
        {
            var text = new TextObject("{=TOR_Honor_Alliance_Title}Honor your alliance with {ALLY} against {ATTACKER}");
            text.SetTextVariable("ALLY", AttackedAlly.Name);
            text.SetTextVariable("ATTACKER", Attacker.Name);
            return text;
        }

        public override TextObject GetSupportTitle()
        {
            var text = new TextObject("{=TOR_Honor_Alliance_Support}Vote on whether to honor the alliance with {ALLY} by joining the war against {ATTACKER}, or to break the alliance.");
            text.SetTextVariable("ALLY", AttackedAlly.Name);
            text.SetTextVariable("ATTACKER", Attacker.Name);
            return text;
        }

        public override TextObject GetChooseTitle()
        {
            var text = new TextObject("{=TOR_Honor_Alliance_Choose}Honor Alliance with {ALLY} Against {ATTACKER}");
            text.SetTextVariable("ALLY", AttackedAlly.Name);
            text.SetTextVariable("ATTACKER", Attacker.Name);
            return text;
        }

        public override TextObject GetSupportDescription()
        {
            var text = new TextObject("{=TOR_Honor_Alliance_Support_Desc}{KINGDOM_LEADER} must decide: Will your realm honor its alliance with {ALLY} by joining the war against {ATTACKER}, or will the alliance be dissolved?");
            text.SetTextVariable("KINGDOM_LEADER", DetermineChooser().Leader.Name);
            text.SetTextVariable("ALLY", AttackedAlly.Name);
            text.SetTextVariable("ATTACKER", Attacker.Name);
            return text;
        }

        public override TextObject GetChooseDescription()
        {
            var text = new TextObject("{=TOR_Honor_Alliance_Choose_Desc}Your ally {ALLY} has been attacked by {ATTACKER}. As a true ally, you must choose: Join the war to defend your ally, or dissolve the alliance and stay out of the conflict.");
            text.SetTextVariable("ALLY", AttackedAlly.Name);
            text.SetTextVariable("ATTACKER", Attacker.Name);
            return text;
        }

        public override IEnumerable<DecisionOutcome> DetermineInitialCandidates()
        {
            // Option 1: Join the war (honor alliance)
            yield return new HonorAllianceOutcome(true, Kingdom, AttackedAlly, Attacker);
            // Option 2: Break the alliance (refuse to join)
            yield return new HonorAllianceOutcome(false, Kingdom, AttackedAlly, Attacker);
        }

        public override Clan DetermineChooser() => Kingdom.RulingClan;

        protected override bool ShouldBeCancelledInternal()
        {
            // Only cancel if the decision conditions are no longer valid
            // Do NOT cancel based on proposer support (base class does this)
            return !CanMakeDecision(out _, false);
        }

        // Allow proposer clan to change opinion without cancelling the decision
        // This is important for HonorAllianceDecision - even if the ruling clan
        // changes their mind, the decision should still be voted on
        protected override bool CanProposerClanChangeOpinion() => true;

        public override void DetermineSponsors(MBReadOnlyList<DecisionOutcome> possibleOutcomes)
        {
            foreach (var outcome in possibleOutcomes)
            {
                var honorOutcome = (HonorAllianceOutcome)outcome;
                if (honorOutcome.ShouldJoinWar)
                {
                    // Proposer sponsors joining the war
                    outcome.SetSponsor(ProposerClan);
                }
                else
                {
                    AssignDefaultSponsor(outcome);
                }
            }
        }

        public override float DetermineSupport(Clan clan, DecisionOutcome possibleOutcome)
        {
            var honorOutcome = (HonorAllianceOutcome)possibleOutcome;
            float support = CalculateJoinWarSupport(clan);

            // Return positive support for join, negative for break (or vice versa)
            return honorOutcome.ShouldJoinWar ? support : -support;
        }

        /// <summary>
        /// Public accessor for AI decision resolution.
        /// </summary>
        public float CalculateJoinWarSupportPublic(Clan clan) => CalculateJoinWarSupport(clan);

        /// <summary>
        /// Calculate how much a clan supports joining the war.
        /// Delegates to TORAllianceModel for trait-modified scoring.
        /// </summary>
        private float CalculateJoinWarSupport(Clan clan)
        {
            var allianceModel = Campaign.Current?.Models?.AllianceModel as TORAllianceModel;
            if (allianceModel != null)
            {
                return allianceModel.CalculateHonorAllianceSupport(clan, Kingdom, AttackedAlly, Attacker);
            }

            // Fallback if model not available
            return 0f;
        }

        public override void ApplyChosenOutcome(DecisionOutcome chosenOutcome)
        {
            var honorOutcome = (HonorAllianceOutcome)chosenOutcome;

            if (honorOutcome.ShouldJoinWar)
            {
                // Mark as alliance war (doesn't count toward offensive war limit)
                var allianceWarBehavior = Campaign.Current.GetCampaignBehavior<TORAllianceWarBehavior>();
                allianceWarBehavior?.MarkAsAllianceWar(Kingdom, Attacker, AttackedAlly);

                // Declare war
                //DeclareWarAction.ApplyByKingdomDecision(Kingdom, Attacker);
                
                DeclareWarAction.ApplyByCallToWarAgreement(Kingdom, Attacker);

                // Notify
                if (Kingdom == Clan.PlayerClan?.Kingdom)
                {
                    var message = new TextObject("{=TOR_Alliance_War_Joined}Your kingdom has joined the war against {ATTACKER} to honor your alliance with {ALLY}.");
                    message.SetTextVariable("ATTACKER", Attacker.Name);
                    message.SetTextVariable("ALLY", AttackedAlly.Name);
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Yellow));
                }
            }
            else
            {
                // Break the alliance
                var allianceBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
                allianceBehavior?.EndAlliance(Kingdom, AttackedAlly);

                // Major relationship penalty for breaking alliance in time of need
                // This is a betrayal - the ally will remember
                ApplyBrokenAllianceRelationPenalty();

                // Notify
                if (Kingdom == Clan.PlayerClan?.Kingdom)
                {
                    var message = new TextObject("{=TOR_Alliance_Broken}Your alliance with {ALLY} has been dissolved - your kingdom refused to join the war against {ATTACKER}. This betrayal will not be forgotten.");
                    message.SetTextVariable("ALLY", AttackedAlly.Name);
                    message.SetTextVariable("ATTACKER", Attacker.Name);
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Red));
                }
            }
        }

        /// <summary>
        /// Applies relationship penalties when breaking an alliance during wartime.
        /// This is considered a major betrayal.
        /// </summary>
        private void ApplyBrokenAllianceRelationPenalty()
        {
            // Major penalty between rulers (-40 is significant)
            const int rulerPenalty = -40;
            const int clanPenalty = -20;

            // Ruler to ruler relationship damage
            if (Kingdom.Leader != null && AttackedAlly.Leader != null)
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                    Kingdom.Leader,
                    AttackedAlly.Leader,
                    rulerPenalty,
                    true);
            }

            // All clans in the betrayed kingdom lose respect for the betrayer's ruler
            foreach (var clan in AttackedAlly.Clans)
            {
                if (clan.Leader != null && clan.Leader != AttackedAlly.Leader && Kingdom.Leader != null)
                {
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                        Kingdom.Leader,
                        clan.Leader,
                        clanPenalty,
                        false); // Don't show notification for each clan
                }
            }

            // Only honorable lords in the betrayer's kingdom disapprove of the dishonorable act
            foreach (var clan in Kingdom.Clans)
            {
                if (clan != Kingdom.RulingClan && clan.Leader != null && Kingdom.Leader != null)
                {
                    int honorLevel = clan.Leader.GetTraitLevel(DefaultTraits.Honor);

                    // Only honorable lords (honor > 0) care about this betrayal
                    if (honorLevel > 0)
                    {
                        int disapproval = -10 * honorLevel; // -10 per honor level (max -30 at honor 3)

                        ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                            Kingdom.Leader,
                            clan.Leader,
                            disapproval,
                            false);
                    }
                }
            }
        }

        public override void ApplySecondaryEffects(MBReadOnlyList<DecisionOutcome> possibleOutcomes, DecisionOutcome chosenOutcome)
        {
            // Relation changes are handled in ApplyChosenOutcome via ApplyBrokenAllianceRelationPenalty
        }

        public override TextObject GetSecondaryEffects()
        {
            return new TextObject("{=TOR_Honor_Alliance_Effects}Breaking the alliance may damage your realm's reputation.");
        }

        public override TextObject GetChosenOutcomeText(DecisionOutcome chosenOutcome, SupportStatus supportStatus, bool isShortVersion = false)
        {
            var honorOutcome = (HonorAllianceOutcome)chosenOutcome;
            TextObject text;

            if (honorOutcome.ShouldJoinWar)
            {
                text = new TextObject("{=TOR_Honor_Alliance_Joined}{KINGDOM} has honored its alliance with {ALLY} and joined the war against {ATTACKER}.");
            }
            else
            {
                text = new TextObject("{=TOR_Honor_Alliance_Dissolved}{KINGDOM} has dissolved its alliance with {ALLY} rather than join the war against {ATTACKER}.");
            }

            text.SetTextVariable("KINGDOM", Kingdom.Name);
            text.SetTextVariable("ALLY", AttackedAlly.Name);
            text.SetTextVariable("ATTACKER", Attacker.Name);
            return text;
        }

        public override DecisionOutcome GetQueriedDecisionOutcome(MBReadOnlyList<DecisionOutcome> possibleOutcomes)
        {
            // Default query is for joining the war
            return possibleOutcomes.FirstOrDefault(t => ((HonorAllianceOutcome)t).ShouldJoinWar);
        }

        public override bool CanMakeDecision(out TextObject reason, bool includeReason = false)
        {
            reason = includeReason ? TextObject.GetEmpty() : null;

            if (AttackedAlly.IsEliminated || Kingdom.IsEliminated || Attacker.IsEliminated)
            {
                if (includeReason)
                    reason = new TextObject("{=TOR_Realm_Eliminated}That realm has been eliminated.");
                return false;
            }

            if (Kingdom.IsAtWarWith(Attacker))
            {
                if (includeReason)
                {
                    reason = new TextObject("{=TOR_Already_At_War}Your realm is already at war with {KINGDOM}.");
                    reason.SetTextVariable("KINGDOM", Attacker.Name);
                }
                return false;
            }

            if (!AttackedAlly.IsAtWarWith(Attacker))
            {
                if (includeReason)
                {
                    reason = new TextObject("{=TOR_War_Ended}{ALLY} is no longer at war with {ATTACKER}.");
                    reason.SetTextVariable("ALLY", AttackedAlly.Name);
                    reason.SetTextVariable("ATTACKER", Attacker.Name);
                }
                return false;
            }

            if (!Kingdom.IsAllyWith(AttackedAlly))
            {
                if (includeReason)
                {
                    reason = new TextObject("{=TOR_No_Alliance}Your realm is no longer allied with {ALLY}.");
                    reason.SetTextVariable("ALLY", AttackedAlly.Name);
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Decision outcome for honoring alliance.
        /// </summary>
        public class HonorAllianceOutcome : DecisionOutcome
        {
            [SaveableField(100)]
            public readonly bool ShouldJoinWar;

            [SaveableField(101)]
            public readonly Kingdom Kingdom;

            [SaveableField(102)]
            public readonly Kingdom AttackedAlly;

            [SaveableField(103)]
            public readonly Kingdom Attacker;

            public HonorAllianceOutcome(bool shouldJoinWar, Kingdom kingdom, Kingdom attackedAlly, Kingdom attacker)
            {
                ShouldJoinWar = shouldJoinWar;
                Kingdom = kingdom;
                AttackedAlly = attackedAlly;
                Attacker = attacker;
            }

            public override TextObject GetDecisionTitle()
            {
                if (ShouldJoinWar)
                    return new TextObject("{=TOR_Join_War}Join the War");
                else
                    return new TextObject("{=TOR_Break_Alliance}Break the Alliance");
            }

            public override TextObject GetDecisionDescription()
            {
                if (ShouldJoinWar)
                {
                    var text = new TextObject("{=TOR_Join_War_Desc}Honor our alliance with {ALLY} and declare war on {ATTACKER}.");
                    text.SetTextVariable("ALLY", AttackedAlly.Name);
                    text.SetTextVariable("ATTACKER", Attacker.Name);
                    return text;
                }
                else
                {
                    var text = new TextObject("{=TOR_Break_Alliance_Desc}Dissolve our alliance with {ALLY} rather than join this war.");
                    text.SetTextVariable("ALLY", AttackedAlly.Name);
                    return text;
                }
            }

            public override string GetDecisionLink() => null;

            public override ImageIdentifier GetDecisionImageIdentifier() => null;
        }
    }

    /// <summary>
    /// Type definer for save/load of HonorAllianceDecision.
    /// </summary>
    public class HonorAllianceDecisionTypeDefiner : SaveableTypeDefiner
    {
        public HonorAllianceDecisionTypeDefiner() : base(789_124) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(HonorAllianceDecision), 1);
            AddClassDefinition(typeof(HonorAllianceDecision.HonorAllianceOutcome), 2);
        }
    }
}
