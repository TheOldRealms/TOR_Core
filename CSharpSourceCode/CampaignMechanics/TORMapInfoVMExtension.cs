using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;

namespace TOR_Core.CampaignMechanics
{
    [ViewModelExtension(typeof(MapInfoVM), "Refresh")]
    public class TORMapInfoVMExtension : BaseViewModelExtension
	{
		private string _windsOfMagic = "0";
		private string _artilleryText = "0";
		private bool _isSpellCaster = false;
		private BasicTooltipViewModel _windsHint;
		private BasicTooltipViewModel _artilleryHint;
		private float _windRechargeRate = 0f;
		private int _maxWinds = 0;
		private int _maxArtillery = 0;
		private int _currentArtilleryItems = 0;

		public TORMapInfoVMExtension(ViewModel vm) : base(vm)
		{
			_windsHint = new BasicTooltipViewModel(GetWindsHintText);
			_artilleryHint = new BasicTooltipViewModel(GetArtilleryHintText);
			RefreshValues();
		}

		private List<TooltipProperty> GetArtilleryHintText()
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			list.Add(new TooltipProperty("Artillery", _maxArtillery.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title));
			list.Add(new TooltipProperty("Current Artillery Pieces in Inventory:", _currentArtilleryItems.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			list.Add(new TooltipProperty("Maximum Deployable Artillery Pieces:", _maxArtillery.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			return list;
		}

		private List<TooltipProperty> GetWindsHintText()
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			list.Add(new TooltipProperty("Winds of Magic", WindsOfMagic, 0, false, TooltipProperty.TooltipPropertyFlags.Title));
			list.Add(new TooltipProperty("", string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
			list.Add(new TooltipProperty("Maximum:", _maxWinds.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			list.Add(new TooltipProperty("Recharge Rate:", _windRechargeRate.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			return list;
		}

		public override void RefreshValues()
		{
			base.RefreshValues();
			IsSpellCaster = Hero.MainHero.IsSpellCaster();
			if (IsSpellCaster)
			{
				var info = Hero.MainHero.GetExtendedInfo();
				WindsOfMagic = ((int)info.GetCustomResourceValue("WindsOfMagic")).ToString();
				_maxWinds = (int)info.MaxWindsOfMagic;
				_windRechargeRate = info.WindsOfMagicRechargeRate;
			}
			var artilleryItems = MobileParty.MainParty.GetArtilleryItems();
			_currentArtilleryItems = 0;
			foreach (var item in artilleryItems)
			{
				_currentArtilleryItems += item.Amount;
			}
			_maxArtillery = MobileParty.MainParty.GetMaxNumberOfArtillery();
			ArtilleryText = _currentArtilleryItems.ToString() + "/" + _maxArtillery.ToString();
		}

		[DataSourceProperty]
		public bool IsSpellCaster
		{
			get
			{
				return this._isSpellCaster;
			}
			set
			{
				if (value != this._isSpellCaster)
				{
					this._isSpellCaster = value;
					_vm.OnPropertyChangedWithValue(value, "IsSpellCaster");
				}
			}
		}

		[DataSourceProperty]
		public string WindsOfMagic
		{
			get
			{
				return this._windsOfMagic;
			}
			set
			{
				if (value != this._windsOfMagic)
				{
					this._windsOfMagic = value;
					_vm.OnPropertyChangedWithValue(value, "WindsOfMagic");
				}
			}
		}

		[DataSourceProperty]
		public string ArtilleryText
		{
			get
			{
				return this._artilleryText;
			}
			set
			{
				if (value != this._artilleryText)
				{
					this._artilleryText = value;
					_vm.OnPropertyChangedWithValue(value, "ArtilleryText");
				}
			}
		}

		[DataSourceProperty]
		public BasicTooltipViewModel WindsHint
		{
			get
			{
				return this._windsHint;
			}
			set
			{
				if (value != this._windsHint)
				{
					this._windsHint = value;
					_vm.OnPropertyChangedWithValue(value, "WindsHint");
				}
			}
		}

		[DataSourceProperty]
		public BasicTooltipViewModel ArtilleryHint
		{
			get
			{
				return this._artilleryHint;
			}
			set
			{
				if (value != this._artilleryHint)
				{
					this._artilleryHint = value;
					_vm.OnPropertyChangedWithValue(value, "ArtilleryHint");
				}
			}
		}
	}
}
