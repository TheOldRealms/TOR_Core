using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Diplomacy
{
    /// <summary>
    /// Voluntary call to war decision - an ally can request help in an EXISTING war.
    /// Unlike HonorAllianceDecision, declining does NOT break the alliance.
    /// This is used when alliances are formed and one ally is already at war.
    /// </summary>
    public class VoluntaryCallToWarDecision : KingdomDecision
    {
        [SaveableField(101)]
        public readonly Kingdom RequestingAlly;

        [SaveableField(102)]
        public readonly Kingdom Enemy;

        public VoluntaryCallToWarDecision(Clan proposerClan, Kingdom requestingAlly, Kingdom enemy)
            : base(proposerClan)
        {
            RequestingAlly = requestingAlly;
            Enemy = enemy;
        }

        protected override int HoursToWait => 24; // Give some time to decide

        public override bool IsAllowed()
        {
            return Kingdom.IsAllyWith(RequestingAlly) &&
                   RequestingAlly.IsAtWarWith(Enemy) &&
                   !Kingdom.IsAtWarWith(Enemy) &&
                   !RequestingAlly.IsEliminated &&
                   !Enemy.IsEliminated;
        }

        public override int GetProposalInfluenceCost() => 0;

        public override TextObject GetGeneralTitle()
        {
            var text = new TextObject("{=TOR_Voluntary_War_Title}{ALLY} Requests Aid Against {ENEMY}");
            text.SetTextVariable("ALLY", RequestingAlly.Name);
            text.SetTextVariable("ENEMY", Enemy.Name);
            return text;
        }

        public override TextObject GetSupportTitle()
        {
            var text = new TextObject("{=TOR_Voluntary_War_Support}Vote on whether to join {ALLY}'s war against {ENEMY}.");
            text.SetTextVariable("ALLY", RequestingAlly.Name);
            text.SetTextVariable("ENEMY", Enemy.Name);
            return text;
        }

        public override TextObject GetChooseTitle()
        {
            var text = new TextObject("{=TOR_Voluntary_War_Choose}Aid {ALLY} Against {ENEMY}?");
            text.SetTextVariable("ALLY", RequestingAlly.Name);
            text.SetTextVariable("ENEMY", Enemy.Name);
            return text;
        }

        public override TextObject GetSupportDescription()
        {
            var text = new TextObject("{=TOR_Voluntary_War_Support_Desc}Your ally {ALLY} is at war with {ENEMY} and requests your aid. This is a request, not an obligation - declining will not affect your alliance.");
            text.SetTextVariable("ALLY", RequestingAlly.Name);
            text.SetTextVariable("ENEMY", Enemy.Name);
            return text;
        }

        public override TextObject GetChooseDescription()
        {
            var text = new TextObject("{=TOR_Voluntary_War_Choose_Desc}Your new ally {ALLY} was already at war with {ENEMY} when your alliance was formed. They request your aid in this conflict. You may join their cause or politely decline - your alliance will remain intact either way.");
            text.SetTextVariable("ALLY", RequestingAlly.Name);
            text.SetTextVariable("ENEMY", Enemy.Name);
            return text;
        }

        public override IEnumerable<DecisionOutcome> DetermineInitialCandidates()
        {
            yield return new VoluntaryCallToWarOutcome(true, Kingdom, RequestingAlly, Enemy);
            yield return new VoluntaryCallToWarOutcome(false, Kingdom, RequestingAlly, Enemy);
        }

        public override Clan DetermineChooser() => Kingdom.RulingClan;

        protected override bool ShouldBeCancelledInternal()
        {
            return !CanMakeDecision(out _, false);
        }

        public override void DetermineSponsors(MBReadOnlyList<DecisionOutcome> possibleOutcomes)
        {
            foreach (var outcome in possibleOutcomes)
            {
                var callOutcome = (VoluntaryCallToWarOutcome)outcome;
                if (callOutcome.ShouldJoinWar)
                {
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
            var callOutcome = (VoluntaryCallToWarOutcome)possibleOutcome;
            float support = CalculateJoinWarSupport(clan);
            return callOutcome.ShouldJoinWar ? support : -support;
        }

        public float CalculateJoinWarSupportPublic(Clan clan) => CalculateJoinWarSupport(clan);

        private float CalculateJoinWarSupport(Clan clan)
        {
            float support = 0f;

            var clanReligion = clan.Leader?.GetDominantReligion();
            var enemyReligion = Enemy.Leader?.GetDominantReligion();
            var allyReligion = RequestingAlly.Leader?.GetDominantReligion();

            // Strong bonus for fighting religious enemies
            if (clanReligion != null && enemyReligion != null)
            {
                if (clanReligion.HostileReligions?.Contains(enemyReligion) == true)
                {
                    support += 40f;
                }
                else
                {
                    float enemySimilarity = clanReligion.GetSimilarityScore(enemyReligion);
                    support -= enemySimilarity * 15f;
                }
            }

            // Bonus for helping co-religionists
            if (clanReligion != null && allyReligion != null)
            {
                float allySimilarity = clanReligion.GetSimilarityScore(allyReligion);
                support += allySimilarity * 10f;
            }

            // Relation with ally - moderate factor
            int allyRelation = clan.Leader.GetRelation(RequestingAlly.Leader);
            support += allyRelation * 0.3f;

            // Relation with enemy
            int enemyRelation = clan.Leader.GetRelation(Enemy.Leader);
            support -= enemyRelation * 0.3f;

            // Strength consideration - less willing if outmatched
            float allianceStrength = Kingdom.CurrentTotalStrength + RequestingAlly.CurrentTotalStrength;
            float enemyStrength = Enemy.CurrentTotalStrength;

            if (enemyStrength > allianceStrength * 2f)
            {
                support -= 25f;
            }
            else if (allianceStrength > enemyStrength * 2f)
            {
                support += 10f;
            }

            // Trait effects - less impactful than HonorAllianceDecision since this is voluntary
            support += clan.Leader.GetTraitLevel(DefaultTraits.Honor) * 10f;
            support += clan.Leader.GetTraitLevel(DefaultTraits.Valor) * 8f;
            support -= clan.Leader.GetTraitLevel(DefaultTraits.Calculating) * 8f;

            // Chaos enemy - always join
            if (Enemy.Culture?.StringId == TORConstants.Cultures.CHAOS)
            {
                support += 80f;
            }

            return support;
        }

        public override void ApplyChosenOutcome(DecisionOutcome chosenOutcome)
        {
            var callOutcome = (VoluntaryCallToWarOutcome)chosenOutcome;

            if (callOutcome.ShouldJoinWar)
            {
                // Mark as alliance war
                var allianceWarBehavior = Campaign.Current.GetCampaignBehavior<TORAllianceWarBehavior>();
                allianceWarBehavior?.MarkAsAllianceWar(Kingdom, Enemy);

                // Declare war
                DeclareWarAction.ApplyByKingdomDecision(Kingdom, Enemy);

                // Small relationship boost with ally for helping
                if (Kingdom.Leader != null && RequestingAlly.Leader != null)
                {
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                        Kingdom.Leader,
                        RequestingAlly.Leader,
                        5,
                        true);
                }

                if (Kingdom == Clan.PlayerClan?.Kingdom)
                {
                    var message = new TextObject("{=TOR_Voluntary_War_Joined}Your kingdom has joined {ALLY}'s war against {ENEMY}.");
                    message.SetTextVariable("ALLY", RequestingAlly.Name);
                    message.SetTextVariable("ENEMY", Enemy.Name);
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Yellow));
                }
            }
            else
            {
                // Decline - no penalty, alliance remains
                if (Kingdom == Clan.PlayerClan?.Kingdom)
                {
                    var message = new TextObject("{=TOR_Voluntary_War_Declined}Your kingdom has declined to join {ALLY}'s war against {ENEMY}. The alliance remains intact.");
                    message.SetTextVariable("ALLY", RequestingAlly.Name);
                    message.SetTextVariable("ENEMY", Enemy.Name);
                    InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Gray));
                }
            }
        }

        public override void ApplySecondaryEffects(MBReadOnlyList<DecisionOutcome> possibleOutcomes, DecisionOutcome chosenOutcome)
        {
        }

        public override TextObject GetSecondaryEffects()
        {
            return new TextObject("{=TOR_Voluntary_War_Effects}Declining will not affect your alliance.");
        }

        public override TextObject GetChosenOutcomeText(DecisionOutcome chosenOutcome, SupportStatus supportStatus, bool isShortVersion = false)
        {
            var callOutcome = (VoluntaryCallToWarOutcome)chosenOutcome;
            TextObject text;

            if (callOutcome.ShouldJoinWar)
            {
                text = new TextObject("{=TOR_Voluntary_War_Joined_Text}{KINGDOM} has joined {ALLY}'s war against {ENEMY}.");
            }
            else
            {
                text = new TextObject("{=TOR_Voluntary_War_Declined_Text}{KINGDOM} has declined to join {ALLY}'s war against {ENEMY}.");
            }

            text.SetTextVariable("KINGDOM", Kingdom.Name);
            text.SetTextVariable("ALLY", RequestingAlly.Name);
            text.SetTextVariable("ENEMY", Enemy.Name);
            return text;
        }

        public override DecisionOutcome GetQueriedDecisionOutcome(MBReadOnlyList<DecisionOutcome> possibleOutcomes)
        {
            return possibleOutcomes.FirstOrDefault(t => ((VoluntaryCallToWarOutcome)t).ShouldJoinWar);
        }

        public override bool CanMakeDecision(out TextObject reason, bool includeReason = false)
        {
            reason = includeReason ? TextObject.GetEmpty() : null;

            if (RequestingAlly.IsEliminated || Kingdom.IsEliminated || Enemy.IsEliminated)
            {
                if (includeReason)
                    reason = new TextObject("{=TOR_Realm_Eliminated}That realm has been eliminated.");
                return false;
            }

            if (Kingdom.IsAtWarWith(Enemy))
            {
                if (includeReason)
                {
                    reason = new TextObject("{=TOR_Already_At_War}Your realm is already at war with {KINGDOM}.");
                    reason.SetTextVariable("KINGDOM", Enemy.Name);
                }
                return false;
            }

            if (!RequestingAlly.IsAtWarWith(Enemy))
            {
                if (includeReason)
                {
                    reason = new TextObject("{=TOR_War_Ended}{ALLY} is no longer at war with {ENEMY}.");
                    reason.SetTextVariable("ALLY", RequestingAlly.Name);
                    reason.SetTextVariable("ENEMY", Enemy.Name);
                }
                return false;
            }

            if (!Kingdom.IsAllyWith(RequestingAlly))
            {
                if (includeReason)
                {
                    reason = new TextObject("{=TOR_No_Alliance}Your realm is no longer allied with {ALLY}.");
                    reason.SetTextVariable("ALLY", RequestingAlly.Name);
                }
                return false;
            }

            return true;
        }

        public class VoluntaryCallToWarOutcome : DecisionOutcome
        {
            [SaveableField(100)]
            public readonly bool ShouldJoinWar;

            [SaveableField(101)]
            public readonly Kingdom Kingdom;

            [SaveableField(102)]
            public readonly Kingdom RequestingAlly;

            [SaveableField(103)]
            public readonly Kingdom Enemy;

            public VoluntaryCallToWarOutcome(bool shouldJoinWar, Kingdom kingdom, Kingdom requestingAlly, Kingdom enemy)
            {
                ShouldJoinWar = shouldJoinWar;
                Kingdom = kingdom;
                RequestingAlly = requestingAlly;
                Enemy = enemy;
            }

            public override TextObject GetDecisionTitle()
            {
                if (ShouldJoinWar)
                    return new TextObject("{=TOR_Join_Ally_War}Join the War");
                else
                    return new TextObject("{=TOR_Decline_Ally_War}Decline");
            }

            public override TextObject GetDecisionDescription()
            {
                if (ShouldJoinWar)
                {
                    var text = new TextObject("{=TOR_Join_Ally_War_Desc}Join {ALLY}'s war and declare war on {ENEMY}.");
                    text.SetTextVariable("ALLY", RequestingAlly.Name);
                    text.SetTextVariable("ENEMY", Enemy.Name);
                    return text;
                }
                else
                {
                    var text = new TextObject("{=TOR_Decline_Ally_War_Desc}Politely decline to join this war. The alliance will remain intact.");
                    return text;
                }
            }

            public override string GetDecisionLink() => null;
            public override ImageIdentifier GetDecisionImageIdentifier() => null;
        }
    }

    public class VoluntaryCallToWarDecisionTypeDefiner : SaveableTypeDefiner
    {
        public VoluntaryCallToWarDecisionTypeDefiner() : base(789_200) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(VoluntaryCallToWarDecision), 1);
            AddClassDefinition(typeof(VoluntaryCallToWarDecision.VoluntaryCallToWarOutcome), 2);
        }
    }
}
