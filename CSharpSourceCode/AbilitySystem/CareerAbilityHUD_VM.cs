using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem
{
    public class CareerAbilityHUD_VM : ViewModel
    {
        private CareerAbility _ability = null;
        private string _name = "";
        private string _spriteName = "";
        private string _coolDownLeft = "";
        private string _WindsOfMagicLeft = "-";
        private bool _isVisible;
        private bool _onCoolDown;
        private bool _isSpell;
        private float _windsOfMagicValue;
        private string _windsCost = "";

        public CareerAbilityHUD_VM() : base()
        {
            
        }

        public void UpdateProperties()
        {
            _ability = Agent.Main.GetCareerAbility();
            IsVisible = _ability != null && (Mission.Current.Mode == MissionMode.Battle || Mission.Current.Mode == MissionMode.Stealth);
            if (IsVisible)
            {
                SpriteName = _ability.Template.SpriteName;
                Name = _ability.Template.Name;
                CoolDownLeft = _ability.GetCoolDownLeft().ToString();
                if (_ability.Template.CoolDownType != CoolDownType.Time)
                {
                    IsOnCoolDown = _ability.IsOnCooldown()&&_ability.ReachedCoolDownLoad();
                }
                else
                {
                    IsOnCoolDown = _ability.IsOnCooldown();
                }
                
            }
        }


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
        public string WindsOfMagicLeft
        {
            get
            {
                return _WindsOfMagicLeft;
            }
            set
            {
                _WindsOfMagicLeft = value;
                base.OnPropertyChangedWithValue(value, "WindsOfMagicLeft");
            }
        }

        [DataSourceProperty]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    base.OnPropertyChangedWithValue(value, "Name");
                }
            }
        }

        [DataSourceProperty]
        public string SpriteName
        {
            get
            {
                return _spriteName;
            }
            set
            {
                if (value != _spriteName)
                {
                    _spriteName = value;
                    base.OnPropertyChangedWithValue(value, "SpriteName");
                }
            }
        }

        [DataSourceProperty]
        public string CoolDownLeft
        {
            get
            {
                return _coolDownLeft;
            }
            set
            {
                if (value != _coolDownLeft)
                {
                    _coolDownLeft = value;
                    base.OnPropertyChangedWithValue(value, "CoolDownLeft");
                }
            }
        }

        [DataSourceProperty]
        public bool IsOnCoolDown
        {
            get
            {
                return _onCoolDown;
            }
            set
            {
                if (value != _onCoolDown)
                {
                    _onCoolDown = value;
                    base.OnPropertyChangedWithValue(value, "IsOnCoolDown");
                }
            }
        }

        [DataSourceProperty]
        public string WindsCost
        {
            get
            {
                return _windsCost;
            }
            set
            {
                if (value != _windsCost)
                {
                    _windsCost = value;
                    base.OnPropertyChangedWithValue(value, "WindsCost");
                }
            }
        }

        [DataSourceProperty]
        public bool IsSpell
        {
            get
            {
                return _isSpell;
            }
            set
            {
                if (value != _isSpell)
                {
                    _isSpell = value;
                    base.OnPropertyChangedWithValue(value, "IsSpell");
                }
            }
        }
    }
}
