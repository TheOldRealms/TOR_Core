using Ink.Parsed;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Extensions.UI;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

public class KnightOldWorldCareerButtonBehavior : CareerButtonBehaviorBase
{
    private const int MINIMUMLEVELFORSEAL = 5;

    private readonly string _secularSealIcon = "reiksguard_icon";
    private readonly string _sigmarSealIcon = "sigmar_comet_icon";
    private readonly string _taalSealIcon = "ghur";
    private readonly string _ulricSealIcon = "whitewolf_icon";
    private readonly string _shallyaSealIcon = "shallya_dove_icon";
    private readonly string _manaanSealIcon = "azyr";
    private readonly string _myrmidiaSealIcon = "blazingsun_icon";
    private CharacterObject _setCharacter;

    public KnightOldWorldCareerButtonBehavior(CareerObject career) : base(career)
    {
        MBTextManager.SetTextVariable("SECULAR_SEAL_ICON", string.Format("<img src=\"{0}\"/>", _secularSealIcon));
        MBTextManager.SetTextVariable("SIGMAR_SEAL_ICON", string.Format("<img src=\"{0}\"/>", _sigmarSealIcon));
        MBTextManager.SetTextVariable("TAAL_SEAL_ICON", string.Format("<img src=\"{0}\"/>", _taalSealIcon));
        MBTextManager.SetTextVariable("ULRIC_SEAL_ICON", string.Format("<img src=\"{0}\"/>", _ulricSealIcon));
        MBTextManager.SetTextVariable("SHALLYA_SEAL_ICON", string.Format("<img src=\"{0}\"/>", _shallyaSealIcon));
        MBTextManager.SetTextVariable("MANAAN_SEAL_ICON", string.Format("<img src=\"{0}\"/>", _manaanSealIcon));
        MBTextManager.SetTextVariable("MYRMIDIA_SEAL_ICON", string.Format("<img src=\"{0}\"/>", _myrmidiaSealIcon));
    }

