using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.WaaaghMeter
{
    public class WaaaghMeterVM : ViewModel
    {
        private const float MaxBarHeight = 220f; // Match the BarHeight constant in XML

        private int _waaaghValue;
        private int _currentLevel;
        private float _fillPercentage;
        private float _fillHeight;
        private string _stateName;
        private Color _progressBarColor;
        private bool _isVisible;

        [DataSourceProperty]
        public int WaaaghValue
        {
            get => _waaaghValue;
            set
            {
                if (_waaaghValue != value)
                {
                    _waaaghValue = value;
                    OnPropertyChangedWithValue(value, nameof(WaaaghValue));
                    UpdateMeterState();
                }
            }
        }

        [DataSourceProperty]
        public int CurrentLevel
        {
            get => _currentLevel;
            set
            {
                if (_currentLevel != value)
                {
                    _currentLevel = value;
                    OnPropertyChangedWithValue(value, nameof(CurrentLevel));
                }
            }
        }

        [DataSourceProperty]
        public float FillPercentage
        {
            get => _fillPercentage;
            set
            {
                if (Math.Abs(_fillPercentage - value) > 0.001f)
                {
                    _fillPercentage = value;
                    OnPropertyChangedWithValue(value, nameof(FillPercentage));
                }
            }
        }

        [DataSourceProperty]
        public float FillHeight
        {
            get => _fillHeight;
            set
            {
                if (Math.Abs(_fillHeight - value) > 0.001f)
                {
                    _fillHeight = value;
                    OnPropertyChangedWithValue(value, nameof(FillHeight));
                }
            }
        }

        [DataSourceProperty]
        public string StateName
        {
            get => _stateName;
            set
            {
                if (_stateName != value)
                {
                    _stateName = value;
                    OnPropertyChangedWithValue(value, nameof(StateName));
                }
            }
        }

        [DataSourceProperty]
        public Color ProgressBarColor
        {
            get => _progressBarColor;
            set
            {
                if (_progressBarColor != value)
                {
                    _progressBarColor = value;
                    OnPropertyChangedWithValue(value, nameof(ProgressBarColor));
                }
            }
        }

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsVisible));

                    // Debug logging
                    InformationManager.DisplayMessage(new InformationMessage($"[WaaaghMeterVM] IsVisible changed to: {value}", new Color(134, 114, 250)));
                }
            }
        }

        public WaaaghMeterVM()
        {
            InformationManager.DisplayMessage(new InformationMessage("[WaaaghMeterVM] Constructor called", new Color(134, 114, 250)));
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            // Only visible for Greenskin culture
            if (Hero.MainHero != null && Hero.MainHero.Culture != null)
            {
                var isGreenskin = Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN;
                IsVisible = isGreenskin;

                InformationManager.DisplayMessage(new InformationMessage($"[WaaaghMeterVM] RefreshValues - Culture: {Hero.MainHero.Culture.StringId}, IsGreenskin: {isGreenskin}", new Color(134, 114, 250)));

                if (IsVisible)
                {
                    WaaaghValue = (int)Hero.MainHero.GetCustomResourceValue("Waaagh");
                    InformationManager.DisplayMessage(new InformationMessage($"[WaaaghMeterVM] Waaagh Value: {WaaaghValue}, FillHeight: {FillHeight}", new Color(134, 114, 250)));
                }
            }
            else
            {
                IsVisible = false;
                InformationManager.DisplayMessage(new InformationMessage("[WaaaghMeterVM] RefreshValues - Hero or Culture is null", new Color(134, 114, 250)));
            }
        }

        private void UpdateMeterState()
        {
            // Get current Waaagh level
            var waaaghLevel = TeefHelper.GetWaaaghLevelForResource(_waaaghValue);
            CurrentLevel = (int)waaaghLevel;

            // Update state name
            StateName = waaaghLevel switch
            {
                WaaaghLevel.InternalFightin => "Internal Fightin'",
                WaaaghLevel.PettySquabblin => "Petty Squabblin'",
                WaaaghLevel.EreWeGo => "'Ere We Go!",
                WaaaghLevel.WAAAGH => "WAAAGH!!!!",
                _ => "Unknown"
            };

            // Calculate fill percentage (0.0 to 1.0)
            // Maximum Waaagh is 1000, so fill percentage is based on that
            const float maxWaaagh = 1000f;
            FillPercentage = Math.Min(_waaaghValue / maxWaaagh, 1.0f);

            // Calculate actual fill height in pixels for UI
            FillHeight = FillPercentage * MaxBarHeight;

            // Just use red color for now
            ProgressBarColor = Color.FromUint(0xFF0000FF); // Red

            InformationManager.DisplayMessage(new InformationMessage($"[WaaaghMeterVM] UpdateMeterState - Level: {CurrentLevel}, State: {StateName}, FillHeight: {FillHeight}", new Color(134, 114, 250)));
        }
    }
}