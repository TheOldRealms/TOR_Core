using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class NecrarchCareerButtonBehavior : CareerButtonBehaviorBase
    {
        public override string CareerButtonIcon => "winds_icon_45";
        private readonly int _costForClick = 200;
        private int gainForClick = 50;
        public NecrarchCareerButtonBehavior(CareerObject career) : base(career)
        {
        }

        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false)
        {
            Hero.MainHero.AddCustomResource("WindsOfMagic", gainForClick);
            Hero.MainHero.AddCustomResource("DarkEnergy", -_costForClick);
            
            if (characterObject.IsHero && characterObject.HeroObject.IsSpellCaster())
            {
                Hero.MainHero.AddCustomResource("WindsOfMagic", gainForClick);
            }
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
        {
            if (PartyScreenManager.Instance.CurrentMode != PartyScreenMode.Normal) return false;
            
            if (!characterObject.IsHero) return false;

            return characterObject.HeroObject.PartyBelongedTo == MobileParty.MainParty&& characterObject.HeroObject.IsSpellCaster();
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
        {
            displayText= new TextObject("");
            if (!characterObject.IsHero) return false;
            
            if ( Hero.MainHero.GetCustomResourceValue("WindsOfMagic")>= Hero.MainHero.GetExtendedInfo().MaxWindsOfMagic)
            {
                displayText = new TextObject("Your Winds are already full");
                return false;
            }

            if (Hero.MainHero.GetCustomResourceValue("DarkEnergy") < _costForClick)
            {
                displayText = new TextObject("Requires " + _costForClick + CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText()+ "for exchange");
                return false;
            }

            displayText = new TextObject("Exchange " + _costForClick + CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText() +" for "+ gainForClick+ CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText() );
            return characterObject.HeroObject.PartyBelongedTo == MobileParty.MainParty;

        }
    }
}