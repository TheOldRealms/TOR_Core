using System;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Cost
{
    class DiplomacyCostCalculator
    {
        private static float AllianceFactor => 5.0f;
        private static float PeaceFactor => 20.0f;
        private static float WarFactor => 10.0f;

        public static InfluenceCost DetermineCostForDeclaringWar(Kingdom kingdom, bool forcePlayerCharacterCosts = false)
        {
            var clanPayingInfluence = forcePlayerCharacterCosts ? Clan.PlayerClan : kingdom.Leader.Clan;

            return new InfluenceCost(clanPayingInfluence, GetKingdomScalingFactorForInfluence(kingdom) * WarFactor);
        }

        public static HybridCost DetermineCostForMakingPeace(Kingdom kingdomMakingPeace, Kingdom otherKingdom, bool forcePlayerCharacterCosts = false)
        {
            return new HybridCost(
                DetermineInfluenceCostForMakingPeace(kingdomMakingPeace, otherKingdom, forcePlayerCharacterCosts),
                DetermineGoldCostForMakingPeace(kingdomMakingPeace, otherKingdom, forcePlayerCharacterCosts),
                DetermineReparationsForMakingPeace(kingdomMakingPeace, otherKingdom, forcePlayerCharacterCosts));
        }

        internal static InfluenceCost DetermineInfluenceCostForMakingPeace(Kingdom kingdomMakingPeace, Kingdom otherKingdom, bool forcePlayerCharacterCosts)
        {
            var clanPayingInfluence = forcePlayerCharacterCosts ? Clan.PlayerClan : kingdomMakingPeace.Leader.Clan;

            return new InfluenceCost(clanPayingInfluence, GetKingdomScalingFactorForInfluence(kingdomMakingPeace) * PeaceFactor);
        }

        internal static GoldCost DetermineGoldCostForMakingPeace(Kingdom kingdomMakingPeace, Kingdom otherKingdom, bool forcePlayerCharacterCosts = false)
        {
            var giver = forcePlayerCharacterCosts ? Hero.MainHero : kingdomMakingPeace.Leader;

            var baseGoldCost = 500;
            int goldCost = Math.Min(baseGoldCost, kingdomMakingPeace.Leader.Gold);
            goldCost = 10 * (goldCost / 10);

            //This is a cost of organization process and thus has no addressee
            return new GoldCost(giver, goldCost);
        }

        private static KingdomWalletCost DetermineReparationsForMakingPeace(Kingdom kingdomMakingPeace, Kingdom otherKingdom, bool forcePlayerCharacterCosts)
        {
            var giver = kingdomMakingPeace;
            var receiver = otherKingdom;
            var reparationsCost = 0;

            // Original code makes use of war exhaustion calculation... not used in current calculation
            reparationsCost = 100 * (reparationsCost / 100);

            return new KingdomWalletCost(giver, receiver, reparationsCost);
        }

        private static float GetKingdomScalingFactorForInfluence(Kingdom kingdom) => MathF.Floor(GetKingdomTierCount(kingdom) * 5.0f);

        private static float GetKingdomScalingFactorForGold(Kingdom kingdom) => MathF.Floor(GetKingdomTierCount(kingdom) * 5f); // or 100f

        private static float GetKingdomWarLoad(Kingdom kingdom)
        {
            return FactionManager.GetEnemyFactions(kingdom)?.Select(x => x.TotalStrength).Aggregate(0f, (result, item) => result + item) / kingdom.TotalStrength ?? 0f;
        }

        private static int GetKingdomTierCount(Kingdom kingdom)
        {
            var tierTotal = 0;
            foreach (var clan in kingdom.Clans)
            {
                tierTotal += clan.IsUnderMercenaryService ? clan.Tier / 3 : clan.Tier;
            }
            return MBMath.ClampInt(tierTotal, 5, 50);
        }

        public static HybridCost DetermineCostForFormingAlliance(Kingdom kingdom, Kingdom otherKingdom, bool forcePlayerCharacterCosts = false)
        {
            return new HybridCost(
                DetermineInfluenceCostForFormingAlliance(kingdom, forcePlayerCharacterCosts),
                DetermineGoldCostForFormingAlliance(kingdom, otherKingdom, forcePlayerCharacterCosts));
        }

        private static InfluenceCost DetermineInfluenceCostForFormingAlliance(Kingdom kingdom, bool forcePlayerCharacterCosts = false)
        {
            var clanPayingInfluence = forcePlayerCharacterCosts ? Clan.PlayerClan : kingdom.Leader.Clan;

            return new InfluenceCost(clanPayingInfluence, GetKingdomScalingFactorForInfluence(kingdom) * AllianceFactor);
        }

        private static GoldCost DetermineGoldCostForFormingAlliance(Kingdom kingdom, Kingdom otherKingdom, bool forcePlayerCharacterCosts = false)
        {
            var giver = forcePlayerCharacterCosts ? Hero.MainHero : kingdom.Leader;

            var otherKingdomWarLoad = GetKingdomWarLoad(otherKingdom) + 1;

            var baseGoldCost = 500;
            var goldCostFactor = 100;
            var goldCost = (int) ((MBMath.ClampFloat((1 / otherKingdomWarLoad), 0f, 1f) * GetKingdomScalingFactorForGold(kingdom) * AllianceFactor * goldCostFactor) + baseGoldCost);

            //This is a cost of organization process and thus has no addressee
            return new GoldCost(giver, goldCost);
        }
    }
}