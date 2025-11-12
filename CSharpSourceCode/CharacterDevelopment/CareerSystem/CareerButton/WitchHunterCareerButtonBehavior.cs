using Helpers;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
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
    public class WitchHunterCareerButtonBehavior(CareerObject career) : CareerButtonBehaviorBase(career)
    {
        private const int ExchangeCost = 3;

        private const int GoldCost = 250;

        private const string RetinueId = "tor_wh_retinue";

        public override string CareerButtonIcon => "CareerSystem\\ghal_maraz";

        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false, bool shiftClick = false)
        {
            var witchHunterRetinue = MBObjectManager.Instance.GetObject<CharacterObject>(RetinueId);

            if (shiftClick)
            {
                var affordable = CareerButtonHelper.GetMaximumExchangeTroops(characterObject, false, 5, 250, ExchangeCost);

                for (int i = 0; i < affordable; i++)
                {
                    CustomResourceManager.AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(), ExchangeCost);
                    PartyScreenHelper.GetActivePartyState().PartyScreenLogic.CurrentData.PartyGoldChangeAmount -= GoldCost;
                    CareerButtonHelper.ExchangeUnitForNewUnit(characterObject, witchHunterRetinue, true);
                }
            }
            else
            {
                CustomResourceManager.AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(), ExchangeCost);
                PartyScreenHelper.GetActivePartyState().PartyScreenLogic.CurrentData.PartyGoldChangeAmount -= GoldCost;
                CareerButtonHelper.ExchangeUnitForNewUnit(characterObject, witchHunterRetinue, true);
            }
        }

        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner)
        {

            if (PartyScreenHelper.GetActivePartyState().PartyScreenMode != PartyScreenMode.Normal) return false;

            if (characterObject.IsHero) return false;

            if (isPrisoner) return false;

            if (!Hero.MainHero.HasCareerChoice("SilverHammerPassive4")) return false;

            if (characterObject.StringId.Contains(RetinueId))
                return false;

            if (!characterObject.IsHero)
                return true;

            return false;
        }

        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
        {
            var index = -1;
            displayText = TextObject.GetEmpty();
            index = Hero.MainHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(characterObject);

            if (index == -1) return false;

            displayText = new TextObject("Upgrades troop to a Witch Hunter Retinue");
            if (characterObject.IsEliteTroop())
            {
                displayText = new TextObject("Knights Cant be upgraded to Retinues");
                return false;
            }

            var healthyTroops = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementNumber(index);
            var woundedTroops = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementWoundedNumber(index);
            if (healthyTroops - woundedTroops < 0)
            {
                displayText = new TextObject("Not enough healthy troops available");
                return false;
            }

            if (characterObject.Culture.StringId == TORConstants.Cultures.BRETONNIA || characterObject.Race != 0)
            {
                displayText = new TextObject("Needs to be part of the empire or southern realms");
                return false;
            }


            var pendingResources = CustomResourceManager.GetPendingResources();
            if (!pendingResources.IsEmpty() && pendingResources[Hero.MainHero.GetCultureSpecificCustomResource()] + ExchangeCost > Hero.MainHero.GetCultureSpecificCustomResourceValue())
            {
                //Sly : dark energy icon instead of prestige? Shows prestige in-game, so I'm unsure in what context this would show in to check.
                //Would this line be localizable or it needs to have a {variable} that's set? Can't even find it to check.
                displayText = new TextObject("Requires atleast " + ExchangeCost + " " + CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText());
                return false;
            }

            return true;
        }
    }
}