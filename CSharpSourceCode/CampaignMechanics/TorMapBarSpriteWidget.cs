using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics
{
    public class TORMapBarSpriteWidget : Widget
    {
        public TORMapBarSpriteWidget(UIContext context) : base(context) { }

        protected override void OnLateUpdate(float dt)
        {
            if(Game.Current.GameType is Campaign && Hero.MainHero != null)
            {
                var resource = Hero.MainHero.GetCultureSpecificCustomResource();
                if(resource != null)
                {
                    Sprite = Context.SpriteData.GetSprite(resource.SmallIconName);
                }
            }   
        }
    }
}
