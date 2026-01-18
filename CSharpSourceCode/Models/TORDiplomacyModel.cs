using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Diplomacy;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORDiplomacyModel : DefaultDiplomacyModel
    {
        // War declaration weights
        private const float WarCultureCompatibilityWeight = 50f;

        // Territorial Integrity weights (hierarchical claims)
        private const float DirectClaimWeight = 200f;      // My own settlement
        private const float CulturalClaimWeight = 80f;     // My culture's settlement held by foreigners
        private const float PantheonClaimWeight = 30f;     // My pantheon's settlement held by other pantheons

        // Lorewise Rivalry weights
        private const float KarakReclamationWeight = 150f;  // Dwarfs vs Greenskins holding Karaks
        private const float AntiChaosWeight = 120f;         // Good factions vs Chaos
        private const float NordlandLaurelornRivalryWeight = 50f; // Nordland vs Laurelorn (territorial dispute)

        // Alliance candidate weights
        private const float AllianceReligionCompatibilityWeight = 100f;
        private const float AllianceCultureCompatibilityWeight = 75f;
        private const float AllianceStrengthWeight = 50f;
        private const float AllianceDistancePenaltyFactor = 0.01f;
        private const float AllianceMinimumScoreThreshold = 50f;

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
                float baseScore = base.GetScoreOfDeclaringWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out reason, includeReason);

                // Add TOR custom scoring factors
                float customScore = CalculateWarTargetScore(declaringKingdom, targetKingdom);

                return baseScore + customScore;
            }

            return base.GetScoreOfDeclaringWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out reason, includeReason);
        }

        public override float GetScoreOfDeclaringPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace)
        {
            // Chaos really shouldn't be allowed to make peace
            if (factionDeclaresPeace.Culture.StringId == TORConstants.Cultures.CHAOS || factionDeclaredPeace.Culture.StringId == TORConstants.Cultures.CHAOS)
            {
                return float.MinValue;
            }

            if (factionDeclaresPeace is Kingdom kingdom && factionDeclaredPeace is Kingdom enemyKingdom)
            {
                int offensiveWars = GetOffensiveWarCount(kingdom);
                int totalWars = kingdom.GetNumActiveKingdomWars();

                // If under minimum wars, don't seek peace
                if (totalWars <= TORConfig.NumMinKingdomWars) return -100000;

                // If over maximum offensive wars, strongly favor peace
                if (offensiveWars > TORConfig.NumMaxKingdomWars) return 100000;

                // If we have alliance wars pushing us over the limit, prioritize peace with NON-alliance enemies
                if (totalWars > TORConfig.NumMaxKingdomWars)
                {
                    var allianceWarBehavior = Campaign.Current?.GetCampaignBehavior<TORAllianceWarBehavior>();
                    if (allianceWarBehavior != null)
                    {
                        bool isAllianceWar = allianceWarBehavior.IsAllianceWar(kingdom, enemyKingdom);
                        if (!isAllianceWar)
                        {
                            // Strongly favor peace with non-alliance enemies when stretched thin
                            return 50000;
                        }
                        // Less likely to abandon alliance wars
                        return -50000;
                    }
                }
            }

            return base.GetScoreOfDeclaringPeace(factionDeclaresPeace, factionDeclaredPeace);
        }

        /// <summary>
        /// Calculates TOR custom war scoring factors for a specific target kingdom.
        /// Used both for target selection and for voting on war decisions.
        /// </summary>
        private float CalculateWarTargetScore(Kingdom declaringKingdom, Kingdom targetKingdom)
        {
            float score = 0f;

            // Distance - closer kingdoms score higher
            float distance = GetKingdomDistance(declaringKingdom, targetKingdom);
            // Normalize and invert (closer = higher score)
            // Simple approach: assume max distance ~2000, closer gets higher score
            if (distance < float.MaxValue)
            {
                float normalizedDistance = Math.Min(distance / 2000f, 1f);
                score += (1f - normalizedDistance) * TORConfig.DeclareWarScoreDistanceMultiplier;
            }

            // Strength - weaker kingdoms score higher
            float strengthRatio = declaringKingdom.GetAllianceTotalStrength() / Math.Max(targetKingdom.GetAllianceTotalStrength(), 1f);
            score += strengthRatio * TORConfig.DeclareWarScoreFactionStrengthMultiplier;

            // Religion - hostile religions score higher
            var religion1 = declaringKingdom.Leader?.GetDominantReligion();
            var religion2 = targetKingdom.Leader?.GetDominantReligion();
            if (religion1 != null && religion2 != null)
            {
                float religionCompatibility = ReligionObjectHelper.CalculateReligionCompatibility(religion1, religion2);
                score += -religionCompatibility * TORConfig.DeclareWarScoreReligiousEffectMultiplier;
            }

            // Culture - hostile cultures score higher
            float cultureCompat = ReligionObjectHelper.CalculateCultureCompatibility(
                declaringKingdom.Culture?.StringId, targetKingdom.Culture?.StringId);
            score += -cultureCompat * WarCultureCompatibilityWeight;

            // Territorial Integrity - hierarchical claims
            int directClaims = 0;
            int culturalClaims = 0;
            int pantheonClaims = 0;
            var myPantheon = declaringKingdom.Leader.GetDominantReligion().Pantheon;

            foreach (var settlement in targetKingdom.Settlements)
            {
                // Skip villages - only towns and castles
                if (settlement.Town == null)
                    continue;

                string rightfulOwner = TORConstants.SettlementPrefixToFaction.GetRightfulOwner(settlement.StringId);
                if (string.IsNullOrEmpty(rightfulOwner))
                    continue;

                // Direct Claim
                if (rightfulOwner == declaringKingdom.StringId)
                {
                    directClaims++;
                    continue;
                }

                // Cultural Claim
                string rightfulCulture = TORConstants.SettlementPrefixToFaction.GetFactionCulture(rightfulOwner);
                if (rightfulCulture != null &&
                    rightfulCulture == declaringKingdom.Culture?.StringId &&
                    targetKingdom.Culture?.StringId != rightfulCulture)
                {
                    culturalClaims++;
                    continue;
                }

                // Pantheon Claim
                var rightfulPantheon = ReligionObjectHelper.GetPantheon(rightfulCulture);
                var holderPantheon = targetKingdom.Leader.GetDominantReligion().Pantheon;
                if (rightfulPantheon == myPantheon && holderPantheon != rightfulPantheon)
                {
                    pantheonClaims++;
                }
            }

            score += directClaims * DirectClaimWeight;
            score += culturalClaims * CulturalClaimWeight;
            score += pantheonClaims * PantheonClaimWeight;

            // Lorewise Rivalries
            score += CalculateLorewiseRivalryScore(declaringKingdom, targetKingdom);

            return score;
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

                        string rightfulOwner = TORConstants.SettlementPrefixToFaction.GetRightfulOwner(settlement.StringId);
                        if (!string.IsNullOrEmpty(rightfulOwner))
                        {
                            string rightfulCulture = TORConstants.SettlementPrefixToFaction.GetFactionCulture(rightfulOwner);
                            if (rightfulCulture == TORConstants.Cultures.DAWI)
                            {
                                karakCount++;
                            }
                        }
                    }
                    score += karakCount * KarakReclamationWeight;
                }
            }

            // 2. ANTI-CHAOS - Good factions vs Chaos
            var targetCulture = targetKingdom.Culture?.StringId;
            if (targetCulture == TORConstants.Cultures.CHAOS)
            {
                var myPantheon = targetKingdom.Leader.GetDominantReligion().Pantheon;
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

            // 3. NORDLAND vs LAURELORN - Territorial forest dispute
            if (declaringKingdom.StringId == TORConstants.Factions.NORDLAND &&
                targetKingdom.StringId == TORConstants.Factions.LAURELORN)
            {
                score += NordlandLaurelornRivalryWeight;
            }
            else if (declaringKingdom.StringId == TORConstants.Factions.LAURELORN &&
                     targetKingdom.StringId == TORConstants.Factions.NORDLAND)
            {
                score += NordlandLaurelornRivalryWeight;
            }

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
            return kingdom.GetNumActiveKingdomWars();
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

            if (mercenaryClan.StringId == "tor_dog_clan_hero_curse" && (kingdom.Culture.StringId == TORConstants.Cultures.SYLVANIA || kingdom.Culture.StringId == "mousillon" || kingdom.Culture.StringId == TORConstants.Cultures.BRETONNIA))
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
                    candidateScores[targetKingdom] = CalculateWarTargetScore(consideringKingdom, targetKingdom);
                }

                var candidate = candidateScores.MaxBy(x => x.Value).Key;
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
                !consideringKingdom.IsAlliedWith(x) &&
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
                    var totalEnemyStrength = consideringKingdom.GetSumEnemyKingdomPower();
                    if (totalEnemyStrength > consideringKingdom.CurrentTotalStrength)
                    {
                        score += candidate.CurrentTotalStrength / consideringKingdom.CurrentTotalStrength * AllianceStrengthWeight;
                    }

                    // Distance consideration - prefer nearby allies
                    float distance = GetKingdomDistance(consideringKingdom, candidate);
                    score -= distance * AllianceDistancePenaltyFactor;

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

        /// <summary>
        /// Gets the approximate distance between two kingdoms based on their mid settlements.
        /// </summary>
        private float GetKingdomDistance(Kingdom kingdom1, Kingdom kingdom2)
        {
            if (kingdom1.FactionMidSettlement == null || kingdom2.FactionMidSettlement == null)
            {
                return float.MaxValue;
            }

            var pos1 = kingdom1.FactionMidSettlement.Position;
            var pos2 = kingdom2.FactionMidSettlement.Position;
            return pos1.Distance(pos2);
        }

        private static float MapToRange(float value, float minSource, float maxSource, float minTarget = float.MinValue, float maxTarget = float.MaxValue)
        {
            if (Math.Abs(maxSource - minSource) < 0.0001f)
            {
                return (minTarget + maxTarget) / 2;
            }
            var result = (value - minSource) / (maxSource - minSource) * (maxTarget - minTarget) + minTarget;
            return result;
        }
    }
}