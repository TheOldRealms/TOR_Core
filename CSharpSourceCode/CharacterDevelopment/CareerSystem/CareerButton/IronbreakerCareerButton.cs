using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

public class IronbreakerCareerButtonBehavior(CareerObject careerObject) : CareerButtonBehaviorBase(careerObject)
{
    private const string IronbreakerId = "tor_dw_ironbreaker";
    private const int BaseExchangeCost = 15;
    private const int GoldCost = 1000;

    private int GetExchangeCost()
    {
        var cost = BaseExchangeCost;
        if (Hero.MainHero.HasCareerChoice("IronPricePassive4"))
        {
            cost = (int)(cost * 0.75f);
        }
        return cost;
    }

    public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner, bool shiftClick)
    {
        var ironbreakerUnit = MBObjectManager.Instance.GetObject<CharacterObject>(IronbreakerId);
        var exchangeCost = GetExchangeCost();

        if (shiftClick)
        {
            var buyableTroops = CareerButtonHelper.GetMaximumExchangeTroops(characterObject, false, 5, GoldCost, exchangeCost);

            for (int i = 0; i < buyableTroops; i++)
            {
                CustomResourceManager.AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(), exchangeCost);
                PartyScreenHelper.GetActivePartyState().PartyScreenLogic.CurrentData.PartyGoldChangeAmount -= GoldCost;
                CareerButtonHelper.ExchangeUnitForNewUnit(characterObject, ironbreakerUnit, true);

            }
        }
        else
        {
            PartyScreenHelper.GetActivePartyState().PartyScreenLogic.CurrentData.PartyGoldChangeAmount -= GoldCost;
            CustomResourceManager.AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(), exchangeCost);
            CareerButtonHelper.ExchangeUnitForNewUnit(characterObject, ironbreakerUnit, true);
        }

        PartyVMExtension.ViewModelInstance.RefreshValues();
    }

    public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
    {
        if (isPrisoner)
            return false;

        if (characterObject.IsHero)
            return false;

        if (characterObject.IsIronbreakerUnit())
        {
            return false;
        }

        if (characterObject.Culture.StringId != TORConstants.Cultures.DAWI)
            return false;

        return true;
    }

    public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
    {
        displayText = TextObject.GetEmpty();

        var pending = CustomResourceManager.GetPendingResources().Values.ToList().Sum();
        var exchangeCost = GetExchangeCost();

        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < pending + exchangeCost)
        {
            displayText = TORTextHelper.GetTextObject("tor_ironbreaker_not_enough_resources_text", "Not enough resources");
            return false;
        }

        var number = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementNumber(characterObject);

        if (number <= 0)
        {
            return false;
        }

        if (characterObject.Level < 16)
        {
            displayText = TORTextHelper.GetTextObject("tor_ironbreaker_level_too_low_text", " not high enough level");
            return false;
        }

        return true;
    }
}