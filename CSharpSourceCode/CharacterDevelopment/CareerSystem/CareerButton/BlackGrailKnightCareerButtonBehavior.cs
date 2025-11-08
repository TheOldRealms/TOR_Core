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
                    CustomResourceManager.AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(),ExchangeCost);
                    CareerButtonHelper.ExchangeUnitForNewUnit(characterObject, knightUnit, isPrisoner);
                }
            }
            else
            {
                CustomResourceManager.AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(),ExchangeCost);
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
                    displayText = new TextObject("Not enough healthy prisoners available");
                    return false;
                }
            }
            else
            {
                var healthytroops = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementNumber(index);
                var woundedtroops = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementWoundedNumber(index);
                if (healthytroops - woundedtroops < 0)
                {
                    displayText = new TextObject("Not enough healthy troops available");
                    return false;
                }
            }
            
            var pendingResources = CustomResourceManager.GetPendingResources();
            if (!pendingResources.IsEmpty()&& pendingResources[Hero.MainHero.GetCultureSpecificCustomResource()] + ExchangeCost > Hero.MainHero.GetCultureSpecificCustomResourceValue())
            {
                displayText = new TextObject("Requires atleast " + ExchangeCost + " " + CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText());
                return false; 
            }

            displayText = new TextObject("");

            if (characterObject.StringId == "tor_br_grail_knight")
            {
                displayText = new TextObject("Grail knights can't be convinced");
                return false;
            }

            return true;
        }
    }
}