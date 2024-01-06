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
            if (!ContainsState("Selected")) AddState("Selected");
            if (!ContainsState("Default")) AddState("Default");
            if (!ContainsState("Pressed")) AddState("Pressed");
            if (!ContainsState("Hovered")) AddState("Hovered");
            if (!ContainsState("Disabled")) AddState("Disabled");
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
            if(propertyName == "IsSelected")
            {
                if(value)
                {
                    SetState("Selected");
                    EventFired("OnSelected", Array.Empty<object>());
                }
                else SetState("Default");
            }
        }
    }
}
