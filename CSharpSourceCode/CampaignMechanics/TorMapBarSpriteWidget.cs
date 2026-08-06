using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.GauntletUI;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics
{
    public class TORMapBarSpriteWidget : IconBrushWidget
    {
        public TORMapBarSpriteWidget(UIContext context) : base(context) { }

        protected override void OnLateUpdate(float dt)
        {
            if(IconID == "resources")
            {
                if (Game.Current.GameType is Campaign && Hero.MainHero != null)
                {
                    var resource = Hero.MainHero.GetCultureSpecificCustomResource();
                    if (resource != null)
                    {
                        Sprite = Context.SpriteData.GetSprite(resource.SmallIconName);
                    }
                }
            }
        }
    }
}