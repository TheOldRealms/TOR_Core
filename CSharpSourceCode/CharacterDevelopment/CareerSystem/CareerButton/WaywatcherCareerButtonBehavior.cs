using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Extensions.UI;
using TOR_Core.Utilities;
using static Helpers.PartyScreenHelper;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

public class WaywatcherCareerButtonBehavior : CareerButtonBehaviorBase
{
    private static CharacterObject _setCharacter;
    private string _swiftshiverShardsIcon = "shiftshiver_shards";
    private string _hagbaneTippsIcon = "hagbane_tips";
    private string _starfireShaftsIcon = "starfire_shafts";

    private static List<ArrowType> _allArrows;


    public override string CareerButtonIcon
    {
        get
        {
            var arrow = GetCurrentActiveArrowType(_setCharacter);
            if (arrow == null) return "winds_icon_45";

            return GetArrowIconAsText(arrow, true);
        }
    }

    public WaywatcherCareerButtonBehavior(CareerObject career) : base(career)
    {
        _allArrows = new List<ArrowType>()
        {
            new()
            {
                Id = "shift",
                Name = TORTextHelper.GetTextObject("tor_waywatcher_arrow_name","Swiftshiver", "Swiftshiver Shards").ToString(),
                Description = TORTextHelper.GetTextObject("tor_waywatcher_arrow_desc","Swiftshiver", "adds 15% Magical damage").ToString(),
                Effect = "apply_swift_shiver_trait",
                Price = 20,
                Symbol = _swiftshiverShardsIcon
            },
            new()
            {
                Id = "hagbane",
                Name = TORTextHelper.GetTextObject("tor_waywatcher_arrow_name","Hagbane", "Hagbane Tipps").ToString(),
                Description = TORTextHelper.GetTextObject("tor_waywatcher_arrow_desc","Hagbane", "adds a 25% chance for 40% movement speed slowdown").ToString(),
                Effect = "apply_hagbane_trait",
                Price = 30,
                Symbol = _hagbaneTippsIcon
            },
            new()
            {
                Id = "starfire",
                Name = TORTextHelper.GetTextObject("tor_waywatcher_arrow_name","Starfire", "Starfire Shafts").ToString(),
                Description = TORTextHelper.GetTextObject("tor_waywatcher_arrow_desc","Starfire", "Adds a 25% chance for fire damage over time and fire vulnerability").ToString(),
                Effect = "apply_starfire_trait",
                Price = 50,
                Symbol = _starfireShaftsIcon
            }
        };

        MBTextManager.SetTextVariable("SWIFTSHIVERSHARDS_ICON", string.Format("<img src=\"{0}\"/>", _swiftshiverShardsIcon));
        MBTextManager.SetTextVariable("HAGBANETIPPS_ICON", string.Format("<img src=\"{0}\"/>", _hagbaneTippsIcon));
        MBTextManager.SetTextVariable("STARFIRESHAFT_ICON", string.Format("<img src=\"{0}\"/>", _starfireShaftsIcon));
    }

    public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false, bool shiftClicked = false)
    {
        _setCharacter = characterObject;
        var list = new List<InquiryElement>();

        for (var i = 0; i < _allArrows.Count; i++)
        {
            if (!Hero.MainHero.HasUnlockedCareerChoiceTier(i + 1))
                break;
            var arrow = _allArrows[i];
            var icon = GetArrowIconAsText(arrow);
            var price = Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI ? arrow.Price * 3 : arrow.Price;

            if (price > Hero.MainHero.GetCultureSpecificCustomResourceValue())
            {
                continue;
            }
            list.Add(new InquiryElement(arrow, new TextObject($"{{{icon}}} {arrow.Name}").ToString(), null, true, $"{arrow.Description}, {price}{Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText()}"));

        }

        var arrowType = GetCurrentActiveArrowType(_setCharacter);

        if (arrowType != null) list.Add(new InquiryElement("remove", $"Remove {arrowType.Name}", null));

        var inquirydata = new MultiSelectionInquiryData(TORTextHelper.GetText("tor_waywatcher_arrow_label", "Choose special arrows."),
            TORTextHelper.GetText("tor_waywatcher_arrow_descr", "Empower your ranged unit with a permanent magical effect."), list,
            true, 1, 1, "Accept", "Cancel", OnSelectedOption, OnCancel, "", false);
        MBInformationManager.ShowMultiSelectionInquiry(inquirydata);
    }

    private void OnCancel(List<InquiryElement> obj)
    {
    }


    private void OnSelectedOption(List<InquiryElement> elements)
    {
        var arrow = elements[0].Identifier as ArrowType;
        if (arrow == null)
        {
            if ((string)elements[0].Identifier != "remove")
            {
                return;
            }
        }

        var partyExtendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);

        var arrowType = GetCurrentActiveArrowType(_setCharacter);

        if (arrowType != null) partyExtendedInfo.RemoveTroopAttribute(_setCharacter.StringId, arrowType.Effect);

        if (arrow != null)
        {
            partyExtendedInfo.AddTroopAttribute(_setCharacter, arrow.Effect);
            var price = Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI ? arrow.Price * 3 : arrow.Price;
            Hero.MainHero.AddCultureSpecificCustomResource(-price);
        }


        ExtendedInfoManager.Instance.ValidatePartyInfos(MobileParty.MainParty);
        if (PartyVMExtension.ViewModelInstance != null) PartyVMExtension.ViewModelInstance.RefreshValues();
    }

    private string GetArrowIconAsText(ArrowType arrowType, bool asText = false)
    {
        if (asText)
            return arrowType.Id switch
            {
                "shift" => _swiftshiverShardsIcon,
                "hagbane" => _hagbaneTippsIcon,
                "starfire" => _starfireShaftsIcon,
                _ => ""
            };
        return arrowType.Id switch
        {
            "shift" => "SWIFTSHIVERSHARDS_ICON",
            "hagbane" => "HAGBANETIPPS_ICON",
            "starfire" => "STARFIRESHAFT_ICON",
            _ => ""
        };
    }

    private ArrowType GetCurrentActiveArrowType(CharacterObject setCharacter)
    {
        if (setCharacter == null) return null;
        var partyExtendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);

        if (partyExtendedInfo.TroopAttributes.TryGetValue(setCharacter.StringId, out var attributes))
            if (attributes.Count > 0)
            {
                var arrow = attributes.Select(attribute => _allArrows.Find(x => x.Effect == attribute))
                    .FirstOrDefault(powerstone => powerstone != null);

                return arrow;
            }

        return null;
    }

    public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
    {
        if (GetActivePartyState().PartyScreenMode != PartyScreenMode.Normal) return false;
        if (characterObject.IsHero) return false;
        if (isPrisoner) return false;

        return characterObject.IsElf() && characterObject.IsRanged;
    }

    public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
    {
        displayText = TextObject.GetEmpty();
        if (GetActivePartyState().PartyScreenMode != PartyScreenMode.Normal) return false;
        if (characterObject.IsHero) return false;
        if (isPrisoner) return false;

        if (characterObject.IsElf() && characterObject.IsRanged)
        {
            _setCharacter = characterObject;

            var type = GetCurrentActiveArrowType(_setCharacter);
            if (type != null)
                displayText = new TextObject(type.Description);
            else
                displayText = TORTextHelper.GetTextObject("tor_waywatcher_select_arrows_text", "select magical arrows for this unit");
        }

        return true;
    }

    public class ArrowType()
    {
        public string Id;
        public string Description;
        public string Name;
        public string Effect;
        public int Price;
        public string Symbol;
    }
}