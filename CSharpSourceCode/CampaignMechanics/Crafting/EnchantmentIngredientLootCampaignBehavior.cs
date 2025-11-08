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
        CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, CalculatePotentialLootedEnchantmentResources);

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
        float renownChange, influenceChange, moraleChange, goldChange, playerEarnedLootPercentage;
        mapEvent.GetBattleRewards(PartyBase.MainParty, out renownChange, out influenceChange, out moraleChange, out goldChange,
            out playerEarnedLootPercentage);

        var model = Campaign.Current.Models.GetEnchantmentIngredientModel();

        var itemRosterToReceive = PlayerEncounter.Current.RosterToReceiveLootItems;

        foreach (var pair in _goodAmounts)
        {
            var amount = model.CalculateResultAmount(pair.Value, pair.Key, playerEarnedLootPercentage);
            var item = TorEnchantingIngredients.GetItemObjectForIngredient(pair.Key);
            itemRosterToReceive.Add(new ItemRosterElement(item, amount));
        }

        // clear value for next battle
        var keys = _goodAmounts.Keys.ToList();
        foreach (var key in keys) _goodAmounts[key] = 0;
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