using System;
using System.Collections.Generic;
using System.Linq;
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
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class ImperialMagisterCareerButtonBehavior : CareerButtonBehaviorBase
    {
        
        private List<PowerStone> _availableStones;
        private Dictionary<string, string> _assignmentOfStones = new Dictionary<string, string>();  // character MBGUID and triggered Effect ID

        public int LesserStonePrice = 10;
        public int GreaterStonePrice = 20;

        public Dictionary<string, string> AssignmentOfStones => _assignmentOfStones;
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
        
        public ImperialMagisterCareerButtonBehavior(CareerObject career) : base(career)
        {
            MBTextManager.SetTextVariable("WINDS_ICON", CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText());

            _availableStones = CreateStoneList();

            var partyId = Hero.MainHero.PartyBelongedTo.Party.Id;

            var partyExtendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(partyId);
            
            if(partyExtendedInfo==null) return;

            var attributes = partyExtendedInfo.TroopAttributes;
        }

        private List<PowerStone> CreateStoneList()
        {
            var list = new List<PowerStone>();
            
            list.AddRange(GetLesserPowerstones());
            list.AddRange(GetGreaterPowerstones());
            list.AddRange(GetMightyPowerStones());

            return list;
        }
        
        

        private List<PowerStone> GetLesserPowerstones()
        {
            var list = new List<PowerStone>();

            var stone1 = new PowerStone(new TextObject("Lesser Fire ruby, +15% Fire damage"), "", "apply_flaming_sword_trait", 5,"LoreOfFire", PowerSize.Lesser);
            var stone2 = new PowerStone(new TextObject("Lesser Lumen Stone, +15% Magic damage"), "", "test", 5,"LoreOfLight", PowerSize.Lesser);
            
            list.Add(stone1);
            list.Add(stone2);
            return list;
        }
        
        private List<PowerStone> GetGreaterPowerstones()
        {
            return new List<PowerStone>();
        }

        private List<PowerStone> GetMightyPowerStones()
        {
            return new List<PowerStone>();
        }


        private bool CharacterHasStone()
        {
            return _setCharacter != null && _assignmentOfStones.ContainsKey(_setCharacter.StringId);
        }


        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
        {
            if (PartyScreenManager.Instance.CurrentMode != PartyScreenMode.Normal) return false;
            
            _setCharacter = characterObject;
            if (!characterObject.IsHero)
            {
                return Hero.MainHero.PartyBelongedTo.MemberRoster.Contains(characterObject);
            }
            
            return characterObject.HeroObject.PartyBelongedTo == PartyBase.MainParty.MobileParty;

            
        }
     

        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false)
        {
            PromptSelectPowerstone(characterObject);
        }

        private void PromptSelectPowerstone(CharacterObject characterObject)
        {
            _setCharacter = characterObject;
            var list = new List<InquiryElement>();


            var troopCount= Hero.MainHero.PartyBelongedTo.Party.MemberRoster.GetElementNumber(characterObject);
            
            var availablePrestige = Hero.MainHero.GetCultureSpecificCustomResourceValue();

            var stones = _availableStones.Where(x => x.Price < availablePrestige).ToList();

            var displayedStones = stones.Where(x => Hero.MainHero.HasKnownLore(x.LoreId)).ToList();
            
            if(Hero.MainHero.HasCareerChoice("CollegeOrdersPassive4"))
            {
                foreach (var hero in Hero.MainHero.PartyBelongedTo.GetMemberHeroes())
                {
                    if(hero== Hero.MainHero) continue;
                    
                    if(hero.Culture.StringId !="empire") continue;

                    var lores = LoreObject.GetAll();

                    foreach (var lore in lores.Where(lore => hero.HasKnownLore(lore.ID)))
                    {
                        displayedStones.AddRange(stones.Where(x => x.LoreId == lore.ID));
                    }
                }

                displayedStones.Distinct();
            }

            foreach (var stone in displayedStones)
            {
                var upkeep = 0;
                switch (stone.StoneLevel)
                {
                    case PowerSize.Lesser : upkeep = 1;
                        break;
                    case PowerSize.Greater: upkeep = 2;
                        break;
                    case PowerSize.Mighty: upkeep = 3;
                        break;
                }
                var text = $"{stone.EffectText}{{NewLine}}Cost: {stone.Price}{{PRESTIGE_ICON}} Upkeep: {troopCount*upkeep}{{WINDS_ICON}}, {upkeep}{{WINDS_ICON}} per unit)";
                list.Add(new InquiryElement(stone, new TextObject(text).ToString(), null));
            }
            
            
            
            if (_assignmentOfStones.ContainsKey(characterObject.StringId))
            {
                list.Add(new InquiryElement("remove", "Remove Stone", null));
            }
            
            var inquirydata = new MultiSelectionInquiryData("Choose Power stone", "Empower your troop with a permanent magical effect of a Power stone. The effect will reduce your total amount of Winds while the stone is active.", list, true, 1, 1, "Confirm", "Cancel", OnSelectedOption, OnCancel);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }


        private PowerStone getPowerstone(string effectID)
        {
            return _availableStones.FirstOrDefault(x => x.EffectID == effectID);
        }

        

        private void OnCancel(List<InquiryElement> obj)
        {
        }

        private void OnSelectedOption(List<InquiryElement> powerStone)
        {
            var stone = powerStone[0].Identifier as PowerStone;
            
            

            if (powerStone[0].Identifier == "remove")
            {
                var effectID = _assignmentOfStones[_setCharacter.StringId];
                var currentStone = getPowerstone(effectID);
                _assignmentOfStones.Remove(_setCharacter.StringId);
                Hero.MainHero.AddCustomResource("Prestige",(int)currentStone.Price*0.4f);
                
                var extendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.Party.Id);
                
                
                extendedInfo.RemoveTroopAttribute(_setCharacter, currentStone.EffectID);
            }
            else {
                
                if (!_assignmentOfStones.ContainsKey(_setCharacter.StringId))
                {
                    _assignmentOfStones.Add(_setCharacter.StringId,stone.EffectID);
                    
                    var extendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.Party.Id);
                    extendedInfo.AddTroopAttribute(_setCharacter, stone.EffectID);
                }
                else
                {
                    _assignmentOfStones[_setCharacter.StringId] = stone.EffectID;
                }
                
                Hero.MainHero.AddCustomResource("Prestige",-stone.Price);
                
            }

            
            
            SpecialbuttonEventManagerHandler.Instance.RefreshPartyVM();

        }
        
        
        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
        {
            
            
            
            var hasAlreadyPowerstone = AssignmentOfStones.ContainsKey(characterObject.StringId);
            
            
            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < LesserStonePrice && !hasAlreadyPowerstone)
            {
                displayText= new TextObject("You do not have enough prestige to create power stones");
                return false;
            }
            
            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < LesserStonePrice && hasAlreadyPowerstone)
            {
                displayText= new TextObject("Remove Powerstone");
                return true;
            }

            LesserStonePrice = 5;

            if (hasAlreadyPowerstone)
            {
                displayText= new TextObject("Create Powerstone empowering your troop or character");
            }
            else
            {
                displayText= new TextObject("Create Powerstone empowering your troop or character");
            }
            
            return true;
        }
    }

    public class PowerStone
    {
        public PowerStone(TextObject text, string icon, string effect, int price, string loreID, PowerSize stoneLevel)
        {
            EffectText = text;
            Icon = icon;
            EffectID = effect;
            Price = price;
            LoreId = loreID;
            StoneLevel = stoneLevel;
        }

        public int Price;
        public PowerSize StoneLevel;
        public TextObject EffectText;
        public string Icon;
        public string LoreId;
        public string EffectID;
        public float Value;
    }

    public enum PowerSize
    {
        Lesser=0,
        Greater=1,
        Mighty=2,
    }
}