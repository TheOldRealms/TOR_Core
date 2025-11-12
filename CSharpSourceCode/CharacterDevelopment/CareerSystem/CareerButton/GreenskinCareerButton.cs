using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

public abstract class GreenskinCareerButton(CareerObject career) : CareerButtonBehaviorBase(career)
{
    public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner, bool shiftClick)
    {
        if (!isPrisoner || !IsEdibleCharacter(characterObject))
            return;

        var meat = CustomResourceManager.GetResourceObject("Meat");
        var amount = GetMeatAmountForCharacter(characterObject);

        if (shiftClick)
        {
            var troopCount = CareerButtonHelper.GetMaximumExchangeTroops(characterObject, isPrisoner, 5, 0, 0);
            for (int i = 0; i < troopCount; i++)
            {
                CustomResourceManager.AddResourceChanges(meat, -amount);
                ChopPrisoner(characterObject);
            }
        }
        else
        {
            // Chop single prisoner
            CustomResourceManager.AddResourceChanges(meat, -amount);
            ChopPrisoner(characterObject);
        }

        PartyVMExtension.ViewModelInstance.GetExtensionInstance().RefreshValues(); //Refresh to display correct resource exchange
    }

    private int GetMeatAmountForCharacter(CharacterObject characterObject)
    {
        var baseValue = characterObject.Level / 3 == 0 ? 1 : characterObject.Level / 3;

        if (characterObject.HasMount())
        {
            baseValue = (int)(baseValue * 1.5f);
        }

        if (characterObject.IsMinotaur())
        {
            baseValue = (int)(baseValue * 3);
        }

        return baseValue;
    }

    public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner)
    {
        // Only show chop button for prisoners
        if (!isPrisoner)
            return false;

        if (!IsEdibleCharacter(characterObject))
            return false;

        // Only show for Greenskin player characters
        if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.GREENSKIN)
            return false;

        return true;
    }

    public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner)
    {
        displayText = TextObject.GetEmpty();

        if (!isPrisoner)
        {
            displayText = new TextObject("Not a prisoner");
            return false;
        }

        if (!IsEdibleCharacter(characterObject))
        {
            displayText = new TextObject("This can't  be chopped");
            return false;
        }

        var prisonerCount = Hero.MainHero.PartyBelongedTo.PrisonRoster.GetElementNumber(characterObject);
        if (prisonerCount <= 0)
        {
            displayText = new TextObject("No prisoners of this type");
            return false;
        }

        displayText = new TextObject("Chop");
        return true;
    }

    public override string CareerButtonIcon => CustomResourceManager.GetResourceObject("Meat").LargeIconName;

    private void ChopPrisoner(CharacterObject prisoner)
    {
        CareerButtonHelper.RemoveUnit(prisoner, true, true);
    }

    private bool IsEdibleCharacter(CharacterObject character)
    {
        // Check if character is human based on culture
        return character.Race == FaceGen.GetRaceOrDefault("dwarf") || character.Race == FaceGen.GetRaceOrDefault("human") ||
               character.Race == FaceGen.GetRaceOrDefault("elf") || character.Race == FaceGen.GetRaceOrDefault("ungor") ||
               character.Race == FaceGen.GetRaceOrDefault("chaos_ud_cultist");
    }
}