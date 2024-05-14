using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CampaignMechanics;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class ImperialWizardCareerButtonBehavior : CareerButtonBehaviorBase
    {
        
        private List<PowerStone> _availableStones;
        private Dictionary<MBGUID, string> _assignmentOfStones = new Dictionary<MBGUID, string>();  // character MBGUID and triggered Effect ID

        public int LesserStonePrice;
        public int GreaterStonePrice;



        public Dictionary<MBGUID, string> AssignmentOfStones => _assignmentOfStones;
        private CharacterObject _setCharacter;
        
        public override string CareerButtonIcon
        {
            get
            {
                if (!CharacterHasStone())
                {
                    return "winds_icon_45";
                }
           
                
                return  "CareerSystem\\grail";
            }
        }
        
        public ImperialWizardCareerButtonBehavior(CareerObject career) : base(career)
        {
            MBTextManager.SetTextVariable("WINDS_ICON", CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText());
        }

        private bool CharacterHasStone()
        {
            return _setCharacter != null && _assignmentOfStones.ContainsKey(_setCharacter.Id);
        }


        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
        {
            _setCharacter = characterObject;
            if (!characterObject.IsHero)
            {
                return Hero.MainHero.PartyBelongedTo.MemberRoster.Contains(characterObject);
            }
            
            return characterObject.HeroObject.PartyBelongedTo == PartyBase.MainParty.MobileParty;
            if (!characterObject.IsHero) return false;

            
        }
     

        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false)
        {
            PromptSelectPowerstone(characterObject);
        }

        private void PromptSelectPowerstone(CharacterObject characterObject)
        {
            _setCharacter = characterObject;
            var list = new List<InquiryElement>();

            var availablePowerStones = new List<PowerStone>(); 
            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() >= LesserStonePrice)
            {
                availablePowerStones.AddRange(GetLesserPowerstones());
            }
            
            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() >= GreaterStonePrice)
            {
                availablePowerStones.AddRange(GetGreaterPowerstones());
            }

            foreach (var stone in availablePowerStones)
            {
                var calculatedCost = 12;
                var upkeepsingle = stone.IsGreaterStone ? 3 : 1;
                var text = $"{stone.EffectText}{{NewLine}}Cost: {stone.Price}{{PRESTIGE_ICON}} Upkeep: {calculatedCost}{{WINDS_ICON}}, {upkeepsingle}{{WINDS_ICON}} per unit)";
                list.Add(new InquiryElement(stone.EffectID, new TextObject(text).ToString(), null));
            }
            
            //list.Add(new InquiryElement("ward_of_arrows", new TextObject("Lesser Fire ruby, +15% Fire damage  {NewLine}Cost: 5{PRESTIGE_ICON} Upkeep: 12{WINDS_ICON}, 2{WINDS_ICON} per unit) ").ToString(), null));
            //list.Add(new InquiryElement("meteoric_ironclad", new TextObject("Greater Fire ruby (+15% Fire damage) 10{PRESTIGE_ICON}").ToString(), null));
            if (_assignmentOfStones.ContainsKey(characterObject.Id))
            {
                list.Add(new InquiryElement("remove", "Remove Stone", null));
            }
            
            
            var inquirydata = new MultiSelectionInquiryData("Choose Power stone", "Empower your troop with a permanent magical effect of a Power stone. The effect will reduce your total amount of Winds while the stone is active.", list, true, 1, 1, "Confirm", "Cancel", OnSelectedOption, OnCancel);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }

        private List<PowerStone> GetGreaterPowerstones()
        {
            return new List<PowerStone>();
        }

        private List<PowerStone> GetLesserPowerstones()
        {
            var list = new List<PowerStone>();

            var stone1 = new PowerStone(new TextObject("Lesser Fire ruby, +15% Fire damage"), "", "apply_flaming_sword_trait", 5, true);
            
            
            list.Add(stone1);
            return list;
            return list;
        }

        private void OnCancel(List<InquiryElement> obj)
        {
        }

        private void OnSelectedOption(List<InquiryElement> powerStone)
        {
            var stone = powerStone[0].Identifier.ToString();

            if (powerStone[0].Identifier == "remove")
            {
                _assignmentOfStones.Remove(_setCharacter.Id);
            }
            else
            {
                if (!_assignmentOfStones.ContainsKey(_setCharacter.Id))
                {
                    _assignmentOfStones.Add(_setCharacter.Id,stone);
                }
                else
                {
                    _assignmentOfStones[_setCharacter.Id] = stone;
                }
            }

            
            
            SpecialbuttonEventManagerHandler.Instance.RefreshPartyVM();

        }
        
        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
        {
            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < LesserStonePrice && !AssignmentOfStones.ContainsKey(characterObject.Id))
            {
                displayText= new TextObject("You do not have enough prestige to create power stones");
                return false;
            }
            
            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < LesserStonePrice && AssignmentOfStones.ContainsKey(characterObject.Id))
            {
                displayText= new TextObject("Remove Powerstone");
                return true;
            }
     
            
            displayText= new TextObject("Create Powerstone empowering your troop or character");
            return true;
        }
    }

    public class PowerStone
    {
        public PowerStone(TextObject text, string icon, string effect, int price, bool greaterStone)
        {
            EffectText = text;
            Icon = icon;
            EffectID = effect;
            Price = price;
            IsGreaterStone = greaterStone;
        }

        public int Price;
        public bool IsGreaterStone;
        public TextObject EffectText;
        public string Icon;
        public string EffectID;
        public float Value;
    }

    public enum PowerStoneEffect
    {
        Test,
        Damage
        
    }
}