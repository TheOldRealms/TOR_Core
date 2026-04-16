using Helpers;
using Ink.Parsed;
using System;
using System.Collections.Generic;
using System.Linq;
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
using TOR_Core.Utilities;
using static Helpers.PartyScreenHelper;

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
        private string _deathIcon = "CareerSystem\\chamon";


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
            if (Hero.MainHero.PartyBelongedTo == null) return new List<PowerStone>();
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
            List<PowerStone> stones = CareerButtonHelper.GetCurrentActiveItems(characterObject, AvailablePowerStones, s => s.Id);
            return stones?.FirstOrDefault();
        }

        public ImperialMagisterCareerButtonBehavior(CareerObject career) : base(career)
        {
            MBTextManager.SetTextVariable("WINDS_ICON",
            CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText());

            MBTextManager.SetTextVariable("FIRE_ICON", string.Format("<img src=\"{0}\"/>", _fireIcon));
            MBTextManager.SetTextVariable("HEAVENS_ICON", string.Format("<img src=\"{0}\"/>", _heavensIcon));
            MBTextManager.SetTextVariable("LIGHT_ICON", string.Format("<img src=\"{0}\"/>", _lightIcon));
            MBTextManager.SetTextVariable("LIFE_ICON", string.Format("<img src=\"{0}\"/>", _lifeIcon));
            MBTextManager.SetTextVariable("BEAST_ICON", string.Format("<img src=\"{0}\"/>", _beastIcon));
            MBTextManager.SetTextVariable("METAL_ICON", string.Format("<img src=\"{0}\"/>", _metalIcon));
            MBTextManager.SetTextVariable("DEATH_ICON", string.Format("<img src=\"{0}\"/>", _metalIcon));
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
                new("fire_dmg_10", TORTextHelper.GetTextObject("tor_powerstone_fire_dmg_10_name", "Lesser Sparkling Fire Ruby"), TORTextHelper.GetTextObject("tor_powerstone_fire_dmg_10_desc", "+15% Fire damage"),
                    "powerstone_fire_trait", 15, 10, "LoreOfFire", PowerSize.Lesser),
                new PowerStone("fire_amp_50", TORTextHelper.GetTextObject("tor_powerstone_fire_amp_50_name", "Lesser Nourishing Fire Ruby"), TORTextHelper.GetTextObject("tor_powerstone_fire_amp_50_desc", "+50% Fire amplification"), "powerstone_fire_amp", 15,
                    10, "LoreOfFire", PowerSize.Lesser),
                new PowerStone("fire_res_20", TORTextHelper.GetTextObject("tor_powerstone_fire_res_20_name", "Lesser Heating Fire Ruby"), TORTextHelper.GetTextObject("tor_powerstone_fire_res_20_desc", "+20% Frost and Fire resistance"),
                    "powerstone_fire_res", 15, 10, "LoreOfFire", PowerSize.Lesser),

                new PowerStone("light_res_20", TORTextHelper.GetTextObject("tor_powerstone_light_res_20_name", "Lesser Protecting Lumen Stone"), TORTextHelper.GetTextObject("tor_powerstone_light_res_20_desc", "+20% physical resistance"),"powerstone_light_res", 15, 10,
                    "LoreOfLight", PowerSize.Lesser),
                new PowerStone("light_mov_25", TORTextHelper.GetTextObject("tor_powerstone_light_mov_25_name", "Lesser Timewarp Lumen Stone"), TORTextHelper.GetTextObject("tor_powerstone_light_mov_25_desc", "+25% movementSpeed"),"powerstone_light_mov", 15, 10,
                    "LoreOfLight", PowerSize.Lesser),
                new PowerStone("light_dmg_15", TORTextHelper.GetTextObject("tor_powerstone_light_dmg_15_name", "Lesser Gleaming Lumen Stone"), TORTextHelper.GetTextObject("tor_powerstone_light_dmg_15_desc", "+15% physical damage amplification"),"powerstone_light_dmg", 15, 10,
                    "LoreOfLight", PowerSize.Lesser),

                new PowerStone("beast_range_res_25", TORTextHelper.GetTextObject("tor_powerstone_beast_range_res_25_name", "Lesser Obfuscating Ghost Amber"), TORTextHelper.GetTextObject("tor_powerstone_beast_range_res_25_desc", "+25% ranged resistance"), "powerstone_beast_res_range", 15, 10,
                    "LoreOfBeasts", PowerSize.Lesser),
                new PowerStone("beast_phys_15", TORTextHelper.GetTextObject("tor_powerstone_beast_phys_15_name", "Lesser Protecting Ghost Amber"), TORTextHelper.GetTextObject("tor_powerstone_beast_phys_15_desc", "+15% physical resistance"), "powerstone_beast_res", 15, 10,
                    "LoreOfBeasts", PowerSize.Lesser),
                new PowerStone("beast_phys_20_ranged", TORTextHelper.GetTextObject("tor_powerstone_beast_phys_20_ranged_name", "Lesser Seeking Ghost Amber"), TORTextHelper.GetTextObject("tor_powerstone_beast_phys_20_ranged_desc", "Add 20% ranged damage"), "powerstone_beast_dmg_range", 15, 10,
                    "LoreOfBeasts", PowerSize.Lesser),

                new PowerStone("life_magical_fire_res_25", TORTextHelper.GetTextObject("tor_powerstone_life_magical_fire_res_25_name", "Lesser Dampening Vitaellum"), TORTextHelper.GetTextObject("tor_powerstone_life_magical_fire_res_25_desc", "Add 25% magical and fire resistance"),
                    "powerstone_life_res_mag", 15, 10,
                    "LoreOfLife", PowerSize.Lesser),
                new PowerStone("life_res_20", TORTextHelper.GetTextObject("tor_powerstone_life_res_20_name", "Lesser Protecting Vitaellum"), TORTextHelper.GetTextObject("tor_powerstone_life_res_20_desc", "Add 20% physical resistance"),"powerstone_life_res_phys", 15, 10,
                    "LoreOfLife", PowerSize.Lesser),
                new PowerStone("life_res_40_debuff", TORTextHelper.GetTextObject("tor_powerstone_life_res_40_debuff_name", "Lesser Heavy Vitaellum"), TORTextHelper.GetTextObject("tor_powerstone_life_res_40_debuff_desc", "Add 35% physical resistance -35% reduced speed"), "powerstone_life_res_debuff", 15, 10,
                    "LoreOfLife", PowerSize.Lesser),

                new PowerStone("heavens_dmg_raged_20", TORTextHelper.GetTextObject("tor_powerstone_heavens_dmg_raged_20_name", "Lesser Wind Saphire"), TORTextHelper.GetTextObject("tor_powerstone_heavens_dmg_raged_20_desc", "+20% physical ranged damage amplification"), "powerstone_heavens_dmg_range",
                    15, 10, "LoreOfHeavens", PowerSize.Lesser),
                new PowerStone("heavens_dmg_20", TORTextHelper.GetTextObject("tor_powerstone_heavens_dmg_20_name", "Lesser Conductive Saphire"), TORTextHelper.GetTextObject("tor_powerstone_heavens_dmg_20_desc", "+20% lightning melee damage"), "powerstone_heavens_trait",
                    15, 10, "LoreOfHeavens", PowerSize.Lesser),

                new PowerStone("heavens_res_25", TORTextHelper.GetTextObject("tor_powerstone_heavens_res_25_name", "Lesser Dissipation Saphire"), TORTextHelper.GetTextObject("tor_powerstone_heavens_res_25_desc", "+25% lightning resistance, +25% magical resistance"),"powerstone_heavens_res",
                    15, 10, "LoreOfHeavens", PowerSize.Lesser),

                new PowerStone("metal_dmg_15", TORTextHelper.GetTextObject("tor_powerstone_metal_dmg_15_name", "Lesser Hardening Goldstone"), TORTextHelper.GetTextObject("tor_powerstone_metal_dmg_15_desc", "+15% physical damage"),"powerstone_metal_dmg1",
                    15,
                    10,
                    "LoreOfMetal", PowerSize.Lesser),
                new PowerStone("metal_dmg_20", TORTextHelper.GetTextObject("tor_powerstone_metal_dmg_20_name", "Lesser Sparkling Goldstone"), TORTextHelper.GetTextObject("tor_powerstone_metal_dmg_20_desc", "+20% physical ranged damage"), "powerstone_metal_dmg2",
                    15,
                    10,
                    "LoreOfMetal", PowerSize.Lesser),
                new PowerStone("metal_res_30_debuff", TORTextHelper.GetTextObject("tor_powerstone_metal_res_30_debuff_name", "Lesser Burdening Goldstone"), TORTextHelper.GetTextObject("tor_powerstone_metal_res_30_debuff_desc", "+30% physical resistance, 35% reduced speed"), "powerstone_metal_res_less",
                    15,
                    10,
                    "LoreOfMetal", PowerSize.Lesser),

                new PowerStone("death_dmg_15", TORTextHelper.GetTextObject("tor_powerstone_death_dmg_15_name", "Lesser Harming Endstone"), TORTextHelper.GetTextObject("tor_powerstone_death_dmg_15_desc", "+15% physical damage"),"powerstone_metal_dmg1",
                15,
                10,
                "LoreOfDeath", PowerSize.Lesser),
                new PowerStone("death_res_mag_20", TORTextHelper.GetTextObject("tor_powerstone_death_res_mag_20_name", "Lesser Encouraging Endstone"), TORTextHelper.GetTextObject("tor_powerstone_death_res_mag_20_desc", "+20% magic damage resistance"), "powerstone_metal_dmg2",
                    15,
                    10,
                    "LoreOfDeath", PowerSize.Lesser),
                new PowerStone("death_res_30_debuff", TORTextHelper.GetTextObject("tor_powerstone_death_res_30_debuff_name", "Lesser Deadening Endstone"), TORTextHelper.GetTextObject("tor_powerstone_death_res_30_debuff_desc", "+25% physical resistance, 20% reduced swing-speed"), "powerstone_metal_res_less",
                    15,
                    10,
                    "LoreOfDeath", PowerSize.Lesser)
            };

            return list;
        }

        private List<PowerStone> GetGreaterPowerstones()
        {
            var list = new List<PowerStone>()
            {
                new PowerStone("fire_dmg_35", TORTextHelper.GetTextObject("tor_powerstone_fire_dmg_35_name", "Greater Enlightening Fire Ruby"), TORTextHelper.GetTextObject("tor_powerstone_fire_dmg_35_desc", "+50% Fire amplification, 15% Fire damage+30%"),
                    "powerstone_fire_trait2",
                    25,
                    20, "LoreOfFire", PowerSize.Greater),
                new PowerStone("fire_amp_50", TORTextHelper.GetTextObject("tor_powerstone_fire_amp_50_greater_name", "Greater Nourishing Fire Ruby"), TORTextHelper.GetTextObject("tor_powerstone_fire_amp_50_greater_desc", "Fire amplification, +20% speed"), "powerstone_fire_amp_mov", 15,
                    20, "LoreOfFire", PowerSize.Greater),

                new PowerStone("light_res_phys_magic_40",
                    TORTextHelper.GetTextObject("tor_powerstone_light_res_phys_magic_40_name", "Greater Protecting Lumenstone"), TORTextHelper.GetTextObject("tor_powerstone_light_res_phys_magic_40_desc", "+40% physical, 40% magical resistance"), "powerstone_light_res2",
                    25, 20,
                    "LoreOfLight", PowerSize.Greater),
                new PowerStone("light_mov_dmg_25",
                    TORTextHelper.GetTextObject("tor_powerstone_light_mov_dmg_25_name", "Greater Timewarp Lumenstone"), TORTextHelper.GetTextObject("tor_powerstone_light_mov_dmg_25_desc", "Add 25% movement Speed and 25% magical melee damage"), "powerstone_light_trait",
                    25, 20,
                    "LoreOfLight", PowerSize.Greater),

                new PowerStone("beast_res_wild", TORTextHelper.GetTextObject("tor_powerstone_beast_res_wild_name", "Greater Ghost Amber of the Wild"), TORTextHelper.GetTextObject("tor_powerstone_beast_res_wild_desc", "Unit is unstoppable, will not show any sign of pain"),
                    "powerstone_beast_wild",
                    25, 20,
                    "LoreOfBeasts", PowerSize.Greater),
                new PowerStone("beast_res_range_25", TORTextHelper.GetTextObject("tor_powerstone_beast_res_range_25_name", "Greater Ghost Amber of the Hunter"), TORTextHelper.GetTextObject("tor_powerstone_beast_res_range_25_desc", "+35% ranged resistance, +25% ranged damage"), "powerstone_beast_range_res_hunt",
                    25, 20,
                    "LoreOfBeasts", PowerSize.Greater),

                new PowerStone("life_thorns", TORTextHelper.GetTextObject("tor_powerstone_life_thorns_name", "Greater Spiky Vitalleum"), TORTextHelper.GetTextObject("tor_powerstone_life_thorns_desc", "Received damage is reapplied by 25% as thorn damage"), "powerstone_life_thorns", 25, 20,
                    "LoreOfLife", PowerSize.Greater),
                new PowerStone("life_res_ward_35", TORTextHelper.GetTextObject("tor_powerstone_life_res_ward_35_name", "Greater Protecting Vitalleum"), TORTextHelper.GetTextObject("tor_powerstone_life_res_ward_35_desc", "+35% Wardsave"), "powerstone_life_res_ward", 25,
                    20,
                    "LoreOfLife", PowerSize.Greater),

                new PowerStone("heavens_trait2", TORTextHelper.GetTextObject("tor_powerstone_heavens_trait2_name", "Greater Amplifying True Sapphires"),
                    TORTextHelper.GetTextObject("tor_powerstone_heavens_trait2_desc", "+40% lightning damage"), "powerstone_heavens_trait2", 25, 20,
                    "LoreOfHeavens", PowerSize.Greater),
                new PowerStone("heavens_res_40", TORTextHelper.GetTextObject("tor_powerstone_heavens_res_40_name", "Greater True Dissipation Sapphires"), TORTextHelper.GetTextObject("tor_powerstone_heavens_res_40_desc", "+40% physical, magic and lightning resistance"),
                    "powerstone_heavens_res2", 25, 20,
                    "LoreOfHeavens", PowerSize.Greater),

                new PowerStone("metal_dmg_20", TORTextHelper.GetTextObject("tor_powerstone_metal_dmg_20_greater_name", "Greater Goldstone of Disintegration"), TORTextHelper.GetTextObject("tor_powerstone_metal_dmg_20_greater_desc", "+20% magical damage, 20% fire damage"), "powerstone_metal_trait",
                    20, 4,
                    "LoreOfMetal", PowerSize.Greater),
                new PowerStone("metal_magic_dmg_20", TORTextHelper.GetTextObject("tor_powerstone_metal_magic_dmg_20_name", "Greater Goldstone of Sharpening"), TORTextHelper.GetTextObject("tor_powerstone_metal_magic_dmg_20_desc", "+50% Armor penetration"), "powerstone_metal_pen", 20, 4,
                    "LoreOfMetal", PowerSize.Greater),

                new PowerStone("death_trait_undead_bane", TORTextHelper.GetTextObject("tor_powerstone_death_trait_undead_bane_name", "Greater Banishing Endstone"), TORTextHelper.GetTextObject("tor_powerstone_death_trait_undead_bane_desc", "50% more damage against undead and vampires"), "powerstone_death_undead_bane",
                20, 4,
                "LoreOfDeath", PowerSize.Greater),
                new PowerStone("death_dmg_mag_20", TORTextHelper.GetTextObject("tor_powerstone_death_dmg_mag_20_name", "Greater Harming Endstone"), TORTextHelper.GetTextObject("tor_powerstone_death_dmg_mag_20_desc", "20% extra magical damage"), "powerstone_death_dmg_trait", 20, 4,
                    "LoreOfDeath", PowerSize.Greater),
            };

            return list;
        }

        private List<PowerStone> GetMightyPowerStones()
        {
            var list = new List<PowerStone>()
            {
                new PowerStone("fire_amp_150", TORTextHelper.GetTextObject("tor_powerstone_fire_amp_150_name", "Mighty Fire Ruby"), TORTextHelper.GetTextObject("tor_powerstone_fire_amp_150_desc", "+150% Fire amp., 15% Fire dmg."),
                    "powerstone_fire_amp3", 50,
                    50, "LoreOfFire", PowerSize.Mighty),

                new PowerStone("light_mov_trait", TORTextHelper.GetTextObject("tor_powerstone_light_mov_trait_name", "Mighty Lumen Stone"), TORTextHelper.GetTextObject("tor_powerstone_light_mov_trait_desc", "40% magical dmg., slows enemies on hit"),
                    "powerstone_light_trait2", 50, 50,
                    "LoreOfLight", PowerSize.Mighty),

                new PowerStone("beast_res_50", TORTextHelper.GetTextObject("tor_powerstone_beast_res_50_name", "Mighty Ghost Amber"),
                    TORTextHelper.GetTextObject("tor_powerstone_beast_res_50_desc", "+50% speed, +50% physical resistance"), "powerstone_beast_range_res2", 50, 50,
                    "LoreOfBeasts", PowerSize.Mighty),

                new PowerStone("life_reg", TORTextHelper.GetTextObject("tor_powerstone_life_reg_name", "Mighty Vitalleum"), TORTextHelper.GetTextObject("tor_powerstone_life_reg_desc", "Regenerate 1 HP every second"), "powerstone_life_reg",
                    50, 50,
                    "LoreOfLife", PowerSize.Mighty),

                new PowerStone("heavens_dmg_elec_frost", TORTextHelper.GetTextObject("tor_powerstone_heavens_dmg_elec_frost_name", "Mighty True Saphires"), TORTextHelper.GetTextObject("tor_powerstone_heavens_dmg_elec_frost_desc", "+20% electric, +20% frost dmg, 20% slowdown"),
                    "powerstone_heavens_dmg2", 50, 50,
                    "LoreOfHeavens", PowerSize.Mighty),

                new PowerStone("metal_magic_dmg_phys", TORTextHelper.GetTextObject("tor_powerstone_metal_magic_dmg_phys_name", "Mighty Goldstone"), TORTextHelper.GetTextObject("tor_powerstone_metal_magic_dmg_phys_desc", "40% Armor penetration, 20% magical, 20% fire"),  "powerstone_metal_trait2", 50, 50,
                    "LoreOfMetal", PowerSize.Mighty),

                new PowerStone("death_magic_surv_100", TORTextHelper.GetTextObject("tor_powerstone_death_magic_surv_100_name", "Mighty Endstone"), TORTextHelper.GetTextObject("tor_powerstone_death_magic_surv_100_desc", "100% survival Chance for Unit"),  "powerstone_metal_trait2", 50, 50,
                "LoreOfDeath", PowerSize.Mighty)
            };

            return list;
        }


        public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false)
        {
            if (PartyScreenHelper.GetActivePartyState().PartyScreenMode != PartyScreenMode.Normal) return false;

            _setCharacter = characterObject;
            if (!characterObject.IsHero) return Hero.MainHero.PartyBelongedTo.MemberRoster.Contains(characterObject);

            return characterObject.HeroObject.PartyBelongedTo == PartyBase.MainParty.MobileParty;
        }


        public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false, bool shiftClick = false)
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
                    fittingStones.AddRange(stones.Where(x => x.LoreId == lore.ID && x.Price <= availablePrestige));
                }

                fittingStones = fittingStones.Distinct().ToList();
            }

            var displayedStones = fittingStones.Where(x => x.StoneLevel == PowerSize.Lesser).ToList();



            if (Hero.MainHero.HasUnlockedCareerChoiceTier(2))
                displayedStones.AddRange(fittingStones.Where(x => x.StoneLevel == PowerSize.Greater).ToList());

            if (Hero.MainHero.HasUnlockedCareerChoiceTier(3))
                displayedStones.AddRange(fittingStones.Where(x => x.StoneLevel == PowerSize.Mighty).ToList());

            var getAllStones = GetAllPowerstones();

            foreach (var alreadyTakenStone in
                     getAllStones.Select(stone =>
                         displayedStones.FirstOrDefault(X => X.Id == stone.Id)).
                         Where(alreadyTakenStone => alreadyTakenStone != null))
            {
                displayedStones.Remove(alreadyTakenStone);
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
                if (!enabled) hintText = TORTextHelper.GetText("tor_powerstone_not_enough_winds_text", "You don't have enough Winds");
                var text =
                    $"{{{icon}}}{stone.StoneName}{emptyspace}{price}{{PRESTIGE_ICON}} Reserved Winds: {upkeep}{{WINDS_ICON}}";

                list.Add(new InquiryElement(stone, new TextObject(text).ToString(), null, enabled, hintText));
            }


            if (currenstone != null)
            {
                list.Add(CareerButtonHelper.CreateRemoveOption(currenstone.StoneName.ToString(), "tor_powerstone_remove_hint", "Remove powerstone and recover some prestige."));
            }

            var isSearchable = list.Count > 9;

            var inquirydata = new MultiSelectionInquiryData(TORTextHelper.GetText("tor_powerstone_choose_title_text", "Choose Power stone"),
                TORTextHelper.GetText("tor_powerstone_choose_description_text", "Empower your troop with a permanent magical effect of a Power stone. The effect will reduce your total amount of Winds while the stone is active."),
                list, true, 1, 1, TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"), OnSelectedOption, OnCancel, "", isSearchable);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata);
        }




        private string GetStoneIcon(string stoneLoreId, bool asText = true)
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
                    case "LoreOfDeath": return _deathIcon;
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
                case "LoreOfDeath": return "DEATH_ICON";
                default: return "{}";
            }
        }


        private void OnCancel(List<InquiryElement> obj)
        {
        }

        private void OnSelectedOption(List<InquiryElement> powerStone)
        {
            var currentStone = GetPowerstone(_setCharacter);
            var currentStones = currentStone != null ? new List<PowerStone> { currentStone } : null;

            CareerButtonHelper.ProcessSelection(
                _setCharacter,
                powerStone,
                currentStones,
                stone => stone.Id,
                onBeforeAdd: stone => Hero.MainHero.AddCustomResource("Prestige", -stone.Price),
                stones =>
                {
                    foreach (var s in stones)
                    {
                        Hero.MainHero.AddCustomResource("Prestige", s.ScrapPrestigeGain);
                    }
                }
            );
        }


        public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText,
            bool isPrisoner = false)
        {

            if (!characterObject.IsHero && characterObject.Level < 16)
            {
                displayText = TORTextHelper.GetTextObject("tor_powerstone_not_experienced_enough_text", "troop is not expierenced enough (tier 4 and above)");
                return false;
            }
            var powerstones = AvailablePowerStones;

            var powerstone = GetPowerstone(characterObject);

            var lowestPrice = 0;
            if (powerstones.Any()) lowestPrice = powerstones.Min(x => x.Price);


            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < lowestPrice && powerstone == null)
            {
                displayText = TORTextHelper.GetTextObject("tor_powerstone_not_enough_prestige_text", "You do not have enough prestige to create power stones");
                return false;
            }

            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < lowestPrice && powerstone != null)
            {
                displayText = TORTextHelper.GetTextObject("tor_powerstone_remove_text", "Remove Powerstone");
                return true;
            }

            if (powerstone != null)
                displayText = new TextObject(powerstone.HintText.ToString());
            else
                displayText = TORTextHelper.GetTextObject("tor_powerstone_create_text", "Create Powerstone empowering your troop or character");

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

                if (!hero.IsSpellCaster()) continue;

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
                if (Hero.MainHero.HasCareerChoice("AncientScrollsPassive3")) reduction += 0.8f;

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