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
        // War declaration weights (scaled by 100 for DenarsToInfluence compatibility)
        private const float WarCultureCompatibilityWeight = 5000f;

        // Territorial Integrity weights (hierarchical claims)
        private const float DirectClaimWeight = 5000f;      // My own settlement
        private const float CulturalClaimWeight = 2500f;     // My culture's settlement held by foreigners
        private const float PantheonClaimWeight = 1000f;     // My pantheon's settlement held by other pantheons

        // Lorewise Rivalry weights
        private const float KarakReclamationWeight = 15000f;  // Dwarfs vs Greenskins holding Karaks
        private const float AntiChaosWeight = 12000f;         // Good factions vs Chaos
        private const float NordlandLaurelornRivalryWeight = 5000f; // Nordland vs Laurelorn (territorial dispute)

        // Territorial distance factor settings
        private const float TerritorialDistanceScaling = 100f;  // Distance at which factor is ~0.5
        private const float TerritorialMinDistanceFactor = 0.2f; // Minimum factor for very distant claims

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
                
                // Add TOR custom scoring factors
                float customScore = CalculateWarTargetScore(declaringKingdom, targetKingdom, evaluatingClan);

                customScore += 0;
                customScore += MBRandom.RandomInt(-25,25);

                TORCommon.Say(customScore+"");
                return customScore;
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
        private float CalculateWarTargetScore(Kingdom declaringKingdom, Kingdom targetKingdom, Clan evaluatingClan)
        {
            // Traitorous behavior penalties (scaled by 100)
            float tradeAgreementPenalty = declaringKingdom.HasTradeAgreementWith(targetKingdom) ? -3000f : 0f;
            float alliancePenalty = declaringKingdom.IsAllyWith(targetKingdom) ? -7000f : 0f;

            // Individual scoring factors
            float richesScore = CalculateRichesScore(targetKingdom);
            float relationScore = CalculateRelationScore(evaluatingClan, targetKingdom);
            float distanceScore = CalculateDistanceScore(declaringKingdom, targetKingdom);
            float religionScore = CalculateReligionScore(declaringKingdom, targetKingdom);
            float cultureScore = CalculateCultureScore(declaringKingdom, targetKingdom);
            float territorialScore = CalculateTerritorialIntegrityScore(declaringKingdom, targetKingdom);
            float rivalryScore = CalculateLorewiseRivalryScore(declaringKingdom, targetKingdom);
            float tacticalScore = CalculateTacticalScore(declaringKingdom, targetKingdom);

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
                        
                        
                        if(!settlement.IsDwarfKarak())continue;
                        
                        karakCount++;
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

            switch (declaringKingdom.StringId)
            {
                // NORDLAND vs LAURELORN - Territorial forest dispute
                case TORConstants.Factions.NORDLAND when
                    targetKingdom.StringId == TORConstants.Factions.LAURELORN:
                case TORConstants.Factions.LAURELORN when
                    targetKingdom.StringId == TORConstants.Factions.NORDLAND:
                    score += NordlandLaurelornRivalryWeight;
                    break;

                // Wissenland and Montfort rivalry
                case TORConstants.Factions.WISSENLAND when
                    targetKingdom.StringId == TORConstants.Factions.MONTFORT:
                case TORConstants.Factions.WISSENLAND when
                    targetKingdom.StringId == TORConstants.Factions.MONTFORT:
                    score += NordlandLaurelornRivalryWeight;
                    break;
            }

            return score;
        }

        /// <summary>
        /// Calculates territorial integrity score based on claims.
        /// - Direct claims: Settlements that rightfully belong to the declaring kingdom
        /// - Cultural claims: Settlements of same culture held by foreign culture
        /// - Pantheon claims: Settlements of same pantheon held by different pantheon
        /// Distance factor applied: nearby claims worth more than distant ones.
        /// </summary>
        private float CalculateTerritorialIntegrityScore(Kingdom declaringKingdom, Kingdom targetKingdom)
        {
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

            return directClaimScore * DirectClaimWeight
                 + culturalClaimScore * CulturalClaimWeight
                 + pantheonClaimScore * PantheonClaimWeight;
        }

        /// <summary>
        /// Calculates score based on target kingdom's riches (prosperity).
        /// Rich kingdoms are more attractive targets, but riches are reduced
        /// if many competitors are already at war with the target.
        /// Returns: -5000 (poor) to +5000 (rich), reduced by competition (scaled by 100)
        /// </summary>
        private float CalculateRichesScore(Kingdom targetKingdom)
        {
            if (targetKingdom.Settlements == null || !targetKingdom.Settlements.Any())
                return 0f;

            // Calculate total prosperity from towns
            float totalProsperity = 0f;
            int townCount = 0;

            foreach (var settlement in targetKingdom.Settlements)
            {
                if (settlement.IsTown && settlement.Town != null)
                {
                    totalProsperity += settlement.Town.Prosperity;
                    townCount++;
                }
            }

            if (townCount == 0)
                return -2500f; // No towns = poor target

            float averageProsperity = totalProsperity / townCount;

            // Scale prosperity to -5000 to +5000 range (scaled by 100)
            // Assume average prosperity is around 3000, rich is 6000+, poor is 1000-
            float normalized = (averageProsperity - 3000f) / 3000f;
            float baseRiches = Math.Max(-5000f, Math.Min(5000f, normalized * 5000f));

            // Competition factor: reduce expected riches if others are already carving them up
            // -40% per competitor, minimum 0.1
            int competitorCount = targetKingdom.GetNumActiveKingdomWars();
            float competitionFactor = Math.Max(0.1f, 1f - competitorCount * 0.4f);

            return baseRiches * competitionFactor;
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
            float distance = GetKingdomDistance(declaringKingdom, targetKingdom);

            if (distance >= float.MaxValue)
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
            var religion1 = declaringKingdom.Leader?.GetDominantReligion();
            var religion2 = targetKingdom.Leader?.GetDominantReligion();

            if (religion1 == null || religion2 == null)
                return 0f;

            float religionCompatibility = ReligionObjectHelper.CalculateReligionCompatibility(religion1, religion2);
            // Negative compatibility = more likely to declare war (scaled by 100)
            return -religionCompatibility * TORConfig.DeclareWarScoreReligiousEffectMultiplier * 100f;
        }

        /// <summary>
        /// Calculates score based on cultural compatibility.
        /// Hostile cultures are more attractive targets.
        /// </summary>
        private float CalculateCultureScore(Kingdom declaringKingdom, Kingdom targetKingdom)
        {
            float cultureCompat = ReligionObjectHelper.CalculateCultureCompatibility(
                declaringKingdom.Culture?.StringId, targetKingdom.Culture?.StringId);
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
            int targetEnemyCount = targetKingdom.GetNumActiveKingdomWars();
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