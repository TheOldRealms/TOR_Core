using Helpers;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    public sealed class TORTradeItemPriceFactorModel : DefaultTradeItemPriceFactorModel
    {
        // nerfing sale prices of equipment by 50%, then increasing it back by .3% for every level
        private const float EQUIPMENT_SELL_PRICE_BASE_MULTIPLIER = 0.5f;
        private const float EQUIPMENT_TRADE_SKILL_BONUS_PER_LEVEL = 0.0073f; // same prices as vanilla at 300 trade
        private const float PLAYER_SMITHED_SALE_PRICE_MULTIPLIER = 0.25f;

        private const string TRADE_PENALTY_REDUCTION_DESCRIPTION =
            "{=str_tor_trade_skill_description}Trade penalty Reduction +0.2% per level for trade goods and animals.\n" +
            "Equipment sell price starts at 50% and increases by +0.73% per Trade level.";
        public static void ApplyTradePenaltyReductionDescriptionOverride()
        {
            var tradePenaltyReductionEffect = DefaultSkillEffects.TradePenaltyReduction;
            var newDescription = new TextObject(TRADE_PENALTY_REDUCTION_DESCRIPTION);

            tradePenaltyReductionEffect.Initialize(tradePenaltyReductionEffect.Name, newDescription);
        }

        public override float GetTradePenalty(
            ItemObject item,
            MobileParty clientParty,
            PartyBase merchant,
            bool isSelling,
            float inStore,
            float supply,
            float demand)
        {
            float vanillaPenalty = base.GetTradePenalty(item, clientParty, merchant, isSelling, inStore, supply, demand);

            // Disguise secondary (Roguery 175): "Trade penalties with factions
            // where you have a crime rating are reduced by 50%." Mirrors vanilla's
            // stripped SmugglerConnections secondary — vanilla's base.GetTradePenalty
            // would ordinarily apply SmugglerConnections' bonus via
            // PerkHelper.AddPerkBonusForParty when the merchant's faction has a
            // non-zero crime rating against the main hero, but the perk is
            // stripped by VanillaPerkStripper, so the bonus never fires. Apply
            // the equivalent reduction manually when the TOR perk is held.
            if (clientParty != null
                && clientParty.IsMainParty
                && merchant?.MapFaction != null
                && merchant.MapFaction.MainHeroCrimeRating > 0f
                && Hero.MainHero != null
                && Hero.MainHero.GetPerkValue(TORPerks.Roguery.Disguise))
            {
                vanillaPenalty *= 1f + TORPerks.Roguery.Disguise.SecondaryBonus;
            }

            // Fence primary (Roguery 250): "-20% weapon sell-price penalty."
            // Vanilla's stripped ArmsDealer perk fired here (same spot — weapon
            // sale path inside GetTradePenalty) with `isPrimaryBonus: true`. TOR
            // takes over on the Fence key: reduce the penalty by |PrimaryBonus|
            // when the party leader holds Fence and we're selling a weapon.
            if (isSelling
                && item != null && item.HasWeaponComponent
                && clientParty != null
                && clientParty.HasPerk(TORPerks.Roguery.Fence))
            {
                vanillaPenalty *= 1f + TORPerks.Roguery.Fence.PrimaryBonus;
            }

            if (clientParty == null || item == null)
            {
                return vanillaPenalty;
            }

            if (!IsEquipmentItem(item))
            {
                return vanillaPenalty;
            }

            // revert vanilla effect on equipment back
            float tradePenaltyFactor = Campaign.Current.Models.PartyTradeModel.GetTradePenaltyFactor(clientParty);
            return vanillaPenalty / tradePenaltyFactor;
        }

        public override int GetPrice(
            EquipmentElement itemRosterElement,
            MobileParty clientParty,
            PartyBase merchant,
            bool isSelling,
            float inStoreValue,
            float supply,
            float demand)
        {
            int vanillaPrice = base.GetPrice(itemRosterElement, clientParty, merchant, isSelling, inStoreValue, supply, demand);

            if (!isSelling)
            {
                return vanillaPrice;
            }

            ItemObject item = itemRosterElement.Item;
            if (item == null || !IsEquipmentItem(item))
            {
                return vanillaPrice;
            }

            int tradeSkillValue = GetTradeSkillValue(clientParty);

            float equipmentSellMultiplier =
                EQUIPMENT_SELL_PRICE_BASE_MULTIPLIER * (1f + tradeSkillValue * EQUIPMENT_TRADE_SKILL_BONUS_PER_LEVEL);

            int adjustedPrice = (int)Math.Round(vanillaPrice * equipmentSellMultiplier, MidpointRounding.AwayFromZero);

            if (clientParty == MobileParty.MainParty && IsPlayerSmithedSaleNerfItem(item))
            {
                adjustedPrice = (int)Math.Round(
                    adjustedPrice * PLAYER_SMITHED_SALE_PRICE_MULTIPLIER,
                    MidpointRounding.AwayFromZero);
            }
            return Math.Max(adjustedPrice, 1);
        }

        private static bool IsEquipmentItem(ItemObject item)
        {
            return !item.IsTradeGood
                && !item.IsAnimal
                && (item.HasWeaponComponent || item.HasArmorComponent || item.Type == ItemObject.ItemTypeEnum.HorseHarness);
        }
        private static bool IsPlayerSmithedSaleNerfItem(ItemObject item)
        {
            return item.IsCraftedByPlayer
                && item.IsCraftedWeapon
                && item.StringId.StartsWith("crafted_item_", StringComparison.Ordinal);
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
    }
}