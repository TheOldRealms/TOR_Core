using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;
using TOR_Core.Utilities;
using static Helpers.PartyScreenHelper;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class BlackGrailKnightCareerButtonBehavior(CareerObject career) : CareerButtonBehaviorBase(career)
    {
        private const string _knightId = "tor_m_knight_of_misfortune";
        private const int ExchangeCost = 15;

        public override string CareerButtonIcon => "CareerSystem\\blackgrail";

        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false, bool shiftClick = false)
        {
            var knightUnit = MBObjectManager.Instance.GetObject<CharacterObject>(_knightId);

            if (shiftClick)
            {
                var affordable = CareerButtonHelper.GetMaximumExchangeTroops(characterObject, isPrisoner, 5, 0, ExchangeCost);

                for (int i = 0; i < affordable; i++)
                {
                    CustomResourceManager.AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(), ExchangeCost);
                    CareerButtonHelper.ExchangeUnitForNewUnit(characterObject, knightUnit, true, isPrisoner);
                }
            }
            else
            {
                CustomResourceManager.AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(), ExchangeCost);
                CareerButtonHelper.ExchangeUnitForNewUnit(characterObject, knightUnit, true, isPrisoner);
            }

            PartyVMExtension.ViewModelInstance.RefreshValues(); //important refresh otherwise the methods don't get re-evaluated.
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
        {
            if (PartyScreenHelper.GetActivePartyState().PartyScreenMode != PartyScreenMode.Normal) return false;

            if (!Hero.MainHero.HasCareerChoice("ScourgeOfBretonniaPassive4")) return false;
            if (characterObject.IsHero) return false;


            if (characterObject.Culture.StringId != TORConstants.Cultures.BRETONNIA) return false;

            if (!characterObject.IsKnightUnit()) return false;

            return true;
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
        {
            displayText = new TextObject("");
            var index = -1;
            if (!isPrisoner)
            {
                index = Hero.MainHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(characterObject);
            }
            else
            {
                index = Hero.MainHero.PartyBelongedTo.PrisonRoster.FindIndexOfTroop(characterObject);
            }

            if (index == -1) return false;

            if (isPrisoner)
            {
                var healthyPrisoners = Hero.MainHero.PartyBelongedTo.PrisonRoster.GetElementNumber(index);
                var woundedPrisoners = Hero.MainHero.PartyBelongedTo.PrisonRoster.GetElementWoundedNumber(index);
                if (healthyPrisoners - woundedPrisoners < 0)
                {
                    displayText = TORTextHelper.GetTextObject("tor_black_grail_not_enough_prisoners_text", "Not enough healthy prisoners available");
                    return false;
                }
            }
            else
            {
                var healthytroops = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementNumber(index);
                var woundedtroops = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementWoundedNumber(index);
                if (healthytroops - woundedtroops < 0)
                {
                    displayText = TORTextHelper.GetTextObject("tor_black_grail_not_enough_troops_text", "Not enough healthy troops available");
                    return false;
                }
            }

            var darkEnergyResource = Hero.MainHero.GetCultureSpecificCustomResource();
            var pendingDarkEnergyCost = CustomResourceManager.GetPendingFor(darkEnergyResource.StringId);
            if (pendingDarkEnergyCost + ExchangeCost > Hero.MainHero.GetCultureSpecificCustomResourceValue())
            {
                var requiresText = TORTextHelper.GetTextObject("tor_black_grail_requires_text", "Requires atleast {EXCHANGE_COST} {DARK_ENERGY_ICON}");
                requiresText.SetTextVariable("EXCHANGE_COST", ExchangeCost);
                requiresText.SetTextVariable("DARK_ENERGY_ICON", CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText());
                displayText = requiresText;
                return false;
            }

            displayText = new TextObject("");

            if (characterObject.StringId == "tor_br_grail_knight")
            {
                displayText = TORTextHelper.GetTextObject("tor_black_grail_no_grail_knights_text", "Grail knights can't be convinced");
                return false;
            }

            return true;
        }
    }
}