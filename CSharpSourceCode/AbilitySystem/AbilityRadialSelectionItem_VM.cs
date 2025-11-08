using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem
{
    public class AbilityRadialSelectionItem_VM : ViewModel
    {
        private Ability _ability;
        private string _spriteName;
        private Action<Ability> _onSelected;

        public AbilityRadialSelectionItem_VM(Ability ability, Action<Ability> onSelected) : base()
        {
            _ability = ability;
            _spriteName = _ability.Template.SpriteName;
            _onSelected = onSelected;
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
                    OnPropertyChangedWithValue(value, "SpriteName");
                }
            }
        }

        public void OnSelected()
        {
            _onSelected(_ability);
        }
    }
}
