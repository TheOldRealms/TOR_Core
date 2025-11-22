using Ink.Parsed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(MapInfoVM), "Refresh")]
    public class TORMapInfoVMExtension : BaseViewModelExtension
    {
        private int _windsOfMagic = 0;
        private float _windRechargeRate = 0f;
        private int _maxWinds = 0;
        private int _maxArtillery = 0;
        private int _currentArtilleryItems = 0;
        private string _remainingBlessingTime;
        private bool _hasBaseVMBeenInitialized = false;
        private bool _haveInfoItemsBeenAdded = false;

        private MapInfoItemVM _windsInfo;
        private MapInfoItemVM _artilleryInfo;
        private MapInfoItemVM _resourceInfo;
        private MapInfoItemVM _blessingInfo;

        public TORMapInfoVMExtension(ViewModel vm) : base(vm)
        {
            _windsInfo = new MapInfoItemVM("winds", GetWindsHintText);
            _artilleryInfo = new MapInfoItemVM("artillery", GetArtilleryHintText);
            _resourceInfo = new MapInfoItemVM("resources", GetCultureResourceHintText);
            _blessingInfo = new MapInfoItemVM("blessing", GetBlessingHintText);
            RefreshValues();
        }

        private List<TooltipProperty> GetCultureResourceHintText()
        {
            var hero = Hero.MainHero;
            var resource = hero.GetCultureSpecificCustomResource();
            string customResourceTitle = resource.Name.ToString();
            var value = hero.GetCustomResourceValue(resource.StringId).ToString("0");
            var icon = resource.GetCustomResourceIconAsText();
            var description = resource.Description.ToString();

            var model = Campaign.Current.Models.GetCustomResourceModel();
            if (model == null) return [];
            var change = model.GetCultureSpecificCustomResourceChange(hero, resource.StringId);

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
            string artilleryTitle = new TextObject("{=tor_ui_artillery_title_str}Artillery").ToString();
            string artilleryInventory = new TextObject("{=tor_ui_artillery_amount_str}Current Artillery Pieces in Inventory:").ToString();
            string artilleryDeployable = new TextObject("{=tor_ui_winds_of_magic_recharge_rate_str}Maximum Deployable Artillery Pieces:").ToString();

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
                new(womTitle, _windsOfMagic.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title),
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
            _remainingBlessingTime = GetBlessingTimeInDays(duration);
            var BlessingTextTime = $"{_remainingBlessingTime} days";
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
            if (_hasBaseVMBeenInitialized && !_haveInfoItemsBeenAdded)
            {
                (_vm as MapInfoVM).SecondaryInfoItems.Add(_windsInfo);
                (_vm as MapInfoVM).SecondaryInfoItems.Add(_artilleryInfo);
                (_vm as MapInfoVM).SecondaryInfoItems.Add(_resourceInfo);
                (_vm as MapInfoVM).SecondaryInfoItems.Add(_blessingInfo);
                _haveInfoItemsBeenAdded = true;
            }

            var heroInfo = Hero.MainHero.GetExtendedInfo();
            _windsOfMagic = (int)heroInfo.GetCustomResourceValue("WindsOfMagic");
            _maxWinds = (int)heroInfo.MaxWindsOfMagic;
            _windRechargeRate = heroInfo.WindsOfMagicRechargeRate;
            _windsInfo.HasWarning = _windsOfMagic < 0;
            _windsInfo.Value = _windsOfMagic.ToString();
            _windsInfo.IntValue = _windsOfMagic;

            _maxArtillery = MobileParty.MainParty.GetMaxNumberOfArtillery();
            _artilleryInfo.HasWarning = false;
            _artilleryInfo.Value = _maxArtillery.ToString();
            _artilleryInfo.IntValue = _maxArtillery;

            var resourceValue = Hero.MainHero.GetCultureSpecificCustomResourceValue();
            _resourceInfo.HasWarning = resourceValue < 0f;
            _resourceInfo.Value = ((int)resourceValue).ToString();
            _resourceInfo.IntValue = (int)resourceValue;

            var info = MobileParty.MainParty.GetPartyInfo();
            if (info != null)
            {
                _remainingBlessingTime = GetBlessingTimeInDays(info.CurrentBlessingRemainingDuration);
                var blessing = info.CurrentBlessingStringId;

                if (blessing == null)
                {
                    _blessingInfo.Value = "-";
                }
                else
                {
                    _blessingInfo.Value = _remainingBlessingTime;
                }
            }
            else _blessingInfo.Value = "-";


            _hasBaseVMBeenInitialized = true;
        }

        private String GetBlessingTimeInDays(int blessingHours)
        {
            return $"{(float)blessingHours / CampaignTime.HoursInDay:0.0}";
        }
    }
}