using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TOR_Core.Extensions;
using TOR_Core.Framework;
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
        CampaignEvents.OnHideoutBattleCompletedEvent.ClearListeners(this);
        CampaignEvents.OnCollectLootsItemsEvent.AddNonSerializedListener(this, SetLootedIngredients);
        CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, SettlementDailyTickEvent);
        CampaignEvents.OnHideoutBattleCompletedEvent.AddNonSerializedListener(this, AddHideoutIngredients);
    }

    /// <summary>
    /// Flat ingredient stash per hideout culture, paid on top of the per-troop drop: a hideout garrison
    /// is small, and most bandit troops drop nothing on their own. Max is inclusive.
    /// A null ingredient rolls one from <see cref="RandomStashPool"/>.
    /// </summary>
    private static readonly Dictionary<string, (TorTradeGoodType? Ingredient, int Min, int Max)> HideoutStashes = new()
    {
        { TORConstants.Cultures.GREENSKIN_BANDIT, (TorTradeGoodType.GemStone, 2, 4) },
        { TORConstants.Cultures.HERRIMAULT, (TorTradeGoodType.BlessedWater, 2, 4) },
        { TORConstants.Cultures.CHAOS_CULTIST, (TorTradeGoodType.WarpstoneDust, 2, 4) },
        { TORConstants.Cultures.BEASTMEN, (TorTradeGoodType.AmberCrystal, 2, 4) },
        { TORConstants.Cultures.EMPIRE_DESERTERS, (null, 1, 2) },
        { TORConstants.Cultures.NORSCAN_RAIDERS, (null, 1, 2) },
    };

    // Dragon Blood and Blessed Water stay out: one is the premium reagent, the other comes from praying.
    private static readonly TorTradeGoodType[] RandomStashPool =
    [
        TorTradeGoodType.ArcaneScroll,
        TorTradeGoodType.AmberCrystal,
        TorTradeGoodType.WarpstoneDust,
        TorTradeGoodType.GemStone
    ];

    private void AddHideoutIngredients(BattleSideEnum winnerSide, HideoutEventComponent eventComponent, HideoutEventComponent.HideoutBattleEndState endState)
    {
        var mapEvent = eventComponent?.MapEvent;
        if (mapEvent == null || mapEvent.PlayerSide != mapEvent.WinningSide) return;
        if (Hero.MainHero.IsEnlisted()) return;

        var cultureId = mapEvent.MapEventSettlement?.Culture?.StringId;
        if (cultureId == null || !HideoutStashes.TryGetValue(cultureId, out var stash)) return;

        var ingredient = stash.Ingredient ?? RandomStashPool[MBRandom.RandomInt(RandomStashPool.Length)];
        var item = TorEnchantingIngredients.GetItemObjectForIngredient(ingredient);
        if (item == null) return;

        var amount = MBRandom.RandomInt(stash.Min, stash.Max + 1);
        PartyBase.MainParty.ItemRoster.AddToCounts(item, amount);

        var text = TORTextHelper.GetTextObject("tor_hideout_ingredients_found_text", "You found {AMOUNT} {ITEM_NAME} hoarded in the hideout.");
        text.SetTextVariable("AMOUNT", amount);
        text.SetTextVariable("ITEM_NAME", item.Name);
        MBInformationManager.AddQuickInformation(text, 0);
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