using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
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

        public override float GetScoreOfDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, IFaction evaluatingClan, out TextObject warReason)
        {
            warReason = new TextObject("It is time to declare war!");

            if(factionDeclaresWar is Kingdom kingdom)
            {
                if (kingdom.GetNumActiveKingdomWars() < TORConfig.NumMinKingdomWars) return 100000;
                if (kingdom.GetNumActiveKingdomWars() >= TORConfig.NumMaxKingdomWars) return -100000;
                else return 5000;
            }

            return base.GetScoreOfDeclaringWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out warReason);
        }

        public override float GetScoreOfDeclaringPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, IFaction evaluatingClan, out TextObject peaceReason)
        {
            peaceReason = new TextObject("We see no reason to stop hostilities");

            // Chaos really shouldn't be allowed to make peace
            if (factionDeclaresPeace.Culture.StringId == TORConstants.Cultures.CHAOS || factionDeclaredPeace.Culture.StringId == TORConstants.Cultures.CHAOS)
            {
                return float.MinValue;
            }

            if(factionDeclaresPeace is Kingdom kingdom && factionDeclaredPeace is Kingdom)
            {
                if (kingdom.GetNumActiveKingdomWars() <= TORConfig.NumMinKingdomWars) return -100000;
                if (kingdom.GetNumActiveKingdomWars() > TORConfig.NumMaxKingdomWars) return 100000;
            }

            return base.GetScoreOfDeclaringPeace(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan, out peaceReason);
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

            if(mercenaryClan.Culture.StringId == TORConstants.Cultures.DRUCHII)
            {
                score = -10000;
            }

            return score;
        }

        public Kingdom GetWarDeclarationTargetCandidate(Kingdom consideringKingdom)
        {
            if(consideringKingdom == null) return null;

            var permissionModel = Campaign.Current?.Models?.KingdomDecisionPermissionModel;
            if (permissionModel == null) return null;

            var kingdomCandidates = Kingdom.All.WhereQ(x => 
                !x.IsEliminated && 
                x != consideringKingdom && 
                permissionModel.IsWarDecisionAllowedBetweenKingdoms(consideringKingdom, x, out _) && 
                !consideringKingdom.IsAtWarWith(x) &&
                (x.GetStanceWith(consideringKingdom)?.PeaceDeclarationDate == null || 
                x.GetStanceWith(consideringKingdom)?.PeaceDeclarationDate.ElapsedDaysUntilNow > TORConfig.MinPeaceDays)).ToListQ();

            var distanceModel = Campaign.Current?.Models?.MapDistanceModel as TORSettlementDistanceModel;
            
            if (kingdomCandidates.Count > 0 && distanceModel != null)
            {
                
                var kingdomListByDistance = kingdomCandidates.SelectQ(x => new Tuple<Kingdom, float>(x, distanceModel.GetDistance(consideringKingdom.FactionMidSettlement, x.FactionMidSettlement))).ToListQ();
                var kingdomListByStrength = kingdomCandidates.SelectQ(x => new Tuple<Kingdom, float>(x, x.TotalStrength)).ToListQ();
                var hostileReligionKingdoms = kingdomCandidates.SelectQ(x => 
                    new Tuple<Kingdom, float>(x, ReligionObjectHelper.CalculateSimilarityScore(x.Leader.GetDominantReligion(), consideringKingdom.Leader.GetDominantReligion()))).ToListQ();

                Dictionary<Kingdom, float> candidateScores = [];
                float minDistance = kingdomListByDistance.MinBy(x => x.Item2).Item2;
                float maxDistance = kingdomListByDistance.MaxBy(x => x.Item2).Item2;

                foreach(var tuple in kingdomListByDistance)
                {
                    candidateScores[tuple.Item1] = Math.Abs(MapToRange(tuple.Item2, minDistance, maxDistance, 0, 1) - 1) * TORConfig.DeclareWarScoreDistanceMultiplier;
                }

                foreach (var tuple in kingdomListByStrength)
                {
                    candidateScores[tuple.Item1] += (consideringKingdom.TotalStrength / tuple.Item1.TotalStrength) * TORConfig.DeclareWarScoreFactionStrengthMultiplier;
                }

                foreach (var tuple in hostileReligionKingdoms)
                {
                    candidateScores[tuple.Item1] += -tuple.Item2 * TORConfig.DeclareWarScoreReligiousEffectMultiplier;
                }
                var candidate = candidateScores.MaxBy(x => x.Value).Key;
                return candidate;
            }
            return null;
        }

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
                var kingdomListByStrength = kingdomCandidates.SelectQ(x => new Tuple<Kingdom, float>(x, x.TotalStrength)).ToListQ();

                Dictionary<Kingdom, float> candidateScores = [];

                foreach (var tuple in kingdomListByStrength)
                {
                    candidateScores[tuple.Item1] = tuple.Item1.TotalStrength / consideringKingdom.TotalStrength;
                }

                var maxvalue = candidateScores.Values.Max();
                if(maxvalue > 1)
                {
                    var candidate = candidateScores.MaxBy(x => x.Value).Key;
                    return candidate;
                }
            }
            return null;
        }

        private static float MapToRange(float value, float minSource, float maxSource, float minTarget = float.MinValue, float maxTarget = float.MaxValue)
        {
            var result = (value - minSource) / (maxSource - minSource) * (maxTarget - minTarget) + minTarget;
            return result;
        }
    }
}