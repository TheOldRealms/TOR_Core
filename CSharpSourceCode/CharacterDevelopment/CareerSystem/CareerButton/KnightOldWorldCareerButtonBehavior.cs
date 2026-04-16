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
    private readonly string _taalSealIcon = "taal_icon";
    private readonly string _ulricSealIcon = "whitewolf_icon";
    private readonly string _shallyaSealIcon = "shallya_dove_icon";
    private readonly string _manaanSealIcon = "manann_icon";
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

            // Check career tier requirement
            if (!Hero.MainHero.HasUnlockedCareerChoiceTier(seal.Tier))
            {
                continue;
            }

            var text = $"{{{icon}}} {displayName}";
            var inquiryElement = new InquiryElement(seal, new TextObject(text).ToString(), null, true, displayDescription.ToString());

            inquiryElements.Add(inquiryElement);
        }

        // Add remove option if seals are already applied
        var currentSeals = GetCurrentActiveSeals(characterObject);
        if (currentSeals != null && currentSeals.Any())
        {
            var sealNames = string.Join(", ", currentSeals.Select(s => s.Name.ToString()));
            inquiryElements.Add(CareerButtonHelper.CreateRemoveOption(sealNames, "tor_purity_seal_remove_hint", "Remove all purity seals from this unit."));
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
        var currentSeals = GetCurrentActiveSeals(_setCharacter);

        CareerButtonHelper.ProcessSelection(
            _setCharacter,
            elements,
            currentSeals,
            seal => seal.SealId
        );
    }

    private List<KnightPuritySeal> GetCurrentActiveSeals(CharacterObject setCharacter)
    {
        return CareerButtonHelper.GetCurrentActiveItems(setCharacter, GetAllPuritySeals(), s => s.SealId);
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
            new("SecularSeal1", "apply_secular_seal_trait1", null, 10, _secularSealIcon, 1),
            new("SecularSeal2", null, null, 10, _secularSealIcon, 2),
            new("SecularSeal3", "apply_secular_seal_trait2", null, 10, _secularSealIcon, 3),
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
            new("SigmarSeal1", "apply_sigmar_seal_trait1", "cult_of_sigmar", 10, _sigmarSealIcon, 1),
            new("SigmarSeal2", "apply_sigmar_seal_trait2", "cult_of_sigmar", 10, _sigmarSealIcon, 2),
            new("SigmarSeal3", null, "cult_of_sigmar", 10, _sigmarSealIcon, 3),

            new("UlricSeal1", "apply_ulric_seal_trait1", "cult_of_ulric", 10, _ulricSealIcon, 1),
            new("UlricSeal2", "apply_ulric_seal_trait2", "cult_of_ulric", 10, _ulricSealIcon, 2),
            new("UlricSeal3", "apply_ulric_seal_trait3", "cult_of_ulric", 10, _ulricSealIcon, 3),

            new("TaalSeal1", "apply_taal_seal_trait1", "cult_of_taal", 10, _taalSealIcon, 1),
            new("TaalSeal2", "apply_taal_seal_trait2", "cult_of_taal", 10, _taalSealIcon, 2),
            new("TaalSeal3", null, "cult_of_taal", 10, _taalSealIcon, 3),

            new("ManaanSeal1", "apply_manaan_seal_trait1", "cult_of_manaan", 10, _manaanSealIcon, 1),
            new("ManaanSeal2", "apply_manaan_seal_trait2", "cult_of_manaan", 10, _manaanSealIcon, 2),
            new("ManaanSeal3", "apply_manaan_seal_trait3", "cult_of_manaan", 10, _manaanSealIcon, 3),

            new("ShallyaSeal1", null, "cult_of_shallya", 10, _shallyaSealIcon, 1),
            new("ShallyaSeal2", "apply_shallya_seal_trait1", "cult_of_shallya", 10, _shallyaSealIcon, 2),
            new("ShallyaSeal3", null, "cult_of_shallya", 10, _shallyaSealIcon, 3),

            new("MyrmidiaSeal1", null, "cult_of_myrmidia", 10, _myrmidiaSealIcon, 1),
            new("MyrmidiaSeal2", null, "cult_of_myrmidia", 10, _myrmidiaSealIcon, 2),
            new("MyrmidiaSeal3", "apply_myrmidia_seal_trait3", "cult_of_myrmidia", 10, _myrmidiaSealIcon, 3),
        };
    }

    public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
    {
        if (characterObject.Race != 0) return false;
        
        if (characterObject.IsRanged) return false;


        if (characterObject.IsKnightUnit() && characterObject.Culture.StringId != TORConstants.Cultures.BRETONNIA) return true;

        if (characterObject.HasAttribute("Knightly"))
        {
            return true;
        }

        return false;
    }

    public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
    {
        displayText = TORTextHelper.GetTextObject("tor_career_button_knightoldworld_seal_accept", "default", "Add a Purity Seal to this knight.");

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

        var playerReligion = Hero.MainHero.GetDominantReligion();
        var isFanatic = playerReligion != null && Hero.MainHero.GetDevotionLevelForReligion(playerReligion) >= DevotionLevel.Fanatic;

        if (characterObject.IsKnightUnit() && characterObject.Tier >= 5 && !characterObject.IsRanged)
        {
            if (characterObject.IsReligiousEliteUnit()) //templar knight case
            {
                var troopReligion = characterObject.GetReligionForReligiousEliteUnit();

                if (isFanatic)
                {
                    // Player is religious enough for templar seals - must match troop's religion
                    if (troopReligion.StringId != playerReligion.StringId)
                    {
                        displayText = TORTextHelper.GetTextObject("tor_career_button_knightoldworld_seal_decline", "templar_religion_no_match", "Your religion does not match this unit's religion.");
                        return false;
                    }
                    displayText = TORTextHelper.GetTextObject("tor_career_button_knightoldworld_seal_accept", "templar_seal", "Apply Templar Seal.");
                    return true;

                }

                // Player is not devout enough - can  apply secular seals to templar knights with perk
                if (!Hero.MainHero.HasCareerChoice("SecularOrdersPassive4"))
                {
                    displayText = TORTextHelper.GetTextObject("tor_career_button_knightoldworld_seal_decline", "secular_no_perk_for_templar", "You need the Secular Orders perk to apply seals to templar knights.");
                    return false;
                }
                displayText = TORTextHelper.GetTextObject("tor_career_button_knightoldworld_seal_accept", "secular_seal", "Apply Secular Seal.");
                return true;
            }

            //secular knight case
            if (isFanatic)                 // Unit is a secular knight - devout players need perk to apply templar seals
            {
                if (Hero.MainHero.HasCareerChoice("SecularOrdersPassive4"))
                {
                    displayText = TORTextHelper.GetTextObject("tor_career_button_knightoldworld_seal_accept", "templar_seal", "Apply Templar Seal.");
                    return true;
                }
                displayText = TORTextHelper.GetTextObject("tor_career_button_knightoldworld_seal_decline", "devout_no_perk_for_secular", "You need the Secular Orders perk to apply Templar seals to secular knights.");
                return false;
            }

            displayText = TORTextHelper.GetTextObject("tor_career_button_knightoldworld_seal_accept", "secular_seal", "Apply Secular Seal.");
            return true;
        }

        displayText = TORTextHelper.GetTextObject("tor_career_button_knightoldworld_seal_decline", "not_eligible", "Not eligible for seal.");
        return false;
    }
}

public class KnightPuritySeal()
{
    public KnightPuritySeal(string sealId, string triggeredEffectIdId, string deityCultId, int price, string sealIcon, int tier) : this()
    {
        Name = GameTexts.TryGetText("TORKnightPuritySealName", out var nameText, sealId) ? nameText : new TextObject(sealId);
        Description = GameTexts.TryGetText("TORKnightPuritySealDescription", out var descriptionText, sealId) ? descriptionText : TORTextHelper.GetTextObject("tor_purity_seal_no_description_text", "No description found");
        triggeredEffectId = triggeredEffectIdId;
        Price = price;
        DeityCultId = deityCultId;
        SealId = sealId;
        SealIcon = sealIcon;
        Tier = tier;
    }

    public TextObject Name = TextObject.GetEmpty();
    public TextObject Description = TextObject.GetEmpty();
    public string SealId;
    public string triggeredEffectId;
    public int Price;
    public string DeityCultId;
    public string SealIcon;
    public int Tier;
}