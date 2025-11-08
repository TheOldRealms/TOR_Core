using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem
{
    public class AbilityHUD_VM : ViewModel
    {
        private Ability _ability = null;
        private string _name = "";
        private string _spriteName = "";
        private string _coolDownLeft = "";
        private string _WindsOfMagicLeft = "-";
        private bool _isVisible;
        private bool _onCoolDown;
        private bool _isSpell;
        private float _windsOfMagicValue;
        private string _windsCost = "";
        private AbilityManagerMissionLogic _abilityLogic;
        private bool _isDisabled;
        private string _disabledText;
        private string _abilityType;

        public AbilityHUD_VM() : base() { }

        public override void RefreshValues()
        {
            _ability = Agent.Main.GetCurrentAbility();
            if(_abilityLogic == null) _abilityLogic = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
            IsVisible = _ability != null && _abilityLogic != null && (Mission.Current.Mode == MissionMode.Battle || Mission.Current.Mode == MissionMode.Stealth);
            if (IsVisible)
            {
                AbilityType = "(" + _ability.Template.AbilityType.ToString() + ")";
                IsSpell = _ability is Spell;
                SpriteName = _ability.Template.SpriteName;
                Name = new TextObject(_ability.Template.Name).ToString();
                WindsCost = _ability.Template.WindsOfMagicCost.ToString();
                CoolDownLeft = _ability.GetCoolDownLeft().ToString();
                IsOnCoolDown = _ability.IsOnCooldown();
                TextObject disabledReason;
                if(_ability.IsDisabled(Agent.Main, out disabledReason))
                {
                    IsDisabled = true;
                    DisabledText = disabledReason.ToString();
                }
                else
                {
                    IsDisabled = false;
                    DisabledText = string.Empty;
                }
                if (Game.Current.GameType is Campaign)
                {
                    if (Agent.Main!=null&& Agent.Main.IsSpellCaster())
                    {
                        SetWindsOfMagicValue((float)(Agent.Main?.GetHero()?.GetCustomResourceValue("WindsOfMagic")));
                    }
                    
                    var windsCost = AddPerkEffectsToWindsCost(Agent.Main?.GetHero(), _ability.Template);
                    WindsCost = windsCost.ToString();
                }
            }
        }

        private int AddPerkEffectsToWindsCost(Hero hero, AbilityTemplate template)
        {
            int result = template.WindsOfMagicCost;
            var model = Campaign.Current.Models.GetAbilityModel();
            if(model != null && hero != null)
            {
                result = model.GetEffectiveWindsCost(hero.CharacterObject, template);
            }
            return result;
        }

        private void SetWindsOfMagicValue(float value)
        {
            _windsOfMagicValue = value;
            _WindsOfMagicLeft = ((int)_windsOfMagicValue).ToString();
            WindsOfMagicLeft = _WindsOfMagicLeft;
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
        public String Name
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

        [DataSourceProperty]
        public string AbilityType
        {
            get
            {
                return _abilityType;
            }
            set
            {
                if (value != _abilityType)
                {
                    _abilityType = value;
                    base.OnPropertyChangedWithValue(value, "AbilityType");
                }
            }
        }

        [DataSourceProperty]
        public bool IsDisabled
        {
            get
            {
                return _isDisabled;
            }
            set
            {
                if (value != _isDisabled)
                {
                    _isDisabled = value;
                    base.OnPropertyChangedWithValue(value, "IsDisabled");
                }
            }
        }

        [DataSourceProperty]
        public string DisabledText
        {
            get
            {
                return _disabledText;
            }
            set
            {
                if (value != _disabledText)
                {
                    _disabledText = value;
                    base.OnPropertyChangedWithValue(value, "DisabledText");
                }
            }
        }
    }
}
