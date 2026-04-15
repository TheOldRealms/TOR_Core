using System.Linq;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TOR_Core.Items
{
    public class TorItemTraitVM : ViewModel
    {
        private HintViewModel _hintText;
        private string _icon;

        public TorItemTraitVM(string traitId)
        {
            var trait = ItemTrait.All.FirstOrDefault(t => t.ItemTraitStringId == traitId);
            if (trait == null)
            {
                _hintText = new HintViewModel(new TaleWorlds.Localization.TextObject($"No itemtrait with id: {traitId} exists."));
                _icon = "<img src=\"winds_icon_45\"/>";
                return;
            }
            _hintText = new HintViewModel(new TaleWorlds.Localization.TextObject(trait.ItemTraitDescription));
            _icon = "<img src=\"" + trait.IconName + "\"/>";
        }

        private TorItemTraitVM() { }

        public static TorItemTraitVM CreateDamageOnlyTraitVM()
        {
            var vm = new TorItemTraitVM();
            vm._hintText = new HintViewModel(new TaleWorlds.Localization.TextObject("{=str_tor_item_trait_default_elemental_damage}This item deals elemental damage."));
            vm._icon = "<img src=\"winds_icon_45\"/>";

            return vm;
        }

        [DataSourceProperty]
        public HintViewModel Hint
        {
            get
            {
                return _hintText;
            }
            set
            {
                if (value != _hintText)
                {
                    _hintText = value;
                    OnPropertyChangedWithValue(value, "Hint");
                }
            }
        }

        [DataSourceProperty]
        public string Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                if (value != _icon)
                {
                    _icon = value;
                    OnPropertyChangedWithValue(value, "Icon");
                }
            }
        }
    }
}