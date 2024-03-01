using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Aggression
{
    sealed internal class DecompiledNativeAggression
    {
        /// <summary>
        /// This code is from the decompiler
        /// Date of binary: 11/30/2023
        /// </summary>
        public static float BaseDistanceFactor(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
        {
            var distance = BaseGetDistance(factionDeclaresWar, factionDeclaredWar);
            var num = (483f + 8.63f * Campaign.AverageDistanceBetweenTwoFortifications) / 2f;
            var num2 = (factionDeclaresWar.Leader == Hero.MainHero || factionDeclaredWar.Leader == Hero.MainHero) ? -300000f : -400000f;
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
                var num4 = num - Campaign.AverageDistanceBetweenTwoFortifications * 1.5f;
                var num5 = -num2 / MathF.Pow(num4, 1.6f);
                var num6 = distance - Campaign.AverageDistanceBetweenTwoFortifications * 1.5f;
                num3 = num2 + num5 * MathF.Pow(MathF.Pow(num4 - num6, 8f), 0.2f);
                if (num3 > 0f)
                {
                    num3 = 0f;
                }
            }
            var num7 = 1f - MathF.Pow(num3 / num2, 0.55f);
            num7 = 0.1f + num7 * 0.9f;

            return num7;
        }

        /// <summary>
        /// This code is from the decompiler
        /// Date of binary: 11/30/2023
        /// </summary>
        public static float BaseGetDistance(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
        {
            if (factionDeclaresWar.Fiefs.Count != 0 && factionDeclaredWar.Fiefs.Count != 0)
            {
                var closestSettlementsToOtherFactionsNearestSettlementToMidPoint = GetClosestSettlementsToOtherFactionsNearestSettlementToMidPoint(factionDeclaredWar, factionDeclaresWar);
                var closestSettlementsToOtherFactionsNearestSettlementToMidPoint2 = GetClosestSettlementsToOtherFactionsNearestSettlementToMidPoint(factionDeclaresWar, factionDeclaredWar);
                var array = new float[]
                {
                    float.MaxValue,
                    float.MaxValue,
                    float.MaxValue
                };
                foreach (var valueTuple in closestSettlementsToOtherFactionsNearestSettlementToMidPoint)
                {
                    if (valueTuple.Item1 != null)
                    {
                        foreach (var valueTuple2 in closestSettlementsToOtherFactionsNearestSettlementToMidPoint2)
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
                var num = array[0];
                var num2 = ((array[1] < float.MaxValue) ? array[1] : num) * 0.67f;
                var num3 = ((array[2] < float.MaxValue) ? array[2] : ((num2 < float.MaxValue) ? num2 : num)) * 0.33f;
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
        public static ValueTuple<Settlement, float>[] GetClosestSettlementsToOtherFactionsNearestSettlementToMidPoint(IFaction faction1, IFaction faction2)
        {
            Settlement toSettlement = null;
            var num = float.MaxValue;
            foreach (var town in faction1.Fiefs)
            {
                var distance = Campaign.Current.Models.MapDistanceModel.GetDistance(town.Settlement, faction1.FactionMidSettlement);
                if (num > distance)
                {
                    toSettlement = town.Settlement;
                    num = distance;
                }
            }
            var array = new ValueTuple<Settlement, float>[]
            {
                new ValueTuple<Settlement, float>(null, float.MaxValue),
                new ValueTuple<Settlement, float>(null, float.MaxValue),
                new ValueTuple<Settlement, float>(null, float.MaxValue)
            };
            foreach (var town2 in faction2.Fiefs)
            {
                var distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(town2.Settlement, toSettlement);
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
        public static float BaseClanFactor(IFaction factionDeclaresWar, IFaction factionDeclaredWar, IFaction evaluatingClan)
        {
            var stanceWith = factionDeclaresWar.GetStanceWith(factionDeclaredWar);
            var clanFactor = 0;
            if (stanceWith.IsNeutral)
            {
                var dailyTributePaid = stanceWith.GetDailyTributePaid(factionDeclaredWar);
                var potentialClanPlunder =
                    evaluatingClan.Leader.Gold +
                    (evaluatingClan.MapFaction.IsKingdomFaction
                      ? (0.5f * (((Kingdom)evaluatingClan.MapFaction).KingdomBudgetWallet / (((Kingdom)evaluatingClan.MapFaction).Clans.Count + 1f)))
                      : 0f);
                var clanIsRulingClanFactor =
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
        public static float BaseFactionFactor(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
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

        /// <summary>
        /// This code is from the decompiler
        /// Date of binary: 11/30/2023
        /// </summary>
        public static int GetValueOfDailyTribute(int dailyTributeAmount)
        {
            return dailyTributeAmount * 70;
        }
    }
}
