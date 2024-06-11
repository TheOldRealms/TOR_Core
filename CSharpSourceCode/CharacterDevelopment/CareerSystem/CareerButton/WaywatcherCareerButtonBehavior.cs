using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

public class WaywatcherCareerButtonBehavior : CareerButtonBehaviorBase
{
    private static CharacterObject _setCharacter;
    private string _shiftshiverShardsIcon = "CareerSystem\\azyr";
    private string _hagbaneTippsIcon = "CareerSystem\\ghur";
    private string _starfireShaftsIcon = "CareerSystem\\aqshy";

    private static ArrowType _shiftshiverShards;
    private static ArrowType _hagbaneTipps;
    private static ArrowType _starfireShafts;

    private static List<ArrowType> _allArrows = new();
    
    
    public override string CareerButtonIcon
    {
        get
        {
            var arrow = GetCurrentActiveArrowType(_setCharacter);
            if (arrow == null) return "winds_icon_45";
            
            return GetArrowIconAsText(arrow,true);

        }
    }
    
    public WaywatcherCareerButtonBehavior(CareerObject career) : base(career)
    {
        _shiftshiverShards = new ArrowType() { Id="shift",  Name = "Shiftshiver Shards", Description = "adds 25% Magical damage", Effect = "test", Symbol = _shiftshiverShardsIcon, };
        _hagbaneTipps = new ArrowType() {Id="hagbane", Name = "Hagbane Tipps", Description = "adds a 25% chance for 40% movement speed slowdown", Effect = "test", Symbol = _hagbaneTippsIcon, };
        _starfireShafts = new ArrowType() {Id="starfire", Name = "Starfire Shafts", Description = "Adds 25% armor penetration" ,Effect = "test", Symbol = _starfireShaftsIcon };
        _allArrows.Add(_shiftshiverShards);
        _allArrows.Add(_hagbaneTipps);
        _allArrows.Add(_starfireShafts);
        
        MBTextManager.SetTextVariable("SHIFTSHIVERSHARDS_ICON", string.Format("<img src=\"{0}\"/>",_shiftshiverShardsIcon));
        MBTextManager.SetTextVariable("HAGBANETIPPS_ICON", string.Format("<img src=\"{0}\"/>",_hagbaneTippsIcon));
        MBTextManager.SetTextVariable("STARFIRESHAFT_ICON", string.Format("<img src=\"{0}\"/>",_starfireShaftsIcon));
    }

    public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false)
    {
       _setCharacter = characterObject;
        var list = new List<InquiryElement>();
        
        for (int i = 0; i < _allArrows.Count; i++)
        {
            var arrow = _allArrows[i];
            var icon = GetArrowIconAsText(arrow);
            list.Add(new InquiryElement(arrow,new TextObject($"{{{icon}}} {arrow.Name}").ToString(),null,true,$"{arrow.Description}"));
            if (!Hero.MainHero.HasUnlockedCareerChoiceTier(i + 1))
                break;
        }
        
        var arrowType = GetCurrentActiveArrowType(_setCharacter);

        if (arrowType != null)
        {
            list.Add(new InquiryElement("remove", $"Remove {arrowType.Name}", null));
        }

        var inquirydata = new MultiSelectionInquiryData("Choose special arrows",
            "Empower your ranged Unit with a permanent magical effect.",
            list, true, 1, 1, "Accept", "Cancel", OnSelectedOption, OnCancel, "", false);
        MBInformationManager.ShowMultiSelectionInquiry(inquirydata);
    }

    private void OnCancel(List<InquiryElement> obj)
    {
        
    }



    private void OnSelectedOption(List<InquiryElement> elements)
    {
        var arrow = elements[0].Identifier as ArrowType;
    var partyExtendedInfo =
            ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
        var attributes = partyExtendedInfo.TroopAttributes.FirstOrDefault(x => x.Key == _setCharacter.StringId)
            .Value;


        var arrowType = GetCurrentActiveArrowType(_setCharacter);

        if (arrowType != null)
        {
            partyExtendedInfo.RemoveTroopAttribute(_setCharacter.StringId, arrowType.Effect);
        }


        if (elements[0].Identifier == "remove")
        {
        }
        else
        {

            partyExtendedInfo.AddTroopAttribute(_setCharacter, arrow.Effect);
        }


        ExtendedInfoManager.Instance.ValidatePartyInfos(MobileParty.MainParty);

        if (PartyVMExtension.ViewModelInstance != null)
        {
            PartyVMExtension.ViewModelInstance.RefreshValues();
        }
    }
    
    private string GetArrowIconAsText(ArrowType arrowType, bool asText = false)
    {
        if (asText)
        {
            return arrowType.Id switch
            {
                "shift" => _shiftshiverShardsIcon,
                "hagbane" => _hagbaneTippsIcon,
                "starfire" => _starfireShaftsIcon,
                _ => ""
            };
        }
        return arrowType.Id switch
        {
            "shift" => "SHIFTSHIVERSHARDS_ICON",
            "hagbane" => "HAGBANETIPPS_ICON",
            "starfire" => "STARFIRESHAFT_ICON",
            _ => ""
        };
    }

    private ArrowType GetCurrentActiveArrowType(CharacterObject setCharacter)
    {
        if (setCharacter == null) return null;
        var partyExtendedInfo =
            ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);

        if (partyExtendedInfo.TroopAttributes.TryGetValue(setCharacter.StringId, out var attributes))
        {
            if (attributes.Count > 0)
            {
                var arrow = attributes.Select(attribute => _allArrows.Find( x => x.Effect == attribute ))
                    .FirstOrDefault(powerstone => powerstone != null);

                return arrow;
            }
        }

        return null;
    }

    public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
    {
        if (characterObject.IsHero) return false;
        if (isPrisoner) return false;

        return characterObject.IsElf() && characterObject.IsRanged;
    }

    public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
    {
        displayText = new TextObject("select special Arrows this troop is shooting in combat)");
        return true;
    }

    public class ArrowType()
    {
        public string Id;
        public string Description;
        public string Name;
        public string Effect;
        public string Symbol;
    }
}