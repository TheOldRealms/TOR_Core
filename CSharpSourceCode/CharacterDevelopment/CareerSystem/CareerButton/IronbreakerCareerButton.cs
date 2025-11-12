using HarmonyLib;
using Helpers;
using SandBox.View.Map;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

public class IronbreakerCareerButtonBehavior(CareerObject careerObject) : CareerButtonBehaviorBase(careerObject)
{
    private const string IronbreakerId = "tor_dw_ironbreaker";
    private const int ExchangeCost = 15;
    private const int GoldCost = 1000;

    public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner, bool shiftClick)
    {
        var ironbreakerUnit = MBObjectManager.Instance.GetObject<CharacterObject>(IronbreakerId);

        if (shiftClick)
        {
            var buyableTroops = CareerButtonHelper.GetMaximumExchangeTroops(characterObject, false, 5, GoldCost, ExchangeCost);

            for (int i = 0; i < buyableTroops; i++)
            {
                CustomResourceManager.AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(), ExchangeCost);
                PartyScreenHelper.GetActivePartyState().PartyScreenLogic.CurrentData.PartyGoldChangeAmount -= GoldCost;
                CareerButtonHelper.ExchangeUnitForNewUnit(characterObject, ironbreakerUnit, true);

            }
        }
        else
        {
            PartyScreenHelper.GetActivePartyState().PartyScreenLogic.CurrentData.PartyGoldChangeAmount -= GoldCost;
            CustomResourceManager.AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(), ExchangeCost);
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

        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < pending + ExchangeCost)
        {
            displayText = new TextObject("Not enough resources");
            return false;
        }

        var number = Hero.MainHero.PartyBelongedTo.MemberRoster.GetElementNumber(characterObject);

        if (number <= 0)
        {
            return false;
        }

        if (characterObject.Level < 16)
        {
            displayText = new TextObject(" not high enough level");
            return false;
        }

        return true;
    }
}