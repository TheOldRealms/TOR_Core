using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TOR_Core.CampaignMechanics.Diplomacy;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORDiplomacyModel : DefaultDiplomacyModel
    {
        // War declaration weights (scaled by 100 for DenarsToInfluence compatibility)
        private const float WarCultureCompatibilityWeight = 3000f;  // Reduced to allow same-culture civil wars

        // Territorial Integrity weights (hierarchical claims)
        private const float DirectClaimWeight = 5000f;      // My own settlement
        private const float CulturalClaimWeight = 2500f;     // My culture's settlement held by foreigners
        private const float PantheonClaimWeight = 1000f;     // My pantheon's settlement held by other pantheons

        // Lorewise Rivalry/Affinity weights
        private const float KarakReclamationWeight = 15000f;  // Dwarfs vs Greenskins holding Karaks
        private const float AntiChaosWeight = 12000f;         // Good factions vs Chaos
        private const float LoreRivalryWarWeight = 5000f;     // Base weight for lore rivalries (multiplied by rivalry level)
        private const float LoreAffinityWarPenalty = -3000f;  // Base penalty for lore affinities (multiplied by affinity level)

        // Territorial distance factor settings
        private const float TerritorialDistanceScaling = 100f;  // Distance at which factor is ~0.5
        private const float TerritorialMinDistanceFactor = 0.2f; // Minimum factor for very distant claims

        // Alliance candidate weights
        private const float AllianceReligionCompatibilityWeight = 100f;
        private const float AllianceCultureCompatibilityWeight = 75f;
        private const float AllianceStrengthWeight = 50f;
        private const float AllianceDistancePenaltyFactor = 0.01f;
        private const float AllianceMinimumScoreThreshold = 50f;
        private const float AllianceLoreRivalryPenalty = -80f;     // Penalty for lore rivalries (multiplied by rivalry level)
        private const float AllianceLoreAffinityBonus = 60f;       // Bonus for lore affinities (multiplied by affinity level)

        // Peace declaration weights
        private const float PeaceLosingWarWeight = 50f;           // Base weight for losing wars
        private const float PeaceLosingWarThreshold = 0.7f;       // Strength ratio below which we're "losing"
        private const float PeaceWarDurationWeight = 0.5f;        // Per-day weight for war weariness
        private const float PeaceWarDurationThreshold = 30f;      // Days before weariness kicks in
        private const float PeaceRelationWeight = 0.3f;           // Base relation effect
        private const float PeaceRelationDurationScale = 0.01f;   // Additional per-day scaling for relations
        private const float PeaceCompatibilityWeight = 20f;       // Religion/culture compatibility base
        private const float PeaceCompatibilityDurationScale = 0.3f; // Additional per-day scaling
        private const float PeaceTerritorialSatisfactionWeight = 40f; // Bonus when claims satisfied
        private const float PeaceOverWarLimitWeight = 80f;        // Strong factor for being over limit
        private const float PeaceExternalThreatWeight = 60f;      // Weight for common threats
        private const float PeaceExternalThreatThreshold = 1.5f;  // Threat must be 1.5x our strength
        private const float PeaceAllianceWarPenalty = -60f;       // Penalty for abandoning alliance war
        private const float PeaceLoreRivalryPenalty = -50f;       // Penalty for lore-based rivalries
        private const float PeaceLoreAffinityBonus = 40f;         // Bonus for lore-based affinities

        // Note: Trait modifiers and distance thresholds are now in DiplomacyHelpers

        public override int GetInfluenceCostOfProposingPeace(Clan proposingClan) => 150;
        public override int GetInfluenceCostOfProposingWar(Clan proposingClan) => 150;

        public override float GetRelationIncreaseFactor(Hero hero1, Hero hero2, float relationChange)
        {
            var baseValue = base.GetRelationIncreaseFactor(hero1, hero2, relationChange);
            var values = new ExplainedNumber(baseValue);

            var playerHero = hero1.IsHumanPlayerCharacter || hero2.IsHumanPlayerCharacter ? (hero1.IsHumanPlayerCharacter ? hero1 : hero2) : null;
            if (playerHero == null) return baseValue;

            var conversationHero = !hero1.IsHumanPlayerCharacter || !hero2.IsHumanPlayerCharacter ? (!hero1.IsHumanPlayerCharacter ? hero1 : hero2) : null;
            if (playerHero.HasAnyCareer())
            {
                var choices = playerHero.GetAllCareerChoices();

                if (choices.Contains("CourtleyPassive1"))
                {
                    if (baseValue > 0)
                    {
                        var choice = TORCareerChoices.GetChoice("CourtleyPassive1");
                        if (choice != null)
                        {
                            var value = choice.Passive.InterpretAsPercentage ? choice.Passive.EffectMagnitude / 100 : choice.Passive.EffectMagnitude;
                            values.AddFactor(value);
                        }
                    }
                }

                if (choices.Contains("JustCausePassive4"))
                {
                    if (baseValue > 0)
                    {
                        if (conversationHero != null && conversationHero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                        {
                            var choice = TORCareerChoices.GetChoice("JustCausePassive4");
                            if (choice != null)
                            {
                                var value = choice.Passive.InterpretAsPercentage ? choice.Passive.EffectMagnitude / 100 : choice.Passive.EffectMagnitude;
                                values.AddFactor(value);
                            }
                        }
                    }
                }
            }
            return values.ResultNumber;
        }

        public override float GetScoreOfDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, Clan evaluatingClan, out TextObject reason, bool includeReason = false)
        {
            reason = new TextObject("It is time to declare war!");

            if (factionDeclaresWar is Kingdom declaringKingdom && factionDeclaredWar is Kingdom targetKingdom)
            {
                // Use offensive war count (excludes alliance/defensive wars)
                int offensiveWars = GetOffensiveWarCount(declaringKingdom);

                // Maximum war limit - cannot declare more wars
                if (offensiveWars >= TORConfig.NumMaxKingdomWars)
                    return -100000;

                // War count is acceptable - combine base game score with TOR factors
                
                // Add TOR custom scoring factors
                float customScore = CalculateWarTargetScore(declaringKingdom, targetKingdom, evaluatingClan);

                // Small random variance to prevent identical scores
                customScore += MBRandom.RandomInt(-25, 25);

                return customScore;
            }

            return base.GetScoreOfDeclaringWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out reason, includeReason);
        }

        public override float GetScoreOfDeclaringPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace)
        {
            // Chaos really shouldn't be allowed to make peace
            if (factionDeclaresPeace.Culture.StringId == TORConstants.Cultures.CHAOS ||
                factionDeclaredPeace.Culture.StringId == TORConstants.Cultures.CHAOS)
            {
                return float.MinValue;
            }

            if (factionDeclaresPeace is Kingdom kingdom && factionDeclaredPeace is Kingdom enemyKingdom)
            {
                int totalWars = kingdom.GetWarCount();

                // If under minimum wars, don't seek peace
                if (totalWars <= TORConfig.NumMinKingdomWars)
                    return -100000;

                // Calculate detailed peace score using ruling clan's perspective
                return CalculatePeaceScore(kingdom, enemyKingdom, kingdom.RulingClan);
            }

            return base.GetScoreOfDeclaringPeace(factionDeclaresPeace, factionDeclaredPeace);
        }

        /// <summary>
        /// Calculates detailed peace score for a specific enemy kingdom.
        /// Positive score = want peace, Negative score = want to continue war.
        /// </summary>
        public float CalculatePeaceScore(Kingdom kingdom, Kingdom enemyKingdom, Clan evaluatingClan)
        {
            float score = 0f;
            var leader = evaluatingClan?.Leader;

            // Get trait modifiers
            float honorModifier = DiplomacyHelpers.GetTraitModifier(leader, DefaultTraits.Honor);
            float honorInverseModifier = DiplomacyHelpers.GetInverseTraitModifier(leader, DefaultTraits.Honor);
            float valorInverseModifier = DiplomacyHelpers.GetInverseTraitModifier(leader, DefaultTraits.Valor);
            float calculatingModifier = DiplomacyHelpers.GetTraitModifier(leader, DefaultTraits.Calculating);
            float mercyModifier = DiplomacyHelpers.GetTraitModifier(leader, DefaultTraits.Mercy);
            float generosityInverseModifier = DiplomacyHelpers.GetInverseTraitModifier(leader, DefaultTraits.Generosity);

            // Get war context
            var allianceWarBehavior = Campaign.Current?.GetCampaignBehavior<TORAllianceWarBehavior>();
            bool isAllianceWar = allianceWarBehavior?.IsAllianceWar(kingdom, enemyKingdom) ?? false;
            float warDurationDays = kingdom.GetStanceWith(enemyKingdom)?.WarStartDate.ElapsedDaysUntilNow ?? 0f;

            // === PRO-PEACE FACTORS ===

            // 1. Strength comparison - losing wants peace, winning wants war
            score += CalculateStrengthPeaceScore(kingdom, enemyKingdom) * honorInverseModifier * calculatingModifier;

            // 2. War duration / weariness - Valor (inverse) increases effect
            score += CalculateWarWearinessScore(warDurationDays) * valorInverseModifier;

            // 3. Personal relations - immediate + duration scaling
            score += CalculateRelationPeaceScore(evaluatingClan, enemyKingdom, warDurationDays);

            // 4. Religion/Culture compatibility - Mercy increases, immediate + duration scaling
            score += CalculateCompatibilityPeaceScore(kingdom, enemyKingdom, warDurationDays) * mercyModifier;

            // 5. Territorial satisfaction - Generosity (inverse) mitigates
            score += CalculateTerritorialSatisfactionScore(kingdom, enemyKingdom) * generosityInverseModifier;

            // 6. Over war limit - strong factor, no trait modifier
            score += CalculateWarLimitPeaceScore(kingdom, enemyKingdom, isAllianceWar);

            // 7. Common external threat
            score += CalculateExternalThreatPeaceScore(kingdom, enemyKingdom);

            // 8. Lore rivalries and affinities
            score += CalculateLoreRelationshipPeaceScore(kingdom, enemyKingdom);

            // === ALLIANCE WAR PENALTY ===
            if (isAllianceWar)
            {
                score += PeaceAllianceWarPenalty * honorModifier;
            }

            return score;
        }

        /// <summary>
        /// Calculates peace score based on relative strength.
        /// Positive when losing (want peace), negative when winning (want to continue).
        /// </summary>
        private float CalculateStrengthPeaceScore(Kingdom kingdom, Kingdom enemyKingdom)
        {
            float ourStrength = kingdom.GetAllianceTotalStrength();
            float theirStrength = Math.Max(enemyKingdom.GetAllianceTotalStrength(), 1f);
            float strengthRatio = ourStrength / theirStrength;

            // Clearly winning - want to press advantage (anti-peace)
            if (strengthRatio > 1.4f)
            {
                float winningFactor = Math.Min(strengthRatio - 1f, 2f);
                return -PeaceLosingWarWeight * winningFactor;
            }

            // Roughly even - neutral
            if (strengthRatio >= 1f)
                return 0f;

            // Below threshold, we're clearly losing (pro-peace)
            if (strengthRatio < PeaceLosingWarThreshold)
            {
                float losingFactor = PeaceLosingWarThreshold / Math.Max(strengthRatio, 0.1f);
                return PeaceLosingWarWeight * losingFactor;
            }

            // Between 0.7 and 1.0 - mild pressure for peace
            return PeaceLosingWarWeight * 0.5f * (1f - strengthRatio) / (1f - PeaceLosingWarThreshold);
        }

        /// <summary>
        /// Calculates peace score based on war duration (weariness).
        /// Longer wars increase desire for peace.
        /// </summary>
        private float CalculateWarWearinessScore(float warDurationDays)
        {
            if (warDurationDays <= PeaceWarDurationThreshold)
                return 0f;

            float daysOverThreshold = warDurationDays - PeaceWarDurationThreshold;
            return daysOverThreshold * PeaceWarDurationWeight;
        }

        /// <summary>
        /// Calculates peace score based on personal relations with enemy lords.
        /// Positive relations = pro-peace (fighting friends wears on you).
        /// Negative relations = anti-peace (want to keep fighting hated enemies).
        /// Scales with war duration in both directions.
        /// </summary>
        private float CalculateRelationPeaceScore(Clan evaluatingClan, Kingdom enemyKingdom, float warDurationDays)
        {
            float avgRelation = DiplomacyHelpers.CalculateClanToKingdomRelation(evaluatingClan, enemyKingdom);

            // Immediate effect (works for both positive and negative)
            float immediateScore = avgRelation * PeaceRelationWeight;

            // Duration scaling - fighting friends/enemies intensifies over time
            // Positive relations: longer war = more desire for peace
            // Negative relations: longer war = more desire to continue (revenge)
            float durationEffect = avgRelation * warDurationDays * PeaceRelationDurationScale;

            return immediateScore + durationEffect;
        }

        /// <summary>
        /// Calculates peace score based on religion/culture compatibility.
        /// Positive compatibility = pro-peace (fighting kin wears on you).
        /// Negative compatibility = anti-peace (want to keep fighting heretics/enemies).
        /// Scales with war duration in both directions.
        /// </summary>
        private float CalculateCompatibilityPeaceScore(Kingdom kingdom, Kingdom enemyKingdom, float warDurationDays)
        {
            float religionCompat = DiplomacyHelpers.GetReligionCompatibility(kingdom, enemyKingdom);
            float cultureCompat = DiplomacyHelpers.GetCultureCompatibility(kingdom, enemyKingdom);
            float totalCompat = religionCompat + cultureCompat;

            // Immediate effect (works for both positive and negative)
            float immediateScore = totalCompat * PeaceCompatibilityWeight;

            // Duration scaling
            // Positive: fighting kin becomes harder over time
            // Negative: righteous war against heretics intensifies over time
            float durationEffect = totalCompat * warDurationDays * PeaceCompatibilityDurationScale;

            return immediateScore + durationEffect;
        }

        /// <summary>
        /// Calculates peace score based on territorial claims.
        /// Positive when satisfied (they don't hold our claims).
        /// Negative when they hold our rightful lands (want to continue war).
        /// </summary>
        private float CalculateTerritorialSatisfactionScore(Kingdom kingdom, Kingdom enemyKingdom)
        {
            int ourClaimsTheyHold = 0;
            int theirClaimsWeHold = 0;

            // Check how many of our rightful settlements the enemy holds
            foreach (var settlement in enemyKingdom.Settlements)
            {
                if (settlement.Town == null) continue;
                string rightfulOwner = TORConstants.SettlementPrefixToFaction.GetRightfulOwner(settlement.StringId);
                if (rightfulOwner == kingdom.StringId)
                    ourClaimsTheyHold++;
            }

            // Check how many of their rightful settlements we hold
            foreach (var settlement in kingdom.Settlements)
            {
                if (settlement.Town == null) continue;
                string rightfulOwner = TORConstants.SettlementPrefixToFaction.GetRightfulOwner(settlement.StringId);
                if (rightfulOwner == enemyKingdom.StringId)
                    theirClaimsWeHold++;
            }

            // They hold our claims - want to continue war to reclaim (anti-peace)
            if (ourClaimsTheyHold > 0)
                return -PeaceTerritorialSatisfactionWeight * ourClaimsTheyHold;

            // Satisfied = we hold their stuff but they don't hold ours (pro-peace)
            if (theirClaimsWeHold > 0)
                return PeaceTerritorialSatisfactionWeight;

            // Neutral = neither holds the other's claims (mild pro-peace)
            return PeaceTerritorialSatisfactionWeight * 0.3f;
        }

        /// <summary>
        /// Calculates peace score based on being over war limit.
        /// Strong factor that pushes toward peace with non-alliance wars.
        /// </summary>
        private float CalculateWarLimitPeaceScore(Kingdom kingdom, Kingdom enemyKingdom, bool isAllianceWar)
        {
            int offensiveWars = GetOffensiveWarCount(kingdom);
            int totalWars = kingdom.GetWarCount();

            // Not over limit - no pressure
            if (totalWars <= TORConfig.NumMaxKingdomWars)
                return 0f;

            int warsOverLimit = totalWars - (int) TORConfig.NumMaxKingdomWars;

            // Alliance wars - less willing to abandon even when over limit
            if (isAllianceWar)
                return PeaceOverWarLimitWeight * warsOverLimit * 0.3f;

            // Offensive wars - much more willing to peace when over limit
            return PeaceOverWarLimitWeight * warsOverLimit;
        }

        /// <summary>
        /// Calculates peace score based on common external threats.
        /// If Chaos or other major threat exists and is stronger, want peace with minor enemies.
        /// </summary>
        private float CalculateExternalThreatPeaceScore(Kingdom kingdom, Kingdom enemyKingdom)
        {
            float ourStrength = kingdom.GetAllianceTotalStrength();

            // Find the biggest external threat (not this enemy)
            float biggestThreatStrength = 0f;
            foreach (var otherEnemy in kingdom.GetEnemyKingdoms())
            {
                if (otherEnemy == enemyKingdom) continue;

                // Chaos is always a priority threat
                if (otherEnemy.Culture?.StringId == TORConstants.Cultures.CHAOS)
                {
                    biggestThreatStrength = Math.Max(biggestThreatStrength, otherEnemy.GetAllianceTotalStrength() * 1.5f);
                }
                else
                {
                    biggestThreatStrength = Math.Max(biggestThreatStrength, otherEnemy.GetAllianceTotalStrength());
                }
            }

            // No significant external threat
            if (biggestThreatStrength < ourStrength * PeaceExternalThreatThreshold)
                return 0f;

            // External threat is significant - want peace with this enemy to focus
            float threatRatio = biggestThreatStrength / ourStrength;

            // The current enemy's strength relative to the threat
            // Want peace more with weaker enemies when facing big threat
            float enemyStrength = enemyKingdom.GetAllianceTotalStrength();
            float priorityFactor = biggestThreatStrength / Math.Max(enemyStrength, 1f);

            return PeaceExternalThreatWeight * (threatRatio - PeaceExternalThreatThreshold) * Math.Min(priorityFactor, 3f);
        }

        /// <summary>
        /// Calculates peace score based on lore rivalries and affinities.
        /// Rivalries return negative (anti-peace), affinities return positive (pro-peace).
        /// </summary>
        private float CalculateLoreRelationshipPeaceScore(Kingdom kingdom, Kingdom enemyKingdom)
        {
            float score = 0f;

            // Rivalries - want to keep fighting
            float rivalryLevel = DiplomacyHelpers.GetLoreRivalryLevel(kingdom, enemyKingdom);
            score += rivalryLevel * PeaceLoreRivalryPenalty;

            // Affinities - more willing to make peace with friends
            float affinityLevel = DiplomacyHelpers.GetLoreAffinityLevel(kingdom, enemyKingdom);
            score += affinityLevel * PeaceLoreAffinityBonus;

            return score;
        }

        /// <summary>
        /// Calculates TOR custom war scoring factors for a specific target kingdom.
        /// Used both for target selection and for voting on war decisions.
        /// </summary>
        private float CalculateWarTargetScore(Kingdom declaringKingdom, Kingdom targetKingdom, Clan evaluatingClan)
        {
            // DISTANCE CHECK FIRST - prevent proxy wars against distant kingdoms
            if (!DiplomacyHelpers.IsWithinWarDistance(declaringKingdom, targetKingdom))
                return -200000f; // Too far - don't even consider this war

            var leader = evaluatingClan?.Leader;

            // Get trait modifiers for the evaluating clan's leader
            float honorModifier = DiplomacyHelpers.GetTraitModifier(leader, DefaultTraits.Honor);
            float generosityInverseModifier = DiplomacyHelpers.GetInverseTraitModifier(leader, DefaultTraits.Generosity);
            float calculatingModifier = DiplomacyHelpers.GetTraitModifier(leader, DefaultTraits.Calculating);
            float calculatingInverseModifier = DiplomacyHelpers.GetInverseTraitModifier(leader, DefaultTraits.Calculating);
            float mercyModifier = DiplomacyHelpers.GetTraitModifier(leader, DefaultTraits.Mercy);
            float valorModifier = DiplomacyHelpers.GetTraitModifier(leader, DefaultTraits.Valor);

            // Traitorous behavior penalties (scaled by 100, modified by Honor)
            // Dishonorable lords (-2 Honor) barely care about breaking agreements
            float tradeAgreementPenalty = declaringKingdom.HasTradeAgreementWith(targetKingdom) ? -3000f * honorModifier : 0f;
            float alliancePenalty = declaringKingdom.IsAllyWith(targetKingdom) ? -7000f * honorModifier : 0f;

            // Individual scoring factors
            // Riches: Greedy lords (low Generosity) care more, Calculating lords amplify awareness
            float richesScore = CalculateRichesScore(declaringKingdom, targetKingdom) * generosityInverseModifier * calculatingModifier;
            // Relations: Cruel lords (-2 Mercy) ignore personal relationships
            float relationScore = CalculateRelationScore(evaluatingClan, targetKingdom) * mercyModifier;
            float distanceScore = CalculateDistanceScore(declaringKingdom, targetKingdom);
            // Religion: Pragmatists (high Calculating) and cruel lords (low Mercy) ignore religion
            float religionScore = CalculateReligionScore(declaringKingdom, targetKingdom) * calculatingInverseModifier * mercyModifier;
            // Culture: Honorable lords respect cultural bonds, Pragmatists ignore cultural sentiment
            float cultureScore = CalculateCultureScore(declaringKingdom, targetKingdom) * honorModifier * calculatingInverseModifier;
            // Territorial: Greedy lords (low Generosity) protect direct claims, Merciful lords liberate kin
            float territorialScore = CalculateTerritorialIntegrityScore(declaringKingdom, targetKingdom, leader);
            // Rivalry: Brave lords (+2 Valor) are more motivated by glory/grudges
            float rivalryScore = CalculateLorewiseRivalryScore(declaringKingdom, targetKingdom) * valorModifier;
            // Tactical: Cruel lords strike harder when strong, Brave/Merciful lords ignore unfavorable odds, Calculating amplifies
            float baseTacticalScore = CalculateTacticalScore(declaringKingdom, targetKingdom);
            float mercyInverseModifier = DiplomacyHelpers.GetInverseTraitModifier(leader, DefaultTraits.Mercy);
            float adjustedTactical = baseTacticalScore >= 0
                ? baseTacticalScore * valorModifier * mercyInverseModifier  // Positive: brave + cruel hit harder
                : baseTacticalScore / Math.Max(valorModifier * mercyModifier, 0.1f);  // Negative: brave + merciful ignore bad odds
            float tacticalScore = adjustedTactical * calculatingModifier;

            float totalScore = tradeAgreementPenalty
                             + alliancePenalty
                             + richesScore
                             + relationScore
                             + distanceScore
                             + religionScore
                             + cultureScore
                             + territorialScore
                             + rivalryScore
                             + tacticalScore;

            return totalScore;
        }

        /// <summary>
        /// Calculates lorewise rivalry bonuses based on Warhammer lore.
        /// </summary>
        private float CalculateLorewiseRivalryScore(Kingdom declaringKingdom, Kingdom targetKingdom)
        {
            float score = 0f;

            // 1. KARAK RECLAMATION - Dwarfs vs Greenskins holding Karaks
            if (declaringKingdom.Culture?.StringId == TORConstants.Cultures.DAWI)
            {
                var targetPantheon = targetKingdom.Leader.GetDominantReligion().Pantheon;
                if (targetPantheon == Pantheon.Greenskin)
                {
                    // Count how many Karaks (dwarf settlements) the greenskins hold
                    int karakCount = 0;
                    foreach (var settlement in targetKingdom.Settlements)
                    {
                        if (settlement.Town == null) continue;
                        if (!settlement.IsDwarfKarak()) continue;
                        karakCount++;
                    }
                    score += karakCount * KarakReclamationWeight;
                }
            }

            // 2. ANTI-CHAOS - Good factions vs Chaos
            var targetCulture = targetKingdom.Culture?.StringId;
            if (targetCulture == TORConstants.Cultures.CHAOS)
            {
                var myPantheon = declaringKingdom.Leader.GetDominantReligion().Pantheon;
                // Good pantheons: Empire, Bretonnia, Dwarfs, Elves
                if (myPantheon == Pantheon.Human ||
                    myPantheon == Pantheon.Dwarven ||
                    myPantheon == Pantheon.Elven)
                {
                    // Count Chaos-held settlements (any settlement they hold is an affront)
                    int chaosSettlements = targetKingdom.Settlements.Count(s => s.Town != null);
                    score += chaosSettlements * AntiChaosWeight;
                }
            }

            // 3. LORE RIVALRIES - Use centralized helper for faction-specific rivalries
            float rivalryLevel = DiplomacyHelpers.GetLoreRivalryLevel(declaringKingdom, targetKingdom);
            score += rivalryLevel * LoreRivalryWarWeight;

            // 4. LORE AFFINITIES - Friendly factions are less likely to war
            float affinityLevel = DiplomacyHelpers.GetLoreAffinityLevel(declaringKingdom, targetKingdom);
            score += affinityLevel * LoreAffinityWarPenalty;

            return score;
        }

        /// <summary>
        /// Calculates territorial integrity score based on claims.
        /// - Direct claims: Settlements that rightfully belong to the declaring kingdom (modified by Generosity inverse)
        /// - Cultural claims: Settlements of same culture held by foreign culture (modified by Mercy)
        /// - Pantheon claims: Settlements of same pantheon held by different pantheon (modified by Mercy)
        /// Distance factor applied: nearby claims worth more than distant ones.
        /// </summary>
        private float CalculateTerritorialIntegrityScore(Kingdom declaringKingdom, Kingdom targetKingdom, Hero leader = null)
        {
            // Trait modifiers for territorial claims
            float generosityInverseMod = DiplomacyHelpers.GetInverseTraitModifier(leader, DefaultTraits.Generosity);
            float mercyMod = DiplomacyHelpers.GetTraitModifier(leader, DefaultTraits.Mercy);
            float directClaimScore = 0f;
            float culturalClaimScore = 0f;
            float pantheonClaimScore = 0f;

            var myPantheon = declaringKingdom.Leader?.GetDominantReligion()?.Pantheon ?? Pantheon.Human;
            var myMidSettlement = declaringKingdom.FactionMidSettlement;

            foreach (var settlement in targetKingdom.Settlements)
            {
                // Skip villages - only towns and castles
                if (settlement.Town == null)
                    continue;

                string rightfulOwner = TORConstants.SettlementPrefixToFaction.GetRightfulOwner(settlement.StringId);
                if (string.IsNullOrEmpty(rightfulOwner))
                    continue;

                // Calculate distance factor - closer settlements get higher weight
                float distanceFactor = CalculateSettlementDistanceFactor(myMidSettlement, settlement);

                // Direct Claim - this settlement belongs to us
                if (rightfulOwner == declaringKingdom.StringId)
                {
                    directClaimScore += distanceFactor;
                    continue;
                }

                // Cultural Claim - settlement belongs to our culture but held by foreign culture
                string rightfulCulture = TORConstants.SettlementPrefixToFaction.GetFactionCulture(rightfulOwner);
                if (rightfulCulture != null &&
                    rightfulCulture == declaringKingdom.Culture?.StringId &&
                    targetKingdom.Culture?.StringId != rightfulCulture)
                {
                    culturalClaimScore += distanceFactor;
                    continue;
                }

                // Pantheon Claim - settlement belongs to our pantheon but held by different pantheon
                var rightfulPantheon = ReligionObjectHelper.GetPantheon(rightfulCulture);
                var holderPantheon = targetKingdom.Leader?.GetDominantReligion()?.Pantheon ?? Pantheon.Human;
                if (rightfulPantheon == myPantheon && holderPantheon != rightfulPantheon)
                {
                    pantheonClaimScore += distanceFactor;
                }
            }

            // Apply trait modifiers: Greedy lords care about direct claims, Merciful lords care about liberating kin
            return directClaimScore * DirectClaimWeight * generosityInverseMod
                 + culturalClaimScore * CulturalClaimWeight * mercyMod
                 + pantheonClaimScore * PantheonClaimWeight * mercyMod;
        }

        // Resource value tiers for missing resource bonus
        private static readonly Dictionary<string, float> ResourceValues = new()
        {
            // Strategic (high value)
            { "iron_mine", 1500f },
            { "silver_mine", 1500f },
            // Valuable
            { "salt_mine", 800f },
            { "europe_horse_ranch", 600f },
            { "steppe_horse_ranch", 600f },
            { "desert_horse_ranch", 600f },
            { "battanian_horse_ranch", 600f },
            { "sturgian_horse_ranch", 600f },
            { "vlandian_horse_ranch", 600f },
            // Useful materials
            { "lumberjack", 400f },
            { "clay_mine", 300f },
            { "flax_plant", 300f },
            // Luxury
            { "silk_plant", 500f },
            { "trapper", 400f },
            { "vineyard", 300f },
            // Food (common but still valuable)
            { "wheat_farm", 200f },
            { "fisherman", 200f },
            { "cattle_farm", 200f },
            { "sheep_farm", 200f },
            { "swine_farm", 200f },
            { "date_farm", 150f },
            { "olive_trees", 150f },
        };

        /// <summary>
        /// Calculates score based on target kingdom's overall wealth.
        /// Considers: village hearth, town prosperity, and missing resources.
        /// </summary>
        private float CalculateRichesScore(Kingdom declaringKingdom, Kingdom targetKingdom)
        {
            if (targetKingdom.Settlements == null || !targetKingdom.Settlements.Any())
                return 0f;

            float totalHearth = 0f;
            float totalProsperity = 0f;
            HashSet<string> ourResources = new();
            HashSet<string> theirResources = new();

            // Gather our kingdom's resources
            foreach (var settlement in declaringKingdom.Settlements)
            {
                if (settlement.IsVillage && settlement.Village?.VillageType != null)
                    ourResources.Add(settlement.Village.VillageType.StringId);
            }

            // Gather target kingdom's wealth and resources
            foreach (var settlement in targetKingdom.Settlements)
            {
                if (settlement.IsVillage && settlement.Village != null)
                {
                    totalHearth += settlement.Village.Hearth;
                    if (settlement.Village.VillageType != null)
                        theirResources.Add(settlement.Village.VillageType.StringId);
                }
                else if (settlement.IsTown && settlement.Town != null)
                {
                    totalProsperity += settlement.Town.Prosperity;
                }
            }

            // Calculate missing resource bonus (resources they have that we don't)
            float missingResourceScore = 0f;
            foreach (var resource in theirResources)
            {
                if (!ourResources.Contains(resource))
                {
                    missingResourceScore += ResourceValues.GetValueOrDefault(resource, 100f);
                }
            }

            // Weight components (scaled for war scoring)
            // Hearth: ~200-800 per village, total could be 2000-8000 for a kingdom
            float hearthScore = totalHearth * 0.5f;  // 1000-4000 range
            // Prosperity: ~3000-6000 per town, total could be 10000-30000
            float prosperityScore = totalProsperity * 0.1f;  // 1000-3000 range
            // Missing resources: 0-5000+ depending on what they have

            float totalScore = hearthScore + prosperityScore + missingResourceScore;

            // Competition factor: reduce if others are already at war with target
            int competitorCount = targetKingdom.GetWarCount();
            float competitionFactor = Math.Max(0.2f, 1f - competitorCount * 0.25f);

            return totalScore * competitionFactor;
        }

        /// <summary>
        /// Calculates score based on evaluating clan's relations with target kingdom's lords.
        /// Returns: -3000 (friendly relations) to +3000 (hostile relations) (scaled by 100)
        /// </summary>
        private float CalculateRelationScore(Clan evaluatingClan, Kingdom targetKingdom)
        {
            if (evaluatingClan?.Leader == null || targetKingdom == null)
                return 0f;

            var clanLeader = evaluatingClan.Leader;
            float totalRelation = 0f;
            int lordCount = 0;

            foreach (var targetClan in targetKingdom.Clans)
            {
                if (targetClan.Leader != null && targetClan.Leader.IsAlive)
                {
                    totalRelation += clanLeader.GetRelation(targetClan.Leader);
                    lordCount++;
                }
            }

            if (lordCount == 0)
                return 1000f; // No lords = neutral

            float averageRelation = totalRelation / lordCount;

            // Map: +80 -> -3000, +-25 -> +1000, -80 -> +3000 (scaled by 100)
            // Higher relation = less likely to declare war
            if (averageRelation >= 80)
                return -3000f;
            if (averageRelation >= 25)
                return -1500f;
            if (averageRelation >= -25)
                return 1000f;
            if (averageRelation >= -80)
                return 2000f;
            return 3000f;
        }

        /// <summary>
        /// Calculates score based on distance between kingdoms.
        /// Close targets get bonus, distant targets get penalty.
        /// Neutral point at ~500 distance. (scaled by 100)
        /// </summary>
        private float CalculateDistanceScore(Kingdom declaringKingdom, Kingdom targetKingdom)
        {
            float distance = DiplomacyHelpers.GetKingdomDistance(declaringKingdom, targetKingdom);

            if (distance >= 300)
                return -5000f; // No valid distance = strong penalty

            // Neutral distance where score is 0
            const float neutralDistance = 500f;
            // Scale factor for how much distance affects score (scaled by 100)
            const float distanceScaleFactor = 5f;

            // Below neutral: positive, above neutral: negative
            // At 0 distance: +2500, at 500: 0, at 1000: -2500, at 1500: -5000
            float distanceScore = (neutralDistance - distance) * distanceScaleFactor;

            // Clamp to reasonable range
            return Math.Max(-5000f, Math.Min(2500f, distanceScore));
        }

        /// <summary>
        /// Calculates score based on religious compatibility.
        /// Hostile religions are more attractive targets.
        /// </summary>
        private float CalculateReligionScore(Kingdom declaringKingdom, Kingdom targetKingdom)
        {
            float religionCompatibility = DiplomacyHelpers.GetReligionCompatibility(declaringKingdom, targetKingdom);
            // Negative compatibility = more likely to declare war (scaled by 100)
            return -religionCompatibility * TORConfig.DeclareWarScoreReligiousEffectMultiplier * 100f;
        }

        /// <summary>
        /// Calculates score based on cultural compatibility.
        /// Hostile cultures are more attractive targets.
        /// </summary>
        private float CalculateCultureScore(Kingdom declaringKingdom, Kingdom targetKingdom)
        {
            float cultureCompat = DiplomacyHelpers.GetCultureCompatibility(declaringKingdom, targetKingdom);
            // Negative compatibility = more likely to declare war
            return -cultureCompat * WarCultureCompatibilityWeight;
        }

        /// <summary>
        /// Calculates tactical score based on war situations of both kingdoms. (scaled by 100)
        /// - Strength comparison: weaker targets are more attractive
        /// - Target already fighting wars = opportunity (they're stretched thin)
        /// - We already fighting wars = penalty (we're stretched thin)
        /// </summary>
        private float CalculateTacticalScore(Kingdom declaringKingdom, Kingdom targetKingdom)
        {
            float score = 0f;

            // Strength comparison - weaker kingdoms are more attractive targets
            // Uses alliance strength to account for defensive pacts
            float ourStrength = declaringKingdom.GetAllianceTotalStrength();
            float theirStrength = Math.Max(targetKingdom.GetAllianceTotalStrength(), 1f);
            float strengthRatio = ourStrength / theirStrength;
            // ratio > 1 = we're stronger (bonus), ratio < 1 = they're stronger (penalty)
            // Scaled by 100 for DenarsToInfluence compatibility
            score += strengthRatio * TORConfig.DeclareWarScoreFactionStrengthMultiplier * 100f;

            // Target's current enemy count - opportunity to strike while they're distracted
            // Bonus for each enemy, but plateau after 2 (scaled by 100)
            // 0 enemies: 0, 1 enemy: +1500, 2+ enemies: +3000 (cap)
            int targetEnemyCount = targetKingdom.GetWarCount();
            score += Math.Min(targetEnemyCount * 1500f, 3000f);

            // Our current war count - penalty for overextension (scaled by 100)
            // Each offensive war we're fighting reduces appetite for new wars
            int ourOffensiveWars = GetOffensiveWarCount(declaringKingdom);
            // 0 wars: +2000 bonus (eager), 1 war: 0, 2 wars: -2000, 3+ wars: -4000 (cap)
            score += (1 - ourOffensiveWars) * 2000f;
            score = Math.Max(score, -4000f); // Floor at -4000

            return score;
        }

        /// <summary>
        /// Gets the number of offensive wars (excluding alliance/defensive wars).
        /// </summary>
        private int GetOffensiveWarCount(Kingdom kingdom)
        {
            var allianceWarBehavior = Campaign.Current?.GetCampaignBehavior<TORAllianceWarBehavior>();
            if (allianceWarBehavior != null)
            {
                return allianceWarBehavior.GetOffensiveWarCount(kingdom);
            }
            // Fallback to total wars if behavior not available
            return kingdom.GetWarCount();
        }

        public override float GetScoreOfMercenaryToJoinKingdom(Clan mercenaryClan, Kingdom kingdom)
        {
            var score = base.GetScoreOfMercenaryToJoinKingdom(mercenaryClan, kingdom);

            if (kingdom == null || mercenaryClan == null) return score;

            if (kingdom.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                if (mercenaryClan.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                {
                    score = +1000;
                }
                else
                {
                    score = -10000;
                }
            }

            if (kingdom.Culture.StringId != TORConstants.Cultures.BRETONNIA && mercenaryClan.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                score = -10000;
            }

            if (mercenaryClan.StringId == "tor_dog_clan_hero_curse" && (kingdom.Culture.StringId == TORConstants.Cultures.SYLVANIA || kingdom.Culture.StringId == TORConstants.Cultures.MOUSILLON || kingdom.Culture.StringId == TORConstants.Cultures.BRETONNIA))
            {
                score = -10000;
            }

            if (mercenaryClan.Culture.StringId == TORConstants.Cultures.DRUCHII)
            {
                score = -10000;
            }

            return score;
        }

        /// <summary>
        /// Calculates the best war target candidate for a kingdom.
        ///
        /// BASE GAME (DefaultDiplomacyModel) considerations:
        /// - War declaration permission checks (not at war, not recently made peace, etc.)
        /// - Clan influences and kingdom decision mechanics
        ///
        /// TOR ADDITIONS - War Target Scoring Factors (higher score = more likely target):
        /// 1. DISTANCE: Closer kingdoms score higher (configurable multiplier)
        /// 2. STRENGTH: Weaker kingdoms score higher (our strength / their strength × multiplier)
        /// 3. RELIGION: Hostile religions score higher (negative compatibility × multiplier)
        /// 4. CULTURE: Hostile cultures score higher (negative compatibility × 50)
        /// 5. TERRITORIAL INTEGRITY (Hierarchical claims):
        ///    - Direct Claims (+200 per settlement): Our faction's rightful settlements
        ///    - Cultural Claims (+80 per settlement): Our culture's settlements held by foreigners
        ///    - Pantheon Claims (+30 per settlement): Our pantheon's settlements held by other faiths
        /// </summary>
        public Kingdom GetWarDeclarationTargetCandidate(Kingdom consideringKingdom)
        {
            if (consideringKingdom == null) return null;

            var permissionModel = Campaign.Current?.Models?.KingdomDecisionPermissionModel;
            if (permissionModel == null) return null;

            var kingdomCandidates = Kingdom.All.WhereQ(x =>
                !x.IsEliminated &&
                x != consideringKingdom &&
                permissionModel.IsWarDecisionAllowedBetweenKingdoms(consideringKingdom, x, out _) &&
                !consideringKingdom.IsAtWarWith(x) &&
                (x.GetStanceWith(consideringKingdom)?.PeaceDeclarationDate == null ||
                x.GetStanceWith(consideringKingdom)?.PeaceDeclarationDate.ElapsedDaysUntilNow > TORConfig.MinPeaceDays)).ToListQ();

            if (kingdomCandidates.Count > 0)
            {
                // Calculate scores for all candidates using shared scoring method
                Dictionary<Kingdom, float> candidateScores = [];

                foreach (var targetKingdom in kingdomCandidates)
                {
                    candidateScores[targetKingdom] = CalculateWarTargetScore(consideringKingdom, targetKingdom, consideringKingdom.RulingClan);
                }

                var candidate = candidateScores.OrderByDescending(x => x.Value).Take(3).GetRandomElementInefficiently().Key;
                return candidate;
            }
            return null;
        }

        /// <summary>
        /// Calculates the best peace target candidate for a kingdom.
        /// </summary>
        public Kingdom GetPeaceDeclarationTargetCandidate(Kingdom consideringKingdom, bool isEmergency = false)
        {
            if (consideringKingdom == null) return null;

            var permissionModel = Campaign.Current?.Models?.KingdomDecisionPermissionModel;
            if (permissionModel == null) return null;

            var kingdomCandidates = Kingdom.All.WhereQ(x =>
                !x.IsEliminated &&
                x != consideringKingdom &&
                permissionModel.IsPeaceDecisionAllowedBetweenKingdoms(consideringKingdom, x, out _) &&
                consideringKingdom.IsAtWarWith(x) &&
                (x.GetStanceWith(consideringKingdom)?.WarStartDate.ElapsedDaysUntilNow > TORConfig.MinWarDays ||
                isEmergency)).ToListQ();

            if (kingdomCandidates.Count > 0)
            {
                var kingdomListByStrength = kingdomCandidates.SelectQ(x =>
                    new Tuple<Kingdom, float>(x, x.GetAllianceTotalStrength())).ToListQ();

                Dictionary<Kingdom, float> candidateScores = [];

                foreach (var tuple in kingdomListByStrength)
                {
                    // Prefer making peace with stronger enemies
                    candidateScores[tuple.Item1] = tuple.Item1.GetAllianceTotalStrength() / consideringKingdom.GetAllianceTotalStrength();
                }

                var maxvalue = candidateScores.Values.Max();
                if (maxvalue > 1)
                {
                    var candidate = candidateScores.MaxBy(x => x.Value).Key;
                    return candidate;
                }
            }
            return null;
        }

        /// <summary>
        /// Calculates the best alliance target candidate for a kingdom.
        /// </summary>
        public Kingdom GetAllianceDeclarationTargetCandidate(Kingdom consideringKingdom)
        {
            if (consideringKingdom == null) return null;

            var permissionModel = Campaign.Current?.Models?.KingdomDecisionPermissionModel;
            if (permissionModel == null) return null;

            var kingdomCandidates = Kingdom.All.WhereQ(x =>
                !x.IsEliminated &&
                x != consideringKingdom &&
                !consideringKingdom.IsAtWarWith(x) &&
                !consideringKingdom.IsAllyWith(x) &&
                permissionModel.IsStartAllianceDecisionAllowedBetweenKingdoms(consideringKingdom, x, out _)).ToListQ();

            if (kingdomCandidates.Count > 0)
            {
                Dictionary<Kingdom, float> candidateScores = [];

                foreach (var candidate in kingdomCandidates)
                {
                    float score = 0;

                    // Religious similarity bonus
                    var religionScore = ReligionObjectHelper.CalculateReligionCompatibility(
                        candidate.Leader.GetDominantReligion(),
                        consideringKingdom.Leader.GetDominantReligion());
                    score += religionScore * AllianceReligionCompatibilityWeight;

                    // Cultural compatibility bonus
                    float cultureCompat = ReligionObjectHelper.CalculateCultureCompatibility(
                        consideringKingdom.Culture?.StringId, candidate.Culture?.StringId);
                    score += cultureCompat * AllianceCultureCompatibilityWeight;

                    // Strength consideration - prefer allying with stronger kingdoms when threatened
                    var totalEnemyStrength = consideringKingdom.GetTotalEnemyAllianceStrength();
                    if (totalEnemyStrength > consideringKingdom.CurrentTotalStrength)
                    {
                        score += candidate.CurrentTotalStrength / consideringKingdom.CurrentTotalStrength * AllianceStrengthWeight;
                    }

                    // Distance consideration - prefer nearby allies
                    float distance = DiplomacyHelpers.GetKingdomDistance(consideringKingdom, candidate);
                    score -= distance * AllianceDistancePenaltyFactor;

                    // Lore rivalry penalty - rivals make poor allies
                    float rivalryLevel = DiplomacyHelpers.GetLoreRivalryLevel(consideringKingdom, candidate);
                    score += rivalryLevel * AllianceLoreRivalryPenalty;

                    // Lore affinity bonus - friends make good allies
                    float affinityLevel = DiplomacyHelpers.GetLoreAffinityLevel(consideringKingdom, candidate);
                    score += affinityLevel * AllianceLoreAffinityBonus;

                    candidateScores[candidate] = score;
                }

                if (candidateScores.Any())
                {
                    var bestCandidate = candidateScores.MaxBy(x => x.Value);
                    if (bestCandidate.Value > AllianceMinimumScoreThreshold)
                    {
                        return bestCandidate.Key;
                    }
                }
            }
            return null;
        }

        // Note: Trade agreements were removed in 1.3. Alliances are now handled by IAllianceCampaignBehavior.
        // Note: GetKingdomDistance moved to DiplomacyHelpers

        /// <summary>
        /// Calculates a distance factor for territorial claims.
        /// Returns 1.0 for nearby settlements, decreasing to TerritorialMinDistanceFactor for distant ones.
        /// Uses inverse formula: factor = 1 / (1 + distance / scaling)
        /// </summary>
        private float CalculateSettlementDistanceFactor(TaleWorlds.CampaignSystem.Settlements.Settlement fromSettlement, TaleWorlds.CampaignSystem.Settlements.Settlement toSettlement)
        {
            if (fromSettlement == null || toSettlement == null)
                return 0;

            float distance = fromSettlement.Position.Distance(toSettlement.Position);

            // Inverse decay formula: closer = higher factor
            // At distance 0: factor = 1.0
            // At distance = TerritorialDistanceScaling (500): factor ≈ 0.5
            // At very large distances: approaches TerritorialMinDistanceFactor
            float rawFactor = 1f / (1f + distance / TerritorialDistanceScaling);

            // Scale to range [TerritorialMinDistanceFactor, 1.0]
            return TerritorialMinDistanceFactor + rawFactor * (1f - TerritorialMinDistanceFactor);
        }

    }
}