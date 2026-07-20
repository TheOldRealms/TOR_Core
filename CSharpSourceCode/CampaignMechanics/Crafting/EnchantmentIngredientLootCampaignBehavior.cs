using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Crafting;
/// <summary>
/// Adds lootable BattleIngredients to the lootpool.
/// </summary>
public class EnchantmentIngredientLootCampaignBehavior : CampaignBehaviorBase
{
    private Dictionary<TorTradeGoodType, float> _goodsFactors= new()
    {
        { TorTradeGoodType.AmberCrystal, 0f },
        { TorTradeGoodType.BlessedWater, 0f },
        { TorTradeGoodType.WarpstoneDust, 0f },
        { TorTradeGoodType.ArcaneScroll, 0f },
        { TorTradeGoodType.DragonBlood, 0f },
        { TorTradeGoodType.GemStone, 0f }
    };

    public ItemObject GetDungeonLootIngredient => TorEnchantingIngredients.DragonBlood;

    public override void RegisterEvents()
    {
        CampaignEvents.OnCollectLootsItemsEvent.ClearListeners(this);
        CampaignEvents.DailyTickSettlementEvent.ClearListeners(this);
        CampaignEvents.OnCollectLootsItemsEvent.AddNonSerializedListener(this, SetLootedIngredients);
        CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, SettlementDailyTickEvent);
    }


    private void SettlementDailyTickEvent(Settlement settlement)
    {
        foreach (var ingredientItem in TorEnchantingIngredients.All.SelectQ(x => x.Value))
        {
            var rosterElement = settlement.ItemRoster.FirstOrDefaultQ(x => x.EquipmentElement.Item == ingredientItem);
            if (rosterElement.Amount > 0)
            {
                settlement.ItemRoster.Remove(rosterElement);
            }
        }
    }

    public override void SyncData(IDataStore dataStore)
    {

    }

    private void SetLootedIngredients(PartyBase winnerParty, ItemRoster gainedLoots)
    {
        if (winnerParty != PartyBase.MainParty) return;

        var mapEvent = winnerParty.MapEventSide.MapEvent;

        if (mapEvent == null) return;
        if (Hero.MainHero.IsEnlisted()) return;
        if (!mapEvent.HasWinner) return;
        if (mapEvent.PlayerSide != mapEvent.WinningSide) return;

        var ingredientKeys = _goodsFactors.Keys.ToList();
        foreach (var key in ingredientKeys) _goodsFactors[key] = 0f;

        var enemySideEnum = mapEvent.PlayerSide == BattleSideEnum.Attacker
            ? BattleSideEnum.Defender
            : BattleSideEnum.Attacker;

        var enemySide = mapEvent.GetMapEventSide(enemySideEnum);
        if (enemySide == null) return;

        var model = Campaign.Current.Models.GetEnchantmentIngredientModel();

        foreach (var party in enemySide.Parties)
        {
            foreach (var troop in party.Troops)
            {
                var character = troop.Troop;
                foreach (var ingredientType in ingredientKeys)
                {
                    _goodsFactors[ingredientType] += model.GetIngredientDropFactorForCharacter(character, ingredientType, mapEvent);
                }
            }
        }

        PlayerEncounter.Current.GetBattleRewards(out _, out _, out _, out var playerEarnedLootRate, out _);

        foreach (var ingredientType in ingredientKeys)
        {
            var factorSum = _goodsFactors[ingredientType];
            var amount = model.CalculateResultAmount(factorSum, ingredientType, playerEarnedLootRate);
            if (amount <= 0) continue;

            var item = TorEnchantingIngredients.GetItemObjectForIngredient(ingredientType);
            gainedLoots.Add(new ItemRosterElement(item, amount));
        }

        // clear value for next battle
        foreach (var key in ingredientKeys) _goodsFactors[key] = 0f;
    }
}