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
        
        private Dictionary<string, PowerStone> _availableStones = new Dictionary<string, PowerStone>(); 
        

        public Dictionary<string, PowerStone> AvailablePowerStones => _availableStones;
        private CharacterObject _setCharacter;
        
        public override string CareerButtonIcon
        {
            get
            {
                var stone = GetPowerstone(_setCharacter);
                if (stone!=null)
                {
                    return "winds_icon_45";
                }
           
                
                return  "CareerSystem\\grail";
            }
        }

        private PowerStone GetPowerstone(CharacterObject characterObject)
        {
            var partyExtendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
            
            if (partyExtendedInfo.TroopAttributes.TryGetValue(characterObject.StringId, out var attributes))
            {
                foreach (var attribute in attributes)
                {
                    if (_availableStones.TryGetValue(attribute, out var powerstone))
                    {
                        return powerstone;
                    }
                }
            }

            return null;
        }

        public ImperialMagisterCareerButtonBehavior(CareerObject career) : base(career)
        {
            MBTextManager.SetTextVariable("WINDS_ICON", CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText());

            var stones = CreateStoneList();
            
            
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

            var stone1 = new PowerStone("stone1", new TextObject("Lesser Fire ruby, +15% Fire damage"), "", "apply_flaming_sword_trait", 15,2 ,"LoreOfFire", PowerSize.Lesser);
            var stone2 = new PowerStone("stone2", new TextObject("Lesser Lumen Stone, +15% Magic damage"), "", "test", 10,5,"LoreOfLight", PowerSize.Lesser);
            
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

            var stones = _availableStones.Values.ToList();
            
            var troopCount= Hero.MainHero.PartyBelongedTo.Party.MemberRoster.GetElementNumber(characterObject);
            
            var availablePrestige = Hero.MainHero.GetCultureSpecificCustomResourceValue();

            var displayedStones = stones.Where(x => Hero.MainHero.HasKnownLore(x.LoreId) && x.Price<=availablePrestige).ToList();
            
            if(Hero.MainHero.HasCareerChoice("CollegeOrdersPassive4"))
            {
                foreach (var hero in Hero.MainHero.PartyBelongedTo.GetMemberHeroes())
                {
                    if(hero== Hero.MainHero) continue;
                    
                    if(hero.Culture.StringId !="empire") continue;

                    var lores = LoreObject.GetAll();

                    foreach (var lore in lores.Where(lore => hero.HasKnownLore(lore.ID)))
                    {
                        displayedStones.AddRange(stones.Where(x => x.LoreId == lore.ID &&  x.Price<=availablePrestige));
                    }
                }

                displayedStones= displayedStones.Distinct().ToList();
            }

            foreach (var stone in displayedStones)
            {
                var upkeep = stone.Upkeep;
                var price = stone.Price;
                
                var text = $"{stone.EffectText}{{NewLine}}Cost: {price}{{PRESTIGE_ICON}} Upkeep: {(troopCount * upkeep)}{{WINDS_ICON}}, {upkeep}{{WINDS_ICON}} per unit)";
                list.Add(new InquiryElement(stone, new TextObject(text).ToString(), null));
            }
            
            
            
            if (GetPowerstone(characterObject)!=null)
            {
                list.Add(new InquiryElement("remove", "Remove Stone", null));
            }
            
            var inquirydata = new MultiSelectionInquiryData("Choose Power stone", "Empower your troop with a permanent magical effect of a Power stone. The effect will reduce your total amount of Winds while the stone is active.", list, true, 1, 1, "Confirm", "Cancel", OnSelectedOption, OnCancel);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }
        

        

        private void OnCancel(List<InquiryElement> obj)
        {
        }

        private void OnSelectedOption(List<InquiryElement> powerStone)
        {
            var stone = powerStone[0].Identifier as PowerStone;

            if (stone == null) throw new Exception("error power stone was not found");

            if (powerStone[0].Identifier == "remove")
            {
                var currentStone = _availableStones[_setCharacter.StringId];
                Hero.MainHero.AddCustomResource("Prestige",(int)currentStone.Price*0.4f);
                
                var extendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.Party.Id);
                
                
                extendedInfo.RemoveTroopAttribute(_setCharacter, currentStone.Id );
            }
            else {
                
                if (!_availableStones.ContainsKey(_setCharacter.StringId))
                {
                    _availableStones.Add(_setCharacter.StringId,stone);
                    
                    var extendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.Party.Id);
                    extendedInfo.AddTroopAttribute(_setCharacter, stone.EffectId);
                }
                else
                {
                    _availableStones[_setCharacter.StringId] = stone;
                }
                
                Hero.MainHero.AddCustomResource("Prestige",-stone.Price);
                
            }

            
            
            SpecialbuttonEventManagerHandler.Instance.RefreshPartyVM();

        }
        
        
        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
        {
            var powerstones = AvailablePowerStones;
            
            
            var hasAlreadyPowerstone = powerstones.ContainsKey(characterObject.StringId);

            var lowestPrice = 0;
            if (powerstones.Any())
            {
                lowestPrice = powerstones.Min(x => x.Value.Price);
            }
            
            
            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < lowestPrice && !hasAlreadyPowerstone)
            {
                displayText= new TextObject("You do not have enough prestige to create power stones");
                return false;
            }
            
            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < lowestPrice && hasAlreadyPowerstone)
            {
                displayText= new TextObject("Remove Powerstone");
                return true;
            }

            if (hasAlreadyPowerstone)
            {
                var stone = AvailablePowerStones[characterObject.StringId];
                displayText= new TextObject(stone.EffectText.ToString());
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
        public PowerStone(string id, TextObject text, string icon, string effect, int price, int upkeep, string loreID, PowerSize stoneLevel)
        {
            EffectText = text;
            Id = id; 
            Icon = icon;
            EffectId = effect;
            _price = price;
            LoreId = loreID;
            StoneLevel = stoneLevel;
            _upkeep = upkeep;
        }

        public int Upkeep
        {
            get
            {
                float upkeep = _upkeep;
                if (Hero.MainHero.HasCareerChoice("ImperialEnchantmentPassive4"))
                {
                    upkeep -= upkeep * 0.25f;
                }
                    
                if (Hero.MainHero.HasCareerChoice("ArcaneKnowledgePassive1"))
                {
                    upkeep -= upkeep * 0.25f;
                }
                
                return (int) upkeep;
            }
            
        }

        public int Price
        {
            get
            {
                var value =(float) this._price;
                if (Hero.MainHero.HasCareerChoice("TeclisTeachingsPassive4"))
                {
                    value *= 1 - 0.35f;
                }

                return (int)value;
            }
        }

        public string Id;

        private readonly int _upkeep;
        private readonly int _price;
        public PowerSize StoneLevel;
        public TextObject EffectText;
        public string Icon;
        public string LoreId;
        public string EffectId;
    }

    public enum PowerSize
    {
        Lesser=0,
        Greater=1,
        Mighty=2,
    }
}