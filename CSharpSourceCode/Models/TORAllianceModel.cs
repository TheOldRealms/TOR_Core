using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    /// <summary>
    /// TOR Alliance Model - Determines AI willingness to form alliances.
    /// Alliances are defensive pacts that pull allies into wars.
    /// </summary>
    public class TORAllianceModel : DefaultAllianceModel
    {
        // Distance scoring
        private const float CloseDistanceThreshold = 200f;
        private const float DistancePenaltyMultiplier = 0.5f;

        // Reliability scoring (culture/religion)
        private const float SameReligionBonus = 30f;
        private const float SameCultureBonus = 20f;
        private const float HostileReligionPenalty = -50f;
        private const float ReligionCompatibilityWeight = 20f;
        private const float CultureCompatibilityWeight = 15f;

        // Power scoring
        private const float PowerRatioWeight = 20f;
        private const float MinimumUsefulStrengthRatio = 0.3f;  // Ally should have at least 30% of our strength

        // Strategic positioning
        private const float ProximityToEnemyWeight = 25f;
        private const float SharedBorderWithEnemyBonus = 15f;

        // Common enemies
        private const float SharedActiveWarWeight = 40f;
        private const float SharedHostileFactionWeight = 20f;
        private const float SharedThreatWeight = 15f;

        // Entanglement risk
        private const float EnemyCountPenaltyWeight = -10f;
        private const float StrongEnemyPenaltyWeight = -15f;
        private const float InheritedWarRiskWeight = -20f;

        // Alliance network
        private const float CompatibleAllyBonus = 15f;
        private const float IncompatibleAllyPenalty = -20f;
        private const float HostileAllyPenalty = -40f;

        // Protective alliance
        private const float ProtectThreatenedWeight = 25f;
        private const float ProtectWeakerWeight = 15f;

        // Lore considerations
        private const float EonirDiplomacyBonus = 15f;

        // Personality trait weights
        private const float HonorTraitWeight = 15f;

        private static readonly TextObject _loreText = new("{=TOR_Alliance_Lore}Faction disposition");
        private static readonly TextObject _distanceText = new("{=TOR_Alliance_Distance}Geographic distance");
        private static readonly TextObject _reliabilityText = new("{=TOR_Alliance_Reliability}Partner reliability");
        private static readonly TextObject _powerText = new("{=TOR_Alliance_Power}Military strength");
        private static readonly TextObject _positioningText = new("{=TOR_Alliance_Position}Strategic positioning");
        private static readonly TextObject _commonEnemiesText = new("{=TOR_Alliance_CommonEnemy}Common enemies");
        private static readonly TextObject _entanglementText = new("{=TOR_Alliance_Entangle}Entanglement risk");
        private static readonly TextObject _protectiveText = new("{=TOR_Alliance_Protect}Protective instinct");
        private static readonly TextObject _honorText = new("{=TOR_Alliance_Honor}Alliance commitment");
        private static readonly TextObject _allianceNetworkText = new("{=TOR_Alliance_Network}Alliance network");

        private static readonly TextObject _chaosCannotAllyText = new("{=TOR_Alliance_Chaos}The forces of Chaos do not form alliances.");
        private static readonly TextObject _greenskinCannotAllyText = new("{=TOR_Alliance_Greenskin}Greenskins do not understand alliances.");
        private static readonly TextObject _allianceLimitText = new("{=TOR_Alliance_Limit}Alliance limit reached");

        public override int MaxNumberOfAlliances => 2;

        public override ExplainedNumber GetScoreOfStartingAlliance(
            Kingdom proposingKingdom,
            Kingdom targetKingdom,
            IFaction evaluatingFaction,
            out TextObject explanationText,
            bool includeDescription = false)
        {
            var score = base.GetScoreOfStartingAlliance(
                proposingKingdom, targetKingdom, evaluatingFaction,
                out explanationText, includeDescription);

            // Hard lore restrictions - return early with massive penalty
            if (!CanFormAlliance(proposingKingdom, targetKingdom))
            {
                score.Add(-10000f, _loreText);
                return score;
            }

            // Alliance limit check - can't ally if either kingdom is at max
            if (proposingKingdom.GetAllianceCount() >= MaxNumberOfAlliances ||
                targetKingdom.GetAllianceCount() >= MaxNumberOfAlliances)
            {
                score.Add(-10000f, _allianceLimitText);
                return score;
            }

            // Distance check - too far to be useful allies
            if (!DiplomacyHelpers.IsWithinAllianceDistance(proposingKingdom, targetKingdom))
            {
                score.Add(-500f, _distanceText);
                return score;
            }

            // Get the evaluating leader for personality traits
            Hero evaluatingLeader = GetEvaluatingLeader(evaluatingFaction);

            // Get trait modifiers
            float honorModifier = DiplomacyHelpers.GetTraitModifier(evaluatingLeader, DefaultTraits.Honor);
            float calculatingModifier = DiplomacyHelpers.GetTraitModifier(evaluatingLeader, DefaultTraits.Calculating);
            float mercyModifier = DiplomacyHelpers.GetTraitModifier(evaluatingLeader, DefaultTraits.Mercy);
            float generosityModifier = DiplomacyHelpers.GetTraitModifier(evaluatingLeader, DefaultTraits.Generosity);
            float valorModifier = DiplomacyHelpers.GetTraitModifier(evaluatingLeader, DefaultTraits.Valor);
            float valorInverseModifier = DiplomacyHelpers.GetInverseTraitModifier(evaluatingLeader, DefaultTraits.Valor);
            float generosityInverseModifier = DiplomacyHelpers.GetInverseTraitModifier(evaluatingLeader, DefaultTraits.Generosity);

            // Check compatibility for common enemies calculation
            bool isCompatible = IsCompatiblePartner(proposingKingdom, targetKingdom);

            // Calculate individual scores
            float loreScore = CalculateLoreConsiderations(proposingKingdom, targetKingdom);
            float distanceScore = CalculateDistanceScore(proposingKingdom, targetKingdom);
            float reliabilityScore = CalculateReliabilityScore(proposingKingdom, targetKingdom) * honorModifier;
            float powerScore = CalculatePowerScore(proposingKingdom, targetKingdom) * calculatingModifier * generosityInverseModifier;
            float positioningScore = CalculateStrategicPositioningScore(proposingKingdom, targetKingdom) * calculatingModifier;
            float commonEnemiesScore = CalculateCommonEnemiesScore(proposingKingdom, targetKingdom, isCompatible, calculatingModifier);
            float entanglementScore = CalculateEntanglementRiskScore(proposingKingdom, targetKingdom) * calculatingModifier * valorInverseModifier;
            float allianceNetworkScore = CalculateAllianceNetworkScore(proposingKingdom, targetKingdom) * calculatingModifier;

            // Protective alliances require both mercy AND generosity - cruel or greedy lords don't consider them
            float protectiveScore = 0f;
            if (mercyModifier > 1f && generosityModifier > 1f)
            {
                // Only merciful AND generous lords consider protective alliances
                // Scale by how merciful/generous they are above baseline
                float mercyFactor = mercyModifier - 1f;      // 0 to 0.75
                float generosityFactor = generosityModifier - 1f;  // 0 to 0.75
                protectiveScore = CalculateProtectiveAllianceScore(proposingKingdom, targetKingdom) * (1f + mercyFactor + generosityFactor);
            }

            // Honor bonus - honorable lords value alliance commitments
            float honorScore = (honorModifier - 1f) * HonorTraitWeight;

            // Add scores to explained number
            if (loreScore != 0) score.Add(loreScore, _loreText);
            if (distanceScore != 0) score.Add(distanceScore, _distanceText);
            if (reliabilityScore != 0) score.Add(reliabilityScore, _reliabilityText);
            if (powerScore != 0) score.Add(powerScore, _powerText);
            if (positioningScore != 0) score.Add(positioningScore, _positioningText);
            if (commonEnemiesScore != 0) score.Add(commonEnemiesScore, _commonEnemiesText);
            if (entanglementScore != 0) score.Add(entanglementScore, _entanglementText);
            if (allianceNetworkScore != 0) score.Add(allianceNetworkScore, _allianceNetworkText);
            if (protectiveScore != 0) score.Add(protectiveScore, _protectiveText);
            if (honorScore != 0) score.Add(honorScore, _honorText);

            return score;
        }

        /// <summary>
        /// Gets the leader of the evaluating faction.
        /// </summary>
        private Hero GetEvaluatingLeader(IFaction evaluatingFaction)
        {
            if (evaluatingFaction is Clan clan)
                return clan.Leader;
            if (evaluatingFaction is Kingdom kingdom)
                return kingdom.Leader;
            return null;
        }

        /// <summary>
        /// Checks if two kingdoms are compatible (same religion or culture).
        /// Used to determine if common enemy score applies fully or only for calculating lords.
        /// </summary>
        private bool IsCompatiblePartner(Kingdom kingdom1, Kingdom kingdom2)
        {
            if (DiplomacyHelpers.AreSameReligion(kingdom1, kingdom2))
                return true;
            if (DiplomacyHelpers.AreSameCulture(kingdom1, kingdom2))
                return true;

            // Check pantheon compatibility
            var pantheon1 = DiplomacyHelpers.GetKingdomPantheon(kingdom1);
            var pantheon2 = DiplomacyHelpers.GetKingdomPantheon(kingdom2);

            // Same pantheon = compatible
            if (pantheon1 == pantheon2)
                return true;

            // Human-Dwarven-Elven are generally compatible (Order vs Chaos)
            if (IsOrderPantheon(pantheon1) && IsOrderPantheon(pantheon2))
                return true;

            return false;
        }

        private bool IsOrderPantheon(Pantheon pantheon)
        {
            return pantheon == Pantheon.Human ||
                   pantheon == Pantheon.Dwarven ||
                   pantheon == Pantheon.Elven;
        }

        /// <summary>
        /// Lore-based alliance considerations.
        /// Eonir are natural diplomats.
        /// </summary>
        private float CalculateLoreConsiderations(Kingdom proposingKingdom, Kingdom targetKingdom)
        {
            float score = 0f;

            // Eonir are skilled diplomats
            if (proposingKingdom.Culture?.StringId == TORConstants.Cultures.EONIR)
                score += EonirDiplomacyBonus;

            return score;
        }

        /// <summary>
        /// Distance score - closer allies are more useful.
        /// Returns: 0 (close) to -90 (far but within range)
        /// </summary>
        private float CalculateDistanceScore(Kingdom proposingKingdom, Kingdom targetKingdom)
        {
            float distance = DiplomacyHelpers.GetKingdomDistance(proposingKingdom, targetKingdom);

            if (distance <= CloseDistanceThreshold)
                return 0f;

            // Gradual penalty for distance
            return (CloseDistanceThreshold - distance) * DistancePenaltyMultiplier;
        }

        /// <summary>
        /// Reliability score based on religion and culture compatibility.
        /// Trustworthy partners make better allies.
        /// Returns: -50 (hostile) to +50 (very reliable)
        /// </summary>
        private float CalculateReliabilityScore(Kingdom proposingKingdom, Kingdom targetKingdom)
        {
            // Same religion = very reliable
            if (DiplomacyHelpers.AreSameReligion(proposingKingdom, targetKingdom))
                return SameReligionBonus;

            // Hostile religions = unreliable (will backstab)
            if (DiplomacyHelpers.AreReligionsHostile(proposingKingdom, targetKingdom))
                return HostileReligionPenalty;

            float score = 0f;

            // Same culture = reliable
            if (DiplomacyHelpers.AreSameCulture(proposingKingdom, targetKingdom))
                score += SameCultureBonus;

            // Otherwise use compatibility scores
            float religionCompat = DiplomacyHelpers.GetReligionCompatibility(proposingKingdom, targetKingdom);
            float cultureCompat = DiplomacyHelpers.GetCultureCompatibility(proposingKingdom, targetKingdom);

            score += religionCompat * ReligionCompatibilityWeight;
            score += cultureCompat * CultureCompatibilityWeight;

            return score;
        }

        /// <summary>
        /// Power score - evaluate if ally has enough strength to be useful.
        /// Uses alliance strength (includes their current allies).
        /// Returns: -20 (too weak) to +30 (powerful ally)
        /// </summary>
        private float CalculatePowerScore(Kingdom proposingKingdom, Kingdom targetKingdom)
        {
            // Use alliance strength - their allies would become our allies too
            float ourStrength = Math.Max(proposingKingdom.GetAllianceTotalStrength(), 1f);
            float theirStrength = targetKingdom.GetAllianceTotalStrength();

            float strengthRatio = theirStrength / ourStrength;

            // Too weak to be useful
            if (strengthRatio < MinimumUsefulStrengthRatio)
                return -20f;

            // Scale: 0.3 ratio = 0, 1.0 ratio = ~14, 2.0 ratio = ~28
            float score = (strengthRatio - MinimumUsefulStrengthRatio) * PowerRatioWeight;

            return MBMath.ClampFloat(score, -20f, 30f);
        }

        /// <summary>
        /// Strategic positioning - can this ally help against our current enemies?
        /// Returns: 0 (no positioning value) to +40 (excellent position)
        /// </summary>
        private float CalculateStrategicPositioningScore(Kingdom proposingKingdom, Kingdom targetKingdom)
        {
            float score = 0f;

            // Check if target kingdom is close to any of our enemies
            foreach (var enemyKingdom in proposingKingdom.GetEnemyKingdoms())
            {
                float distanceToEnemy = DiplomacyHelpers.GetKingdomDistance(targetKingdom, enemyKingdom);

                // Close to our enemy = can help us fight
                if (distanceToEnemy <= DiplomacyHelpers.MaxWarDistance)
                {
                    score += ProximityToEnemyWeight;

                    // Shares border with enemy = even better (buffer state)
                    if (distanceToEnemy <= 150f)
                        score += SharedBorderWithEnemyBonus;
                }
            }

            return score;
        }

        /// <summary>
        /// Common enemies score - shared threats create natural alliances.
        /// For incompatible partners, only calculating lords see value.
        /// Returns: 0 to +80 (many shared enemies)
        /// </summary>
        private float CalculateCommonEnemiesScore(Kingdom proposingKingdom, Kingdom targetKingdom, bool isCompatible, float calculatingModifier)
        {
            float score = 0f;

            // Shared active wars - both fighting the same enemy
            foreach (var enemyKingdom in proposingKingdom.GetEnemyKingdoms())
            {
                if (targetKingdom.IsAtWarWith(enemyKingdom))
                {
                    score += SharedActiveWarWeight;
                }
            }

            // Shared hostile factions (religions/cultures that hate both of us)
            var ourPantheon = DiplomacyHelpers.GetKingdomPantheon(proposingKingdom);
            var theirPantheon = DiplomacyHelpers.GetKingdomPantheon(targetKingdom);

            // Both threatened by Chaos
            if (ourPantheon != Pantheon.Chaos && theirPantheon != Pantheon.Chaos)
            {
                bool chaosExists = Kingdom.All.Any(k => !k.IsEliminated &&
                    DiplomacyHelpers.GetKingdomPantheon(k) == Pantheon.Chaos);
                if (chaosExists)
                    score += SharedThreatWeight;
            }

            // Both threatened by Greenskins
            if (ourPantheon != Pantheon.Greenskin && theirPantheon != Pantheon.Greenskin)
            {
                bool greenskinExists = Kingdom.All.Any(k => !k.IsEliminated &&
                    DiplomacyHelpers.GetKingdomPantheon(k) == Pantheon.Greenskin);
                if (greenskinExists)
                    score += SharedThreatWeight;
            }

            // Shared hostile religions
            if (DiplomacyHelpers.AreReligionsHostile(proposingKingdom, targetKingdom) == false)
            {
                // Check if we share enemies based on religion
                var ourReligion = proposingKingdom.Leader?.GetDominantReligion();
                var theirReligion = targetKingdom.Leader?.GetDominantReligion();

                if (ourReligion?.HostileReligions != null && theirReligion?.HostileReligions != null)
                {
                    foreach (var hostile in ourReligion.HostileReligions)
                    {
                        if (theirReligion.HostileReligions.Contains(hostile))
                            score += SharedHostileFactionWeight;
                    }
                }
            }

            // Apply compatibility modifier
            if (isCompatible)
            {
                return score;
            }
            else
            {
                // Incompatible partners - only calculating lords see value
                // calculatingModifier ranges from 0.25 (-2 trait) to 1.75 (+2 trait)
                // We want: -2 trait = 0, 0 trait = 0, +2 trait = full score
                float pragmatismFactor = Math.Max(0f, calculatingModifier - 1f); // 0 to 0.75
                return score * pragmatismFactor * 1.33f; // Scale so +2 calculating gets full score
            }
        }

        /// <summary>
        /// Entanglement risk - will this alliance drag us into unwanted wars?
        /// Returns: 0 (low risk) to -60 (high risk)
        /// </summary>
        private float CalculateEntanglementRiskScore(Kingdom proposingKingdom, Kingdom targetKingdom)
        {
            float score = 0f;

            // How many wars are they fighting?
            int theirWarCount = targetKingdom.GetWarCount();
            score += theirWarCount * EnemyCountPenaltyWeight;

            // Are their enemies strong? Would we inherit dangerous wars?
            float ourStrength = proposingKingdom.CurrentTotalStrength;

            foreach (var enemyKingdom in targetKingdom.GetEnemyKingdoms())
            {
                // Skip if we're already at war with them (not additional risk)
                if (proposingKingdom.IsAtWarWith(enemyKingdom))
                    continue;

                float enemyStrength = enemyKingdom.CurrentTotalStrength;

                // Strong enemy we'd inherit = risk
                if (enemyStrength > ourStrength * 0.5f)
                    score += StrongEnemyPenaltyWeight;

                // Any inherited war is a risk
                score += InheritedWarRiskWeight;
            }

            return MBMath.ClampFloat(score, -60f, 0f);
        }

        /// <summary>
        /// Alliance network score - evaluates the compatibility of their current allies with us.
        /// Compatible allies = good (joining a friendly network)
        /// Hostile allies = bad (their allies might cause problems for us)
        /// Returns: -60 (hostile network) to +45 (very compatible network)
        /// </summary>
        private float CalculateAllianceNetworkScore(Kingdom proposingKingdom, Kingdom targetKingdom)
        {
            float score = 0f;

            // Check each of their current allies
            foreach (var theirAlly in targetKingdom.GetAlliedKingdoms())
            {
                // Skip if it's us (shouldn't happen but be safe)
                if (theirAlly == proposingKingdom)
                    continue;

                // Check if their ally is hostile to us
                if (DiplomacyHelpers.AreReligionsHostile(proposingKingdom, theirAlly))
                {
                    score += HostileAllyPenalty;
                    continue;
                }

                // Check if their ally is compatible with us
                if (IsCompatiblePartner(proposingKingdom, theirAlly))
                {
                    score += CompatibleAllyBonus;
                }
                else
                {
                    // Incompatible but not hostile - mild concern
                    score += IncompatibleAllyPenalty;
                }
            }

            return MBMath.ClampFloat(score, -60f, 45f);
        }

        /// <summary>
        /// Protective alliance score - merciful lords want to protect threatened kingdoms.
        /// Returns: 0 (not threatened) to +40 (severely threatened)
        /// </summary>
        private float CalculateProtectiveAllianceScore(Kingdom proposingKingdom, Kingdom targetKingdom)
        {
            float score = 0f;

            // Is target kingdom under threat?
            int theirWarCount = targetKingdom.GetWarCount();
            if (theirWarCount > 0)
            {
                score += theirWarCount * ProtectThreatenedWeight;
            }

            // Is target kingdom weaker than their enemies?
            float theirStrength = targetKingdom.CurrentTotalStrength;
            float enemyStrength = targetKingdom.GetTotalEnemyStrength();

            if (enemyStrength > theirStrength)
            {
                float vulnerabilityRatio = Math.Min(enemyStrength / Math.Max(theirStrength, 1f), 3f);
                score += vulnerabilityRatio * ProtectWeakerWeight;
            }

            return MBMath.ClampFloat(score, 0f, 40f);
        }
        
        public bool CanFormAlliance(Kingdom kingdom1, Kingdom kingdom2)
        {
            var pantheon1 = DiplomacyHelpers.GetKingdomPantheon(kingdom1);
            var pantheon2 = DiplomacyHelpers.GetKingdomPantheon(kingdom2);

            // Chaos cannot ally
            if (pantheon1 == Pantheon.Chaos || pantheon2 == Pantheon.Chaos)
                return false;

            // Greenskins cannot ally
            if (pantheon1 == Pantheon.Greenskin || pantheon2 == Pantheon.Greenskin)
                return false;

            return true;
        }

        /// <summary>
        /// Checks if a kingdom can consider forming alliances at all.
        /// Returns false for Chaos/Greenskins or if at max alliances.
        /// </summary>
        public bool CanKingdomConsiderAlliance(Kingdom kingdom)
        {
            if (kingdom == null)
                return false;

            var pantheon = DiplomacyHelpers.GetKingdomPantheon(kingdom);
            if (pantheon == Pantheon.Chaos || pantheon == Pantheon.Greenskin)
                return false;

            if (kingdom.AlliedKingdoms.Count >= MaxNumberOfAlliances)
                return false;

            return true;
        }

        /// <summary>
        /// Gets potential alliance partners for a kingdom.
        /// Undead factions prioritize other Undead, others use distance-based selection.
        /// Returns top scored candidates that pass all filters.
        /// </summary>
        public List<Kingdom> GetPotentialAlliancePartners(Kingdom kingdom, int maxCandidates = 3)
        {
            if (!CanKingdomConsiderAlliance(kingdom))
                return new List<Kingdom>();

            var myPantheon = DiplomacyHelpers.GetKingdomPantheon(kingdom);
            var isUndead = myPantheon == Pantheon.Undead;

            // Get candidate kingdoms based on faction type
            List<Kingdom> candidateKingdoms = GetCandidateKingdoms(kingdom, isUndead);

            // Filter by alliance rules
            var validPartners = candidateKingdoms
                .Where(k => CanFormAlliance(kingdom, k))
                .Where(k => !kingdom.IsAtWarWith(k))
                .Where(k => !kingdom.IsAllyWith(k))
                .Where(k => k.AlliedKingdoms.Count < MaxNumberOfAlliances)
                .ToList();

            if (!validPartners.Any())
                return new List<Kingdom>();

            // Score and return top candidates
            var scoredPartners = validPartners
                .Select(k => new
                {
                    Kingdom = k,
                    Score = GetScoreOfStartingAlliance(kingdom, k, kingdom.RulingClan, out _).ResultNumber
                })
                .Where(x => x.Score > 50)
                .OrderByDescending(x => x.Score)
                .TakeRandom(maxCandidates)
                .Select(x => x.Kingdom)
                .ToList();

            return scoredPartners;
        }

        private List<Kingdom> GetCandidateKingdoms(Kingdom kingdom, bool isUndead)
        {
            if (isUndead)
            {
                // Undead prioritize other Undead factions regardless of distance
                var undeadKingdoms = Kingdom.All
                    .Where(k => k != kingdom && !k.IsEliminated)
                    .Where(k => DiplomacyHelpers.GetKingdomPantheon(k) == Pantheon.Undead)
                    .ToList();

                if (undeadKingdoms.Any())
                    return undeadKingdoms;

                // No other undead - use all kingdoms sorted by distance
                return Kingdom.All
                    .Where(k => k != kingdom && !k.IsEliminated)
                    .OrderBy(k => DiplomacyHelpers.GetKingdomDistance(kingdom, k))
                    .ToList();
            }

            // Normal factions - take 5 closest
            return Kingdom.All
                .Where(k => k != kingdom && !k.IsEliminated)
                .OrderBy(k => DiplomacyHelpers.GetKingdomDistance(kingdom, k))
                .Take(5)
                .ToList();
        }
    }
}
