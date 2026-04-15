using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.CampaignMechanics.Crafting
{
    public class EnchantingItemTableauVM : ViewModel
    {
        private string _stringId = string.Empty;
        private string _bannerCode = string.Empty;
        private string _itemModifierId = string.Empty;

        public void FillFrom(EquipmentElement item)
        {
            StringId = (item.Item != null) ? item.Item.StringId : string.Empty;
            BannerCode = Clan.PlayerClan.Banner.Serialize();
            ItemModifierId = (item.ItemModifier != null) ? item.ItemModifier.StringId : string.Empty;
        }

        [DataSourceProperty]
        public string StringId
        {
            get => _stringId;
            set
            {
                _stringId = value;
                OnPropertyChangedWithValue(value, "StringId");
            }
        }

        [DataSourceProperty]
        public string BannerCode
        {
            get => _bannerCode;
            set
            {
                if (_bannerCode != value)
                {
                    _bannerCode = value;
                    OnPropertyChangedWithValue(value, "BannerCode");
                }
            }
        }

        [DataSourceProperty]
        public string ItemModifierId
        {
            get => _itemModifierId;
            set
            {
                if (_itemModifierId != value)
                {
                    _itemModifierId = value;
                    OnPropertyChangedWithValue(value, "ItemModifierId");
                }
            }
        }
    }
}