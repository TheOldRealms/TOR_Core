using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TOR_Core.Models
{
    public class TORMinorFactionsModel : DefaultMinorFactionsModel
    {
        private const float MIN_VANILLA_AWARD_FACTOR = 1f;
        private const float MAX_VANILLA_AWARD_FACTOR = 80f;

        private const int MIN_MERCENARY_AWARD = 100;
        private const int MAX_MERCENARY_AWARD = 300;

        public override int GetMercenaryAwardFactorToJoinKingdom(Clan mercenaryClan, Kingdom kingdom, bool neededAmountForClanToJoinCalculation = false)
        {
            if (neededAmountForClanToJoinCalculation)
            {
                return base.GetMercenaryAwardFactorToJoinKingdom(mercenaryClan, kingdom, true);
            }

            var vanillaAwardFactor = CalculateVanillaMercenaryAwardFactor(mercenaryClan, kingdom);
            var clampedAwardFactor = MathF.Max(MIN_VANILLA_AWARD_FACTOR, MathF.Min(MAX_VANILLA_AWARD_FACTOR, vanillaAwardFactor));

            var normalizedAwardFactor =
                (clampedAwardFactor - MIN_VANILLA_AWARD_FACTOR) /
                (MAX_VANILLA_AWARD_FACTOR - MIN_VANILLA_AWARD_FACTOR);

            var result = MathF.Round(
                MIN_MERCENARY_AWARD +
                normalizedAwardFactor * (MAX_MERCENARY_AWARD - MIN_MERCENARY_AWARD));

            if (mercenaryClan.IsMinorFaction && kingdom.Leader == Hero.MainHero && kingdom.Leader.GetPerkValue(DefaultPerks.Trade.ManOfMeans))
            {
                result = MathF.Round(result * (1f + DefaultPerks.Trade.ManOfMeans.PrimaryBonus));
            }

            if (mercenaryClan.Culture.HasFeat(DefaultCulturalFeats.VlandianRenownMercenaryFeat))
            {
                result += (int)(result * 0.15f);
            }

            return result;
        }

        private static float CalculateVanillaMercenaryAwardFactor(Clan mercenaryClan, Kingdom kingdom)
        {
            var powerRatioToEnemies = FactionHelper.GetPowerRatioToEnemies(kingdom);

            var enemyPressureFactor =
                powerRatioToEnemies > 2f
                    ? 0.5f
                    : powerRatioToEnemies > 1f
                        ? 1f - (powerRatioToEnemies - 1f) * 0.5f
                        : powerRatioToEnemies > 0.5f
                            ? 1f + (1f - powerRatioToEnemies)
                            : 1.5f + (0.5f - powerRatioToEnemies);

            var tributeRatioFactorBase = MathF.Sqrt(FactionHelper.GetPowerRatioToTributePayedKingdoms(kingdom));
            var tributeRatioFactor = tributeRatioFactorBase > 1f ? 1f : 2f - tributeRatioFactorBase;
            var mercenaryClanTierFactor = 1f + mercenaryClan.Leader.Clan.Tier * 0.15f;

            var weightedClanCount = 0;
            var weightedGoldTotal = 0f;
            var poorClanWeightTotal = 0f;

            foreach (var clan in kingdom.Clans)
            {
                if (clan.IsUnderMercenaryService)
                {
                    continue;
                }

                var weight = clan.Leader != kingdom.Leader ? 1 : 2;
                weightedGoldTotal += weight * clan.Leader.Gold;
                weightedClanCount += weight;

                if (clan.Leader.Gold < 30000)
                {
                    poorClanWeightTotal += weight * (1f - clan.Leader.Gold / 30000f);
                }
            }

            var kingdomBudgetWallet = MathF.Min(1000000, kingdom.KingdomBudgetWallet);
            var averageGold = (weightedGoldTotal + kingdomBudgetWallet * 0.5f) / weightedClanCount;
            var kingdomWealthFactor = MathF.Min(3.5f, MathF.Pow((averageGold + 2000f) / 10000f, 0.3f)) - 0.5f;
            var povertyPenaltyFactor = 1f - 0.8f * (poorClanWeightTotal / weightedClanCount);

            var clanCountFactor = kingdom.Clans.Count < 10
                ? 1f + (float)((10 - kingdom.Clans.Count) * (10 - kingdom.Clans.Count)) / 81f
                : 1f;

            var mercenaryClanCount = 0;
            foreach (var clan in kingdom.Clans)
            {
                if (clan.IsUnderMercenaryService)
                {
                    mercenaryClanCount++;
                }
            }

            var fiefWeight = 0;
            foreach (var town in kingdom.Fiefs)
            {
                fiefWeight += town.IsTown ? 2 : 1;
            }

            var fiefPressureFactor = fiefWeight < 25
                ? 1f + MathF.Min(1f, (25 - fiefWeight) * (25 - fiefWeight) * 0.003f)
                : 1f;

            if (fiefPressureFactor > 1f && enemyPressureFactor < 1f)
            {
                var adjustedEnemyPressureFactor = MathF.Pow(1f / enemyPressureFactor, 1f / fiefPressureFactor);
                enemyPressureFactor *= adjustedEnemyPressureFactor;
            }

            if (mercenaryClanCount > 0)
            {
                if (fiefPressureFactor > 1f)
                {
                    fiefPressureFactor = 1f + (fiefPressureFactor - 1f) / MathF.Sqrt(1 + mercenaryClanCount);
                }

                if (enemyPressureFactor > 1f)
                {
                    enemyPressureFactor = 1f + (enemyPressureFactor - 1f) / MathF.Pow(1 + mercenaryClanCount, 0.33f);
                }

                if (tributeRatioFactor > 1f)
                {
                    tributeRatioFactor = 1f + (tributeRatioFactor - 1f) / MathF.Sqrt(1 + mercenaryClanCount);
                }
            }

            return clanCountFactor
                * enemyPressureFactor
                * tributeRatioFactor
                * mercenaryClanTierFactor
                * kingdomWealthFactor
                * povertyPenaltyFactor
                * fiefPressureFactor
                * 6f;
        }
    }
}