using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;

namespace TOR_Core.CampaignMechanics
{
    [ViewModelExtension(typeof(MapInfoVM), "Refresh")]
    public class TORMapInfoVMExtension : BaseViewModelExtension
	{
		private string _windsOfMagic = "0";
        private string _cultureResourceText = "0";
        private string _artilleryText = "0";
		private bool _isSpellCaster = false;
		private BasicTooltipViewModel _windsHint;
        private BasicTooltipViewModel _cultureResourceHint;
        private BasicTooltipViewModel _artilleryHint;
		private float _windRechargeRate = 0f;
		private int _maxWinds = 0;
		private int _maxArtillery = 0;
		private int _currentArtilleryItems = 0;
		private bool _hasCultureResource;

        public TORMapInfoVMExtension(ViewModel vm) : base(vm)
		{
			_windsHint = new BasicTooltipViewModel(GetWindsHintText);
			_artilleryHint = new BasicTooltipViewModel(GetArtilleryHintText);
            _cultureResourceHint = new BasicTooltipViewModel(GetCultureResourceHintText);
            RefreshValues();
		}

        private List<TooltipProperty> GetCultureResourceHintText()
        {
	        string customResourceTitle = Hero.MainHero.GetCultureSpecificCustomResource().LocalizedName.ToString();
	        var value = Hero.MainHero.GetCultureSpecificCustomResourceValue().ToString("0.00");
	        var icon = Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText();
	        var change = Hero.MainHero.GetCultureSpecificCustomResourceChange();

	        List<TooltipProperty> list = new List<TooltipProperty>();
	        list.Add(new TooltipProperty(customResourceTitle, value+icon, 0, false, TooltipProperty.TooltipPropertyFlags.Title));
	        
	        foreach (var elem in change.GetLines())
	        {
		        if (!elem.number.ApproximatelyEqualsTo(0.0f))
		        {
			        list.Add(new TooltipProperty(elem.name, elem.number.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.None));
		        }
	        }
	        
	        return list;
        }

        private List<TooltipProperty> GetArtilleryHintText()
		{
			string artilleryTitle = new TextObject ("{=tor_ui_artillery_title_str}Artillery").ToString();
			string artilleryInventory = new TextObject ("{=tor_ui_artillery_amount_str}Current Artillery Pieces in Inventory:").ToString();
			string artilleryDeployable = new TextObject ("{=tor_ui_winds_of_magic_recharge_rate_str}Maximum Deployable Artillery Pieces:").ToString();
			
			List<TooltipProperty> list = new List<TooltipProperty>();
			list.Add(new TooltipProperty(artilleryTitle, _maxArtillery.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title));
			list.Add(new TooltipProperty(artilleryInventory, _currentArtilleryItems.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			list.Add(new TooltipProperty(artilleryDeployable, _maxArtillery.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			return list;
		}

		private List<TooltipProperty> GetWindsHintText()
		{
			string womTitle = new TextObject ("{=tor_ui_winds_of_magic_title_str}Winds of Magic").ToString();
			string womMaximum = new TextObject ("{=tor_ui_winds_of_magic_maximum_str}Maximum:").ToString();
			string womRechargeRate = new TextObject ("{=tor_ui_winds_of_magic_recharge_rate_str}Recharge Rate:").ToString();
			
			List<TooltipProperty> list = new List<TooltipProperty>();
			list.Add(new TooltipProperty(womTitle, WindsOfMagic, 0, false, TooltipProperty.TooltipPropertyFlags.Title));
			list.Add(new TooltipProperty(womMaximum, _maxWinds.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
			list.Add(new TooltipProperty(womRechargeRate, _windRechargeRate.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
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
			var resource = Hero.MainHero.GetCultureSpecificCustomResource();
            HasCultureResource = resource != null;
			if(resource != null)
			{
				CultureResourceText = Hero.MainHero.GetCultureSpecificCustomResourceValue().ToString("0.0");
			}
			
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
        public string CultureResourceText
        {
            get
            {
                return this._cultureResourceText;
            }
            set
            {
                if (value != this._cultureResourceText)
                {
                    this._cultureResourceText = value;
                    _vm.OnPropertyChangedWithValue(value, "CultureResourceText");
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

        [DataSourceProperty]
        public BasicTooltipViewModel CultureResourceHint
        {
            get
            {
                return this._cultureResourceHint;
            }
            set
            {
                if (value != this._cultureResourceHint)
                {
                    this._cultureResourceHint = value;
                    _vm.OnPropertyChangedWithValue(value, "CultureResourceHint");
                }
            }
        }

        [DataSourceProperty]
        public bool HasCultureResource
        {
            get
            {
                return this._hasCultureResource;
            }
            set
            {
                if (value != this._hasCultureResource)
                {
                    this._hasCultureResource = value;
                    _vm.OnPropertyChangedWithValue(value, "HasCultureResource");
                }
            }
        }
    }
}
