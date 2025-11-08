using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

public class SlayerCareerButtonBehavior(CareerObject career) : CareerButtonBehaviorBase(career)
{
    private const string SlayerUnitId = "tor_dw_slayer";
    private const int ExchangeCostPerTier = 15;
    private const int GoldCostPerTier = 1000;
    
    public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner, bool shiftClick)
    {
        var slayerUnit = MBObjectManager.Instance.GetObject<CharacterObject>(SlayerUnitId);
        var tier = characterObject.Tier;
        
        if (shiftClick)
        {
            var buyableTroops = 5;
            
            for (int i = 0; i < buyableTroops; i++)
            {
               
                
                CustomResourceManager.AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(),- ExchangeCostPerTier * tier);
                PartyScreenHelper.GetActivePartyState().PartyScreenLogic.CurrentData.PartyGoldChangeAmount += tier * GoldCostPerTier;
                CareerButtonHelper.ExchangeUnitForNewUnit(characterObject, slayerUnit, true);
                
            }
        }
        else
        {
            CustomResourceManager.AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(),- ExchangeCostPerTier *tier);
            PartyScreenHelper.GetActivePartyState().PartyScreenLogic.CurrentData.PartyGoldChangeAmount += tier * GoldCostPerTier;
            CareerButtonHelper.ExchangeUnitForNewUnit(characterObject, slayerUnit, true);
        }
        
        PartyVMExtension.ViewModelInstance.RefreshValues();
    }

    public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner)
    {

        if (!Hero.MainHero.HasCareerChoice("ShameOfTheAncestorsPassive4"))
        {
            return false;
        }
        if (isPrisoner)
        {
            return false;
        }
        if(!characterObject.IsRegular)
            return false;

        if (characterObject.Culture.StringId != TORConstants.Cultures.DAWI)
        {
            return false;
        }
        
        return true;
    }

    public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner)
    {
        displayText = new TextObject("");
        
        if (characterObject.IsEliteTroop())
        {
            if (characterObject.Tier < 6)
            {
                displayText = new TextObject("Tier too low. Unit can't be ashamed");
                return false;
            }
        }
        else
        {
            if (characterObject.Tier < 4)
            {
                displayText = new TextObject("Tier too low. Unit can't be ashamed");
                return false;
            }
        }

        return true;
    }
}