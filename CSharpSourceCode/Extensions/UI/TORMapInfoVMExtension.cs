using System;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

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
        private const double MAP_INFO_REFRESH_THROTTLE_SECONDS = 0.2;
        private static readonly long MapInfoRefreshThrottleTicks =
            (long)(Stopwatch.Frequency * MAP_INFO_REFRESH_THROTTLE_SECONDS);
        private long _lastRefreshTimestamp;
        private static readonly FieldInfo SpeedInfoField =
    typeof(MapInfoVM).GetField("_speedInfo", BindingFlags.Instance | BindingFlags.NonPublic);

        private MapInfoItemVM _windsInfo;
        private MapInfoItemVM _artilleryInfo;
        private MapInfoItemVM _resourceInfo;
        private MapInfoItemVM _blessingInfo;
        private MapInfoItemVM _partySpeedInfo;
        public TORMapInfoVMExtension(ViewModel vm) : base(vm)
        {
            _windsInfo = new MapInfoItemVM("winds", GetWindsHintText);
            _artilleryInfo = new MapInfoItemVM("artillery", GetArtilleryHintText);
            _resourceInfo = new MapInfoItemVM("resources", GetCultureResourceHintText);
            _blessingInfo = new MapInfoItemVM("blessing", GetBlessingHintText);
            _partySpeedInfo = new MapInfoItemVM("speed", GetPartySpeedHintText);
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
            string artilleryTitle = TORTextHelper.GetText("tor_ui_artillery_title", "Artillery");
            string artilleryInventory = TORTextHelper.GetText("tor_ui_artillery_amount", "Current Artillery Pieces in Inventory:");
            string artilleryDeployable = TORTextHelper.GetText("tor_ui_artillery_max_deployable", "Maximum Deployable Artillery Pieces:");

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
            string womTitle = TORTextHelper.GetText("tor_ui_winds_of_magic_title", "Winds of Magic");
            string womMaximum = TORTextHelper.GetText("tor_ui_winds_of_magic_maximum", "Maximum:");
            string womRechargeRate = TORTextHelper.GetText("tor_ui_winds_of_magic_recharge_rate", "Recharge Rate:");

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
            var blessingTitle = TORTextHelper.GetText("tor_ui_blessing_title", "Blessing: ");
            var durationTitle = TORTextHelper.GetText("tor_ui_blessing_duration", "Duration:");
            var effect = TORTextHelper.GetText("tor_ui_blessing_effect", "Effect:");

            var list = new List<TooltipProperty>();
            if (Hero.MainHero.PartyBelongedTo == null) return list;

            // Add Devotion section
            var heroInfo = Hero.MainHero.GetExtendedInfo();
            if (heroInfo != null)
            {
                list.Add(new TooltipProperty(TORTextHelper.GetText("tor_ui_devotion", "Devotion"), "", 0, false,
                    TooltipProperty.TooltipPropertyFlags.Title));

                if (heroInfo.ReligionDevotionLevels.Count > 0)
                {
                    foreach (var entry in heroInfo.ReligionDevotionLevels.Where(x => x.Value > 0).OrderByDescending(x => x.Value))
                    {
                        var religion = ReligionObject.All.FirstOrDefault(x => x.StringId == entry.Key);
                        if (religion == null) continue;

                        var godName = GameTexts.TryGetText("tor_religion_name_of_god", out var nameText, religion.StringId)
                            ? nameText.ToString()
                            : religion.DeityName.ToString();

                        var devotionLevel = Hero.MainHero.GetDevotionLevelForReligion(religion);
                        var devotionText = GameTexts.TryGetText("tor_religion_devotionlevel", out var levelText, devotionLevel.ToString())
                            ? levelText.ToString()
                            : devotionLevel.ToString();

                        var displayText = Game.Current.CheatMode ? $"{devotionText} ({entry.Value})" : devotionText;
                        list.Add(new TooltipProperty(godName, displayText, 0, false,
                            TooltipProperty.TooltipPropertyFlags.None));
                    }
                }
                else
                {
                    list.Add(new TooltipProperty(TORTextHelper.GetText("tor_ui_no_devotion", "No devotion"), "", 0, false,
                        TooltipProperty.TooltipPropertyFlags.None));
                }

                list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.None)); // Spacer
            }

            // Add Active Blessing section
            var partyInfo = Hero.MainHero.PartyBelongedTo.GetPartyInfo();
            var blessing = partyInfo.CurrentBlessingStringId;

            if (blessing == null)
            {
                list.Add(new TooltipProperty(TORTextHelper.GetText("tor_ui_active_blessing", "Active Blessing"), "", 0, false,
                    TooltipProperty.TooltipPropertyFlags.Title));
                list.Add(new TooltipProperty(TORTextHelper.GetText("tor_ui_no_blessing", "No active blessing"), "", 0, false,
                    TooltipProperty.TooltipPropertyFlags.None));
                if (Hero.MainHero.IsVampire())
                    list.Add(new TooltipProperty("You are a vampire, you are your own god", "", 0, false,
                        TooltipProperty.TooltipPropertyFlags.None));
                return list;
            }

            var religionObject = ReligionObject.All.FirstOrDefault(x => x.StringId == blessing);
            if (religionObject == null) return list;
            var effectText = GameTexts.FindText("tor_religion_blessing_effect_description", religionObject.StringId);

            var duration = partyInfo.CurrentBlessingRemainingDuration;

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

        private void ReplaceSpeedInfoItemIfNeeded(MapInfoVM mapInfoVm)
        {
            var existingSpeedInfo = (MapInfoItemVM)SpeedInfoField.GetValue(mapInfoVm);
            if (ReferenceEquals(existingSpeedInfo, _partySpeedInfo))
            {
                return;
            }
            if (existingSpeedInfo != null)
            {
                _partySpeedInfo.Value = existingSpeedInfo.Value;

                _partySpeedInfo.VisualId = existingSpeedInfo.VisualId;
            }

            for (int i = 0; i < mapInfoVm.PrimaryInfoItems.Count; i++)
            {
                var item = mapInfoVm.PrimaryInfoItems[i];
                if (item.ItemId == "speed" && !ReferenceEquals(item, _partySpeedInfo))
                {
                    mapInfoVm.PrimaryInfoItems.RemoveAt(i);
                    mapInfoVm.PrimaryInfoItems.Insert(i, _partySpeedInfo);
                    break;
                }
            }

            SpeedInfoField.SetValue(mapInfoVm, _partySpeedInfo);
        }
        private static string FormatSignedSpeedNumber(float value)
        {
            return value.ToString("+0.##;-0.##;0.##", CultureInfo.InvariantCulture);
        }

        private List<TooltipProperty> GetPartySpeedHintText()
        {
            var inspectedParty = MobileParty.MainParty.Army?.LeaderParty ?? MobileParty.MainParty;

            var effectiveSpeed = (inspectedParty.IsActive && inspectedParty.CurrentNavigationFace.IsValid())
                ? inspectedParty.Speed
                : 0f;

            var effectiveSpeedRounded = MathF.Round(effectiveSpeed, 1);
            var speedModel = Campaign.Current.Models.PartySpeedCalculatingModel;
            var explainedBase = speedModel.CalculateBaseSpeed(inspectedParty, includeDescriptions: true);
            var explained = speedModel.CalculateFinalSpeed(inspectedParty, explainedBase);

            List<TooltipProperty> list =
            [
                new TooltipProperty("Party Speed", effectiveSpeedRounded.ToString("0.0", CultureInfo.InvariantCulture), 0, false, TooltipProperty.TooltipPropertyFlags.Title),
    ];

            foreach (var line in explained.GetLines())
            {
                if (!line.number.ApproximatelyEqualsTo(0.0f))
                {
                    list.Add(new TooltipProperty(
                        line.name,
                        FormatSignedSpeedNumber(line.number),
                        0,
                        false,
                        TooltipProperty.TooltipPropertyFlags.None));
                }
            }

            list.Add(new TooltipProperty(
                "Total",
                effectiveSpeedRounded.ToString("0.0", CultureInfo.InvariantCulture),
                0,
                false,
                TooltipProperty.TooltipPropertyFlags.RundownResult));

            return list;
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            var mapInfoVm = (MapInfoVM)_vm;
            ReplaceSpeedInfoItemIfNeeded(mapInfoVm);
            if (_hasBaseVMBeenInitialized && !_haveInfoItemsBeenAdded)
            {
                (_vm as MapInfoVM).SecondaryInfoItems.Add(_windsInfo);
                (_vm as MapInfoVM).SecondaryInfoItems.Add(_artilleryInfo);
                (_vm as MapInfoVM).SecondaryInfoItems.Add(_resourceInfo);
                (_vm as MapInfoVM).SecondaryInfoItems.Add(_blessingInfo);
                _haveInfoItemsBeenAdded = true;
            }
            var nowTimestamp = Stopwatch.GetTimestamp();
            if (_lastRefreshTimestamp != 0 &&
                (nowTimestamp - _lastRefreshTimestamp) <= MapInfoRefreshThrottleTicks)
            {
                return;
            }
            _lastRefreshTimestamp = nowTimestamp;

            var heroInfo = Hero.MainHero.GetExtendedInfo();
            _windsOfMagic = (int)heroInfo.GetCustomResourceValue("WindsOfMagic");
            _maxWinds = (int)heroInfo.MaxWindsOfMagic;
            _windRechargeRate = heroInfo.WindsOfMagicRechargeRate;
            _windsInfo.HasWarning = _windsOfMagic < 0;
            _windsInfo.Value = _windsOfMagic.ToString();
            _windsInfo.IntValue = _windsOfMagic;

            _maxArtillery = MobileParty.MainParty.GetMaxNumberOfArtillery();
            _currentArtilleryItems = MobileParty.MainParty.GetArtilleryItems().Count;
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