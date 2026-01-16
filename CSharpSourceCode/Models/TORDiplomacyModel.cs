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

            if (factionDeclaresWar is Kingdom kingdom)
            {
                // Use offensive war count (excludes alliance/defensive wars)
                int offensiveWars = GetOffensiveWarCount(kingdom);

                if (offensiveWars < TORConfig.NumMinKingdomWars) return 100000;
                if (offensiveWars >= TORConfig.NumMaxKingdomWars) return -100000;
                else return 5000;
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
                // Calculate distance using simple position-based approach
                var kingdomListByDistance = kingdomCandidates.SelectQ(x =>
                    new Tuple<Kingdom, float>(x, GetKingdomDistance(consideringKingdom, x))).ToListQ();
                var kingdomListByStrength = kingdomCandidates.SelectQ(x =>
                    new Tuple<Kingdom, float>(x, x.GetAllianceTotalStrength())).ToListQ();
                var hostileReligionKingdoms = kingdomCandidates.SelectQ(x =>
                    new Tuple<Kingdom, float>(x, ReligionObjectHelper.CalculateSimilarityScore(x.Leader.GetDominantReligion(), consideringKingdom.Leader.GetDominantReligion()))).ToListQ();

                Dictionary<Kingdom, float> candidateScores = [];
                float minDistance = kingdomListByDistance.MinBy(x => x.Item2).Item2;
                float maxDistance = kingdomListByDistance.MaxBy(x => x.Item2).Item2;

                foreach (var tuple in kingdomListByDistance)
                {
                    // Higher score for closer kingdoms
                    candidateScores[tuple.Item1] = Math.Abs(MapToRange(tuple.Item2, minDistance, maxDistance, 0, 1) - 1) * TORConfig.DeclareWarScoreDistanceMultiplier;
                }

                foreach (var tuple in kingdomListByStrength)
                {
                    // Higher score for weaker kingdoms
                    candidateScores[tuple.Item1] += (consideringKingdom.GetAllianceTotalStrength() / tuple.Item1.GetAllianceTotalStrength()) * TORConfig.DeclareWarScoreFactionStrengthMultiplier;
                }

                foreach (var tuple in hostileReligionKingdoms)
                {
                    // Higher score for hostile religions (negative similarity)
                    candidateScores[tuple.Item1] += -tuple.Item2 * TORConfig.DeclareWarScoreReligiousEffectMultiplier;
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
                    var religionScore = ReligionObjectHelper.CalculateSimilarityScore(
                        candidate.Leader.GetDominantReligion(),
                        consideringKingdom.Leader.GetDominantReligion());
                    score += religionScore * 100;

                    // Cultural similarity bonus
                    if (candidate.Culture == consideringKingdom.Culture)
                    {
                        score += 50;
                    }

                    // Strength consideration - prefer allying with stronger kingdoms when threatened
                    var totalEnemyStrength = consideringKingdom.GetSumEnemyKingdomPower();
                    if (totalEnemyStrength > consideringKingdom.CurrentTotalStrength)
                    {
                        score += candidate.CurrentTotalStrength / consideringKingdom.CurrentTotalStrength * 50;
                    }

                    // Distance consideration - prefer nearby allies
                    float distance = GetKingdomDistance(consideringKingdom, candidate);
                    score -= distance * 0.01f;

                    candidateScores[candidate] = score;
                }

                if (candidateScores.Any())
                {
                    var bestCandidate = candidateScores.MaxBy(x => x.Value);
                    if (bestCandidate.Value > 50) // Only consider if score is high enough
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