    public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false, bool shiftClick = false)
    {
        _setCharacter = characterObject;

        var seals = new List<KnightPuritySeal>();

        var secular = false;

        var dominantReligion = Hero.MainHero.GetDominantReligion();
        if (dominantReligion == null || Hero.MainHero.GetDevotionLevelForReligion(dominantReligion) < DevotionLevel.Fanatic)
        {
            seals = GetSecularSeals();
            secular = true;
        }
        else
        {
            seals = GetTemplarPuritySeals();
        }

        var inquiryElements = new List<InquiryElement>();
        for (var i = 0; i < seals.Count; i++)
        {
            var seal = seals[i];
            var icon = GetSealIcon(seal.DeityCultId);

            var price = seal.Price;

            var displayName = seal.Name;
            var displayDescription = seal.Description;

            if (price > Hero.MainHero.GetCultureSpecificCustomResourceValue())
            {
                continue;
            }

            if (!secular)
            {
                if (Hero.MainHero.GetDominantReligion().StringId != seal.DeityCultId)
                {
                    continue;
                }
            }

            var text = $"{{{icon}}} {displayName}";
            var inquiryElement = new InquiryElement(seal, new TextObject(text).ToString(), null, true, displayDescription.ToString());

            inquiryElements.Add(inquiryElement);
        }

        var count = 1;

        if (Hero.MainHero.HasCareerChoice("PathOfGloryPassive4"))
        {
            count = 2;
        }

        var inquirydata = new MultiSelectionInquiryData(TORTextHelper.GetText("tor_purity_seal_choose_title_text", "Choose Purity Seal"), TORTextHelper.GetText("tor_purity_seal_choose_description_text", "Empower your knight units with powerful seals."), inquiryElements,
            true, 1, count, TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"), OnSelectedOption, OnCancel);
        MBInformationManager.ShowMultiSelectionInquiry(inquirydata);
    }

    private void OnCancel(List<InquiryElement> obj)
    {

    }

    private void OnSelectedOption(List<InquiryElement> elements)
    {
        var seals = new List<KnightPuritySeal>();
        foreach (var elem in elements)
        {
            var seal = elem.Identifier as KnightPuritySeal;
            seals.Add(seal);
        }

        var partyExtendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
        var currentSeals = GetCurrentActiveSeals(_setCharacter);
        if (currentSeals != null && !currentSeals.IsEmpty())
        {
            foreach (var elem in currentSeals)
            {
                partyExtendedInfo.RemoveTroopAttribute(_setCharacter.StringId, elem.SealId);
            }

        }

        foreach (var seal in seals)
        {
            partyExtendedInfo.AddTroopAttribute(_setCharacter, seal.SealId);
        }

        if (PartyVMExtension.ViewModelInstance != null) PartyVMExtension.ViewModelInstance.RefreshValues();

    }

    private List<KnightPuritySeal> GetCurrentActiveSeals(CharacterObject setCharacter)
    {
        if (setCharacter == null) return null;
        var partyExtendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);

        if (partyExtendedInfo.TroopAttributes.TryGetValue(setCharacter.StringId, out var attributes))
            if (attributes.Count > 0)
            {
                var availableSeals = GetSecularSeals();

                availableSeals.AddRange(GetTemplarPuritySeals());

                var seals = new List<KnightPuritySeal>();

                foreach (var seal in availableSeals)
                {
                    foreach (var attribute in attributes)
                    {
                        if (seal.SealId == attribute)
                        {
                            seals.Add(seal);
                        }
                    }
                }

                return seals;
            }

        return null;
    }

    public List<KnightPuritySeal> GetAllPuritySeals()
    {
        var list = new List<KnightPuritySeal>();
        list.AddRange(GetSecularSeals());
        list.AddRange(GetTemplarPuritySeals());
        return list;
    }

    private List<KnightPuritySeal> GetSecularSeals()
    {
        return new List<KnightPuritySeal>
        {
            new("SecularSeal1",  "apply_secular_seal_trait1", null, 10, _secularSealIcon),
            new("SecularSeal2" ,null, null, 10, _secularSealIcon),
            new("SecularSeal3", "apply_secular_seal_trait2", null, 10, _secularSealIcon),
        };
    }
    private string GetSealIcon(string cultId)
    {
        return cultId switch
        {
            "cult_of_sigmar" => "SIGMAR_SEAL_ICON",
            "cult_of_ulric" => "ULRIC_SEAL_ICON",
            "cult_of_taal" => "TAAL_SEAL_ICON",
            "cult_of_manaan" => "MANAAN_SEAL_ICON",
            "cult_of_shallya" => "SHALLYA_SEAL_ICON",
            "cult_of_myrmidia" => "MYRMIDIA_SEAL_ICON",
            _ => "SECULAR_SEAL_ICON"
        };
    }

    private List<KnightPuritySeal> GetTemplarPuritySeals()
    {
        return new List<KnightPuritySeal>
        {
            new("SigmarSeal1","apply_sigmar_seal_trait1","cult_of_sigmar",10, _sigmarSealIcon),
            new("SigmarSeal2",null,"cult_of_sigmar",10,_sigmarSealIcon),
            new("SigmarSeal3","apply_sigmar_seal_trait2","cult_of_sigmar",10,_sigmarSealIcon),

            new("UlricSeal1","apply_ulric_seal_trait1","cult_of_ulric",10, _ulricSealIcon),
            new("UlricSeal2","apply_ulric_seal_trait2","cult_of_ulric",10, _ulricSealIcon),
            new("UlricSeal3","apply_ulric_seal_trait3","cult_of_ulric",10, _ulricSealIcon),

            new("TaalSeal1","apply_taal_seal_trait1","cult_of_taal",10, _taalSealIcon),
            new("TaalSeal2",null,"cult_of_taal",10, _taalSealIcon),
            new("TaalSeal3","apply_taal_seal_trait2","cult_of_taal",10,_taalSealIcon),

            new("ManaanSeal1","apply_manaan_seal_trait1","cult_of_manaan",10, _manaanSealIcon),
            new("ManaanSeal2","apply_manaan_seal_trait2","cult_of_manaan",10, _manaanSealIcon),
            new("ManaanSeal3","apply_manaan_seal_trait3","cult_of_manaan",10, _manaanSealIcon),

            new("ShallyaSeal1",null,"cult_of_shallya",10, _shallyaSealIcon),
            new("ShallyaSeal2","apply_shallya_seal_trait1","cult_of_shallya",10, _shallyaSealIcon),
            new("ShallyaSeal3","apply_shallya_seal_trait2","cult_of_shallya",10,_shallyaSealIcon),

            new("MyrmidiaSeal1","apply_myrmidia_seal_trait1","cult_of_myrmidia",10, _myrmidiaSealIcon),
            new("MyrmidiaSeal2","apply_myrmidia_seal_trait2","cult_of_myrmidia",10, _myrmidiaSealIcon),
            new("MyrmidiaSeal3","apply_myrmidia_seal_trait3","cult_of_myrmidia",10, _myrmidiaSealIcon),
        };
    }

    public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
    {
        if (characterObject.Race != 0) return false;

        if (characterObject.IsKnightUnit() && characterObject.Culture.StringId != TORConstants.Cultures.BRETONNIA) return true;

        if (characterObject.HasAttribute("Knightly"))
        {
            return true;
        }

        return false;
    }

    public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
    {
        displayText = TORTextHelper.GetTextObject("tor_purity_seal_add_text", "Add a Purity Seal to this knight.");

        var currentSeals = GetCurrentActiveSeals(characterObject);

        if (currentSeals != null && !currentSeals.IsEmpty())
        {
            displayText = TextObject.GetEmpty();
            foreach (var seal in currentSeals)
            {
                var text = displayText.ToString();
                text += seal.Description;
                text += "\n";
                displayText = new TextObject(text);
            }

            return true;
        }

        if (characterObject.IsReligiousEliteUnit())
        {
            var troopReligion = characterObject.GetReligionForReligiousEliteUnit();
            var playerReligion = Hero.MainHero.GetDominantReligion();
            var isDevout = playerReligion != null && Hero.MainHero.GetDevotionLevelForReligion(playerReligion) >= DevotionLevel.Fanatic;

            if (isDevout)
            {
                // Player is religious enough for templar seals - must match troop's religion
                if (troopReligion != playerReligion)
                {
                    displayText = TORTextHelper.GetTextObject("tor_purity_seal_religion_mismatch_text", "Your religion does not match this unit's religion.");
                    return false;
                }
            }
            else
            {
                // Player is not devout enough - can only apply secular seals to templar knights with perk
                if (!Hero.MainHero.HasCareerChoice("SecularOrdersPassive4"))
                {
                    displayText = TORTextHelper.GetTextObject("tor_purity_seal_secular_on_religious_text", "You need the Secular Orders perk to apply seals to templar knights");
                    return false;
                }
            }
        }

        var devotion = Hero.MainHero.GetDominantReligion();

        if (devotion != null || Hero.MainHero.HasCareerChoice("SecularOrdersPassive3"))
        {
            if (characterObject.Tier < MINIMUMLEVELFORSEAL)
            {
                GameTexts.SetVariable("MINIMUMSEALLEVEL", MINIMUMLEVELFORSEAL);
                displayText = TORTextHelper.GetTextObject("tor_purity_seal_tier_too_low_text", "Unit tier is not high enough. Minimum tier is {MINIMUMSEALLEVEL}.");
                return false;
            }
            return true;
        }

        displayText = TORTextHelper.GetTextObject("tor_purity_seal_not_religious_enough_text", "You are not religious enough to provide a seal.");
        return false;
    }
}

public class KnightPuritySeal()
{
    public KnightPuritySeal(string sealId, string triggeredEffectIdId, string deityCultId, int price, string sealIcon) : this()
    {
        Name = GameTexts.TryGetText("TORKnightPuritySealName", out var nameText, sealId) ? nameText : new TextObject(sealId);
        Description = GameTexts.TryGetText("TORKnightPuritySealDescription", out var descriptionText, sealId) ? descriptionText : TORTextHelper.GetTextObject("tor_purity_seal_no_description_text", "No description found");
        triggeredEffectId = triggeredEffectIdId;
        Price = price;
        DeityCultId = deityCultId;
        SealId = sealId;
        SealIcon = sealIcon;
    }

    public TextObject Name = TextObject.GetEmpty();
    public TextObject Description = TextObject.GetEmpty();
    public string SealId;
    public string triggeredEffectId;
    public int Price;
    public string DeityCultId;
    public string SealIcon;
}