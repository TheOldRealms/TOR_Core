using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TOR_Core.Items
{
    public class TorItemTraitVM : ViewModel
    {
        private HintViewModel _hintText;
        private string _icon;

        public TorItemTraitVM(ItemTrait trait)
        {
            _hintText = new HintViewModel(new TaleWorlds.Localization.TextObject(trait.ItemTraitDescription));
            _icon = "<img src=\"" + trait.IconName + "\"/>";
        }

		private TorItemTraitVM() { }

        public static TorItemTraitVM CreateDamageOnlyTraitVM()
        {
			var vm = new TorItemTraitVM();
			vm._hintText = new HintViewModel(new TaleWorlds.Localization.TextObject("{=tor_item_trait_default_elemental_damage_str}This item deals elemental damage."));
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