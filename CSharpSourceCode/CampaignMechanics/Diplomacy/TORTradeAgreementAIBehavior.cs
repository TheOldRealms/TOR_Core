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
    /// AI behavior for proactively proposing trade agreements.
    /// Vanilla Bannerlord has no AI logic for this - this adds it.
    /// </summary>
    public class TORTradeAgreementAIBehavior : CampaignBehaviorBase
    {
        // How often AI considers trade agreements (in days)
        private const int ConsiderationIntervalMinDays = 5;
        private const int ConsiderationIntervalMaxDays = 10;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, OnDailyTickClan);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No persistent data needed for now
        }

        /// <summary>
        /// Daily tick for each clan - ruling clans trigger kingdom-wide trade considerations.
        /// </summary>
        private void OnDailyTickClan(Clan clan)
        {
            if (clan == null || clan.IsEliminated || clan.Kingdom == null)
                return;

            // Only ruling clan triggers the kingdom's trade considerations
            if (clan != clan.Kingdom.RulingClan)
                return;

            // Skip player kingdom - let player decide
            if (clan.Kingdom == Clan.PlayerClan?.Kingdom)
                return;

            // Each kingdom has its own consideration interval based on its ID
            // This spreads out trade considerations across different days
            if (!ShouldConsiderTradeToday(clan.Kingdom))
                return;

            ConsiderTradeAgreements(clan.Kingdom);
        }

        /// <summary>
        /// Main entry point for a kingdom considering trade agreements.
        /// Selects lords to evaluate and processes their recommendations.
        /// </summary>
        private void ConsiderTradeAgreements(Kingdom kingdom)
        {
            if (!CanKingdomConsiderTrade(kingdom))
                return;

            var potentialPartners = GetPotentialTradePartners(kingdom);
            if (!potentialPartners.Any())
                return;

            var lordsConsidering = GetLordsConsideringTrade(kingdom);

            foreach (var targetKingdom in potentialPartners)
            {
                foreach (var lord in lordsConsidering)
                {
                    float chance = CalculateTradeProposalChance(lord, kingdom, targetKingdom);

                    if (MBRandom.RandomFloat * 100f < chance)
                    {
                        ProposeTradeAgreement(kingdom, targetKingdom, lord);
                        return; // Only one proposal per tick
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a kingdom is allowed to consider trade agreements at all.
        /// Handles lore restrictions (Chaos, Greenskins, etc.)
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

            // All other factions (including Undead) can trade
            return true;
        }

        /// <summary>
        /// Gets list of potential trade partners for a kingdom.
        /// Takes 5 closest kingdoms, filters valid targets, scores them, returns top 3.
        /// </summary>
        private List<Kingdom> GetPotentialTradePartners(Kingdom kingdom)
        {
            var tradeModel = Campaign.Current?.Models?.TradeAgreementModel;
            if (tradeModel == null)
                return new List<Kingdom>();

            // Get all kingdoms with distance
            var kingdomsWithDistance = Kingdom.All
                .Where(k => k != kingdom && !k.IsEliminated)
                .Select(k => new { Kingdom = k, Distance = GetKingdomDistance(kingdom, k) })
                .OrderBy(x => x.Distance)
                .Take(5) // Take 5 closest
                .Select(x => x.Kingdom)
                .ToList();

            // Filter by lore and game rules
            var validPartners = kingdomsWithDistance
                .Where(k => IsTradeLoreCompatible(GetKingdomPantheon(kingdom), GetKingdomPantheon(k)))
                .Where(k => tradeModel.CanMakeTradeAgreement(kingdom, k, true, out _))
                .ToList();

            if (!validPartners.Any())
                return new List<Kingdom>();

            // Score each valid partner and take top 3
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
        /// Gets the lords who will consider trade this tick.
        /// Always includes faction leader, plus 1-2 random influential clan leaders.
        /// </summary>
        private List<Hero> GetLordsConsideringTrade(Kingdom kingdom)
        {
            var lords = new List<Hero>();

            // Always include the faction leader
            if (kingdom.Leader != null && kingdom.Leader.IsAlive)
            {
                lords.Add(kingdom.Leader);
            }

            // Add 1-2 random clan leaders
            var otherClanLeaders = kingdom.Clans
                .Where(c => c != kingdom.RulingClan && !c.IsUnderMercenaryService && c.Leader?.IsAlive == true)
                .Select(c => c.Leader)
                .ToList();

            int additionalLords = Math.Min(MBRandom.RandomInt(1, 3), otherClanLeaders.Count);

            for (int i = 0; i < additionalLords; i++)
            {
                var randomLord = otherClanLeaders.GetRandomElement();
                if (randomLord != null && !lords.Contains(randomLord))
                {
                    lords.Add(randomLord);
                    otherClanLeaders.Remove(randomLord);
                }
            }

            return lords;
        }

        /// <summary>
        /// Calculates the chance (0-100) that a lord will propose a trade agreement.
        /// Uses the model's score which already includes culture, religion, and pantheon factors.
        /// </summary>
        private float CalculateTradeProposalChance(Hero lord, Kingdom proposingKingdom, Kingdom targetKingdom)
        {
            var tradeModel = Campaign.Current?.Models?.TradeAgreementModel;
            if (tradeModel == null)
                return 0f;

            // Use the model's score directly - it already includes all the factors
            float modelScore = tradeModel.GetScoreOfStartingTradeAgreement(
                proposingKingdom, targetKingdom, lord.Clan, out _);

            // Model score is 0-100, use it as our chance
            return modelScore;
        }

        /// <summary>
        /// Actually proposes the trade agreement by creating a kingdom decision.
        /// </summary>
        private void ProposeTradeAgreement(Kingdom proposer, Kingdom target, Hero proposingLord)
        {
            if (proposingLord?.Clan == null)
                return;

            // Create the trade agreement decision
            var decision = new TradeAgreementDecision(proposingLord.Clan, target);

            // Add to kingdom's unresolved decisions for council vote
            proposer.AddDecision(decision, true);
        }

        #region Helper Methods

        /// <summary>
        /// Determines if a kingdom should consider trade today based on its unique interval.
        /// Each kingdom has a different interval (5-10 days) and offset, spreading out considerations.
        /// </summary>
        private bool ShouldConsiderTradeToday(Kingdom kingdom)
        {
            // Use hash of kingdom ID for consistent but varied timing per kingdom
            int hash = kingdom.StringId?.GetHashCode() ?? 0;
            if (hash < 0) hash = -hash;

            // Each kingdom gets an interval between min and max days
            int intervalRange = ConsiderationIntervalMaxDays - ConsiderationIntervalMinDays + 1;
            int kingdomInterval = ConsiderationIntervalMinDays + (hash % intervalRange);

            // Each kingdom also gets a unique offset within its interval
            int kingdomOffset = (hash / intervalRange) % kingdomInterval;

            int currentDay = (int)CampaignTime.Now.ToDays;
            return (currentDay + kingdomOffset) % kingdomInterval == 0;
        }

        /// <summary>
        /// Gets the approximate distance between two kingdoms based on their mid settlements.
        /// </summary>
        private float GetKingdomDistance(Kingdom kingdom1, Kingdom kingdom2)
        {
            if (kingdom1.FactionMidSettlement == null || kingdom2.FactionMidSettlement == null)
                return float.MaxValue;

            return kingdom1.FactionMidSettlement.Position.Distance(kingdom2.FactionMidSettlement.Position);
        }

        /// <summary>
        /// Gets the dominant pantheon for a kingdom based on its leader's religion or culture.
        /// </summary>
        private Pantheon GetKingdomPantheon(Kingdom kingdom)
        {
            var leaderReligion = kingdom.Leader?.GetDominantReligion();
            if (leaderReligion != null)
                return leaderReligion.Pantheon;

            return ReligionObjectHelper.GetPantheon(kingdom.Culture?.StringId);
        }

        /// <summary>
        /// Checks if trade is allowed between two pantheons based on lore.
        /// </summary>
        private bool IsTradeLoreCompatible(Pantheon pantheon1, Pantheon pantheon2)
        {
            // Chaos never trades
            if (pantheon1 == Pantheon.Chaos || pantheon2 == Pantheon.Chaos)
                return false;

            // Greenskins never trade
            if (pantheon1 == Pantheon.Greenskin || pantheon2 == Pantheon.Greenskin)
                return false;

            // All other factions (including Undead) can trade with each other
            return true;
        }

        #endregion
    }
}