using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
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
    private Dictionary<TorTradeGoodType, float> _goodAmounts = new()
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
        CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, SetLootedIngredients);
        CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, SettlementDailyTickEvent);
    }


    private void SettlementDailyTickEvent(Settlement settlement)
    {
        foreach (var element in TorEnchantingIngredients.All.SelectQ(ingredient => ingredient.Value).SelectQ(item => settlement.ItemRoster.FirstOrDefaultQ(x => x.EquipmentElement.Item == item)))
        {
            settlement.ItemRoster.Remove(element);
        }
    }

    public override void SyncData(IDataStore dataStore)
    {

    }

    private void SetLootedIngredients(MapEvent mapEvent)
    {
        if (mapEvent == null) return;

        var ingredientKeys = _goodAmounts.Keys.ToList();
        foreach (var key in ingredientKeys)
            _goodAmounts[key] = 0f;

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
                    _goodAmounts[ingredientType] += model.GetIngredientDropFactorForCharacter(character, ingredientType, mapEvent);
                }
            }
        }

        mapEvent.GetBattleRewards(
            PartyBase.MainParty,
            out var _renown, out var _influence, out var _morale, out var _gold,
            out var playerLootShare
        );

        var targetRoster = PlayerEncounter.Current?.RosterToReceiveLootItems ?? PartyBase.MainParty.ItemRoster;

        foreach (var ingredientType in ingredientKeys)
        {
            var factorSum = _goodAmounts[ingredientType];
            var amount = model.CalculateResultAmount(factorSum, ingredientType, playerLootShare);
            if (amount <= 0) continue;

            var item = TorEnchantingIngredients.GetItemObjectForIngredient(ingredientType);
            targetRoster.Add(new ItemRosterElement(item, amount));
        }

        // clear value for next battle
        foreach (var key in ingredientKeys)
            _goodAmounts[key] = 0f;
    }


    private void CalculatePotentialLootedEnchantmentResources(IMission obj)
    {
        var playerEvent = Campaign.Current.MainParty.MapEvent;

        var enemySide = BattleSideEnum.Attacker;
        if (playerEvent == null)
            return;
        if (playerEvent.PlayerSide == BattleSideEnum.Attacker) enemySide = BattleSideEnum.Defender;

        var side = playerEvent.GetMapEventSide(enemySide);

        var model = Campaign.Current.Models.GetEnchantmentIngredientModel();

        foreach (var characterObject in from enemyParties in side.Parties from troop in enemyParties.Troops select troop.Troop)
        {
            var keys = _goodAmounts.Keys.ToList();
            foreach (var key in keys) _goodAmounts[key] += model.GetIngredientDropFactorForCharacter(characterObject, key, playerEvent);
        }
    }
}