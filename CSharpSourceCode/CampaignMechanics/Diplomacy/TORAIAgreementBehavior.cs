using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Models;
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

        // Set to true to enable diplomacy debug logging
        private const bool EnableDiplomacyDebug = true;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, OnDailyTickClan);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        /// <summary>
        /// Daily tick for debugging - logs all wars and agreement counts.
        /// </summary>
        private void OnDailyTick()
        {
            if (!EnableDiplomacyDebug)
                return;

            LogDiplomacyStatus();
        }

        /// <summary>
        /// Logs current diplomacy status for all kingdoms.
        /// </summary>
        private void LogDiplomacyStatus()
        {
            var kingdoms = Kingdom.All.Where(k => !k.IsEliminated).ToList();
            var sb = new StringBuilder();

            sb.AppendLine("=== TOR DIPLOMACY DEBUG ===");
            sb.AppendLine($"Day: {(int)CampaignTime.Now.ToDays}");
            sb.AppendLine();

            // Summary counts
            int totalWars = 0;

            var tradeAgreementBehavior = Campaign.Current.GetCampaignBehavior<TradeAgreementsCampaignBehavior>();

            int totalTradeAgreements = (from kingdom in kingdoms from otherKingdom in kingdoms where tradeAgreementBehavior.HasTradeAgreement(kingdom, otherKingdom) select kingdom).Count();
            int totalAlliances = kingdoms.Sum(kingdom => kingdom.AlliedKingdoms.Count);
            
            totalAlliances /= 2; // Each alliance counted twice
            totalTradeAgreements /= 2; // Each trade agreement counted twice

            // Count unique wars
            var warPairs = new HashSet<string>();
            foreach (var kingdom in kingdoms)
            {
                foreach (var enemy in kingdoms.Where(k => k != kingdom && kingdom.IsAtWarWith(k)))
                {
                    var key = string.Compare(kingdom.StringId, enemy.StringId) < 0
                        ? $"{kingdom.StringId}|{enemy.StringId}"
                        : $"{enemy.StringId}|{kingdom.StringId}";
                    warPairs.Add(key);
                }
            }
            totalWars = warPairs.Count;

            sb.AppendLine($"TOTALS: Wars={totalWars}, Alliances={totalAlliances}, TradeAgreements={totalTradeAgreements}");
            sb.AppendLine();

            // Per-kingdom details
            sb.AppendLine("--- KINGDOM STATUS ---");
            foreach (var kingdom in kingdoms.OrderBy(k => k.Name.ToString()))
            {
                var wars = kingdoms.Where(k => k != kingdom && kingdom.IsAtWarWith(k)).Select(k => k.Name.ToString()).ToList();
                var allies = kingdom.AlliedKingdoms.Select(k => k.Name.ToString()).ToList();
                var tradePartners = kingdom.GetTradeAgreementKingdoms().Select(k => k.Name.ToString()).ToList();

                sb.AppendLine($"{kingdom.Name}:");
                sb.AppendLine($"  Wars ({wars.Count}): {(wars.Any() ? string.Join(", ", wars) : "None")}");
                sb.AppendLine($"  Allies ({allies.Count}): {(allies.Any() ? string.Join(", ", allies) : "None")}");
                sb.AppendLine($"  Trade ({tradePartners.Count}): {(tradePartners.Any() ? string.Join(", ", tradePartners) : "None")}");
            }

            sb.AppendLine();
            sb.AppendLine("--- ALL WARS ---");
            foreach (var warPair in warPairs.OrderBy(x => x))
            {
                var parts = warPair.Split('|');
                var k1 = kingdoms.FirstOrDefault(k => k.StringId == parts[0]);
                var k2 = kingdoms.FirstOrDefault(k => k.StringId == parts[1]);
                if (k1 != null && k2 != null)
                {
                    sb.AppendLine($"  {k1.Name} vs {k2.Name}");
                }
            }

            sb.AppendLine("=== END DIPLOMACY DEBUG ===");
            
            TORCommon.Log(sb.ToString(),LogLevel.Info);
            //Debug.Print(sb.ToString());
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }

        /// <summary>
        /// Daily tick for each clan - ruling clans trigger kingdom-wide agreement considerations.
        /// </summary>
        private void OnDailyTickClan(Clan clan)
        {
            if (clan == null || clan.IsEliminated || clan.Kingdom == null)
                return;
            
            if (clan != clan.Kingdom.RulingClan)    //this reduces bloat code. per faction just one time
                return;
            
            if (clan.Kingdom == Clan.PlayerClan?.Kingdom)
                return;
            
            if (!ShouldConsiderAgreementsToday(clan.Kingdom))
                return;

            // Consider both trade agreements and alliances
            ConsiderTradeAgreements(clan.Kingdom);
            ConsiderAlliances(clan.Kingdom);
        }
        
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
            var pantheon = DiplomacyHelpers.GetKingdomPantheon(kingdom);

            // Chaos never trades
            if (pantheon == Pantheon.Chaos)
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

            var myPantheon = DiplomacyHelpers.GetKingdomPantheon(kingdom);
            var isUndead = myPantheon == Pantheon.Undead;

            List<Kingdom> candidateKingdoms;

            if (isUndead)
            {
                // Vampires prioritize other Undead factions first, regardless of distance
                var undeadKingdoms = Kingdom.All
                    .Where(k => k != kingdom && !k.IsEliminated)
                    .Where(k => DiplomacyHelpers.GetKingdomPantheon(k) == Pantheon.Undead)
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
                        .Select(k => new { Kingdom = k, Distance = DiplomacyHelpers.GetKingdomDistance(kingdom, k) })
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
                    .Select(k => new { Kingdom = k, Distance = DiplomacyHelpers.GetKingdomDistance(kingdom, k) })
                    .OrderBy(x => x.Distance)
                    .Take(5)
                    .Select(x => x.Kingdom)
                    .ToList();
            }

            var validPartners = candidateKingdoms
                .Where(otherkingdom => AllowTrade(otherkingdom,kingdom))
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
        
        private bool AllowTrade(Kingdom kingdom, Kingdom otherKingdom)
        {
            var tradeModel = Campaign.Current?.Models?.TradeAgreementModel;
            return tradeModel != null && tradeModel.CanMakeTradeAgreement(kingdom,otherKingdom,true,out _);
        }

        /// <summary>
        /// Main entry point for a kingdom considering alliances.
        /// </summary>
        private void ConsiderAlliances(Kingdom ownKingdom)
        {
            if (!CanKingdomConsiderAlliance(ownKingdom))
                return;

            var potentialAllies = GetPotentialAlliancePartners(ownKingdom);
            if (!potentialAllies.Any())
                return;

            // Get candidates and randomly select one to evaluate
            var candidates = GetCandidateLords(ownKingdom);
            var proposingLord = SelectProposingLord(candidates);
            if (proposingLord == null)
                return;


            var targetKingdom = potentialAllies.GetRandomElement();
            if(targetKingdom == null) return;
            
            float chance = CalculateAllianceProposalChance(proposingLord, ownKingdom, targetKingdom);

            if (MBRandom.RandomFloat * 100f < chance)
            {
                ProposeAlliance(ownKingdom, targetKingdom, proposingLord);
            }
        }

        /// <summary>
        /// Checks if a kingdom is allowed to consider alliances.
        /// </summary>
        private bool CanKingdomConsiderAlliance(Kingdom kingdom)
        {
            var pantheon = DiplomacyHelpers.GetKingdomPantheon(kingdom);

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

            var myPantheon = DiplomacyHelpers.GetKingdomPantheon(kingdom);
            var isUndead = myPantheon == Pantheon.Undead;

            List<Kingdom> candidateKingdoms;

            if (isUndead)
            {
                // Vampires prioritize other Undead factions first, regardless of distance
                var undeadKingdoms = Kingdom.All
                    .Where(k => k != kingdom && !k.IsEliminated)
                    .Where(k => DiplomacyHelpers.GetKingdomPantheon(k) == Pantheon.Undead)
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
                        .Select(k => new { Kingdom = k, Distance = DiplomacyHelpers.GetKingdomDistance(kingdom, k) })
                        .OrderBy(x => x.Distance)
                        .Select(x => x.Kingdom)
                        .ToList();
                }
            }
            else
            {
                // Normal factions - take 5 closest
                candidateKingdoms = Kingdom.All
                    .Where(k => k != kingdom && !k.IsEliminated)
                    .Select(k => new { Kingdom = k, Distance = DiplomacyHelpers.GetKingdomDistance(kingdom, k) })
                    .OrderBy(x => x.Distance)
                    .Take(5)
                    .Select(x => x.Kingdom)
                    .ToList();
            }

            var validPartners = candidateKingdoms
                .Where(k => IsAllianceLoreCompatible(myPantheon, DiplomacyHelpers.GetKingdomPantheon(k)))
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
                .TakeRandom(3)
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
            
            if (score <= 50)
                return 0f;

            return score;
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
        /// Determines if a kingdom should consider agreements today. No serialization needed. just pure randomization
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
    }
}