using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.WaaaghMeter
{
    public class WaaaghMeterVM : ViewModel
    {
        private const float MaxBarHeight = 350f; // Match the BarHeight constant in XML

        private int _waaaghValue;
        private int _currentLevel;
        private float _fillPercentage;
        private float _fillHeight;
        private string _stateName;
        private Color _progressBarColor;
        private bool _isVisible;
        private bool _isGreenskin;
        private bool _isMapScreenActive = true;

        private BasicTooltipViewModel _level0Hint;
        private BasicTooltipViewModel _level1Hint;
        private BasicTooltipViewModel _level2Hint;
        private BasicTooltipViewModel _level3Hint;

        [DataSourceProperty]
        public BasicTooltipViewModel Level0Hint
        {
            get => _level0Hint;
            set
            {
                if (_level0Hint != value)
                {
                    _level0Hint = value;
                    OnPropertyChangedWithValue(value, nameof(Level0Hint));
                }
            }
        }

        [DataSourceProperty]
        public BasicTooltipViewModel Level1Hint
        {
            get => _level1Hint;
            set
            {
                if (_level1Hint != value)
                {
                    _level1Hint = value;
                    OnPropertyChangedWithValue(value, nameof(Level1Hint));
                }
            }
        }

        [DataSourceProperty]
        public BasicTooltipViewModel Level2Hint
        {
            get => _level2Hint;
            set
            {
                if (_level2Hint != value)
                {
                    _level2Hint = value;
                    OnPropertyChangedWithValue(value, nameof(Level2Hint));
                }
            }
        }

        [DataSourceProperty]
        public BasicTooltipViewModel Level3Hint
        {
            get => _level3Hint;
            set
            {
                if (_level3Hint != value)
                {
                    _level3Hint = value;
                    OnPropertyChangedWithValue(value, nameof(Level3Hint));
                }
            }
        }

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
            private set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsVisible));
                }
            }
        }

        public bool IsMapScreenActive
        {
            get => _isMapScreenActive;
            set
            {
                if (_isMapScreenActive != value)
                {
                    _isMapScreenActive = value;
                    UpdateVisibility();
                }
            }
        }

        private void UpdateVisibility()
        {
            IsVisible = _isGreenskin && _isMapScreenActive;
        }

        public WaaaghMeterVM()
        {

            // Initialize tooltip hints for each level
            Level0Hint = new BasicTooltipViewModel(() => GetLevelTooltipText(WaaaghLevel.InternalFightin));
            Level1Hint = new BasicTooltipViewModel(() => GetLevelTooltipText(WaaaghLevel.PettySquabblin));
            Level2Hint = new BasicTooltipViewModel(() => GetLevelTooltipText(WaaaghLevel.EreWeGo));
            Level3Hint = new BasicTooltipViewModel(() => GetLevelTooltipText(WaaaghLevel.WAAAGH));

            RefreshValues();
        }

        private string GetLevelTooltipText(WaaaghLevel level)
        {
            string levelName = level switch
            {
                WaaaghLevel.InternalFightin => "Internal Fightin'",
                WaaaghLevel.PettySquabblin => "Petty Squabblin'",
                WaaaghLevel.EreWeGo => "'Ere We Go!",
                WaaaghLevel.WAAAGH => "WAAAGH!!!!",
                _ => "Unknown"
            };

            string description = level switch
            {
                WaaaghLevel.InternalFightin => "Da boyz are scrapppin' amongst themselves. Not enough fightin' to go around!",
                WaaaghLevel.PettySquabblin => "Some propa' fights breakin' out. Da boyz are gettin' restless!",
                WaaaghLevel.EreWeGo => "Da WAAAGH! is buildin'! Time to krump some 'eads!",
                WaaaghLevel.WAAAGH => "WAAAGH!!! DA BOYZ ARE READY FOR WAR!!!",
                _ => ""
            };

            int threshold = level switch
            {
                WaaaghLevel.InternalFightin => 0,
                WaaaghLevel.PettySquabblin => 250,
                WaaaghLevel.EreWeGo => 500,
                WaaaghLevel.WAAAGH => 750,
                _ => 0
            };

            return $"{levelName}\n\nCurrent Waaagh: {_waaaghValue}/1000\nThreshold: {threshold}\n\n{description}";
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            // Only visible for Greenskin culture and when on MapScreen
            if (Hero.MainHero != null && Hero.MainHero.Culture != null)
            {
                _isGreenskin = Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN;
                UpdateVisibility();

                if (_isGreenskin)
                {
                    WaaaghValue = (int)Hero.MainHero.GetCustomResourceValue("Waaagh");
                }
            }
            else
            {
                _isGreenskin = false;
                UpdateVisibility();
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