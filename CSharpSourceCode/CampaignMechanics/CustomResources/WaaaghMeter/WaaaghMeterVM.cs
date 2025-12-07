using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.WaaaghMeter
{
    public class WaaaghMeterVM : ViewModel
    {
        private const float BarHeight = 455f; // Must match SuggestedHeight in XML
        private const float IconSize = 52f;   // Hover zone size for centering calculation

        private int _waaaghValue;
        private int _currentLevel;
        private float _fillPercentage;
        private float _fillHeight;
        private string _stateName;
        private bool _isVisible;
        private bool _isGreenskin;

        private BasicTooltipViewModel _level0Hint;
        private BasicTooltipViewModel _level1Hint;
        private BasicTooltipViewModel _level2Hint;
        private BasicTooltipViewModel _level3Hint;
        private BasicTooltipViewModel _barHint;

        // Icon positions (MarginBottom) calculated from thresholds - centers icon on threshold line
        [DataSourceProperty]
        public float Level0Position => CalculateIconPosition(WaaaghLevel.InternalFightin);

        [DataSourceProperty]
        public float Level1Position => CalculateIconPosition(WaaaghLevel.PettySquabblin);

        [DataSourceProperty]
        public float Level2Position => CalculateIconPosition(WaaaghLevel.EreWeGo);

        [DataSourceProperty]
        public float Level3Position => CalculateIconPosition(WaaaghLevel.WAAAGH);

        private float CalculateIconPosition(WaaaghLevel level)
        {
            // Position = (percentage * barHeight) - (iconSize / 2) to center icon on threshold
            return (WaaaghHelper.GetThresholdPercentage(level) * BarHeight) - (IconSize / 2f);
        }

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
        public BasicTooltipViewModel BarHint
        {
            get => _barHint;
            set
            {
                if (_barHint != value)
                {
                    _barHint = value;
                    OnPropertyChangedWithValue(value, nameof(BarHint));
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

        private void UpdateVisibility()
        {
            // With the SpellBook pattern, this MapView only exists on MapScreen
            // so we only need to check if player is Greenskin
            IsVisible = _isGreenskin;
        }

        public WaaaghMeterVM()
        {
            // Initialize tooltip hints for each level using helper methods
            Level0Hint = new BasicTooltipViewModel(() => GetLevelTooltipText(WaaaghLevel.InternalFightin));
            Level1Hint = new BasicTooltipViewModel(() => GetLevelTooltipText(WaaaghLevel.PettySquabblin));
            Level2Hint = new BasicTooltipViewModel(() => GetLevelTooltipText(WaaaghLevel.EreWeGo));
            Level3Hint = new BasicTooltipViewModel(() => GetLevelTooltipText(WaaaghLevel.WAAAGH));
            BarHint = new BasicTooltipViewModel(() => GetBarTooltipText());

            RefreshValues();
        }

        private string GetLevelTooltipText(WaaaghLevel level)
        {
            var levelName = WaaaghHelper.GetLevelName(level);
            var description = WaaaghHelper.GetLevelDescription(level);
            var effects = WaaaghHelper.GetLevelEffects(level);
            var threshold = (int)WaaaghHelper.GetResourceMinimumForWaaaghRank(level);

            return $"{levelName}\nThreshold: {threshold}\n\n{description}\n\nEffects:\n{effects}";
        }

        private string GetBarTooltipText()
        {
            return WaaaghHelper.GetBarTooltip(_waaaghValue).ToString();
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
            var waaaghLevel = WaaaghHelper.GetWaaaghLevelForResource(_waaaghValue);
            CurrentLevel = (int)waaaghLevel;

            // Update state name using helper
            StateName = WaaaghHelper.GetLevelName(waaaghLevel).ToString();

            // Calculate fill percentage (0-100 scale)
            FillPercentage = Math.Min(_waaaghValue / WaaaghHelper.MaxWaaagh * 100f, 100f);

            // Calculate fill height in pixels for UI binding
            FillHeight = (FillPercentage / 100f) * BarHeight;
        }
    }
}
