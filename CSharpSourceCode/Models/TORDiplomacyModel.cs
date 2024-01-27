using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORDiplomacyModel : DefaultDiplomacyModel
    {
        public override float GetRelationIncreaseFactor(Hero hero1, Hero hero2, float relationChange)
        {
            var baseValue =  base.GetRelationIncreaseFactor(hero1, hero2, relationChange);
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
                        if (conversationHero != null && conversationHero.Culture.StringId == "vlandia")
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
            var torScoreOfDeclaringWar = torAggressionScore(factionDeclaresWar, factionDeclaredWar);

            var nativeScoreOfDeclaringWar = base.GetScoreOfDeclaringWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out warReason);

            /// Applying extra multiplers to increase wars
            torScoreOfDeclaringWar *= TORConfig.DeclareWarScoreMultiplierTor;
            nativeScoreOfDeclaringWar *= TORConfig.DeclareWarScoreMultiplierNative;

            //TORCommon.Say($"Declaring war between {factionDeclaresWar.Name} and {factionDeclaredWar.Name}\n\t\t" +
            //    $"Native Score: {nativeScoreOfDeclaringWar},\tTOR Score: {torScoreOfDeclaringWar}");

            return nativeScoreOfDeclaringWar + torScoreOfDeclaringWar;
        }

        public override float GetScoreOfDeclaringPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, IFaction evaluatingClan, out TextObject peaceReason)
        {
            var nativeScoreOfDeclaringPeace = base.GetScoreOfDeclaringPeace(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan, out peaceReason);
            var torScore = torAggressionScore(factionDeclaresPeace, factionDeclaredPeace);

            /// Apply same multipliers as declaration of war to try to them in agreement as much as possible
            nativeScoreOfDeclaringPeace *= TORConfig.DeclareWarScoreMultiplierNative;
            torScore *= TORConfig.DeclareWarScoreMultiplierTor/3f;

            //TORCommon.Say($"Declaring peace between {factionDeclaresPeace.Name} and {factionDeclaredPeace.Name}\n\t\t" +
            //    $"Native Score: {nativeScoreOfDeclaringPeace},\tTOR Score: {torScore}");

            return nativeScoreOfDeclaringPeace - torScore;
        }

        private float torAggressionScore(IFaction faction1, IFaction faction2)
        {
            /// Reverse engineered values to make calculations fall in line with native
            var factionScore = baseFactionFactor(faction1, faction2);
            var distanceScore = baseDistanceFactor(faction1, faction2);

            /// TOR internal score calculations
            var religionScore = determineEffectOfReligion(faction1, faction2);
            // normalize effect of religion based on average hero "agro" to approximately between 0.0 and 1.0
            religionScore = religionScore / ((faction2.Heroes.Count + faction1.Heroes.Count) * 100);

            //TORCommon.Say($"Aggression between {faction1.Name} vs {faction2.Name}; \n\t\t" +
            //    $"Faction Score: {factionScore}, Distance Score: {distanceScore}, Religion Score = {religionScore} ");

            // weigh religion as much as faction strength, distance is kinda between 0-1
            return religionScore * factionScore * distanceScore;
        }

        private float determineEffectOfReligion(IFaction faction1, IFaction faction2)
        {
            var kingdomHeroes = faction1.Heroes;

            float religionValue = 0f;

            foreach (var hero in kingdomHeroes)
            {
                var otherSideHeroes = faction2.Heroes;
                foreach (var enemy in otherSideHeroes)
                {
                    foreach (var religion in ReligionObject.All)
                    {
                        if (hero.GetDevotionLevelForReligion(religion) == DevotionLevel.None)
                            continue;
                        foreach (var comparedToReligion in ReligionObject.All)
                        {
                            religionValue += DeterminePositiveEffect(hero, religion, enemy, comparedToReligion);
                            religionValue += determineNegativeEffect(hero, religion, enemy, comparedToReligion);
                        }
                    }
                }
            }

            return religionValue; // / factionToDeclareWarOn.Heroes.Count;
        }

        // Max Value: 100
        // Min Value: 0
        public static float DeterminePositiveEffect(Hero hero, ReligionObject religion, Hero enemy, ReligionObject comparedToReligion)
        {
            if (religion.HostileReligions.Contains(comparedToReligion))
                return 0;

            var value = 0;
            var heroDevotion = hero.GetDevotionLevelForReligion(religion);
            var enemyDevotion = enemy.GetDevotionLevelForReligion(comparedToReligion);

            var shareAffinity = religion.Affinity == comparedToReligion.Affinity;
            var isSame = religion.Name == comparedToReligion.Name;

            if (isSame)
            {
                switch (heroDevotion)
                {
                    case DevotionLevel.Fanatic:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                        {
                            value += 100;
                        }
                        break;
                    case DevotionLevel.Devoted:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                            value += 25;
                        else
                            value += 50;
                        break;
                    case DevotionLevel.Follower:
                        if (enemyDevotion != DevotionLevel.Fanatic)
                            value += 10;
                        break;
                    case DevotionLevel.None:
                        break;
                }

            }
            else if (shareAffinity)
            {
                switch (heroDevotion)
                {
                    case DevotionLevel.Fanatic:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                            value += 10;
                        break;
                    case DevotionLevel.Devoted:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                            value += 25;
                        else
                            value += 50;
                        break;
                    case DevotionLevel.Follower:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                            value += 0;
                        else
                            value += 25;
                        break;
                    case DevotionLevel.None:
                        break;
                }
            }
            return value;
        }

        // Max Score = 0
        // Min Score = -125
        private float determineNegativeEffect(Hero hero, ReligionObject religion, Hero enemy, ReligionObject comparedToReligion)
        {
            var value = 0;

            var heroDevotion = hero.GetDevotionLevelForReligion(religion);
            var enemyDevotion = enemy.GetDevotionLevelForReligion(comparedToReligion);

            if (heroDevotion == DevotionLevel.None) //If a hero is not devoted at all, he should also not care about any hostility.
                return value;

            var shareAffinity = religion.Affinity == comparedToReligion.Affinity;
            var isSame = religion.Name == comparedToReligion.Name;

            if (isSame) //The Person is only hostile if he is fanatic about his religion, and thinks other don't follow enough
            {
                switch (heroDevotion)
                {
                    case DevotionLevel.Fanatic:
                        if (enemyDevotion == DevotionLevel.None)
                        {
                            value += -20;
                        }
                        break;
                }
            }
            else if (shareAffinity)
            {
                switch (heroDevotion)
                {
                    case DevotionLevel.Fanatic:
                        switch (enemyDevotion)
                        {
                            case DevotionLevel.Fanatic:
                                value += -50;
                                break;
                            case DevotionLevel.Follower:
                            case DevotionLevel.Devoted:
                                value += -20;
                                break;
                        }

                        break;
                    case DevotionLevel.Devoted:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                            value += -30;
                        else
                            value += -15;
                        break;
                    case DevotionLevel.Follower:
                        switch (enemyDevotion)
                        {
                            case DevotionLevel.Fanatic:
                                value += -30;
                                break;
                            case DevotionLevel.Follower:
                            case DevotionLevel.Devoted:
                                value += -20;
                                break;
                        }

                        break;
                    case DevotionLevel.None:
                        break;
                }
            }
            else // hostile and neutral gods.
            {
                switch (heroDevotion)
                {
                    case DevotionLevel.Fanatic:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                        {
                            value += -100;
                        }
                        break;
                    case DevotionLevel.Devoted:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                            value += -50;
                        else
                            value += -25;
                        break;
                    case DevotionLevel.Follower:
                        switch (enemyDevotion)
                        {
                            case DevotionLevel.Fanatic:
                                value += -30;
                                break;
                            case DevotionLevel.Follower:
                            case DevotionLevel.Devoted:
                                value += -20;
                                break;
                        }
                        ;
                        break;
                    case DevotionLevel.None:
                        break;
                }
            }

            if (enemyDevotion == DevotionLevel.None) return value;

            if (religion.HostileReligions.Contains(comparedToReligion))
            {
                value += -25; //additional base value if the god is hostile, irrespective about the state of religion.
            }

            return value;
        }

        #region Reverse Engineered Code
        /// <summary>
        /// This code is from the decompiler
        /// Date of binary: 11/30/2023
        /// </summary>
        private float baseDistanceFactor(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
        {
            float distance = baseGetDistance(factionDeclaresWar, factionDeclaredWar);
            float num = (483f + 8.63f * Campaign.AverageDistanceBetweenTwoFortifications) / 2f;
            float num2 = (factionDeclaresWar.Leader == Hero.MainHero || factionDeclaredWar.Leader == Hero.MainHero) ? -300000f : -400000f;
            float num3;
            if (distance - Campaign.AverageDistanceBetweenTwoFortifications * 1.5f > num)
            {
                num3 = num2;
            }
            else if (distance - Campaign.AverageDistanceBetweenTwoFortifications * 1.5f < 0f)
            {
                num3 = 0f;
            }
            else
            {
                float num4 = num - Campaign.AverageDistanceBetweenTwoFortifications * 1.5f;
                float num5 = -num2 / MathF.Pow(num4, 1.6f);
                float num6 = distance - Campaign.AverageDistanceBetweenTwoFortifications * 1.5f;
                num3 = num2 + num5 * MathF.Pow(MathF.Pow(num4 - num6, 8f), 0.2f);
                if (num3 > 0f)
                {
                    num3 = 0f;
                }
            }
            float num7 = 1f - MathF.Pow(num3 / num2, 0.55f);
            num7 = 0.1f + num7 * 0.9f;

            return num7;
        }

        /// <summary>
        /// This code is from the decompiler
        /// Date of binary: 11/30/2023
        /// </summary>
        private float baseGetDistance(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
        {
            if (factionDeclaresWar.Fiefs.Count != 0 && factionDeclaredWar.Fiefs.Count != 0)
            {
                ValueTuple<Settlement, float>[] closestSettlementsToOtherFactionsNearestSettlementToMidPoint = getClosestSettlementsToOtherFactionsNearestSettlementToMidPoint(factionDeclaredWar, factionDeclaresWar);
                ValueTuple<Settlement, float>[] closestSettlementsToOtherFactionsNearestSettlementToMidPoint2 = this.getClosestSettlementsToOtherFactionsNearestSettlementToMidPoint(factionDeclaresWar, factionDeclaredWar);
                float[] array = new float[]
                {
                    float.MaxValue,
                    float.MaxValue,
                    float.MaxValue
                };
                foreach (ValueTuple<Settlement, float> valueTuple in closestSettlementsToOtherFactionsNearestSettlementToMidPoint)
                {
                    if (valueTuple.Item1 != null)
                    {
                        foreach (ValueTuple<Settlement, float> valueTuple2 in closestSettlementsToOtherFactionsNearestSettlementToMidPoint2)
                        {
                            if (valueTuple2.Item1 != null)
                            {
                                float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(valueTuple.Item1, valueTuple2.Item1);
                                if (distance < array[2])
                                {
                                    if (distance < array[1])
                                    {
                                        if (distance < array[0])
                                        {
                                            array[2] = array[1];
                                            array[1] = array[0];
                                            array[0] = distance;
                                        }
                                        else
                                        {
                                            array[2] = array[1];
                                            array[1] = distance;
                                        }
                                    }
                                    else
                                    {
                                        array[2] = distance;
                                    }
                                }
                            }
                        }
                    }
                }
                float num = array[0];
                float num2 = ((array[1] < float.MaxValue) ? array[1] : num) * 0.67f;
                float num3 = ((array[2] < float.MaxValue) ? array[2] : ((num2 < float.MaxValue) ? num2 : num)) * 0.33f;
                return (num + num2 + num3) / 2f;
            }
            if (factionDeclaresWar.Leader == Hero.MainHero || factionDeclaredWar.Leader == Hero.MainHero)
            {
                return 100f;
            }
            return 0.4f * (factionDeclaresWar.InitialPosition - factionDeclaredWar.InitialPosition).Length;
        }
        /// <summary>
        /// This code is from the decompiler
        /// Date of binary: 11/30/2023
        /// </summary>
        private ValueTuple<Settlement, float>[] getClosestSettlementsToOtherFactionsNearestSettlementToMidPoint(IFaction faction1, IFaction faction2)
        {
            Settlement toSettlement = null;
            float num = float.MaxValue;
            foreach (Town town in faction1.Fiefs)
            {
                float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(town.Settlement, faction1.FactionMidSettlement);
                if (num > distance)
                {
                    toSettlement = town.Settlement;
                    num = distance;
                }
            }
            ValueTuple<Settlement, float>[] array = new ValueTuple<Settlement, float>[]
            {
                new ValueTuple<Settlement, float>(null, float.MaxValue),
                new ValueTuple<Settlement, float>(null, float.MaxValue),
                new ValueTuple<Settlement, float>(null, float.MaxValue)
            };
            foreach (Town town2 in faction2.Fiefs)
            {
                float distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(town2.Settlement, toSettlement);
                if (distance2 < array[2].Item2)
                {
                    if (distance2 < array[1].Item2)
                    {
                        if (distance2 < array[0].Item2)
                        {
                            array[2] = array[1];
                            array[1] = array[0];
                            array[0].Item1 = town2.Settlement;
                            array[0].Item2 = distance2;
                        }
                        else
                        {
                            array[2] = array[1];
                            array[1].Item1 = town2.Settlement;
                            array[1].Item2 = distance2;
                        }
                    }
                    else
                    {
                        array[2].Item1 = town2.Settlement;
                        array[2].Item2 = distance2;
                    }
                }
            }
            return array;
        }

        /// <summary>
        /// This code is from the decompiler
        /// Date of binary: 11/30/2023
        /// </summary>
        private float baseClanFactor(IFaction factionDeclaresWar, IFaction factionDeclaredWar, IFaction evaluatingClan)
        {
            StanceLink stanceWith = factionDeclaresWar.GetStanceWith(factionDeclaredWar);
            int clanFactor = 0;
            if (stanceWith.IsNeutral)
            {
                int dailyTributePaid = stanceWith.GetDailyTributePaid(factionDeclaredWar);
                float potentialClanPlunder =
                    evaluatingClan.Leader.Gold +
                    (evaluatingClan.MapFaction.IsKingdomFaction
                      ? (0.5f * (((Kingdom)evaluatingClan.MapFaction).KingdomBudgetWallet / (((Kingdom)evaluatingClan.MapFaction).Clans.Count + 1f)))
                      : 0f);
                float clanIsRulingClanFactor =
                    (!evaluatingClan.IsKingdomFaction && evaluatingClan.Leader != null)
                        ? (
                            (potentialClanPlunder < 50000f)
                            ? (1f + 0.5f * ((50000f - potentialClanPlunder) / 50000f))
                            : (
                                (potentialClanPlunder > 200000f)
                                ? MathF.Max(0.5f, MathF.Sqrt(200000f / potentialClanPlunder))
                                : 1f
                              )
                           )
                        : 1f;
                clanFactor = GetValueOfDailyTribute(dailyTributePaid);
                // Ruling clan should have more influence
                clanFactor = (int)(clanFactor * clanIsRulingClanFactor);
            }
            return clanFactor;
        }

        /// <summary>
        /// This code is from the decompiler
        /// Date of binary: 11/30/2023
        /// </summary>
        private float baseFactionFactor(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
        {
            return -(int)
                MathF.Min(
                    120000f,
                    (
                        MathF.Min(
                            10000f,
                            factionDeclaresWar.TotalStrength)
                        * 0.8f + 2000f
                    )
                    *
                    (
                        MathF.Min(
                            10000f,
                            factionDeclaredWar.TotalStrength)
                        * 0.8f + 2000f
                    )
                    * 0.0012f
                 );
        }
        #endregion
    }
}