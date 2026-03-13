using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.BountyMaster;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CampaignMechanics.ServeAsAHireling;
using TOR_Core.CampaignMechanics.SpellTrainers;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace TOR_Core.Extensions
{
    public static class HeroExtensions
    {

        public static float GetAggregatedStatEffectFromEquipment(this Hero hero, ItemTraitStatType itemTraitStatType)
        {
            float result = 0f;
            if (hero != null && hero.BattleEquipment != null)
            {
                for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
                {
                    var slot = hero.BattleEquipment[i];
                    if (slot.IsEmpty) continue;
                    var traits = slot.Item?.GetTraits();
                    if (traits != null && traits.Count > 0 && traits.AnyQ(x => x.StatsTuple != null && x.StatsTuple.StatType == itemTraitStatType))
                    {
                        foreach (var trait in traits)
                        {
                            if (trait.StatsTuple != null && trait.StatsTuple.StatType == itemTraitStatType)
                            {
                                result += trait.StatsTuple.Value;
                            }
                        }
                    }
                }
            }
            return result;
        }

        public static float GetAggregatedSkillEffectFromEquipment(this Hero hero, string skillId)
        {
            float result = 0f;
            if (hero != null && hero.BattleEquipment != null)
            {
                for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
                {
                    var slot = hero.BattleEquipment[i];
                    if (slot.IsEmpty) continue;
                    var traits = slot.Item?.GetTraits();
                    if (traits != null && traits.Count > 0 && traits.AnyQ(x => x.StatsTuple != null && x.StatsTuple.StatType == ItemTraitStatType.Skill))
                    {
                        foreach (var trait in traits)
                        {
                            if (trait.StatsTuple != null && trait.StatsTuple.StatType == ItemTraitStatType.Skill && trait.StatsTuple.SkillId == skillId)
                            {
                                result += trait.StatsTuple.Value;
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <remarks>
        /// This can only return true for the main hero; anything else passed is guaranteed false.
        /// </remarks>
        public static bool IsEnlisted(this Hero hero)
        {
            if (hero != Hero.MainHero) return false;
            var hirelingCampaignBehavior = Campaign.Current.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            if (hirelingCampaignBehavior != null)
            {
                return hirelingCampaignBehavior.IsEnlisted();
            }
            return false;
        }

        public static Hero GetEnlistingHero(this Hero hero)
        {
            var hirelingCampaignBehavior = Campaign.Current.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            if (hirelingCampaignBehavior != null)
            {
                return hirelingCampaignBehavior.EnlistingLord;
            }
            return null;
        }

        public static bool CanRaiseDead(this Hero hero)
        {
            if (!(hero.Culture.StringId == TORConstants.Cultures.SYLVANIA || hero.Culture.StringId == TORConstants.Cultures.MOUSILLON))
                return false;
            return hero.PartyBelongedTo != null && hero.PartyBelongedTo.GetMemberHeroes().Any(x => x.IsNecromancer());
        }

        /// <summary>
        /// Calculates the Raise dead chance based on the Spellcraft Skillvalue and applies Career Perks.
        /// 0.005 with 200 spell craft would allow an 80% chance of raising dead.
        /// </summary>
        public static float GetRaiseDeadChance(this Hero hero)
        {
            if (!hero.IsNecromancer()) return 0f;

            var chance = new ExplainedNumber();
            var skillValue = hero.GetSkillValue(TORSkills.Spellcraft);
            if (hero.HasCareer(TORCareers.BloodKnight)) return 0.1f;

            var chanceValue = Mathf.Clamp(skillValue * 0.005f, 0.05f, 0.7f);
            chance.Add(chanceValue);

            if (hero.HasAnyCareer())
            {
                var choices = hero.GetAllCareerChoices();

                if (choices.Contains("MasterOfDeadPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("MasterOfDeadPassive3");
                    if (choice != null)
                        chance.AddFactor(choice.GetPassiveValue());
                }
            }

            return chance.ResultNumber;
        }

        /// <summary>
        /// Calculates the chance based on the Spellcraft Skillvalue and applies Career Perks.
        /// 0.005 would allow with 200 spell craft an 80% chance of raising dead.
        /// </summary>
        /// <remarks>
        /// I think this was an attempt that didn't end up being used when dryads were being designed. It's a copy of the GetRaiseDeadChance method with nothing changed.
        /// </remarks>
        public static float GetTreeSpiritChance(this Hero hero)
        {
            if (!hero.IsSpellCaster()) return 0f;

            var chance = new ExplainedNumber();
            var skillValue = hero.GetSkillValue(TORSkills.Spellcraft);

            var chanceValue = Mathf.Clamp(skillValue * 0.005f, 0.05f, 0.7f);
            chance.Add(chanceValue);

            if (hero.HasAnyCareer())
            {
                var choices = hero.GetAllCareerChoices();

                if (choices.Contains("MasterOfDeadPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("MasterOfDeadPassive3");
                    if (choice != null)
                        chance.AddFactor(choice.GetPassiveValue());
                }
            }

            return chance.ResultNumber;
        }


        public static void AddCustomResource(this Hero hero, string id, float amount)
        {
            var info = hero.GetExtendedInfo();
            info?.AddCustomResource(id, amount);
        }

        public static float GetCustomResourceValue(this Hero hero, string id)
        {
            var info = hero.GetExtendedInfo();

            if (info != null)
            {
                return info.GetCustomResourceValue(id);
            }
            else return 0;
        }

        public static CustomResource GetCultureSpecificCustomResource(this Hero hero)
        {
            if (hero == null)
                return null;

            return CustomResourceManager.GetResourceObject(x => x.FirstOrDefault(y => y.Cultures.Contains(hero.Culture.StringId)));
        }

        public static float GetCalculatedCustomResourceUpkeep(this Hero hero, string CustomResourceId)
        {
            var model = Campaign.Current.Models.GetCustomResourceModel();

            if (model != null)
            {
                return model.GetCalculatedCustomResourceUpkeep(hero, CustomResourceId).ResultNumber;
            }

            return 0;
        }

        public static float GetCultureSpecificCustomResourceChange(this Hero hero, string resourceid)
        {
            var model = Campaign.Current.Models.GetCustomResourceModel();

            if (model != null)
            {
                return model.GetCultureSpecificCustomResourceChange(hero, resourceid).ResultNumber;
            }

            return 0;
        }

        public static float GetCultureSpecificCustomResourceValue(this Hero hero)
        {
            if (hero.GetCultureSpecificCustomResource() != null)
            {
                return hero.GetCustomResourceValue(hero.GetCultureSpecificCustomResource().StringId);
            }
            else return 0;
        }

        public static void AddCultureSpecificCustomResource(this Hero hero, float amount)
        {
            if (hero.GetCultureSpecificCustomResource() != null) hero.AddCustomResource(hero.GetCultureSpecificCustomResource().StringId, amount);
        }

        public static Dictionary<CustomResource, float> GetCustomResources(this Hero hero)
        {
            var info = hero.GetExtendedInfo();
            if (info != null)
            {
                return info.GetCustomResources();
            }
            else return null;
        }

        public static float AddWindsOfMagic(this Hero hero, float amount)
        {
            float result = 0;
            var info = hero.GetExtendedInfo();
            info?.AddCustomResource("WindsOfMagic", amount);

            return result;
        }

        public static HeroExtendedInfo GetExtendedInfo(this Hero hero)
        {
            return ExtendedInfoManager.Instance.GetHeroInfoFor(hero.GetInfoKey());
        }

        public static int GetEffectiveWindsCostForSpell(this Hero hero, Spell spell)
        {
            return hero.GetEffectiveWindsCostForSpell(spell.Template);
        }

        public static int GetEffectiveWindsCostForSpell(this Hero hero, AbilityTemplate spell)
        {
            int result = spell.WindsOfMagicCost;
            if (Game.Current.GameType is Campaign)
            {
                var model = Campaign.Current.Models.GetAbilityModel();
                if (model != null)
                {
                    result = model.GetEffectiveWindsCost(hero.CharacterObject, spell);
                }
            }
            return result;
        }

        public static int GetPlaceableArtilleryCount(this Hero hero)
        {
            int count = 0;
            if (hero.CanPlaceArtillery() || hero.HasAttribute("EngineerCompanion") && Hero.MainHero.CanPlaceArtillery())
            {
                var engineering = hero.GetSkillValue(DefaultSkills.Engineering);
                count = (int)Math.Truncate((decimal)engineering / 50);
                if (hero != Hero.MainHero && count == 0) count = 1; //Ensure AI lords can place at least 1 piece.
            }
            return count;
        }

        public static bool CanPlaceArtillery(this Hero hero)
        {
            return hero.HasAttribute("CanPlaceArtillery");
        }

        public static void AddAbility(this Hero hero, string ability)
        {
            var info = hero.GetExtendedInfo();
            if (info != null && !info.AllAbilities.Contains(ability))
            {
                info.AcquiredAbilities.Add(ability);
                TORCampaignEvents.Instance.OnAbilityLearned(hero, ability);
            }
        }

        public static void RemoveAttribute(this Hero hero, string attribute)
        {
            var info = hero.GetExtendedInfo();
            if (info != null && info.AllAttributes.Contains(attribute))
            {
                info.AcquiredAttributes.Remove(attribute);
            }
        }
        public static void AddAttribute(this Hero hero, string attribute)
        {
            var info = hero.GetExtendedInfo();
            if (info != null && !info.AllAttributes.Contains(attribute))
            {
                info.AcquiredAttributes.Add(attribute);
            }
        }

        public static void AddEnchantmentBlueprint(this Hero hero, string bluePrint, bool showNotification = false)
        {
            var info = hero.GetExtendedInfo();
            if (info != null && !info.HasKnownEnchantmentBlueprint(bluePrint))
            {
                info.AddKnownEnchantmentBlueprint(bluePrint);
                if (showNotification)
                {
                    var itemTrait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == bluePrint);
                    var learnedEnchantmentText = TORTextHelper.GetTextObject("tor_learned_enchantment_text", "{HERO_NAME} learned the enchantment {ENCHANTMENT_NAME}");
                    learnedEnchantmentText.SetTextVariable("HERO_NAME", hero.Name);
                    learnedEnchantmentText.SetTextVariable("ENCHANTMENT_NAME", itemTrait!.ItemTraitName);
                    MBInformationManager.AddQuickInformation(learnedEnchantmentText, 0, hero.CharacterObject);
                }
            }
        }

        public static bool HasAttribute(this Hero hero, string attribute)
        {
            if (hero.GetExtendedInfo() != null)
            {
                return hero.GetExtendedInfo().AllAttributes.Contains(attribute);
            }
            else return false;
        }

        public static bool HasAbility(this Hero hero, string ability)
        {
            if (hero.GetExtendedInfo() != null)
            {
                return hero.GetExtendedInfo().AllAbilities.Contains(ability);
            }
            else return false;
        }

        public static ChivalryLevel GetChivalryLevel(this Hero hero)
        {
            var customResource = GetCustomResourceValue(hero, "Chivalry");
            return ChivalryHelper.GetChivalryLevelForResource(customResource);
        }

        public static ForestHarmonyLevel GetForestHarmonyLevel(this Hero hero)
        {
            var customResource = GetCustomResourceValue(hero, "ForestHarmony");
            return ForestHarmonyHelper.GetForestHarmonyLevelForResource(customResource);
        }

        public static bool HasChivalryLevel(this Hero hero, ChivalryLevel level)
        {
            return ChivalryHelper.HasChivalryLevel(hero, level);
        }

        public static void SetSpellCastingLevel(this Hero hero, SpellCastingLevel level)
        {
            if (hero.GetExtendedInfo() != null)
            {
                hero.GetExtendedInfo().SpellCastingLevel = level;
            }
        }

        public static void AddKnownLore(this Hero hero, string loreID)
        {
            hero.GetExtendedInfo()?.AddKnownLore(loreID);
        }

        public static void AddKnownEnchantmentBlueprint(this Hero hero, string enchantmentID)
        {
            hero.GetExtendedInfo()?.AddKnownEnchantmentBlueprint(enchantmentID);
        }

        public static bool HasKnownEnchantmentBlueprint(this Hero hero, string enchantmentID)
        {
            return hero.GetExtendedInfo() != null && hero.GetExtendedInfo().HasKnownEnchantmentBlueprint(enchantmentID);
        }

        public static bool HasKnownLore(this Hero hero, string loreID)
        {
            if (hero.GetExtendedInfo() != null)
            {
                return hero.GetExtendedInfo().HasKnownLore(loreID);
            }
            else return false;
        }

        public static int GetKnownLoreCount(this Hero hero)
        {
            if (hero.GetExtendedInfo() != null)
            {
                return hero.GetExtendedInfo().GetKnownLoreCount();
            }

            return 0;
        }

        public static bool IsSpellCaster(this Hero hero)
        {
            return hero.HasAttribute("SpellCaster");
        }

        public static bool IsAbilityUser(this Hero hero)
        {
            return hero.HasAttribute("AbilityUser");
        }

        public static bool IsNecromancer(this Hero hero)
        {
            return hero.HasAttribute("Necromancer") || hero.HasKnownLore("Necromancy");
        }

        public static bool IsSpellSinger(this Hero hero)
        {
            return hero.Culture.StringId == TORConstants.Cultures.ASRAI && hero.HasAttribute("SpellCaster");
        }

        public static bool IsUndead(this Hero hero)
        {
            return hero.HasAttribute("Undead");
        }

        public static bool IsTreeSpirit(this Hero hero)
        {
            return hero.HasAttribute("TreeSpirit");
        }

        public static bool IsChaos(this Hero hero)
        {
            if (hero.CharacterObject.IsCultist()) return true;
            if (hero.CharacterObject.IsBeastman()) return true;
            return false;
        }
        public static bool IsOrc(this Hero hero)
        {
            return hero.CharacterObject.Race == FaceGen.GetRaceOrDefault("orc");
        }
        public static bool IsGoblin(this Hero hero)
        {
            return hero.CharacterObject.Race == FaceGen.GetRaceOrDefault("goblin");
        }
        public static bool IsDwarf(this Hero hero)
        {
            return hero.CharacterObject.Race == FaceGen.GetRaceOrDefault("dwarf");
        }

        public static bool IsVampire(this Hero hero)
        {
            return hero.CharacterObject.Race == FaceGen.GetRaceOrDefault("vampire") || hero.CharacterObject.Race == FaceGen.GetRaceOrDefault("necrarch");
        }

        public static bool IsPriest(this Hero hero)
        {
            if (hero?.GetExtendedInfo() is not HeroExtendedInfo info) return false;
            var heroAttributes = info.AllAttributes;
            var priestAttributes = new[]{
                "Priest",
                "PriestLady",
                "PriestSigmar",
                "PriestUlric",
                "PriestShallya"};

            foreach (var attribute in priestAttributes)
            {
                if (heroAttributes.Contains(attribute)) return true;
            }

            return false;
        }

        public static bool IsAICompanion(this Hero hero)
        {
            return hero.HasAttribute("AICompanion") && hero.Occupation == Occupation.Special;
        }

        public static bool IsBountyMaster(this Hero hero)
        {
            var behavior = Campaign.Current.GetCampaignBehavior<BountyMasterCampaignBehavior>();
            if (behavior != null)
            {
                return behavior.IsBountyMaster(hero);
            }
            else return false;
        }

        public static bool IsSpellTrainer(this Hero hero)
        {
            var behavior = Campaign.Current.GetCampaignBehavior<SpellTrainerInTownBehavior>();
            if (behavior != null)
            {
                return behavior.IsSpellTrainer(hero);
            }

            return false;
        }

        public static bool IsEnchanter(this Hero hero)
        {
            var behavior = Campaign.Current.GetCampaignBehavior<EnchanterTownBehavior>();
            if (behavior != null)
            {
                return behavior.IsEnchanter(hero);
            }
            else return false;
        }

        public static bool IsImperialMagister(this Hero hero)
        {
            return hero.Culture.StringId == TORConstants.Cultures.EMPIRE && hero.IsSpellCaster(); //This will need to use an attribute if empire gets other spellcasters besides magisters
        }

        public static bool IsBretonnianKnight(this Hero hero)
        {
            return hero.Culture.StringId == TORConstants.Cultures.BRETONNIA && hero.HasAttribute("BrettonianKnight");
        }

        /// <remarks>
        /// Checks for the template id rather than the name of the hero since other languages will have the name localized to something we don't want to bother predicting.
        /// Because he's a special hero, he's built from a template, therefore we check the template's id rather than the hero's id which would be something like CharacterObject_xxxx and vary between campaigns based on heroes spawned.
        /// </remarks>
        public static bool IsMasterEngineer(this Hero hero)
        {
            if (hero != null && hero.Template != null)
                return hero.Occupation == Occupation.Special && hero.Template.StringId.Contains("nulnengineernpc");
            return false;
        }


        public static bool HasCareerChoice(this Hero hero, string choiceID)
        {
            bool result = false;
            if (hero?.GetExtendedInfo() is HeroExtendedInfo extendedInfo && hero.HasAnyCareer())
            {
                return extendedInfo.CareerChoices.Contains(choiceID);
            }
            return result;
        }

        public static bool HasCareerChoice(this Hero hero, CareerChoiceObject choice)
        {
            bool result = false;
            if (hero?.GetExtendedInfo() is HeroExtendedInfo extendedInfo)
            {
                return extendedInfo.CareerChoices.Contains(choice.StringId);
            }
            return result;
        }

        public static bool TryAddCareerChoice(this Hero hero, CareerChoiceObject choice)
        {
            if (hero != null)
            {
                var info = hero.GetExtendedInfo();
                if (info != null && !info.CareerChoices.Contains(choice.StringId))
                {
                    int maxChoices = Math.Min(hero.Level + 1, TORConfig.MaximumNumberOfCareerPerkPoints + 1);
                    if (info.CareerChoices.Count < maxChoices)
                    {
                        info.CareerChoices.Add(choice.StringId);
                        return true;
                    }
                }
            }
            return false;
        }

        public static List<string> GetAllCareerChoices(this Hero hero)
        {
            if (!hero.HasAnyCareer())
                return [];

            return hero.GetExtendedInfo().CareerChoices;
        }

        public static bool TryRemoveCareerChoice(this Hero hero, CareerChoiceObject choice)
        {
            var info = hero.GetExtendedInfo();
            if (hero != null && info != null)
            {
                if (info.CareerChoices.Contains(choice.StringId))
                {
                    info.CareerChoices.Remove(choice.StringId);
                    return true;
                }
            }
            return false;
        }

        public static bool HasCareer(this Hero hero, CareerObject career)
        {
            bool result = false;
            if (hero?.GetExtendedInfo() is HeroExtendedInfo heroInfo)
            {
                return heroInfo.CareerID == career.StringId;
            }
            return result;
        }

        public static bool HasAnyCareer(this Hero hero) => Game.Current.GameType is Campaign && hero.GetCareer() != null;

        public static CareerObject GetCareer(this Hero hero)
        {
            CareerObject result = null;
            if (hero?.GetExtendedInfo() is HeroExtendedInfo heroInfo && !string.IsNullOrEmpty(heroInfo.CareerID))
            {
                result = TORCareers.All.FirstOrDefault(x => x.StringId == heroInfo.CareerID);
            }
            return result;
        }

        public static void AddCareer(this Hero hero, CareerObject career)
        {
            if (hero != null)
            {
                var info = hero.GetExtendedInfo();
                if (info != null)
                {
                    if (hero.HasAnyCareer())
                    {
                        info.CareerChoices.Clear();
                    }

                    info.CareerID = career.StringId;
                    info.CareerChoices.Add(career.RootNode.StringId);
                    var careerObj = TORCareerChoices.Instance.GetCareerChoices(hero.GetCareer());
                    RemoveAttribute(hero, "CareerTier" + 1);
                    RemoveAttribute(hero, "CareerTier" + 2);
                    RemoveAttribute(hero, "CareerTier" + 3);
                    careerObj.InitialCareerSetup();
                }
            }
        }

        public static bool HasUnlockedCareerChoiceTier(this Hero hero, int tier)
        {
            var tierText = "CareerTier";
            if (hero.HasAnyCareer() && hero.HasAttribute(tierText + tier)) return true;

            return false;
        }

        /// <summary>
        /// Access item objects from the equipment of the character
        /// Equipment Indexes can define the Range. Note that horses are not a valid item object to be accessed
        /// </summary>
        public static List<ItemObject> GetHeroEquipment(this Hero hero,
            EquipmentIndex BeginningFrom = EquipmentIndex.Weapon0, EquipmentIndex EndingAt = EquipmentIndex.ArmorItemEndSlot, bool civilian = true)
        {
            int index = (int)BeginningFrom;
            int end = (int)EndingAt;
            List<ItemObject> CharacterEquipmentItems = new List<ItemObject>();
            for (int i = index; i <= end; i++)
            {
                var equipment = hero.BattleEquipment;
                if (equipment != null)
                {
                    CharacterEquipmentItems.Add(hero.BattleEquipment[i].Item);
                }

                if (civilian && hero.CivilianEquipment != null)
                {
                    CharacterEquipmentItems.Add(hero.CivilianEquipment[i].Item);
                }
            }


            return CharacterEquipmentItems;
        }

        public static bool HasAnyReligion(this Hero hero) => hero.GetDominantReligion() != null;

        public static ReligionObject GetDominantReligion(this Hero hero) => hero?.GetExtendedInfo()?.DominantReligion;

        public static DevotionLevel GetDevotionLevelForReligion(this Hero hero, ReligionObject religion)
        {
            if (religion == null) return DevotionLevel.None;
            var info = hero.GetExtendedInfo();
            int value = 0;
            if ((bool)(info?.ReligionDevotionLevels?.TryGetValue(religion.StringId, out value)))
            {
                if (value <= 0) return DevotionLevel.None;
                else if (value < TORConstants.DEVOTED_TRESHOLD) return DevotionLevel.Follower;
                else if (value < TORConstants.FANATIC_TRESHOLD) return DevotionLevel.Devoted;
                else return DevotionLevel.Fanatic;
            }
            else return DevotionLevel.None;
        }

        public static void AddReligiousInfluence(this Hero hero, ReligionObject religion, int amount, bool shouldNotify = true)
        {
            var info = hero.GetExtendedInfo();
            if (info != null)
            {
                var copy = info.ReligionDevotionLevels.ToDictionary(x => x.Key, x => x.Value);
                Dictionary<string, DevotionLevel> originalDevotionLevels = [];
                foreach (var ro in ReligionObject.All)
                {
                    if (copy.ContainsKey(ro.StringId))
                    {
                        if (copy[ro.StringId] <= 0) originalDevotionLevels.Add(ro.StringId, DevotionLevel.None);
                        else if (copy[ro.StringId] < TORConstants.DEVOTED_TRESHOLD) originalDevotionLevels.Add(ro.StringId, DevotionLevel.Follower);
                        else if (copy[ro.StringId] < TORConstants.FANATIC_TRESHOLD) originalDevotionLevels.Add(ro.StringId, DevotionLevel.Devoted);
                        else originalDevotionLevels.Add(ro.StringId, DevotionLevel.Fanatic);
                    }
                    else originalDevotionLevels.Add(ro.StringId, DevotionLevel.None);
                }

                int sumTotalDevotion = info.ReligionDevotionLevels.Sum(x => x.Value);
                if (sumTotalDevotion + amount <= TORConstants.MAXIMUM_DEVOTION_LEVEL)
                {
                    // Under cap: simply add the amount
                    if (info.ReligionDevotionLevels.ContainsKey(religion.StringId))
                    {
                        info.ReligionDevotionLevels[religion.StringId] += amount;
                    }
                    else
                    {
                        info.ReligionDevotionLevels.Add(religion.StringId, amount);
                    }
                }
                else
                {
                    // At/over cap: transfer devotion from other religions
                    // Calculate how much we need to take from others
                    var spaceNeeded = amount - (TORConstants.MAXIMUM_DEVOTION_LEVEL - sumTotalDevotion);

                    // Get total devotion in OTHER religions (excluding target)
                    var otherReligionsDevotion = info.ReligionDevotionLevels
                        .Where(x => x.Key != religion.StringId)
                        .Sum(x => x.Value);

                    // Cap the transfer to what's available from others
                    var transferAmount = Math.Min(spaceNeeded, otherReligionsDevotion);

                    // Subtract proportionally from other religions
                    if (transferAmount > 0 && otherReligionsDevotion > 0)
                    {
                        foreach (var entry in info.ReligionDevotionLevels.ToDictionary(x => x.Key, x => x.Value))
                        {
                            if (entry.Key != religion.StringId)
                            {
                                // Calculate proportional reduction
                                var proportion = (float)entry.Value / otherReligionsDevotion;
                                var reduction = (int)Math.Ceiling(transferAmount * proportion);
                                info.ReligionDevotionLevels[entry.Key] = Math.Max(0, entry.Value - reduction);
                            }
                        }
                    }

                    // Add to target religion (capped at max)
                    if (info.ReligionDevotionLevels.ContainsKey(religion.StringId))
                    {
                        info.ReligionDevotionLevels[religion.StringId] = Math.Min(
                            TORConstants.MAXIMUM_DEVOTION_LEVEL,
                            info.ReligionDevotionLevels[religion.StringId] + amount);
                    }
                    else
                    {
                        info.ReligionDevotionLevels.Add(religion.StringId, Math.Min(amount, TORConstants.MAXIMUM_DEVOTION_LEVEL));
                    }

                    // Clean up any religions that dropped to 0
                    var toRemove = info.ReligionDevotionLevels.Where(x => x.Value <= 0).Select(x => x.Key).ToList();
                    foreach (var key in toRemove)
                    {
                        info.ReligionDevotionLevels.Remove(key);
                    }
                }

                Dictionary<string, DevotionLevel> newDevotionLevels = [];
                foreach (var ro in ReligionObject.All)
                {
                    if (info.ReligionDevotionLevels.ContainsKey(ro.StringId))
                    {
                        if (info.ReligionDevotionLevels[ro.StringId] <= 0) newDevotionLevels.Add(ro.StringId, DevotionLevel.None);
                        else if (info.ReligionDevotionLevels[ro.StringId] < TORConstants.DEVOTED_TRESHOLD) newDevotionLevels.Add(ro.StringId, DevotionLevel.Follower);
                        else if (info.ReligionDevotionLevels[ro.StringId] < TORConstants.FANATIC_TRESHOLD) newDevotionLevels.Add(ro.StringId, DevotionLevel.Devoted);
                        else newDevotionLevels.Add(ro.StringId, DevotionLevel.Fanatic);
                    }
                    else newDevotionLevels.Add(ro.StringId, DevotionLevel.None);
                }

                foreach (var entry in originalDevotionLevels)
                {
                    if (newDevotionLevels.ContainsKey(entry.Key))
                    {
                        if (newDevotionLevels[entry.Key] != originalDevotionLevels[entry.Key] && shouldNotify)
                            TORCampaignEvents.Instance.OnDevotionLevelChanged(hero, ReligionObject.All.FirstOrDefault(x => x.StringId == entry.Key), originalDevotionLevels[entry.Key], newDevotionLevels[entry.Key]);
                    }
                }
            }
        }

        public static string GetInfoKey(this Hero hero)
        {
            return hero.StringId;
        }

        public static int GetLastPrayerTime(this Hero hero)
        {
            var behavior = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>();
            return behavior?.LastPrayerTime(hero) ?? 0;
        }
    }
}