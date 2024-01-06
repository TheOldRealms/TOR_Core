using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public AbilityRadialSelection_VM() : base() { }

        public override void RefreshValues()
        {
            if (_abilityLogic == null) _abilityLogic = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
            if(_abilityLogic != null)
            {
                IsVisible = _abilityLogic.CurrentState == AbilityModeState.QuickMenuSelection;
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

        private void OnItemSelected(Ability ability)
        {
            Agent.Main.SelectAbility(ability);
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
