using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;
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
		private BasicTooltipViewModel _blessingHint;
		private BasicTooltipViewModel _windsHint;
        private BasicTooltipViewModel _cultureResourceHint;
        private BasicTooltipViewModel _artilleryHint;
		private float _windRechargeRate = 0f;
		private int _maxWinds = 0;
		private int _maxArtillery = 0;
		private int _currentArtilleryItems = 0;
		private bool _hasCultureResource;
		private string _remainingBlessingTime;

		public TORMapInfoVMExtension(ViewModel vm) : base(vm)
		{
			_windsHint = new BasicTooltipViewModel(GetWindsHintText);
			_windsHint.RefreshValues();
			_artilleryHint = new BasicTooltipViewModel(GetArtilleryHintText);
            _cultureResourceHint = new BasicTooltipViewModel(GetCultureResourceHintText);
            _blessingHint = new BasicTooltipViewModel(GetBlessingHintText);
            RefreshValues();
		}

        private List<TooltipProperty> GetCultureResourceHintText()
        {
	        var hero = Hero.MainHero;
	        var resource = hero.GetCultureSpecificCustomResource();
	        string customResourceTitle = resource.LocalizedName.ToString();
	        var value = hero.GetCustomResourceValue(resource.StringId).ToString("0");
	        var icon = resource.GetCustomResourceIconAsText();
	        var description = resource.Description;

	        var model = Campaign.Current.Models.GetCustomResourceModel();
	        if (model == null) return [];
	        var change = model.GetCultureSpecificCustomResourceChange(hero);

	        var customDescription = resource.GetCustomTooltipDescription();

	        List<TooltipProperty> list =
            [
                new TooltipProperty(customResourceTitle, value+icon, 0, false, TooltipProperty.TooltipPropertyFlags.Title),
                new TooltipProperty("",description , 0, false, TooltipProperty.TooltipPropertyFlags.MultiLine),
                .. customDescription,
            ];
	        if (change.GetLines().Any())
	        {
		        list.Add(new TooltipProperty("Daily Change", "", 0, false, TooltipProperty.TooltipPropertyFlags.RundownResult));
	        }
	        foreach (var elem in change.GetLines())
	        {
		        if (!elem.number.ApproximatelyEqualsTo(0.0f))
		        {
			        list.Add(new TooltipProperty(elem.name, elem.number.ToString("+#;-#;0"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
		        }
	        }
	        
	        
	        
	        return list;
        }

        private List<TooltipProperty> GetArtilleryHintText()
		{
			string artilleryTitle = new TextObject ("{=tor_ui_artillery_title_str}Artillery").ToString();
			string artilleryInventory = new TextObject ("{=tor_ui_artillery_amount_str}Current Artillery Pieces in Inventory:").ToString();
			string artilleryDeployable = new TextObject ("{=tor_ui_winds_of_magic_recharge_rate_str}Maximum Deployable Artillery Pieces:").ToString();
			
			List<TooltipProperty> list =
            [
                new TooltipProperty(artilleryTitle, _maxArtillery.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title),
                new TooltipProperty(artilleryInventory, _currentArtilleryItems.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None),
                new TooltipProperty(artilleryDeployable, _maxArtillery.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None),
            ];
			return list;
		}

		private List<TooltipProperty> GetWindsHintText()
		{
			string womTitle = new TextObject("{=tor_ui_winds_of_magic_title_str}Winds of Magic").ToString();
			string womMaximum = new TextObject("{=tor_ui_winds_of_magic_maximum_str}Maximum:").ToString();
			string womRechargeRate = new TextObject("{=tor_ui_winds_of_magic_recharge_rate_str}Recharge Rate:").ToString();

			var list = new List<TooltipProperty>
            {
                new(womTitle, WindsOfMagic, 0, false, TooltipProperty.TooltipPropertyFlags.Title),
                new(womMaximum, _maxWinds.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None),
                new(womRechargeRate, string.Format("{0:0.00}", _windRechargeRate), 0, false, TooltipProperty.TooltipPropertyFlags.None)
            };
			return list;
		}

		private List<TooltipProperty> GetBlessingHintText()
		{
			var blessingTitle = new TextObject("{=tor_ui_winds_of_magic_title_str}Blessing: ").ToString();
			var durationTitle = new TextObject("{=tor_ui_winds_of_magic_maximum_str}Duration:").ToString();
			var effect = new TextObject("{=tor_ui_winds_of_magic_recharge_rate_str}Effect:").ToString();

			var list = new List<TooltipProperty>();
			if (Hero.MainHero.PartyBelongedTo == null) return list;
			var info = Hero.MainHero.PartyBelongedTo.GetPartyInfo();
			var blessing = info.CurrentBlessingStringId;
			if (blessing == null)
			{
				list.Add(new TooltipProperty("currently no active blessing", "", 0, false,
					TooltipProperty.TooltipPropertyFlags.None));
				if (Hero.MainHero.IsVampire())
					list.Add(new TooltipProperty("You are a vampire, you are your own god", "", 0, false,
						TooltipProperty.TooltipPropertyFlags.None));
				return list;
			}

			var religionObject = ReligionObject.All.FirstOrDefault(x => x.StringId == blessing);
			if (religionObject == null) return list;
			var effectText = GameTexts.FindText("tor_religion_blessing_effect_description", religionObject.StringId);

			var duration = info.CurrentBlessingRemainingDuration;

			var blessingText = GameTexts.FindText("tor_religion_blessing_name", religionObject.StringId);

			list.Add(new TooltipProperty(blessingTitle, blessingText.ToString, 0, false,
				TooltipProperty.TooltipPropertyFlags.Title));
			RemainingBlessingTime = GetBlessingTimeInDays(duration);
			var BlessingTextTime = $"{RemainingBlessingTime} days";
			list.Add(new TooltipProperty(durationTitle, BlessingTextTime, 0, false,
				TooltipProperty.TooltipPropertyFlags.None));

			if (effectText != null)
				list.Add(new TooltipProperty(effect, effectText.ToString, 0, false,
					TooltipProperty.TooltipPropertyFlags.MultiLine));

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
				_windsHint.RefreshValues();
			}

			if (Hero.MainHero.PartyBelongedTo != null && Hero.MainHero.PartyBelongedTo.HasAnyActiveBlessing())
			{
				var time = Hero.MainHero.PartyBelongedTo.GetPartyInfo().CurrentBlessingRemainingDuration;
				RemainingBlessingTime = GetBlessingTimeInDays(time);
			}

			var artilleryItems = MobileParty.MainParty.GetArtilleryItems();
			_currentArtilleryItems = 0;
			foreach (var item in artilleryItems) _currentArtilleryItems += item.Amount;
			_maxArtillery = MobileParty.MainParty.GetMaxNumberOfArtillery();
			ArtilleryText = _currentArtilleryItems.ToString() + "/" + _maxArtillery.ToString();
			var resource = Hero.MainHero.GetCultureSpecificCustomResource();
			HasCultureResource = resource != null;
			if (HasCultureResource)
				CultureResourceText = ((int)Hero.MainHero.GetCultureSpecificCustomResourceValue()).ToString();
		}

		private String GetBlessingTimeInDays(int blessingHours)
		{
			return $"{(float)blessingHours / CampaignTime.HoursInDay:0.0}";
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
		public string RemainingBlessingTime
		{
			get
			{
				return this._remainingBlessingTime;
			}
			set
			{
				if (value != this._remainingBlessingTime)
				{
					this._remainingBlessingTime = value;
					_vm.OnPropertyChangedWithValue(value, "RemainingBlessingTime");
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
		public BasicTooltipViewModel BlessingHint
		{
			get
			{
				return this._blessingHint;
			}
			set
			{
				if (value != this._blessingHint)
				{
					this._blessingHint = value;
					_vm.OnPropertyChangedWithValue(value, "BlessingHint");
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
