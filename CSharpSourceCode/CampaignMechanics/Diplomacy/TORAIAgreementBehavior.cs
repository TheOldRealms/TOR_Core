using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Diplomacy
{
    /// <summary>
    /// AI behavior for proactively proposing trade agreements and alliances.
    /// Vanilla Bannerlord has no AI logic for this - this adds it.
    /// </summary>
    public class TORAIAgreementBehavior : CampaignBehaviorBase
    {
        // How often AI considers agreements (in days)
        private const int ConsiderationIntervalMinDays = 5;
        private const int ConsiderationIntervalMaxDays = 10;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, OnDailyTickClan);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No persistent data needed
        }

        /// <summary>
        /// Daily tick for each clan - ruling clans trigger kingdom-wide agreement considerations.
        /// </summary>
        private void OnDailyTickClan(Clan clan)
        {
            if (clan == null || clan.IsEliminated || clan.Kingdom == null)
                return;

            // Only ruling clan triggers the kingdom's considerations
            if (clan != clan.Kingdom.RulingClan)
                return;

            // Skip player kingdom - let player decide
            if (clan.Kingdom == Clan.PlayerClan?.Kingdom)
                return;

            // Each kingdom has its own consideration interval based on its ID
            if (!ShouldConsiderAgreementsToday(clan.Kingdom))
                return;

            // Consider both trade agreements and alliances
            ConsiderTradeAgreements(clan.Kingdom);
            ConsiderAlliances(clan.Kingdom);
        }

        #region Trade Agreements

        /// <summary>
        /// Main entry point for a kingdom considering trade agreements.
        /// </summary>
        private void ConsiderTradeAgreements(Kingdom kingdom)
        {
            if (!CanKingdomConsiderTrade(kingdom))
                return;

            var potentialPartners = GetPotentialTradePartners(kingdom);
            if (!potentialPartners.Any())
                return;

            // Get candidates and randomly select one to evaluate
            var candidates = GetCandidateLords(kingdom);
            var proposingLord = SelectProposingLord(candidates);
            if (proposingLord == null)
                return;

            foreach (var targetKingdom in potentialPartners)
            {
                float chance = CalculateTradeProposalChance(proposingLord, kingdom, targetKingdom);

                if (MBRandom.RandomFloat * 100f < chance)
                {
                    ProposeTradeAgreement(kingdom, targetKingdom, proposingLord);
                    return; // Only one proposal per tick
                }
            }
        }

        /// <summary>
        /// Checks if a kingdom is allowed to consider trade agreements.
        /// </summary>
        private bool CanKingdomConsiderTrade(Kingdom kingdom)
        {
            var pantheon = GetKingdomPantheon(kingdom);

            // Chaos never trades
            if (pantheon == Pantheon.Chaos)
                return false;

            // Greenskins never trade
            if (pantheon == Pantheon.Greenskin)
                return false;

            return true;
        }

        /// <summary>
        /// Gets list of potential trade partners for a kingdom.
        /// Vampires/Undead prioritize other Undead factions regardless of distance.
        /// </summary>
        private List<Kingdom> GetPotentialTradePartners(Kingdom kingdom)
        {
            var tradeModel = Campaign.Current?.Models?.TradeAgreementModel;
            if (tradeModel == null)
                return new List<Kingdom>();

            var myPantheon = GetKingdomPantheon(kingdom);
            var isUndead = myPantheon == Pantheon.Undead;

            List<Kingdom> candidateKingdoms;

            if (isUndead)
            {
                // Vampires prioritize other Undead factions first, regardless of distance
                var undeadKingdoms = Kingdom.All
                    .Where(k => k != kingdom && !k.IsEliminated)
                    .Where(k => GetKingdomPantheon(k) == Pantheon.Undead)
                    .ToList();

                if (undeadKingdoms.Any())
                {
                    candidateKingdoms = undeadKingdoms;
                }
                else
                {
                    // No other undead - look at wider range (10 closest instead of 5)
                    candidateKingdoms = Kingdom.All
                        .Where(k => k != kingdom && !k.IsEliminated)
                        .Select(k => new { Kingdom = k, Distance = GetKingdomDistance(kingdom, k) })
                        .OrderBy(x => x.Distance)
                        .Take(10)
                        .Select(x => x.Kingdom)
                        .ToList();
                }
            }
            else
            {
                // Normal factions - take 5 closest
                candidateKingdoms = Kingdom.All
                    .Where(k => k != kingdom && !k.IsEliminated)
                    .Select(k => new { Kingdom = k, Distance = GetKingdomDistance(kingdom, k) })
                    .OrderBy(x => x.Distance)
                    .Take(5)
                    .Select(x => x.Kingdom)
                    .ToList();
            }

            var validPartners = candidateKingdoms
                .Where(k => IsTradeLoreCompatible(myPantheon, GetKingdomPantheon(k)))
                .Where(k => tradeModel.CanMakeTradeAgreement(kingdom, k, true, out _))
                .ToList();

            if (!validPartners.Any())
                return new List<Kingdom>();

            var scoredPartners = validPartners
                .Select(k => new
                {
                    Kingdom = k,
                    Score = tradeModel.GetScoreOfStartingTradeAgreement(kingdom, k, kingdom.RulingClan, out _)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(3)
                .Select(x => x.Kingdom)
                .ToList();

            return scoredPartners;
        }

        /// <summary>
        /// Calculates the chance that a lord will propose a trade agreement.
        /// </summary>
        private float CalculateTradeProposalChance(Hero lord, Kingdom proposingKingdom, Kingdom targetKingdom)
        {
            var tradeModel = Campaign.Current?.Models?.TradeAgreementModel;
            if (tradeModel == null)
                return 0f;

            return tradeModel.GetScoreOfStartingTradeAgreement(
                proposingKingdom, targetKingdom, lord.Clan, out _);
        }

        /// <summary>
        /// Proposes the trade agreement by creating a kingdom decision.
        /// </summary>
        private void ProposeTradeAgreement(Kingdom proposer, Kingdom target, Hero proposingLord)
        {
            if (proposingLord?.Clan == null)
                return;

            var decision = new TradeAgreementDecision(proposingLord.Clan, target);
            proposer.AddDecision(decision, true);
        }

        /// <summary>
        /// Checks if trade is allowed between two pantheons.
        /// </summary>
        private bool IsTradeLoreCompatible(Pantheon pantheon1, Pantheon pantheon2)
        {
            if (pantheon1 == Pantheon.Chaos || pantheon2 == Pantheon.Chaos)
                return false;

            if (pantheon1 == Pantheon.Greenskin || pantheon2 == Pantheon.Greenskin)
                return false;

            return true;
        }

        #endregion

        #region Alliances

        /// <summary>
        /// Main entry point for a kingdom considering alliances.
        /// </summary>
        private void ConsiderAlliances(Kingdom kingdom)
        {
            if (!CanKingdomConsiderAlliance(kingdom))
                return;

            var potentialAllies = GetPotentialAlliancePartners(kingdom);
            if (!potentialAllies.Any())
                return;

            // Get candidates and randomly select one to evaluate
            var candidates = GetCandidateLords(kingdom);
            var proposingLord = SelectProposingLord(candidates);
            if (proposingLord == null)
                return;

            foreach (var targetKingdom in potentialAllies)
            {
                float chance = CalculateAllianceProposalChance(proposingLord, kingdom, targetKingdom);

                if (MBRandom.RandomFloat * 100f < chance)
                {
                    ProposeAlliance(kingdom, targetKingdom, proposingLord);
                    return; // Only one proposal per tick
                }
            }
        }

        /// <summary>
        /// Checks if a kingdom is allowed to consider alliances.
        /// </summary>
        private bool CanKingdomConsiderAlliance(Kingdom kingdom)
        {
            var pantheon = GetKingdomPantheon(kingdom);

            // Chaos cannot form alliances
            if (pantheon == Pantheon.Chaos)
                return false;

            // Greenskins cannot form alliances
            if (pantheon == Pantheon.Greenskin)
                return false;

            // Check if already at max alliances
            var allianceModel = Campaign.Current?.Models?.AllianceModel;
            if (allianceModel != null && kingdom.AlliedKingdoms.Count >= allianceModel.MaxNumberOfAlliances)
                return false;

            return true;
        }

        /// <summary>
        /// Gets list of potential alliance partners for a kingdom.
        /// Vampires/Undead prioritize other Undead factions regardless of distance.
        /// </summary>
        private List<Kingdom> GetPotentialAlliancePartners(Kingdom kingdom)
        {
            var allianceModel = Campaign.Current?.Models?.AllianceModel;
            if (allianceModel == null)
                return new List<Kingdom>();

            var myPantheon = GetKingdomPantheon(kingdom);
            var isUndead = myPantheon == Pantheon.Undead;

            List<Kingdom> candidateKingdoms;

            if (isUndead)
            {
                // Vampires prioritize other Undead factions first, regardless of distance
                var undeadKingdoms = Kingdom.All
                    .Where(k => k != kingdom && !k.IsEliminated)
                    .Where(k => GetKingdomPantheon(k) == Pantheon.Undead)
                    .ToList();

                if (undeadKingdoms.Any())
                {
                    candidateKingdoms = undeadKingdoms;
                }
                else
                {
                    // No other undead - look at wider range (10 closest instead of 5)
                    candidateKingdoms = Kingdom.All
                        .Where(k => k != kingdom && !k.IsEliminated)
                        .Select(k => new { Kingdom = k, Distance = GetKingdomDistance(kingdom, k) })
                        .OrderBy(x => x.Distance)
                        .Take(10)
                        .Select(x => x.Kingdom)
                        .ToList();
                }
            }
            else
            {
                // Normal factions - take 5 closest
                candidateKingdoms = Kingdom.All
                    .Where(k => k != kingdom && !k.IsEliminated)
                    .Select(k => new { Kingdom = k, Distance = GetKingdomDistance(kingdom, k) })
                    .OrderBy(x => x.Distance)
                    .Take(5)
                    .Select(x => x.Kingdom)
                    .ToList();
            }

            var validPartners = candidateKingdoms
                .Where(k => IsAllianceLoreCompatible(myPantheon, GetKingdomPantheon(k)))
                .Where(k => !kingdom.IsAtWarWith(k))
                .Where(k => !kingdom.IsAllyWith(k))
                .Where(k => k.AlliedKingdoms.Count < allianceModel.MaxNumberOfAlliances)
                .ToList();

            if (!validPartners.Any())
                return new List<Kingdom>();

            var scoredPartners = validPartners
                .Select(k => new
                {
                    Kingdom = k,
                    Score = allianceModel.GetScoreOfStartingAlliance(kingdom, k, kingdom.RulingClan, out _).ResultNumber
                })
                .Where(x => x.Score > 50) // Alliance threshold is higher than trade
                .OrderByDescending(x => x.Score)
                .Take(3)
                .Select(x => x.Kingdom)
                .ToList();

            return scoredPartners;
        }

        /// <summary>
        /// Calculates the chance that a lord will propose an alliance.
        /// </summary>
        private float CalculateAllianceProposalChance(Hero lord, Kingdom proposingKingdom, Kingdom targetKingdom)
        {
            var allianceModel = Campaign.Current?.Models?.AllianceModel;
            if (allianceModel == null)
                return 0f;

            float score = allianceModel.GetScoreOfStartingAlliance(
                proposingKingdom, targetKingdom, lord.Clan, out _).ResultNumber;

            // Alliance score is typically higher values, scale it down for chance
            // Only propose if score > 50, and scale chance from there
            if (score <= 50)
                return 0f;

            return (score - 50) * 2; // Score of 100 = 100% chance, Score of 75 = 50% chance
        }

        /// <summary>
        /// Proposes the alliance by creating a kingdom decision.
        /// </summary>
        private void ProposeAlliance(Kingdom proposer, Kingdom target, Hero proposingLord)
        {
            if (proposingLord?.Clan == null)
                return;

            var decision = new StartAllianceDecision(proposingLord.Clan, target);
            proposer.AddDecision(decision, true);
        }

        /// <summary>
        /// Checks if alliance is allowed between two pantheons.
        /// </summary>
        private bool IsAllianceLoreCompatible(Pantheon pantheon1, Pantheon pantheon2)
        {
            // Chaos cannot form alliances
            if (pantheon1 == Pantheon.Chaos || pantheon2 == Pantheon.Chaos)
                return false;

            // Greenskins cannot form alliances
            if (pantheon1 == Pantheon.Greenskin || pantheon2 == Pantheon.Greenskin)
                return false;

            return true;
        }

        #endregion

        #region Shared Helper Methods

        /// <summary>
        /// Gets the candidate lords for proposing agreements.
        /// Returns the ruling clan leader + up to 2 random clan leaders.
        /// </summary>
        private List<Hero> GetCandidateLords(Kingdom kingdom)
        {
            var candidates = new List<Hero>();

            // Always include the ruling clan leader
            if (kingdom.Leader != null && kingdom.Leader.IsAlive)
            {
                candidates.Add(kingdom.Leader);
            }

            // Get up to 2 random other clan leaders
            var otherClanLeaders = kingdom.Clans
                .Where(c => c != kingdom.RulingClan && !c.IsUnderMercenaryService && c.Leader?.IsAlive == true)
                .Select(c => c.Leader)
                .ToList();

            int additionalLords = Math.Min(2, otherClanLeaders.Count);

            for (int i = 0; i < additionalLords; i++)
            {
                var randomLord = otherClanLeaders.GetRandomElement();
                if (randomLord != null)
                {
                    candidates.Add(randomLord);
                    otherClanLeaders.Remove(randomLord);
                }
            }

            return candidates;
        }

        /// <summary>
        /// Randomly selects one lord from the candidates to make the proposal.
        /// </summary>
        private Hero SelectProposingLord(List<Hero> candidates)
        {
            if (candidates == null || !candidates.Any())
                return null;

            return candidates.GetRandomElement();
        }

        /// <summary>
        /// Determines if a kingdom should consider agreements today.
        /// </summary>
        private bool ShouldConsiderAgreementsToday(Kingdom kingdom)
        {
            int hash = kingdom.StringId?.GetHashCode() ?? 0;
            if (hash < 0) hash = -hash;

            int intervalRange = ConsiderationIntervalMaxDays - ConsiderationIntervalMinDays + 1;
            int kingdomInterval = ConsiderationIntervalMinDays + (hash % intervalRange);
            int kingdomOffset = (hash / intervalRange) % kingdomInterval;

            int currentDay = (int)CampaignTime.Now.ToDays;
            return (currentDay + kingdomOffset) % kingdomInterval == 0;
        }

        /// <summary>
        /// Gets the approximate distance between two kingdoms.
        /// </summary>
        private float GetKingdomDistance(Kingdom kingdom1, Kingdom kingdom2)
        {
            if (kingdom1.FactionMidSettlement == null || kingdom2.FactionMidSettlement == null)
                return float.MaxValue;

            return kingdom1.FactionMidSettlement.Position.Distance(kingdom2.FactionMidSettlement.Position);
        }

        /// <summary>
        /// Gets the dominant pantheon for a kingdom.
        /// </summary>
        private Pantheon GetKingdomPantheon(Kingdom kingdom)
        {
            var leaderReligion = kingdom.Leader?.GetDominantReligion();
            if (leaderReligion != null)
                return leaderReligion.Pantheon;

            return ReligionObjectHelper.GetPantheon(kingdom.Culture?.StringId);
        }

        #endregion
    }
}