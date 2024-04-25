using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.BountyMaster;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CampaignMechanics.SpellTrainers;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Models;
using TOR_Core.Utilities;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace TOR_Core.Extensions
{
    public static class HeroExtensions
    {
        public static bool CanRaiseDead(this Hero hero)
        {
            return hero.PartyBelongedTo != null && hero.PartyBelongedTo.GetMemberHeroes().Any(x => x.IsNecromancer());
            //return hero.IsHumanPlayerCharacter && hero.IsNecromancer();
        }

        /// <summary>
        /// Calculates the Raise dead chance based on the Spellcraft Skillvalue and applies Career Perks.
        /// 0.005 would allow with 200 spell craft an 80% chance of raising dead.
        /// </summary>
        /// <param name="hero"></param>
        /// <returns></returns>
        public static float GetRaiseDeadChance(this Hero hero)
        {
            if (!hero.IsNecromancer()) return 0f;
            
            var chance = new ExplainedNumber();
            var skillValue = hero.GetSkillValue(TORSkills.SpellCraft);

            var chanceValue = Mathf.Clamp(skillValue * 0.005f, 0.05f, 0.7f);
           chance.Add(chanceValue);

            if (hero.HasAnyCareer())
            {
                var choices = hero.GetAllCareerChoices();

                if (choices.Contains("MasterOfDeadPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("MasterOfDeadPassive3");
                    if(choice!=null)
                        chance.AddFactor(choice.GetPassiveValue());
                }
            }

            return chance.ResultNumber;
        }

        public static void AddCustomResource(this Hero hero, string id, float amount)
        {
            var info = hero.GetExtendedInfo();
            if (info != null)
            {
                info.AddCustomResource(id, amount);
            }
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

        public static float GetCultureSpecificCustomResourceValue(this Hero hero)
        {
            if (hero.GetCultureSpecificCustomResource() != null)
            {
                return hero.GetCustomResourceValue(hero.GetCultureSpecificCustomResource().StringId);
            }
            else return 0;
        }
        
        public static ExplainedNumber GetCultureSpecificCustomResourceChange(this Hero hero)
        {
            if (hero.PartyBelongedTo == null) return new ExplainedNumber();

            var number = new ExplainedNumber(0,true);
            if (hero.GetCultureSpecificCustomResource() != null)
            {
                var upkeep =  GetCalculatedCustomResourceUpkeep(hero, hero.GetCultureSpecificCustomResource().StringId);
                
                if (upkeep.ResultNumber < 0)
                {
                    foreach (var line in upkeep.GetLines())
                    {
                        number.Add((int)line.number, new TextObject(line.name));
                    }
                }

                if (hero == Hero.MainHero)
                {
                    CareerHelper.ApplyBasicCareerPassives(Hero.MainHero, ref number,PassiveEffectType.CustomResourceGain, false); 
                }

                if (hero.HasCareer(TORCareers.BlackGrailKnight)&& hero.HasCareerChoice("BlackGrailVowPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("BlackGrailVowPassive4");
                    if (hero.PartyBelongedTo != null)
                    {
                        var heroes = hero.PartyBelongedTo.GetMemberHeroes();
                        heroes.Remove(Hero.MainHero);

                        foreach (var companion in heroes)
                        {
                            if (companion.IsVampire() || companion.IsNecromancer())
                            {
                                number.Add(choice.GetPassiveValue(),choice.BelongsToGroup.Name);
                            }
                        }
                    }
                }

                if (hero.HasCareer(TORCareers.GrailKnight)&&hero.HasCareerChoice("QuestingVowPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("QuestingVowPassive4");
                    var heroes = hero.PartyBelongedTo.GetMemberHeroes();
                    heroes.Remove(Hero.MainHero);
                    foreach (var companion in heroes)
                    {
                        if (companion.IsBretonnianKnight())
                        {
                            number.Add(choice.GetPassiveValue(),choice.BelongsToGroup.Name);
                        }
                    }
                }

                if (hero.HasCareer(TORCareers.Necrarch) && hero.HasCareerChoice("EverlingsSecretPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("EverlingsSecretPassive3");
                    if (choice!=null)
                    {
                        if (hero.GetExtendedInfo().MaxWindsOfMagic <= hero.GetCustomResourceValue("WindsOfMagic"))
                        {
                            number.Add(hero.GetExtendedInfo().WindsOfMagicRechargeRate * CampaignTime.HoursInDay, choice.BelongsToGroup.Name);
                        }
                    }
                    
                }

                if (hero.Culture.StringId == "vlandia")
                {
                    if (hero.PartyBelongedTo != null)
                    {
                        if (hero.PartyBelongedTo.HasBlessing("cult_of_lady"))
                        {
                            var obj = ReligionObject.All.FirstOrDefault( x=>x.StringId=="cult_of_lady");
                            if (obj != null)
                            {
                                
                                number.Add(15,new TextObject("Blessing of the Lady"));
                            }
                            
                        }
                    }

                    if (hero.IsClanLeader)
                    {
                        foreach (var clanmember in hero.Clan.Heroes)
                        {
                            if(clanmember==hero) continue;

                            if (clanmember.IsAlive&&clanmember.IsPartyLeader)
                            {
                                number.Add(2,new TextObject("Clan members with Party"));
                            }
                        }
                    }

                    if (hero.GetChivalryLevel() == ChivalryLevel.PureHearted)
                    {
                        number.Add(5,new TextObject(ChivalryLevel.PureHearted.ToString()));
                    }
                    
                    if (hero.GetChivalryLevel() == ChivalryLevel.TrueChivalrous)
                    {
                        number.Add(15,new TextObject(ChivalryLevel.PureHearted.ToString()));
                    }
                    
                }
            } 
            return number;
        }

        public static ExplainedNumber GetCalculatedCustomResourceUpkeep(this Hero hero, string resourceID="")
        {
            if (resourceID == "")
            {
                resourceID = hero.GetCultureSpecificCustomResource().StringId;
            }
            var upkeep = new ExplainedNumber(0,true,new TextObject("Upkeep"));
            foreach (var element in hero.PartyBelongedTo.MemberRoster.GetTroopRoster())
            {
                if (element.Character.HasCustomResourceUpkeepRequirement())
                {
                    var resource = element.Character.GetCustomResourceRequiredForUpkeep();
                
                    if(resource.Item1.StringId !=resourceID) continue;
                    var unitUpkeet = new ExplainedNumber(resource.Item2*element.Number);
                    if (hero == Hero.MainHero)
                    {
                        CareerHelper.ApplyBasicCareerPassives(Hero.MainHero, ref unitUpkeet,PassiveEffectType.CustomResourceUpkeepModifier, true, element.Character); 
                    }
                    
                    upkeep.Add(-unitUpkeet.ResultNumber,new TextObject("Upkeep"));
                    
                }
            }

            foreach (var settlement in hero.Clan.Settlements)
            {
                if (!settlement.IsCastle && !settlement.IsTown)
                {
                    continue;
                }
                if(settlement.Town.GarrisonParty==null) continue;
                var garrison = settlement.Town.GarrisonParty.MemberRoster.GetTroopRoster();
                foreach (var elem in garrison)
                {
                    if (elem.Character.HasCustomResourceUpgradeRequirement())
                    {
                        var resource = elem.Character.GetCustomResourceRequiredForUpkeep();
                        
                        if(resource==null) continue;
                
                        if(resource.Item1.StringId !=resourceID) continue;
                        var garrisonFactor = 0.25f; //base reduction bonus 
                        var garrisonUnitUpkeep = new ExplainedNumber(resource.Item2*elem.Number*garrisonFactor);
                        if (hero == Hero.MainHero)
                        {
                            CareerHelper.ApplyBasicCareerPassives(Hero.MainHero, ref garrisonUnitUpkeep,PassiveEffectType.CustomResourceUpkeepModifier, true, elem.Character); 
                        }
                    
                        upkeep.Add(-garrisonUnitUpkeep.ResultNumber,new TextObject("Garrison Upkeep"));
                    }
                }
            }
            
            return upkeep;
        }

        public static void AddCultureSpecificCustomResource(this Hero hero, float amount)
        {
            if(hero.GetCultureSpecificCustomResource() != null) hero.AddCustomResource(hero.GetCultureSpecificCustomResource().StringId, amount);
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
            if(info != null)
            {
                info.AddCustomResource("WindsOfMagic", amount);
            }
            
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
            if (hero.CanPlaceArtillery())
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
            if (info != null && !info.AllAbilites.Contains(ability))
            {
                info.AcquiredAbilities.Add(ability);
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
                return hero.GetExtendedInfo().AllAbilites.Contains(ability);
            }
            else return false;
        }

        public static ChivalryLevel GetChivalryLevel(this Hero hero)
        {
            var customResource = GetCustomResourceValue(hero, "Chivalry");
            return ChivalryHelper.GetChivalryLevelForResource(customResource);
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
            if (hero.GetExtendedInfo() != null)
            {
                hero.GetExtendedInfo().AddKnownLore(loreID);
            }
        }

        public static bool HasKnownLore(this Hero hero, string loreID)
        {
            if (hero.GetExtendedInfo() != null)
            {
                return hero.GetExtendedInfo().HasKnownLore(loreID);
            }
            else return false;
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
            return hero.HasAttribute("Necromancer");
        }

        public static bool IsUndead(this Hero hero)
        {
            return hero.HasAttribute("Undead");
        }

        public static bool IsChaos(this Hero hero)
        {
            if (hero.IsCultist()) return true;
            if (hero.CharacterObject.IsBeastman()) return true;
            return false;
        }

        public static bool IsVampire(this Hero hero)
        {
            return hero.CharacterObject.Race == FaceGen.GetRaceOrDefault("vampire");
        }

        public static bool IsPriest(this Hero hero)
        {
            return hero.HasAttribute("Priest");
        }

        public static bool IsCultist(this Hero hero)
        {
            return hero.CharacterObject.Race == FaceGen.GetRaceOrDefault("chaos_ud_cultist");
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
            else return false;
        }

        public static bool IsMasterEngineer(this Hero hero)
        {
            if(hero!=null)
                return hero.Occupation == Occupation.Special&& hero.Name.Contains("Master Engineer");
            return false;
        }
        
        public static bool IsZoologist(this Hero hero)
        {
            if(hero!=null)
                return hero.Occupation == Occupation.Special&& hero.Name.Contains("Zoologist");
            return false;
        }

        public static bool HasCareerChoice(this Hero hero, string choiceID)
        {
            bool result = false;
            if (hero != null && hero.GetExtendedInfo() != null&& hero.HasAnyCareer())
            {
                return hero.GetExtendedInfo().CareerChoices.Contains(choiceID);
            }
            return result;
        }

        public static bool HasCareerChoice(this Hero hero, CareerChoiceObject choice)
        {
            bool result = false;
            if (hero != null && hero.GetExtendedInfo() != null)
            {
                return hero.GetExtendedInfo().CareerChoices.Contains(choice.StringId);
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
                    int maxChoices = Math.Min(hero.Level+1, TORConfig.MaximumNumberOfCareerPerkPoints + 1);
                    if(info.CareerChoices.Count < maxChoices)
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
                return new List<string>();

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
            if (hero != null && hero.GetExtendedInfo() != null)
            {
                return hero.GetExtendedInfo().CareerID == career.StringId;
            }
            return result;
        }


        public static bool IsBretonnianKnight(this Hero hero)       //Potentially a cleaner way to check that
        {
            return !hero.IsSpellCaster() && hero.Culture.StringId == "vlandia";
        }

        public static bool HasAnyCareer(this Hero hero) => Game.Current.GameType is Campaign && hero.GetCareer() != null;

        public static CareerObject GetCareer(this Hero hero)
        {
           
            CareerObject result = null;
            if (hero != null && hero.GetExtendedInfo() != null && !string.IsNullOrEmpty(hero.GetExtendedInfo().CareerID))
            {
                result = TORCareers.All.FirstOrDefault(x=>x.StringId == hero.GetExtendedInfo().CareerID);
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
                    RemoveAttribute(hero,"CareerTier"+1);
                    RemoveAttribute(hero,"CareerTier"+2);
                    RemoveAttribute(hero,"CareerTier"+3);
                    careerObj.InitialCareerSetup();
                }
            }
        }

        public static bool HasUnlockedCareerChoiceTier(this Hero hero, int tier)
        {
            var tierText = "CareerTier";
            if(hero.HasAnyCareer()&& hero.HasAttribute(tierText + tier))return true;
            
            return false;
        }

        public static bool HasAnyReligion(this Hero hero) => hero.GetDominantReligion() != null;

        public static ReligionObject GetDominantReligion(this Hero hero) => hero.GetExtendedInfo() != null ? hero.GetExtendedInfo().DominantReligion : null;

        public static DevotionLevel GetDevotionLevelForReligion(this Hero hero, ReligionObject religion)
        {
            var info = hero.GetExtendedInfo();
            if (info == null || !info.ReligionDevotionLevels.ContainsKey(religion.StringId)) return DevotionLevel.None;
            else
            {
                var value = info.ReligionDevotionLevels[religion.StringId];
                if (value <= 0) return DevotionLevel.None;
                else if (value < TORConstants.DEVOTED_TRESHOLD) return DevotionLevel.Follower;
                else if (value < TORConstants.FANATIC_TRESHOLD) return DevotionLevel.Devoted;
                else return DevotionLevel.Fanatic;
            }
        }

        public static void AddReligiousInfluence(this Hero hero, ReligionObject religion, int amount, bool shouldNotify = true)
        {
            var info = hero.GetExtendedInfo();
            if(info != null)
            {
                var copy = info.ReligionDevotionLevels.ToDictionary(x => x.Key, x => x.Value);
                Dictionary<string, DevotionLevel> originalDevotionLevels = new Dictionary<string, DevotionLevel>();
                foreach(var ro in ReligionObject.All)
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
                if(sumTotalDevotion + amount <= TORConstants.MAXIMUM_DEVOTION_LEVEL)
                {
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
                    var newAmount = TORConstants.MAXIMUM_DEVOTION_LEVEL - sumTotalDevotion;
                    if (info.ReligionDevotionLevels.ContainsKey(religion.StringId))
                    {
                        foreach (var entry in info.ReligionDevotionLevels.ToDictionary(x => x.Key, x => x.Value))
                        {
                            if (entry.Key == religion.StringId)
                            {
                                info.ReligionDevotionLevels[entry.Key] += newAmount;
                            }
                            else info.ReligionDevotionLevels[entry.Key] -= newAmount;
                            info.ReligionDevotionLevels[entry.Key] = Math.Min(TORConstants.MAXIMUM_DEVOTION_LEVEL, info.ReligionDevotionLevels[entry.Key]);
                        }
                    }
                    else
                    {
                        foreach (var entry in info.ReligionDevotionLevels.ToDictionary(x => x.Key, x => x.Value))
                        {
                            info.ReligionDevotionLevels[entry.Key] -= newAmount;
                            
                        }
                        info.ReligionDevotionLevels.Add(religion.StringId, newAmount);
                    }
                }

                Dictionary<string, DevotionLevel> newDevotionLevels = new Dictionary<string, DevotionLevel>();
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

                foreach(var entry in originalDevotionLevels)
                {
                    if (newDevotionLevels.ContainsKey(entry.Key))
                    {
                        if (newDevotionLevels[entry.Key] != originalDevotionLevels[entry.Key] && shouldNotify) 
                            TORCampaignEvents.Instance.OnDevotionLevelChanged(hero, ReligionObject.All.FirstOrDefault(x=>x.StringId == entry.Key), originalDevotionLevels[entry.Key], newDevotionLevels[entry.Key]);
                    }
                }
            }
        }

        public static string GetInfoKey(this Hero hero)
        {
            return hero.StringId;
        }
    }
}
