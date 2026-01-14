using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Extensions.UI;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

public class OrcBossCareerButton(CareerObject career) : GreenskinCareerButton(career)
{
    private string _regularIcon = "teef_icon_100";
    private string _extorsionIcon = TORPaths.NormalizeAssetPath("CareerSystem\\teef_extorsion");

    private CharacterObject _setCharacter;
    private bool _setCharacterIsPrisoner;
    public override string CareerButtonIcon
    {
        get
        {
            if (_setCharacterIsPrisoner)
            {
                return base.CareerButtonIcon;
            }

            //TODO change the icon depending on if the Unit already is under extorsion or not.
            if (_setCharacter == null)
            {
                return null;
            }
            return SuffersFromExtorsion(_setCharacter) ? _extorsionIcon : _regularIcon;
        }
    }

    public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner)
    {
        _setCharacter = characterObject;
        _setCharacterIsPrisoner = isPrisoner;
        if (characterObject.Culture.StringId == TORConstants.Cultures.GREENSKIN)
        {
            return true;
        }
        return base.ShouldButtonBeVisible(characterObject, isPrisoner);
    }

    public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner)
    {
        if (isPrisoner)
        {
            return base.ShouldButtonBeActive(characterObject, out displayText, true);
        }
        displayText = new TextObject("");
        if (characterObject.Culture.StringId == TORConstants.Cultures.GREENSKIN)
        {
            displayText = TORTextHelper.GetTextObject("tor_orc_boss_extort_teef_text", "Extort Teef but damage party morale.");
            return true;
        }

        return false;
    }

    public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner, bool shiftClick)
    {
        if (isPrisoner)
        {
            base.ButtonClickedEvent(characterObject, isPrisoner, shiftClick);
            return;
        }
        //flags the unit to be under extorsion

        if (SuffersFromExtorsion(characterObject))
        {
            RemoveExtorsion(characterObject);
            return;
        }
        AddExtorsion(characterObject);
    }

    private void RemoveExtorsion(CharacterObject characterObject)
    {
        var partyExtendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
        partyExtendedInfo.RemoveTroopAttribute(characterObject.StringId, "Extorsion");

        ExtendedInfoManager.Instance.ValidatePartyInfos(MobileParty.MainParty);

        if (PartyVMExtension.ViewModelInstance != null)
        {
            PartyVMExtension.ViewModelInstance.RefreshValues();
        }
    }

    private void AddExtorsion(CharacterObject characterObject)
    {
        var partyExtendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
        partyExtendedInfo.AddTroopAttribute(characterObject, "Extorsion");

        ExtendedInfoManager.Instance.ValidatePartyInfos(MobileParty.MainParty);

        if (PartyVMExtension.ViewModelInstance != null)
        {
            PartyVMExtension.ViewModelInstance.RefreshValues();
        }
    }

    private bool SuffersFromExtorsion(CharacterObject characterObject)
    {
        var partyExtendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
        var attributes = partyExtendedInfo.TroopAttributes.FirstOrDefault(x => x.Key == characterObject.StringId).Value;
        return attributes?.Contains("Extorsion") ?? false;
    }

}