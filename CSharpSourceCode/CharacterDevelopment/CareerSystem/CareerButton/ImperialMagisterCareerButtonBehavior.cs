using System;
using System.Collections.Generic;
using System.Linq;
using Ink.Parsed;
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
        
        private List<PowerStone> _availableStones = new List<PowerStone>(); 
        

        public List<PowerStone> AvailablePowerStones => _availableStones;
        private CharacterObject _setCharacter;
        
        public override string CareerButtonIcon
        {
            get
            {
                var stone = GetPowerstone(_setCharacter);
                if (stone==null)
                {
                    return "winds_icon_45";
                }
           
                
                return  "CareerSystem\\grail";
            }
        }

        public List<PowerStone> GetAllPowerstones()
        {
            var list = new List<PowerStone>();
            var partyExtendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);

            var characterToAttributes = partyExtendedInfo.TroopAttributes;

            foreach (var attributes in characterToAttributes.Values)
            {
                foreach (var attribute in attributes)
                {
                   var stone = _availableStones.FirstOrDefault(x=> x.Id == attribute);
                   if (stone != null)
                   {
                       list.Add(stone);
                   }
                   
                }
            }
            
            return list;
        }
        
        public PowerStone GetPowerstone(CharacterObject characterObject)
        {
            if (characterObject == null) return null;
            var partyExtendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
            
            if (partyExtendedInfo.TroopAttributes.TryGetValue(characterObject.StringId, out var attributes))
            {
                return attributes.Select(attribute => _availableStones.Find(x => x.Id == attribute)).FirstOrDefault(powerstone => powerstone != null);
            }

            return null;
        }

        public ImperialMagisterCareerButtonBehavior(CareerObject career) : base(career)
        {
            MBTextManager.SetTextVariable("WINDS_ICON", CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText());
            
            _availableStones= CreateStoneList();
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
            var list = new List<PowerStone>()
            {
                new PowerStone("fire_dmg_10", new TextObject("Lesser Fire ruby, +15% Fire damage"), "",
                    "apply_flaming_sword_trait", 15, 2, "LoreOfFire", PowerSize.Lesser),
                new PowerStone("fire_amp_50", new TextObject("Fire damage is amplified by 50%"), "", "PLACEHOLDER", 15,
                    2, "LoreOfFire", PowerSize.Lesser),
                new PowerStone("fire_res_20", new TextObject("increase Fire and Frost resistance by 20%"), "",
                    "PLACEHOLDER", 15, 2, "LoreOfFire", PowerSize.Lesser),

                new PowerStone("light_res_25", new TextObject("Add 20% physical resistance"), "", "test", 15, 3,
                    "LoreOfLight", PowerSize.Lesser),
                new PowerStone("light_mov_25", new TextObject("Add 25% movementSpeed"), "", "test", 15, 2,
                    "LoreOfLight", PowerSize.Lesser),
                new PowerStone("light_dmg_20", new TextObject("Add 20% movementSpeed"), "", "test", 20, 3,
                    "LoreOfLight", PowerSize.Lesser),

                new PowerStone("beast_res_25", new TextObject("Add 25% ranged resistance"), "", "test", 15, 1,
                    "LoreOfBeasts", PowerSize.Lesser),
                new PowerStone("beast_phys_15", new TextObject("Add 15% physical resistance"), "", "test", 15, 2,
                    "LoreOfBeasts", PowerSize.Lesser),
                new PowerStone("beast_phys_20_ranged", new TextObject("Add 20% ranged damage"), "", "test", 15, 4,
                    "LoreOfBeasts", PowerSize.Lesser),
                
                new PowerStone("life_magical_fire_res_25", new TextObject("Add 25% magical and fire resistance"), "", "test", 15, 1,
                    "LoreOfLife", PowerSize.Lesser),
                new PowerStone("life_res_20", new TextObject("Add 20% physical resistance"), "", "test", 15, 2,
                    "LoreOfLife", PowerSize.Lesser),
                new PowerStone("life_res_20", new TextObject("Add 20% physical resistance"), "", "test", 15, 2,
                    "LoreOfLife", PowerSize.Lesser),
                
                new PowerStone("heavens_dmg_raged_20", new TextObject("Add 20% electric ranged damage"), "", "test", 20, 3,
                    "LoreOfHeavens", PowerSize.Lesser),
                new PowerStone("heavens_dmg_melee_20", new TextObject("Add 20% electric melee damage"), "", "test", 20, 3,
                    "LoreOfHeavens", PowerSize.Lesser),
                new PowerStone("heavens_res_20", new TextObject("Add 30% electrical resistance"), "", "test", 10, 2,
                    "LoreOfHeavens", PowerSize.Lesser),
                
                new PowerStone("metal_dmg_15", new TextObject("Add 15% physical damge"), "", "test", 15, 2,
                    "LoreOfLife", PowerSize.Lesser),
                new PowerStone("metal_magic_dmg_20", new TextObject("Add 20% magical ranged damage"), "", "test", 15, 2,
                    "LoreOfLife", PowerSize.Lesser),
                new PowerStone("heavens_dmg_raged_20", new TextObject("Add 20% electric ranged damage"), "", "test", 15, 2,
                    "LoreOfLife", PowerSize.Lesser),
                
            };
            
            return list;
        }
        
        private List<PowerStone> GetGreaterPowerstones()
        {
                      var list = new List<PowerStone>()
            {
                new PowerStone("fire_dmg_10", new TextObject("Lesser Fire ruby, +15% Fire damage"), "",
                    "apply_flaming_sword_trait", 15, 2, "LoreOfFire", PowerSize.Greater),
                new PowerStone("fire_amp_50", new TextObject("Fire damage is amplified by 50%"), "", "PLACEHOLDER", 15,
                    2, "LoreOfFire", PowerSize.Greater),

                new PowerStone("light_res_phys_magic_25", new TextObject("Add 25% physical and 25% magical  resistance"), "", "test", 15, 3,
                    "LoreOfLight", PowerSize.Greater),
                new PowerStone("light_mov_dmg_25", new TextObject("Add 25% movement Speed and 25% magical melee damage"), "", "test", 15, 2,
                    "LoreOfLight", PowerSize.Greater),

                new PowerStone("beast_res_25", new TextObject("Add 25% ranged resistance and add 25% movement speed"), "", "test", 15, 1,
                    "LoreOfBeasts", PowerSize.Greater),
                new PowerStone("beast_res_range_25", new TextObject("Add 25% ranged resistance and 25% ranged damage"), "", "test", 15, 2,
                    "LoreOfBeasts", PowerSize.Greater),
                
                new PowerStone("life_physical_50", new TextObject("Add 50% physical resistance"), "", "test", 15, 1,
                    "LoreOfLife", PowerSize.Greater),
                new PowerStone("life_res_all_35", new TextObject("Add 35% all non physical resistance"), "", "test", 15, 2,
                    "LoreOfLife", PowerSize.Greater),
                
                new PowerStone("heavens_amp_100_15_dmg", new TextObject("Add 100% electric amplyfication, 15% electric damage"), "", "test", 20, 3,
                    "LoreOfHeavens", PowerSize.Greater),
                new PowerStone("heavens_res_40", new TextObject("Adds 40% electric, 40% frost resistance. Adds 20% physical resistance"), "", "test", 20, 3,
                    "LoreOfHeavens", PowerSize.Greater),
                
                new PowerStone("metal_dmg_15", new TextObject("Add 20% physical damage, 20% fire damage"), "", "test", 15, 2,
                    "LoreOfLife", PowerSize.Greater),
                new PowerStone("metal_magic_dmg_20", new TextObject("Adds 25% Armor penetration"), "", "test", 15, 2,
                    "LoreOfLife", PowerSize.Greater),
                
            };
            
            return list;
        }

        private List<PowerStone> GetMightyPowerStones()
        {
                                 var list = new List<PowerStone>()
            {
                new PowerStone("fire_amp_150", new TextObject("Fire damage is amplified by 150%, 15% Fire damage"), "", "PLACEHOLDER", 15,
                    2, "LoreOfFire", PowerSize.Mighty),
                
                new PowerStone("light_mov_dmg_25", new TextObject("40% magical melee damage, slows enemies on hit"), "", "test", 15, 2,
                    "LoreOfLight", PowerSize.Mighty),

                new PowerStone("beast_res_25", new TextObject("Add 50% movement speed and  and add 50% physical resistance"), "", "test", 15, 1,
                    "LoreOfBeasts", PowerSize.Mighty),
                
                new PowerStone("life_physical_50", new TextObject("Regenerate 1 Health point every second"), "", "test", 15, 1,
                    "LoreOfLife", PowerSize.Mighty),
                
                new PowerStone("heavens_res_40", new TextObject("Adds 20% electric, 20% frost damage, 20% slowdown"), "", "test", 20, 3,
                    "LoreOfHeavens", PowerSize.Mighty),
                
                new PowerStone("metal_magic_dmg_20", new TextObject("Add 20% magical ranged damage"), "", "test", 15, 2,
                    "LoreOfLife", PowerSize.Mighty),
                
            };
            
            return list;
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

            var stones = _availableStones.ToList();
            
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
            
            
            if (Hero.MainHero.HasUnlockedCareerChoiceTier(1))
            {
                 displayedStones.Select(x => x.StoneLevel == PowerSize.Lesser).ToList();
            }
            else if (Hero.MainHero.HasUnlockedCareerChoiceTier(2))
            {
                displayedStones.Select(x => x.StoneLevel == PowerSize.Lesser && x.StoneLevel == PowerSize.Greater);
            }
            

            

            foreach (var stone in displayedStones)
            {
                var upkeep = stone.Upkeep;
                var price = stone.Price;
                
                var text = $"{stone.EffectText}{{NewLine}}{price}{{PRESTIGE_ICON}} Upkeep: {(troopCount * upkeep)}{{WINDS_ICON}}({upkeep}{{WINDS_ICON}}/Unit)";
                list.Add(new InquiryElement(stone, new TextObject(text).ToString(), new ImageIdentifier(ImageIdentifierType.Item)));
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
            
            var partyExtendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
            var attributes = partyExtendedInfo.TroopAttributes.FirstOrDefault(x => x.Key == _setCharacter.StringId).Value;



            PowerStone currentStone = GetPowerstone(_setCharacter);
            
            

            if (powerStone[0].Identifier == "remove")
            {
                
                if (currentStone != null)
                {
                    Hero.MainHero.AddCustomResource("Prestige",currentStone.Price*0.4f);
                    partyExtendedInfo.RemoveTroopAttribute(_setCharacter.StringId, currentStone.Id );
                }
                
            }
            else {
                
                if ( currentStone!=null && !attributes.Contains(currentStone.Id))
                {
                    partyExtendedInfo.RemoveTroopAttribute(_setCharacter.StringId, currentStone.Id);
                }
                
                partyExtendedInfo.AddTroopAttribute(_setCharacter, stone.Id);
                Hero.MainHero.AddCustomResource("Prestige",-stone.Price);
                
            }

            
            ExtendedInfoManager.Instance.ValidatePartyInfos(partyExtendedInfo,MobileParty.MainParty);
            SpecialbuttonEventManagerHandler.Instance.RefreshPartyVM();

        }
        
        
        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false)
        {
            var powerstones = AvailablePowerStones;

            var powerstone = GetPowerstone(characterObject);

            var lowestPrice = 0;
            if (powerstones.Any())
            {
                lowestPrice = powerstones.Min(x => x.Price);
            }
            
            
            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < lowestPrice && powerstone==null )
            {
                displayText= new TextObject("You do not have enough prestige to create power stones");
                return false;
            }
            
            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < lowestPrice && powerstone != null)
            {
                displayText= new TextObject("Remove Powerstone");
                return true;
            }

            if (powerstone != null)
            {
                displayText= new TextObject(powerstone.EffectText.ToString());
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