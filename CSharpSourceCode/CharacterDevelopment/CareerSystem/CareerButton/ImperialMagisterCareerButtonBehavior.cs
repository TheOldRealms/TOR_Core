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

        private string fire_icon = "CareerSystem\\aqshy";
        private string light_icon = "CareerSystem\\hysh";
        private string heavens_icon = "CareerSystem\\azyr";
        private string life_icon = "CareerSystem\\ghyran";
        private string beast_icon = "CareerSystem\\ghur";
        
        
        public List<PowerStone> AvailablePowerStones => _availableStones;
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
                


                return "CareerSystem\\grail";
            }
        }

        public List<PowerStone> GetAllPowerstones()
        {
            var list = new List<PowerStone>();
            if(Hero.MainHero.PartyBelongedTo==null) return new List<PowerStone>();
            var partyExtendedInfo =
                ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);

            var characterToAttributes = partyExtendedInfo.TroopAttributes;

            foreach (var attributes in characterToAttributes.Values)
            foreach (var attribute in attributes)
            {
                var stone = _availableStones.FirstOrDefault(x => x.Id == attribute);
                if (stone != null) list.Add(stone);
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
                    var stones = attributes.Select(attribute => _availableStones.Find(x => x.Id == attribute))
                        .Where(powerstone => powerstone != null).ToList();
                    
                    var first = stones[0];
                
                    attributes.Clear();
                
                    attributes.Add(first.Id);
                    return first;
                }
         
                
         
            }

            return null;
        }

        public ImperialMagisterCareerButtonBehavior(CareerObject career) : base(career)
        {
            MBTextManager.SetTextVariable("WINDS_ICON",
            CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText());
            
            MBTextManager.SetTextVariable("FIRE_ICON", string.Format("<img src=\"{0}\"/>",fire_icon));
            MBTextManager.SetTextVariable("HEAVENS_ICON", string.Format("<img src=\"{0}\"/>",heavens_icon));
            MBTextManager.SetTextVariable("LIGHT_ICON", string.Format("<img src=\"{0}\"/>",light_icon));
            MBTextManager.SetTextVariable("LIFE_ICON", string.Format("<img src=\"{0}\"/>",life_icon));
            MBTextManager.SetTextVariable("BEAST_ICON", string.Format("<img src=\"{0}\"/>",beast_icon));
            _availableStones = CreateStoneList();
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
                new PowerStone("fire_dmg_10", new TextObject("Lesser Sparkling Fire Ruby"),new TextObject("+15% Fire damage"),
                    "powerstone_fire_trait", 15, 2, "LoreOfFire", PowerSize.Lesser),
                new PowerStone("fire_amp_50", new TextObject("Lesser Nourishing Fire Ruby"), new TextObject("+50% Fire amplification "), "powerstone_fire_amp", 15,
                    2, "LoreOfFire", PowerSize.Lesser),
                new PowerStone("fire_res_20", new TextObject("Lesser Heating Fire Ruby"), new TextObject("+20% Frost and Fire resistance"),
                    "powerstone_fire_res", 15, 2, "LoreOfFire", PowerSize.Lesser),

                new PowerStone("light_res_20", new TextObject("Lesser Protecting Lumen Stone"), new TextObject("+20% physical resistance"),"powerstone_light_res", 15, 3,
                    "LoreOfLight", PowerSize.Lesser),
                new PowerStone("light_mov_25", new TextObject("Lesser Timewarp Lumen Stone"), new TextObject("+25% movementSpeed"),"powerstone_light_mov", 15, 2,
                    "LoreOfLight", PowerSize.Lesser),
                new PowerStone("light_dmg_15",new TextObject("Lesser Gleaming Lumen Stone"), new TextObject("+15% physical damage"),"powerstone_light_dmg", 20, 3,
                    "LoreOfLight", PowerSize.Lesser),

                new PowerStone("beast_range_res_25", new TextObject("Lesser Obfuscating Ghost Amber"),new TextObject("+25% ranged resistance"), "powerstone_beast_res_range", 15, 1,
                    "LoreOfBeasts", PowerSize.Lesser),
                new PowerStone("beast_phys_15", new TextObject("Lesser Protecting Ghost Amber"),new TextObject("+15% physical resistance"), "powerstone_beast_res", 15, 2,
                    "LoreOfBeasts", PowerSize.Lesser),
                new PowerStone("beast_phys_20_ranged", new TextObject("Lesser Seeking Ghost Amber"),new TextObject("Add 20% ranged damage"), "powerstone_beast_dmg", 15, 4,
                    "LoreOfBeasts", PowerSize.Lesser),

                new PowerStone("life_magical_fire_res_25", new TextObject("Lesser Dampening Vitaellum"),new TextObject("Add 25% magical and fire resistance"),
                    "powerstone_life_res_mag", 15, 1,
                    "LoreOfLife", PowerSize.Lesser),
                new PowerStone("life_res_20", new TextObject("Lesser Protecting Vitaellum"), new TextObject("Add 20% physical resistance"),"powerstone_life_res_phy", 15, 2,
                    "LoreOfLife", PowerSize.Lesser),
                new PowerStone("life_res_40_debuff",new TextObject("Lesser Heavy Vitaellum"), new TextObject("Add 40% physical resistance -30% reduced speed"), "powerstone_life_res_debuff", 15, 2,
                    "LoreOfLife", PowerSize.Lesser),

                new PowerStone("heavens_dmg_raged_20", new TextObject("Lesser Wind Saphire"),new TextObject("+20% electric ranged damage"), "powerstone_heavens_dmg_range", 20,
                    2,
                    "LoreOfHeavens", PowerSize.Lesser),
                new PowerStone("heavens_dmg_melee_20", new TextObject("Lesser Conductive Saphire"), new TextObject("+20% electric melee damage"), "powerstone_heavens_dmg_melee",20,
                    2,
                    "LoreOfHeavens", PowerSize.Lesser),
                new PowerStone("heavens_res_25", new TextObject("Lesser Dissipation  Saphire"), new TextObject("+25% electrical resistance, + 25% magical resistance"),"powerstone_heavens_res", 10, 2,
                    "LoreOfHeavens", PowerSize.Lesser),

                new PowerStone("metal_dmg_15", new TextObject("Lesser Hardening Goldstone"), new TextObject("+15% physical damage"),"powerstone_metal_dmg_phy", 15, 2,
                    "LoreOfMetal", PowerSize.Lesser),
                new PowerStone("metal_magic_dmg_20", new TextObject("Lesser Sparkling Goldstone"),new TextObject("+20% magical ranged damage"), "powerstone_metal_dmg_mag", 15, 2,
                    "LoreOfMetal", PowerSize.Lesser),
                new PowerStone("metal_res_30_debuff", new TextObject("Lesser Burdening Goldstone"),new TextObject("+30% physical resistance, 20% reduced speed"), "powerstone_metal_res_less", 15,
                    2,
                    "LoreOfMetal", PowerSize.Lesser)
            };

            return list;
        }

        private List<PowerStone> GetGreaterPowerstones()
        {
            var list = new List<PowerStone>()
            {
                new PowerStone("fire_dmg_35", new TextObject("Greater Enlightening Fire Ruby"),new TextObject("+35% Fire damage, +15% speed"),
                    "powerstone_fire_trait2", 15, 4, "LoreOfFire", PowerSize.Greater),
                new PowerStone("fire_amp_50", new TextObject("Greater Nourishing Fire Ruby"),new TextObject("+50% Fire amplification, 15% Fire damage"), "powerstone_fire_amp2", 15,
                    4, "LoreOfFire", PowerSize.Greater),

                new PowerStone("light_res_phys_magic_40",
                    new TextObject("Greater Protecting Lumenstone"),new TextObject("+40% physical, 40% magical resistance"), "powerstone_light_res2", 15, 3,
                    "LoreOfLight", PowerSize.Greater),
                new PowerStone("light_mov_dmg_25",
                    new TextObject("Greater Timewarp Lumenstone"),new TextObject("Add 25% movement Speed and 25% magical melee damage"), "powerstone_light_mov2", 15, 4,
                    "LoreOfLight", PowerSize.Greater),

                new PowerStone("beast_res_wild",new TextObject("Greater Ghost Amber of the Wild"), new TextObject("+35% ranged resistance, +25% speed"), "powerstone_beast_range_res_wild", 15, 4,
                    "LoreOfBeasts", PowerSize.Greater),
                new PowerStone("beast_res_range_25", new TextObject("Greater Ghost Amber of the Hunter"),new TextObject("+35% ranged resistance,+25% ranged damage"), "powerstone_beast_range_res_hunt", 15, 4,
                    "LoreOfBeasts", PowerSize.Greater),

                new PowerStone("life_physical_50", new TextObject("Greater Enduring Vitalleum"),new TextObject("+50% physical resistance"), "powerstone_life_res_phy2", 15, 4,
                    "LoreOfLife", PowerSize.Greater),
                new PowerStone("life_res_ward_35", new TextObject("Greater Protecting Vitalleum"),new TextObject("+35% Wardsave"), "powerstone_life_res_ward", 15,
                    4,
                    "LoreOfLife", PowerSize.Greater),

                new PowerStone("heavens_amp_100_15_dmg",new TextObject("Greater Amplifying True Sapphires"),
                    new TextObject("+100% electric amplification, +15% electric damage"), "powerstone_heavens_amp", 20, 25,
                    "LoreOfHeavens", PowerSize.Greater),
                new PowerStone("heavens_res_40", new TextObject("Greater True Dissipation Sapphires"),new TextObject("+40% electric, magic & frost resistance"),
                    "powerstone_heavens_res2", 20, 4,
                    "LoreOfHeavens", PowerSize.Greater),

                new PowerStone("metal_dmg_15", new TextObject("Greater Goldstone of Disintegration"),new TextObject("+20% physical damage, 20% fire damage"), "powerstone_metal_dmg2",
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
                    "PLACEHOLDER", 50,
                    25, "LoreOfFire", PowerSize.Mighty),

                new PowerStone("light_mov_dmg_40", new TextObject("Mighty Lumen Stone"),new TextObject("40% magical dmg., slows enemies on hit"),
                    "powerstone_fire_amp3", 50, 25,
                    "LoreOfLight", PowerSize.Mighty),

                new PowerStone("beast_res_50", new TextObject("Mighty Ghost Amber"),
                    new TextObject("+50% speed, +50% physical resistance"), "beast_range_res2", 50, 25,
                    "LoreOfBeasts", PowerSize.Mighty),

                new PowerStone("life_reg",new TextObject("Mighty Vitalleum"), new TextObject("Regenerate 1 HP every second"), "powerstone_life_reg",
                    50, 25,
                    "LoreOfLife", PowerSize.Mighty),

                new PowerStone("heavens_dmg_elec_frost", new TextObject("Mighty True Saphires"),new TextObject("+20% electric, +20%frost dmg, 20% slowdown"),
                    "powerstone_heavens_dmg2", 50, 25,
                    "LoreOfHeavens", PowerSize.Mighty),

                new PowerStone("metal_magic_dmg_phys_fire", new TextObject("Mighty Goldstone"),new TextObject("30% Armor penetration, 20% physical, 20% fire"),  "powerstone_metal_dmg3", 50, 25,
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

            var stones = _availableStones.ToList();

            var troopCount = Hero.MainHero.PartyBelongedTo.Party.MemberRoster.GetElementNumber(characterObject);

            var availablePrestige = Hero.MainHero.GetCultureSpecificCustomResourceValue();

            var MaximumWinds = Hero.MainHero.GetExtendedInfo().MaxWindsOfMagic;
            
            var displayedStones = stones.Where(x =>
                Hero.MainHero.HasKnownLore(x.LoreId) && x.Price <= availablePrestige &&
                x.Upkeep * troopCount < MaximumWinds).ToList();

            if (Hero.MainHero.HasCareerChoice("CollegeOrdersPassive4"))
            {
                foreach (var hero in Hero.MainHero.PartyBelongedTo.GetMemberHeroes())
                {
                    if (hero == Hero.MainHero) continue;

                    if (hero.Culture.StringId != "empire") continue;

                    var lores = LoreObject.GetAll();

                    foreach (var lore in lores.Where(lore => hero.HasKnownLore(lore.ID)))
                        displayedStones.AddRange(stones.Where(x =>
                            x.LoreId == lore.ID && x.Price <= availablePrestige));
                }

                displayedStones = displayedStones.Distinct().ToList();
            }


            if (Hero.MainHero.HasUnlockedCareerChoiceTier(1))
                displayedStones.Select(x => x.StoneLevel == PowerSize.Lesser).ToList();
            else if (Hero.MainHero.HasUnlockedCareerChoiceTier(2))
                displayedStones.Select(x => x.StoneLevel == PowerSize.Lesser && x.StoneLevel == PowerSize.Greater);


            foreach (var stone in displayedStones)
            {
                
                var upkeep = stone.Upkeep;
                var price = stone.Price;
                var emptyspace = "\n";
                var icon = GetStoneIcon(stone.LoreId);
                var enabled = MaximumWinds > troopCount * upkeep;
                var hintText = stone.HintText.ToString();
                if (!enabled) hintText = "You don't have enough Winds";
                var text =
                    $"{{{icon}}}{stone.EffectText}{emptyspace}{price}{{PRESTIGE_ICON}} Upkeep: {troopCount * upkeep}{{WINDS_ICON}}({upkeep}{{WINDS_ICON}}p. Unit)";
                
                list.Add(new InquiryElement(stone, new TextObject(text).ToString(), null,enabled,hintText));
            }


            if (GetPowerstone(characterObject) != null) list.Add(new InquiryElement("remove", "Remove Stone", null));

            var isSearchable = list.Count > 9;

            var inquirydata = new MultiSelectionInquiryData("Choose Power stone",
                "Empower your troop with a permanent magical effect of a Power stone. The effect will reduce your total amount of Winds while the stone is active.",
                list, true, 1, 1, "Create Stone", "Cancel", OnSelectedOption, OnCancel, "", isSearchable);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata);
        }

        private string GetStoneIcon(string stoneLoreId, bool asText=true)
        {
            if (!asText)
            {
                switch (stoneLoreId)
                {
                    case "LoreOfFire": return fire_icon;
                    case "LoreOfLight": return life_icon;
                    case "LoreOfHeavens": return heavens_icon;
                    case "LoreOfBeasts": return beast_icon;
                    case "LoreOfLife": return life_icon;
                    case "LoreOfMetal": return life_icon;
             
                }
            }
            switch (stoneLoreId)
            {
                case "LoreOfFire": return "FIRE_ICON"; return fire_icon;
                case "LoreOfLight": return "LIGHT_ICON";
                case "LoreOfHeavens": return "HEAVENS_ICON";
                case "LoreOfBeasts": return "BEAST_ICON";
                case "LoreOfLife": return "LIFE_ICON";
                case "LoreOfMetal": return "LIFE_ICON";
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
            SpecialbuttonEventManagerHandler.Instance.RefreshPartyVM();
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
                displayText = new TextObject(powerstone.EffectText.ToString());
            else
                displayText = new TextObject("Create Powerstone empowering your troop or character");

            return true;
        }
    }

    public class PowerStone
    {
        public PowerStone(string id, TextObject text, TextObject hintText, string effect, int price, int upkeep, string loreID,
            PowerSize stoneLevel)
        {
            EffectText = text;
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
                if (Hero.MainHero.HasCareerChoice("ImperialEnchantmentPassive4")) upkeep -= upkeep * 0.25f;

                if (Hero.MainHero.HasCareerChoice("ArcaneKnowledgePassive1")) upkeep -= upkeep * 0.25f;

                return (int)upkeep;
            }
        }

        public float ScrapPrestigeGain
        {
            get
            {
                var reduction = 0.4f;
                if (Hero.MainHero.HasCareerChoice("AncientScrollsPassive4")) reduction += 0.4f;

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
        public TextObject EffectText;
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