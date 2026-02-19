using Helpers;
using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public sealed class TradeSkillPatch
    {
        // nerfing sale prices of equipment by 50%, then increasing it back by .3% for every level
        private const float EQUIPMENT_SELL_PRICE_BASE_MULTIPLIER = 0.5f;
        private const float EQUIPMENT_TRADE_SKILL_BONUS_PER_LEVEL = 0.0073f; // same prices as vanilla at 300 trade
        private const string TRADE_PENALTY_REDUCTION_DESCRIPTION =
    "{=str_tor_trade_skill_description}Trade penalty Reduction +0.2% per level for trade goods and animals.\n" +
    "Trade penalty Reduction +0.7% per level for equipment.";

        [HarmonyPatch(typeof(DefaultTradeItemPriceFactorModel), nameof(DefaultTradeItemPriceFactorModel.GetTradePenalty))]
        private static class EquipmentTradePenaltyUndoPatch
        {
            [HarmonyPostfix]
            private static void Postfix(
                ItemObject item,
                MobileParty clientParty,
                PartyBase merchant,
                bool isSelling,
                float inStore,
                float supply,
                float demand,
                ref float __result)
            {
                if (clientParty == null)
                {
                    return;
                }

                if (!IsEquipmentItem(item))
                {
                    return;
                }
                // revert vanilla effect on equipment back
                float tradePenaltyFactor = Campaign.Current.Models.PartyTradeModel.GetTradePenaltyFactor(clientParty);
                __result /= tradePenaltyFactor;
            }
        }

        [HarmonyPatch(typeof(DefaultTradeItemPriceFactorModel), nameof(DefaultTradeItemPriceFactorModel.GetPrice))]
        private static class EquipmentSellPricePatch
        {
            [HarmonyPostfix]
            private static void Postfix(
                EquipmentElement itemRosterElement,
                MobileParty clientParty,
                PartyBase merchant,
                bool isSelling,
                float inStoreValue,
                float supply,
                float demand,
                ref int __result)
            {
                if (!isSelling)
                {
                    return;
                }

                ItemObject item = itemRosterElement.Item;
                if (!IsEquipmentItem(item))
                {
                    return;
                }

                int tradeSkillValue = GetTradeSkillValue(clientParty);

                float equipmentSellMultiplier =
                    EQUIPMENT_SELL_PRICE_BASE_MULTIPLIER * (1f + tradeSkillValue * EQUIPMENT_TRADE_SKILL_BONUS_PER_LEVEL);

                int adjustedPrice = MathF.Round(__result * equipmentSellMultiplier);
                adjustedPrice = MathF.Max(adjustedPrice, 1);

                __result = adjustedPrice;
            }
        }

        private static bool IsEquipmentItem(ItemObject item)
        {
            return !item.IsTradeGood
                && !item.IsAnimal
                && (item.HasWeaponComponent || item.HasArmorComponent || item.Type == ItemObject.ItemTypeEnum.HorseHarness);
        }

        private static int GetTradeSkillValue(MobileParty clientParty)
        {
            if (clientParty == null)
            {
                return 0;
            }

            CharacterObject partyLeader = SkillHelper.GetEffectivePartyLeaderForSkill(clientParty.Party);
            return partyLeader?.GetSkillValue(DefaultSkills.Trade) ?? 0;
        }

        [HarmonyPatch(typeof(SkillHelper), nameof(SkillHelper.GetEffectDescriptionForSkillLevel))]
        private static class TradePenaltyReductionDescriptionPatch
        {
            [HarmonyPostfix]
            private static void Postfix(SkillEffect effect, int level, ref TextObject __result)
            {
                if (effect != null && effect.StringId == "TradePenaltyReduction")
                {
                    __result = new TextObject(TRADE_PENALTY_REDUCTION_DESCRIPTION);
                }
            }
        }
    }
}
