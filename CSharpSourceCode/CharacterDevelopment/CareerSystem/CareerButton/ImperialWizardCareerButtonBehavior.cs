using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class ImperialWizardCareerButtonBehavior : CareerButtonBehaviorBase
    {
        
        private List<PowerStone> _availableStones;
        private Dictionary<MBGUID, PowerStone> _assignmentOfStones = new Dictionary<MBGUID, PowerStone>();

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
            list.Add(new InquiryElement("test", "stone 1 ", null));
            list.Add(new InquiryElement("test2", "stone 2 ", null));
            if (_assignmentOfStones.ContainsKey(characterObject.Id))
            {
                list.Add(new InquiryElement("remove", "Remove Stone", null));
            }
            
            
            var inquirydata = new MultiSelectionInquiryData("Choose Power stone", "empower .", list, true, 1, 1, "Confirm", "Cancel", OnSelectedOption, OnCancel);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }

        private void OnCancel(List<InquiryElement> obj)
        {
        }

        private void OnSelectedOption(List<InquiryElement> powerStone)
        {
            var stone = powerStone[0].Identifier as PowerStone;

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
            displayText= new TextObject("yes yes");
            return true;
        }
    }

    public class PowerStone
    {
        public PowerStone(TextObject text, string icon, PowerStoneEffect effect)
        {
            Text = text;
            Icon = icon;
            Effect = effect;
        }
        public TextObject Text;
        public string Icon;
        public PowerStoneEffect Effect= PowerStoneEffect.Test;
        public float Value;
    }

    public enum PowerStoneEffect
    {
        Test,
        Damage
        
    }
}