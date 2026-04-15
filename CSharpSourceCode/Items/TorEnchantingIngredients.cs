using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace TOR_Core.Items
{
    public static class TorEnchantingIngredients
    {
        private static ItemObject _arcaneScroll = null;
        private static ItemObject _blessedWater = null;
        private static ItemObject _dragonBlood = null;
        private static ItemObject _amberCrystal = null;
        private static ItemObject _warpstoneDust = null;
        private static ItemObject _gemStone = null;
        private static readonly Dictionary<TorTradeGoodType, ItemObject> _ingredients = [];

        public static ItemObject ArcaneScroll => _arcaneScroll;
        public static ItemObject BlessedWater => _blessedWater;
        public static ItemObject DragonBlood => _dragonBlood;
        public static ItemObject AmberCrystal => _amberCrystal;
        public static ItemObject WarpstoneDust => _warpstoneDust;
        public static ItemObject GemStone => _gemStone;
        public static Dictionary<TorTradeGoodType, ItemObject> All => _ingredients;

        public static ItemObject GetItemObjectForIngredient(TorTradeGoodType ingredientType)
        {
            if (_ingredients.TryGetValue(ingredientType, out ItemObject itemObject))
            {
                return itemObject;
            }
            return null;
        }

        public static void LoadIngredients()
        {
            _arcaneScroll = null;
            _blessedWater = null;
            _dragonBlood = null;
            _amberCrystal = null;
            _warpstoneDust = null;
            _gemStone = null;
            _ingredients.Clear();
            _arcaneScroll = Campaign.Current?.ObjectManager.GetObject<ItemObject>(x => x.StringId == "tor_tradegood_arcane_scroll");
            _blessedWater = Campaign.Current?.ObjectManager.GetObject<ItemObject>(x => x.StringId == "tor_tradegood_blessed_water");
            _dragonBlood = Campaign.Current?.ObjectManager.GetObject<ItemObject>(x => x.StringId == "tor_tradegood_dragonblood");
            _amberCrystal = Campaign.Current?.ObjectManager.GetObject<ItemObject>(x => x.StringId == "tor_tradegood_amber_crystal");
            _warpstoneDust = Campaign.Current?.ObjectManager.GetObject<ItemObject>(x => x.StringId == "tor_tradegood_warpstone_dust");
            _gemStone = Campaign.Current?.ObjectManager.GetObject<ItemObject>(x => x.StringId == "tor_tradegood_gemstone");
            _ingredients.Add(TorTradeGoodType.ArcaneScroll, _arcaneScroll);
            _ingredients.Add(TorTradeGoodType.BlessedWater, _blessedWater);
            _ingredients.Add(TorTradeGoodType.DragonBlood, _dragonBlood);
            _ingredients.Add(TorTradeGoodType.AmberCrystal, _amberCrystal);
            _ingredients.Add(TorTradeGoodType.WarpstoneDust, _warpstoneDust);
            _ingredients.Add(TorTradeGoodType.GemStone, _gemStone);
        }
    }

    public enum TorTradeGoodType
    {
        ArcaneScroll,
        BlessedWater,
        DragonBlood,
        AmberCrystal,
        WarpstoneDust,
        GemStone,
        Invalid
    }
}