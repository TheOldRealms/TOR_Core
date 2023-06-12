using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.Extensions;

namespace TOR_Core.Items
{
    public class TorImageIdentifierWidget : ImageIdentifierWidget
    {
        private bool _isMagicItem = false;

        public TorImageIdentifierWidget(UIContext context) : base(context)
        {
            PropertyChanged += TorImageIdentifierWidget_PropertyChanged;
        }

        private void TorImageIdentifierWidget_PropertyChanged(PropertyOwnerObject widget, string propertyName, object identifier)
        {
            if(propertyName == "ImageId")
            {
                var item = MBObjectManager.Instance.GetObject<ItemObject>((string)identifier);
                if(item != null) _isMagicItem = item.IsMagicalItem();
            }
        }

        protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
        {
            if (ParentWidget is BrushWidget)
            {
                var widget = ParentWidget as BrushWidget;
                if (_isMagicItem) widget.Brush.Color = TaleWorlds.Library.Color.ConvertStringToColor("#FF39FFEB");
                else widget.Brush.Color = TaleWorlds.Library.Color.White;
            }
            base.OnRender(twoDimensionContext, drawContext);
        }
    }
}
