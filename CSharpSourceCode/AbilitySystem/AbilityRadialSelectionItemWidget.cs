using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TOR_Core.AbilitySystem
{
    public class AbilityRadialSelectionItemWidget : ButtonWidget
    {
        public AbilityRadialSelectionItemWidget(UIContext context) : base(context)
        {

        }

        protected override void OnConnectedToRoot()
        {
            base.OnConnectedToRoot();
            boolPropertyChanged += AbilityRadialSelectionItemWidget_boolPropertyChanged;
        }

        protected override void OnDisconnectedFromRoot()
        {
            base.OnDisconnectedFromRoot();
            boolPropertyChanged -= AbilityRadialSelectionItemWidget_boolPropertyChanged;
        }

        private void AbilityRadialSelectionItemWidget_boolPropertyChanged(PropertyOwnerObject widget, string propertyName, bool value)
        {
            if(propertyName == "IsSelected" && value)
            {
                EventFired("OnSelected", Array.Empty<object>());
            }
        }
    }
}
