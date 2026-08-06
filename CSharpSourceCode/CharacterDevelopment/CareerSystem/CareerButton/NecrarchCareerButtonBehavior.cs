using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using static Helpers.PartyScreenHelper;

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

        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false, bool shiftClick = false)
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
            if (PartyScreenHelper.GetActivePartyState().PartyScreenMode != PartyScreenMode.Normal) return false;

            if (!characterObject.IsHero) return false;

            return characterObject.HeroObject.PartyBelongedTo == MobileParty.MainParty && characterObject.HeroObject.IsSpellCaster();
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
        {
            displayText = new TextObject("");
            if (!characterObject.IsHero) return false;

            if (Hero.MainHero.GetCustomResourceValue("WindsOfMagic") >= Hero.MainHero.GetExtendedInfo().MaxWindsOfMagic)
            {
                displayText = TORTextHelper.GetTextObject("tor_necrarch_winds_full_text", "Your Winds are already full");
                return false;
            }

            if (Hero.MainHero.GetCustomResourceValue("DarkEnergy") < _costForClick)
            {
                var requiresText = TORTextHelper.GetTextObject("tor_necrarch_requires_text", "Requires {COST}{DARK_ENERGY_ICON}for exchange");
                requiresText.SetTextVariable("COST", _costForClick);
                requiresText.SetTextVariable("DARK_ENERGY_ICON", CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText());
                displayText = requiresText;
                return false;
            }

            var exchangeText = TORTextHelper.GetTextObject("tor_necrarch_exchange_text", "Exchange {COST}{DARK_ENERGY_ICON} for {GAIN}{WINDS_ICON}");
            exchangeText.SetTextVariable("COST", _costForClick);
            exchangeText.SetTextVariable("DARK_ENERGY_ICON", CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText());
            exchangeText.SetTextVariable("GAIN", gainForClick);
            exchangeText.SetTextVariable("WINDS_ICON", CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText());
            displayText = exchangeText;
            return characterObject.HeroObject.PartyBelongedTo == MobileParty.MainParty;

        }
    }
}