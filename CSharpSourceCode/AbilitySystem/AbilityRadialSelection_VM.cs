using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem
{
    public class AbilityRadialSelection_VM : ViewModel
    {
        private bool _isVisible;
        private MBBindingList<AbilityRadialSelectionItem_VM> _abilities = new MBBindingList<AbilityRadialSelectionItem_VM> ();
        private AbilityManagerMissionLogic _abilityLogic;
        private AbilityHUD_VM _abilityVM;
        private bool _errorMessageVisible;
        private string _errorMessageText;
        private Timer _timer;

        public AbilityRadialSelection_VM() : base() 
        {
            _timer = new Timer(2000);
            _timer.Interval = 2000;
            _timer.AutoReset = false;
            _timer.Elapsed += (s, e) =>
            {
                _timer.Stop();
                ErrorMessageVisible = false;
            };

        }

        public override void RefreshValues()
        {
            if (_abilityLogic == null) _abilityLogic = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
            if(_abilityLogic != null)
            {
                IsVisible = _abilityLogic.CurrentState == AbilityModeState.QuickMenuSelection;
                if (IsVisible)
                {
                    if (CurrentAbility == null) CurrentAbility = new AbilityHUD_VM();
                    if (CurrentAbility != null) CurrentAbility.RefreshValues();
                }
            }
        }

        public void FillAbilities(Agent agent)
        {
            _abilities.Clear();
            var comp = agent.GetComponent<AbilityComponent>();
            if(comp != null && comp.KnownAbilitySystem.Count > 0)
            {
                foreach(var ability in comp.KnownAbilitySystem)
                {
                    _abilities.Add(new AbilityRadialSelectionItem_VM(ability, OnItemSelected));
                }
            }
        }

        public void DisplayErrorMessage(string message)
        {
            if (ErrorMessageVisible && _timer.Enabled) return;
            else
            {
                ErrorMessageVisible = true;
                ErrorMessageText = message;
                _timer.Start();
            }
        }

        private void OnItemSelected(Ability ability)
        {
            Agent.Main.SelectAbility(ability);
        }

        [DataSourceProperty]
        public AbilityHUD_VM CurrentAbility
        {
            get
            {
                return _abilityVM;
            }
            set
            {
                if (value != _abilityVM)
                {
                    _abilityVM = value;
                    base.OnPropertyChangedWithValue(value, "CurrentAbility");
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
        public bool ErrorMessageVisible
        {
            get
            {
                return _errorMessageVisible;
            }
            set
            {
                if (value != _errorMessageVisible)
                {
                    _errorMessageVisible = value;
                    base.OnPropertyChangedWithValue(value, "ErrorMessageVisible");
                }
            }
        }

        [DataSourceProperty]
        public string ErrorMessageText
        {
            get
            {
                return _errorMessageText;
            }
            set
            {
                if (value != _errorMessageText)
                {
                    _errorMessageText = value;
                    base.OnPropertyChangedWithValue(value, "ErrorMessageText");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<AbilityRadialSelectionItem_VM> Abilities
        {
            get
            {
                return _abilities;
            }
            set
            {
                if (value != _abilities)
                {
                    _abilities = value;
                    base.OnPropertyChangedWithValue(value, "Abilities");
                }
            }
        }
    }
}
