using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.AbilitySystem
{
    public class CareerAbilityHUD_VM : ViewModel
    {
        private bool _isVisible;
        private int _chargeLevel;

        public CareerAbilityHUD_VM() : base() { }

        public void UpdateProperties()
        {
            IsVisible = true;
            ChargeLevel = Convert.ToInt32(CareerAbility.ChargeLevel);
        }

        public CareerAbility CareerAbility { get; set; }

        [DataSourceProperty]
        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    base.OnPropertyChangedWithValue(value, "IsVisible");
                }
            }
        }

        [DataSourceProperty]
        public int ChargeLevel
        {
            get
            {
                return _chargeLevel;
            }
            set
            {
                if (value != _chargeLevel)
                {
                    _chargeLevel = value;
                    base.OnPropertyChangedWithValue(value, "ChargeLevel");
                }
            }
        }
    }
}
