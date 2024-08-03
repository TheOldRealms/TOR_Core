using System;
using System.Collections.Generic;
using System.Linq;
using Ink.Parsed;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CampaignMechanics;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Extensions.UI;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class ImperialMagisterCareerButtonBehavior : CareerButtonBehaviorBase
    {
        private string _fireIcon = "CareerSystem\\aqshy";
        private string _lightIcon = "CareerSystem\\hysh";
        private string _heavensIcon = "CareerSystem\\azyr";
        private string _lifeIcon = "CareerSystem\\ghyran";
        private string _beastIcon = "CareerSystem\\ghur";
        private string _metalIcon = "CareerSystem\\chamon";
        
        
        public List<PowerStone> AvailablePowerStones { get; } = new List<PowerStone>();

        private CharacterObject _setCharacter;

        public override string CareerButtonIcon
        {
            get
            {
                var stone = GetPowerstone(_setCharacter);
                if (stone == null) return "winds_icon_45";
                else
                {
                    return GetStoneIcon(stone.LoreId, false);
                }

            }
        }

        public List<PowerStone> GetAllPowerstones()
        {
            var list = new List<PowerStone>();
            if(Hero.MainHero.PartyBelongedTo==null) return new List<PowerStone>();
            var partyExtendedInfo =
                ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);

            var characterToAttributes = partyExtendedInfo.TroopAttributes;

            foreach (var characterAttributeContainer in characterToAttributes)
            {
                foreach (var attribute in characterAttributeContainer.Value)
                {
                    var stone = AvailablePowerStones.FirstOrDefault(x => x.Id == attribute);
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
            var partyExtendedInfo =
                ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);

            if (partyExtendedInfo.TroopAttributes.TryGetValue(characterObject.StringId, out var attributes))
            {
                if (attributes.Count > 0)
                {
                    var stones = attributes.Select(attribute => AvailablePowerStones.Find( x => x.Id == attribute ))
                        .Where(powerstone => powerstone != null).ToList();
                    
                    var first = stones[0];
                    
                    return first;
                }
            }
            
            return null;
        }

        public ImperialMagisterCareerButtonBehavior(CareerObject career) : base(career)
        {
            MBTextManager.SetTextVariable("WINDS_ICON",
            CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText());
            
            MBTextManager.SetTextVariable("FIRE_ICON", string.Format("<img src=\"{0}\"/>",_fireIcon));
            MBTextManager.SetTextVariable("HEAVENS_ICON", string.Format("<img src=\"{0}\"/>",_heavensIcon));
            MBTextManager.SetTextVariable("LIGHT_ICON", string.Format("<img src=\"{0}\"/>",_lightIcon));
            MBTextManager.SetTextVariable("LIFE_ICON", string.Format("<img src=\"{0}\"/>",_lifeIcon));
            MBTextManager.SetTextVariable("BEAST_ICON", string.Format("<img src=\"{0}\"/>",_beastIcon));
            MBTextManager.SetTextVariable("METAL_ICON", string.Format("<img src=\"{0}\"/>",_metalIcon));
            AvailablePowerStones = CreateStoneList();
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
                new("fire_dmg_10", new TextObject("Lesser Sparkling Fire Ruby"),new TextObject("+15% Fire damage"),
                    "powerstone_fire_trait", 15, 10, "LoreOfFire", PowerSize.Lesser),
                new PowerStone("fire_amp_50", new TextObject("Lesser Nourishing Fire Ruby"), new TextObject("+50% Fire amplification "), "powerstone_fire_amp", 15,
                    10, "LoreOfFire", PowerSize.Lesser),
                new PowerStone("fire_res_20", new TextObject("Lesser Heating Fire Ruby"), new TextObject("+20% Frost and Fire resistance"),
                    "powerstone_fire_res", 15, 10, "LoreOfFire", PowerSize.Lesser),

                new PowerStone("light_res_20", new TextObject("Lesser Protecting Lumen Stone"), new TextObject("+20% physical resistance"),"powerstone_light_res", 15, 10,
                    "LoreOfLight", PowerSize.Lesser),
                new PowerStone("light_mov_25", new TextObject("Lesser Timewarp Lumen Stone"), new TextObject("+25% movementSpeed"),"powerstone_light_mov", 15, 10,
                    "LoreOfLight", PowerSize.Lesser),
                new PowerStone("light_dmg_15",new TextObject("Lesser Gleaming Lumen Stone"), new TextObject("+15% physical damage amplification"),"powerstone_light_dmg", 15, 10,
                    "LoreOfLight", PowerSize.Lesser),

                new PowerStone("beast_range_res_25", new TextObject("Lesser Obfuscating Ghost Amber"),new TextObject("+25% ranged resistance"), "powerstone_beast_res_range", 15, 10,
                    "LoreOfBeasts", PowerSize.Lesser),
                new PowerStone("beast_phys_15", new TextObject("Lesser Protecting Ghost Amber"),new TextObject("+15% physical resistance"), "powerstone_beast_res", 15, 10,
                    "LoreOfBeasts", PowerSize.Lesser),
                new PowerStone("beast_phys_20_ranged", new TextObject("Lesser Seeking Ghost Amber"),new TextObject("Add 20% ranged damage"), "powerstone_beast_dmg_range", 15, 10,
                    "LoreOfBeasts", PowerSize.Lesser),

                new PowerStone("life_magical_fire_res_25", new TextObject("Lesser Dampening Vitaellum"),new TextObject("Add 25% magical and fire resistance"),
                    "powerstone_life_res_mag", 15, 10,
                    "LoreOfLife", PowerSize.Lesser),
                new PowerStone("life_res_20", new TextObject("Lesser Protecting Vitaellum"), new TextObject("Add 20% physical resistance"),"powerstone_life_res_phys", 15, 10,
                    "LoreOfLife", PowerSize.Lesser),
                new PowerStone("life_res_40_debuff",new TextObject("Lesser Heavy Vitaellum"), new TextObject("Add 35% physical resistance -35% reduced speed"), "powerstone_life_res_debuff", 15, 10,
                    "LoreOfLife", PowerSize.Lesser),

                new PowerStone("heavens_dmg_raged_20", new TextObject("Lesser Wind Saphire"),new TextObject("+20% physical ranged damage amplification"), "powerstone_heavens_dmg_range", 
                    15, 10, "LoreOfHeavens", PowerSize.Lesser),
                new PowerStone("heavens_dmg_20", new TextObject("Lesser Conductive Saphire"), new TextObject("+20% lightning melee damage"), "powerstone_heavens_trait",
                    15, 10, "LoreOfHeavens", PowerSize.Lesser),
                
                new PowerStone("heavens_res_25", new TextObject("Lesser Dissipation  Saphire"), new TextObject("+25% lightning resistance, + 25% magical resistance"),"powerstone_heavens_res",
                    15, 10, "LoreOfHeavens", PowerSize.Lesser),

                new PowerStone("metal_dmg_15", new TextObject("Lesser Hardening Goldstone"), new TextObject("+15% physical damage"),"powerstone_metal_dmg1", 
                    15, 
                    10,
                    "LoreOfMetal", PowerSize.Lesser),
                new PowerStone("metal_dmg_20", new TextObject("Lesser Sparkling Goldstone"),new TextObject("+20% physical ranged damage"), "powerstone_metal_dmg2", 
                    15, 
                    10,
                    "LoreOfMetal", PowerSize.Lesser),
                new PowerStone("metal_res_30_debuff", new TextObject("Lesser Burdening Goldstone"),new TextObject("+30% physical resistance, 35% reduced speed"), "powerstone_metal_res_less", 
                    15,
                    10,
                    "LoreOfMetal", PowerSize.Lesser)
            };

            return list;
        }

        private List<PowerStone> GetGreaterPowerstones()
        {
            var list = new List<PowerStone>()
            {
                new PowerStone("fire_dmg_35", new TextObject("Greater Enlightening Fire Ruby"),new TextObject("+50% Fire amplification, 15% Fire damage+30% "),
                    "powerstone_fire_trait2", 
                    25, 
                    20, "LoreOfFire", PowerSize.Greater),
                new PowerStone("fire_amp_50", new TextObject("Greater Nourishing Fire Ruby"),new TextObject("Fire amplification, +20% speed"), "powerstone_fire_amp_mov", 15,
                    20, "LoreOfFire", PowerSize.Greater),

                new PowerStone("light_res_phys_magic_40",
                    new TextObject("Greater Protecting Lumenstone"),new TextObject("+40% physical, 40% magical resistance"), "powerstone_light_res2", 
                    25, 20,
                    "LoreOfLight", PowerSize.Greater),
                new PowerStone("light_mov_dmg_25",
                    new TextObject("Greater Timewarp Lumenstone"),new TextObject("Add 25% movement Speed and 25% magical melee damage"), "powerstone_light_trait",
                    25, 20,
                    "LoreOfLight", PowerSize.Greater),

                new PowerStone("beast_res_wild",new TextObject("Greater Ghost Amber of the Wild"), new TextObject("Unit is unstopable, will not show any sign of pain"), 
                    "powerstone_beast_wild", 
                    25, 20,
                    "LoreOfBeasts", PowerSize.Greater),
                new PowerStone("beast_res_range_25", new TextObject("Greater Ghost Amber of the Hunter"),new TextObject("+35% ranged resistance,+25% ranged damage"), "powerstone_beast_range_res_hunt", 
                    25, 20,
                    "LoreOfBeasts", PowerSize.Greater),

                new PowerStone("life_thorns", new TextObject("Greater Spiky Vitalleum"),new TextObject("Received damage is reapplied by 25%  as thorne damage"), "powerstone_life_thorns", 25, 20,
                    "LoreOfLife", PowerSize.Greater),
                new PowerStone("life_res_ward_35", new TextObject("Greater Protecting Vitalleum"),new TextObject("+35% Wardsave"), "powerstone_life_res_ward", 25,
                    20,
                    "LoreOfLife", PowerSize.Greater),

                new PowerStone("heavens_trait2",new TextObject("Greater Amplifying True Sapphires"),
                    new TextObject("+40% lightning damage"), "powerstone_heavens_trait2", 25, 20,
                    "LoreOfHeavens", PowerSize.Greater),
                new PowerStone("heavens_res_40", new TextObject("Greater True Dissipation Sapphires"),new TextObject("+40% physical, magic & lightning resistance"),
                    "powerstone_heavens_res2", 25, 20,
                    "LoreOfHeavens", PowerSize.Greater),

                new PowerStone("metal_dmg_20", new TextObject("Greater Goldstone of Disintegration"),new TextObject("+20% magical damage, 20% fire damage"), "powerstone_metal_trait",
                    20, 4,
                    "LoreOfMetal", PowerSize.Greater),
                new PowerStone("metal_magic_dmg_20", new TextObject("Greater Goldstone of Sharpening"),new TextObject("+50% Armor penetration"), "powerstone_metal_pen", 20, 4,
                    "LoreOfMetal", PowerSize.Greater)
            };

            return list;
        }

        private List<PowerStone> GetMightyPowerStones()
        {
            var list = new List<PowerStone>()
            {
                new PowerStone("fire_amp_150", new TextObject("Mighty Fire Ruby"),new TextObject("+150% Fire amp., 15% Fire dmg."),
                    "powerstone_fire_amp3", 50,
                    50, "LoreOfFire", PowerSize.Mighty),

                new PowerStone("light_mov_trait", new TextObject("Mighty Lumen Stone"),new TextObject("40% magical dmg., slows enemies on hit"),
                    "powerstone_light_trait2", 50, 50,
                    "LoreOfLight", PowerSize.Mighty),

                new PowerStone("beast_res_50", new TextObject("Mighty Ghost Amber"),
                    new TextObject("+50% speed, +50% physical resistance"), "powerstone_beast_range_res2", 50, 50,
                    "LoreOfBeasts", PowerSize.Mighty),

                new PowerStone("life_reg",new TextObject("Mighty Vitalleum"), new TextObject("Regenerate 1 HP every second"), "powerstone_life_reg",
                    50, 50,
                    "LoreOfLife", PowerSize.Mighty),

                new PowerStone("heavens_dmg_elec_frost", new TextObject("Mighty True Saphires"),new TextObject("+20% electric, +20%frost dmg, 20% slowdown"),
                    "powerstone_heavens_dmg2", 50, 50,
                    "LoreOfHeavens", PowerSize.Mighty),

                new PowerStone("metal_magic_dmg_phys", new TextObject("Mighty Goldstone"),new TextObject("40% Armor penetration, 20% magical, 20% fire"),  "powerstone_metal_trait2", 50, 50,
                    "LoreOfMetal", PowerSize.Mighty)
            };

            return list;
        }


        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
        {
            if (PartyScreenManager.Instance.CurrentMode != PartyScreenMode.Normal) return false;

            _setCharacter = characterObject;
            if (!characterObject.IsHero) return Hero.MainHero.PartyBelongedTo.MemberRoster.Contains(characterObject);

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

            var stones = AvailablePowerStones.ToList();

            var availablePrestige = Hero.MainHero.GetCultureSpecificCustomResourceValue();

            var MaximumWinds = Hero.MainHero.GetExtendedInfo().MaxWindsOfMagic;
            
            var fittingStones = stones.Where(
                x =>
                Hero.MainHero.HasKnownLore(x.LoreId) 
                && x.Price <= availablePrestige 
                && x.Upkeep < MaximumWinds).ToList();

            if (Hero.MainHero.HasCareerChoice("CollegeOrdersPassive4"))
            {
                var lores = PowerstoneHelper.GetPartyLores(Hero.MainHero.PartyBelongedTo.GetMemberHeroes());
                foreach (var lore in lores)
                {
                    fittingStones.AddRange(stones.Where(x=> x.LoreId == lore.ID && x.Price <= availablePrestige));
                }

                fittingStones = fittingStones.Distinct().ToList();
            }
            
            var displayedStones = fittingStones.Where(x => x.StoneLevel == PowerSize.Lesser).ToList();
            
            
            
            if (Hero.MainHero.HasUnlockedCareerChoiceTier(2))
                displayedStones.AddRange(fittingStones.Where(x => x.StoneLevel == PowerSize.Greater).ToList()); 
            
            if (Hero.MainHero.HasUnlockedCareerChoiceTier(3))
                displayedStones.AddRange(fittingStones.Where(x => x.StoneLevel == PowerSize.Mighty).ToList());

            var getAllStones = GetAllPowerstones();

            if (!Hero.MainHero.HasCareerChoice("AncientScrollsPassive4"))
            {
                foreach (var alreadyTakenStone in 
                         getAllStones.Select(stone => 
                             displayedStones.FirstOrDefault(X => X.Id == stone.Id)).
                             Where(alreadyTakenStone => alreadyTakenStone!=null))
                {
                    displayedStones.Remove(alreadyTakenStone);
                }
            }

            var currenstone = GetPowerstone(characterObject);

            if (currenstone != null)
            {
                displayedStones.Remove(currenstone);
            }
            
            foreach (var stone in displayedStones)
            {
                
                var upkeep = stone.Upkeep;
                var price = stone.Price;
                var emptyspace = "\n";
                var icon = GetStoneIcon(stone.LoreId);
                var enabled = MaximumWinds > upkeep;
                var hintText = stone.HintText.ToString();
                if (!enabled) hintText = "You don't have enough Winds";
                var text =
                    $"{{{icon}}}{stone.StoneName}{emptyspace}{price}{{PRESTIGE_ICON}} Reserved Winds: {upkeep}{{WINDS_ICON}}";
                
                list.Add(new InquiryElement(stone, new TextObject(text).ToString(), null,enabled,hintText));
            }


            if (currenstone != null)
            {
                
                list.Add(new InquiryElement("remove", $"Remove {currenstone.StoneName}", null));
            }

            var isSearchable = list.Count > 9;

            var inquirydata = new MultiSelectionInquiryData("Choose Power stone",
                "Empower your troop with a permanent magical effect of a Power stone. The effect will reduce your total amount of Winds while the stone is active.",
                list, true, 1, 1, "Accept", "Cancel", OnSelectedOption, OnCancel, "", isSearchable);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata);
        }


        

        private string GetStoneIcon(string stoneLoreId, bool asText=true)
        {
            if (!asText)
            {
                switch (stoneLoreId)
                {
                    case "LoreOfFire": return _fireIcon;
                    case "LoreOfLight": return _lightIcon;
                    case "LoreOfHeavens": return _heavensIcon;
                    case "LoreOfBeasts": return _beastIcon;
                    case "LoreOfLife": return _lifeIcon;
                    case "LoreOfMetal": return _metalIcon;
             
                }
            }
            switch (stoneLoreId)
            {
                case "LoreOfFire": return "FIRE_ICON";
                case "LoreOfLight": return "LIGHT_ICON";
                case "LoreOfHeavens": return "HEAVENS_ICON";
                case "LoreOfBeasts": return "BEAST_ICON";
                case "LoreOfLife": return "LIFE_ICON";
                case "LoreOfMetal": return "METAL_ICON";
                default: return "{}"; 
            }
        }


        private void OnCancel(List<InquiryElement> obj)
        {
        }

        private void OnSelectedOption(List<InquiryElement> powerStone)
        {
            var stone = powerStone[0].Identifier as PowerStone;

            var partyExtendedInfo =
                ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
            var attributes = partyExtendedInfo.TroopAttributes.FirstOrDefault(x => x.Key == _setCharacter.StringId)
                .Value;


            var currentStone = GetPowerstone(_setCharacter);

            if (currentStone != null)
            {
                partyExtendedInfo.RemoveTroopAttribute(_setCharacter.StringId, currentStone.Id);
            }


            if (powerStone[0].Identifier == "remove")
            {
                if (currentStone != null)
                {
                    Hero.MainHero.AddCustomResource("Prestige", currentStone.ScrapPrestigeGain);
                }
            }
            else
            {

                partyExtendedInfo.AddTroopAttribute(_setCharacter, stone.Id);
                Hero.MainHero.AddCustomResource("Prestige", -stone.Price);
            }


            ExtendedInfoManager.Instance.ValidatePartyInfos(MobileParty.MainParty);

            if (PartyVMExtension.ViewModelInstance != null)
            {
                PartyVMExtension.ViewModelInstance.RefreshValues();
            }
        }


        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText,
            bool isPrisoner = false)
        {

            if (!characterObject.IsHero && characterObject.Level < 16)
            {
                displayText = new TextObject("troop is not expierenced enough (tier 4 and above)");
                return false;
            }
            var powerstones = AvailablePowerStones;

            var powerstone = GetPowerstone(characterObject);

            var lowestPrice = 0;
            if (powerstones.Any()) lowestPrice = powerstones.Min(x => x.Price);


            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < lowestPrice && powerstone == null)
            {
                displayText = new TextObject("You do not have enough prestige to create power stones");
                return false;
            }

            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < lowestPrice && powerstone != null)
            {
                displayText = new TextObject("Remove Powerstone");
                return true;
            }

            if (powerstone != null)
                displayText = new TextObject(powerstone.HintText.ToString());
            else
                displayText = new TextObject("Create Powerstone empowering your troop or character");

            return true;
        }
    }


    public static class PowerstoneHelper
    {
        public static List<LoreObject> GetPartyLores(List<Hero> heroes)
        {
            var result = new List<LoreObject>();
            foreach (var hero in heroes)
            {
                if (hero == Hero.MainHero) continue;

                if (hero.Culture.StringId != "empire") continue;
                    
                if(!hero.IsSpellCaster())continue;

                var lores = LoreObject.GetAll();

                foreach (var lore in lores.Where(lore => hero.HasKnownLore(lore.ID)).Where(lore => !result.Contains(lore)))
                {
                    result.Add(lore);
                }
            }

            return result;

        }
    }

    public class PowerStone
    {
        public PowerStone(string id, TextObject text, TextObject hintText, string effect, int price, int upkeep, string loreID,
            PowerSize stoneLevel)
        {
            StoneName = text;
            Id = id;
            EffectId = effect;
            _price = price;
            LoreId = loreID;
            StoneLevel = stoneLevel;
            _upkeep = upkeep;
            HintText = hintText;
        }

        public int Upkeep
        {
            get
            {
                float upkeep = _upkeep;
                var factor = 1f;
                if (Hero.MainHero.HasCareerChoice("ImperialEnchantmentPassive4"))
                {
                    var choiceEnchantment = TORCareerChoices.GetChoice("ImperialEnchantmentPassive4");
                    factor -= choiceEnchantment.GetPassiveValue();
                }
                
                if (Hero.MainHero.HasCareerChoice("AncientScrollsPassive4"))
                {
                    
                    var lores = PowerstoneHelper.GetPartyLores(Hero.MainHero.PartyBelongedTo.GetMemberHeroes());
                    factor -= lores.Count * 0.05f;
                }

                upkeep = factor * upkeep;
                
                return (int)upkeep;
            }
        }

        public float ScrapPrestigeGain
        {
            get
            {
                var reduction = 0.4f;
                if (Hero.MainHero.HasCareerChoice("AncientScrollsPassive4")) reduction += 0.8f;

                return Price * reduction;
            }
        }

        public int Price
        {
            get
            {
                var value = (float)_price;
                if (Hero.MainHero.HasCareerChoice("TeclisTeachingsPassive4")) value *= 1 - 0.35f;

                return (int)value;
            }
        }

        public string Id;

        public TextObject HintText;
        private readonly int _upkeep;
        private readonly int _price;
        public PowerSize StoneLevel;
        public TextObject StoneName;
        public string LoreId;
        public string EffectId;
    }

    public enum PowerSize
    {
        Lesser = 0,
        Greater = 1,
        Mighty = 2
    }
}