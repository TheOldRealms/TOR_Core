using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem
{
    public class AbilityHUD_VM : ViewModel
    {
        private Ability _ability = null;
        private string _name = "";
        private string _spriteName = "";
        private string _coolDownLeft = "";
        private string _WindsOfMagicLeft = "-";
        private string _usages="";
        private bool _isVisible;
        private bool _onCoolDown;
        private bool _isSpell;
        private bool _hasUsages;
        private bool _hasAlternativeCoolDown;
        
        private float _windsOfMagicValue;
        private string _windsCost = "";
        
        private int _alternativeCooldown=0;

        private int _offset;
        
        public AbilityHUD_VM(bool isLifted) : base()
        {
            Offset = isLifted ? 125 : 15;
        }

        public AbilityHUD_VM()
        {
            Offset = 15;
        }


        protected virtual Ability SelectAbility()
        {
            var ability = Agent.Main.GetCurrentAbility();
            IsVisible = ability != null && (Mission.Current.Mode == MissionMode.Battle || Mission.Current.Mode == MissionMode.Stealth);
            return ability;
        }

        public void UpdateProperties()
        {
            
            _ability = SelectAbility();
            SelectAbility();
            if (IsVisible)
            {
                IsSpell = _ability is Spell;
                SpriteName = _ability.Template.SpriteName;
                Name = _ability.Template.Name;
                WindsCost = _ability.Template.WindsOfMagicCost.ToString();
                CoolDownLeft = _ability.GetCoolDownLeft().ToString();
                
                HasUsages = _ability.HasUsages();

                
                IsOnCoolDown = _ability.IsOnCooldown();

                if (!_ability.ReachedChargeRequirement(out float value) && IsOnCoolDown == false)
                {
                    if (value < 1)
                    {
                        HasAlternativeCoolDown = true;
                        AlternativeCoolDown = (int)(value*100);
                    }
                    IsOnCoolDown = true;
                    CoolDownLeft = AlternativeCoolDown +" %";
                }


                if (HasUsages)
                {
                    var usages = _ability.GetCurrentLeftUsages();
                    if (_onCoolDown)
                    {
                        if (usages > 0&&usages!=_ability.Template.Usages)    
                        {
                            Usages= _ability.GetCurrentLeftUsages().ToString();
                        }
                        else
                        {
                            Usages = "";
                        }

                        
                    }
                    else
                    {
                        Usages= _ability.GetCurrentLeftUsages().ToString();
                    }
                    

                }
                   
                

                
                
                if (Game.Current.GameType is Campaign && _ability is Spell)
                {
                    SetWindsOfMagicValue((float)(Agent.Main?.GetHero()?.GetExtendedInfo()?.CurrentWindsOfMagic));
                    var windsCost = AddPerkEffectsToWindsCost(Agent.Main?.GetHero(), _ability.Template);
                    WindsCost = windsCost.ToString();
                    if (_windsOfMagicValue < windsCost)
                    {
                        if (!IsOnCoolDown)
                        {
                            CoolDownLeft = "";
                        }
                        IsOnCoolDown = true;
                    }
                }
            }
        }

        private int AddPerkEffectsToWindsCost(Hero hero, AbilityTemplate template)
        {
            int result = template.WindsOfMagicCost;
            var model = Campaign.Current.Models.GetSpellcraftModel();
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
        public int Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                _offset = value;
                base.OnPropertyChangedWithValue(value, "Offset");
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
        public int AlternativeCoolDown
        {
            get
            {
                return _alternativeCooldown;
            }
            set
            {
                if (value != _alternativeCooldown)
                {
                    _alternativeCooldown = value;
                    base.OnPropertyChangedWithValue(value, "AlternativeCoolDown");
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
        public string Usages
        {
            get
            {
                return _usages;
            }
            set
            {
                if (value != _usages)
                {
                    _usages = value;
                    base.OnPropertyChangedWithValue(value, "Usages");
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
        public bool HasAlternativeCoolDown
        {
            get
            {
                return _hasAlternativeCoolDown;
            }
            set
            {
                if (value != _hasAlternativeCoolDown)
                {
                    _hasAlternativeCoolDown = value;
                    base.OnPropertyChangedWithValue(value, "HasAlternativeCoolDown");
                }
            }
        }
        
        [DataSourceProperty]
        public bool HasUsages
        {
            get
            {
                return _hasUsages;
            }
            set
            {
                if (value != _hasUsages)
                {
                    _hasUsages = value;
                    base.OnPropertyChangedWithValue(value, "HasUsages");
                }
            }
        }
    }
}
