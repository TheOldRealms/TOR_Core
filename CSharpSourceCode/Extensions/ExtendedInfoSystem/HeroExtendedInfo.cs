using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Ink.Parsed;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    public class HeroExtendedInfo
    {
        [SaveableField(0)] public List<string> AcquiredAbilities = new List<string>();
        [SaveableField(1)] public List<string> AcquiredAttributes = new List<string>();
        [SaveableField(2)] public Dictionary<string, float> CustomResources = new Dictionary<string, float>();
        [SaveableField(3)] public Dictionary<string, int> ReligionDevotionLevels = new Dictionary<string, int>();
        [SaveableField(4)] public SpellCastingLevel SpellCastingLevel = SpellCastingLevel.None;
        [SaveableField(5)] private CharacterObject _baseCharacter;
        [SaveableField(6)] private List<string> _knownLores = new List<string>();
        [SaveableField(7)] private List<string> _selectedAbilities = new List<string>();
        [SaveableField(8)] public string CareerID = string.Empty;
        [SaveableField(9)] public List<string> CareerChoices = new List<string>();

        public CharacterObject BaseCharacter => _baseCharacter;

        public void AddCustomResource(string id, float amount)
        {
            if (!CustomResourceManager.DoesResourceObjectExist(id)) return;
            if (CustomResources.ContainsKey(id))
            {
                CustomResources[id] = Math.Max(0, CustomResources[id] + amount);
                if(id == "WindsOfMagic")
                {
                    CustomResources[id] = Math.Min(MaxWindsOfMagic, CustomResources[id]);
                }
                else CustomResources[id] = Math.Min(TORConfig.MaximumCustomResourceValue, CustomResources[id]);
            }
            else
            {
                CustomResources.Add(id, amount);
                if (id == "WindsOfMagic")
                {
                    CustomResources[id] = Math.Min(MaxWindsOfMagic, CustomResources[id]);
                }
                else CustomResources[id] = Math.Min(TORConfig.MaximumCustomResourceValue, CustomResources[id]);
            }
        }

        public void SetCustomResourceValue(string id, float amount)
        {
            if (!CustomResourceManager.DoesResourceObjectExist(id)) return;
            if (CustomResources.ContainsKey(id))
            {
                CustomResources[id] = Math.Max(0, amount);
                if (id == "WindsOfMagic")
                {
                    CustomResources[id] = Math.Min(MaxWindsOfMagic, CustomResources[id]);
                }
                else CustomResources[id] = Math.Min(TORConfig.MaximumCustomResourceValue, CustomResources[id]);
            }
            else
            {
                CustomResources.Add(id, amount);
                if (id == "WindsOfMagic")
                {
                    CustomResources[id] = Math.Min(MaxWindsOfMagic, CustomResources[id]);
                }
                else CustomResources[id] = Math.Min(TORConfig.MaximumCustomResourceValue, CustomResources[id]);
            }
        }

        public float GetCustomResourceValue(string id)
        {
            if (CustomResources.ContainsKey(id))
            {
                return CustomResources[id];
            }
            else return 0;
        }

        public Dictionary<CustomResource, float> GetCustomResources()
        {
            return CustomResources.ToDictionary(x => CustomResourceManager.GetResourceObject(x.Key), x => x.Value);
        }

        public float MaxWindsOfMagic
        {
            get
            {
                if (!(Game.Current.GameType is Campaign)) return 50;
                else
                {
                    if (BaseCharacter.HeroObject != null && BaseCharacter.HeroObject != Hero.MainHero && BaseCharacter.HeroObject.Occupation == Occupation.Lord && BaseCharacter.HeroObject.IsSpellCaster()) return 100f;
                    ExplainedNumber explainedNumber = new ExplainedNumber(10f, false, null);
                    SkillHelper.AddSkillBonusForCharacter(TORSkills.SpellCraft, TORSkillEffects.MaxWinds, BaseCharacter, ref explainedNumber);
                    if (Hero.MainHero.HasAnyCareer())
                    {
                        if (BaseCharacter.HeroObject == Hero.MainHero)
                        {
                            CareerHelper.ApplyBasicCareerPassives(Hero.MainHero,ref  explainedNumber, PassiveEffectType.WindsOfMagic,false);
                            
                            if (CareerChoices.Contains("DarkVisionPassive4"))
                            {
                                var spellCount = Hero.MainHero.GetExtendedInfo().AcquiredAbilities.Count - 1;
                                var choice = TORCareerChoices.GetChoice("DarkVisionPassive4");
                                explainedNumber.Add(choice.GetPassiveValue() * spellCount);
                            }

                            if (CareerChoices.Contains("DiscipleOfAccursedPassive4"))
                            {
                                var characterEquipment = Hero.MainHero.CharacterObject.GetCharacterEquipment();
                                foreach (var item in characterEquipment)
                                {
                                    var choice = TORCareerChoices.GetChoice("DiscipleOfAccursedPassive4");
                                    if (item.IsMagicalItem())
                                    {
                                        explainedNumber.Add(choice.GetPassiveValue());
                                    } 
                                }
                            }
                        }
                        else if (BaseCharacter.HeroObject.PartyBelongedTo!=null && BaseCharacter.HeroObject.PartyBelongedTo.IsMainParty)
                        {
                            if (Hero.MainHero != null)
                            {
                                var choices = Hero.MainHero.GetAllCareerChoices();
                                if (choices.Contains("EnvoyOfTheLadyPassive3"))
                                {
                                    var choice = TORCareerChoices.GetChoice("EnvoyOfTheLadyPassive3");
                                    explainedNumber.Add(choice.GetPassiveValue());
                                }
                            
                                if (choices.Contains("LieOfLadyPassive2"))
                                {
                                    var choice = TORCareerChoices.GetChoice("LieOfLadyPassive2");
                                    explainedNumber.Add(choice.GetPassiveValue());
                                }
                            }
                            
                            if (CareerChoices.Contains("LieOfLadyPassive2"))
                            {
                                var choice = TORCareerChoices.GetChoice("LieOfLadyPassive2");
                                explainedNumber.Add(choice.GetPassiveValue());
                            }
                            
                            if (CareerChoices.Contains("WellspringOfDharPassive3"))
                            {
                                var choice = TORCareerChoices.GetChoice("WellspringOfDharPassive3");
                                explainedNumber.Add(choice.GetPassiveValue());
                            }
                        }
                    }
                        
                    
                  
                    return explainedNumber.ResultNumber;
                }
            }
        }
        public float WindsOfMagicRechargeRate
        {
            get
            {
                if (!(Game.Current.GameType is Campaign)) return 0.2f;
                else
                {
                    if (BaseCharacter.HeroObject != null && BaseCharacter.HeroObject != Hero.MainHero && BaseCharacter.HeroObject.Occupation == Occupation.Lord && BaseCharacter.HeroObject.IsSpellCaster()) return 2f;
                    ExplainedNumber explainedNumber = new ExplainedNumber(1f, false, null);
                    SkillHelper.AddSkillBonusForCharacter(TORSkills.SpellCraft, TORSkillEffects.WindsRechargeRate, BaseCharacter, ref explainedNumber);

                    if ( BaseCharacter.HeroObject != null&& BaseCharacter.HeroObject.PartyBelongedTo!=null&&  BaseCharacter.HeroObject.PartyBelongedTo.IsMainParty )
                    {
                        CareerHelper.ApplyBasicCareerPassives(BaseCharacter.HeroObject, ref explainedNumber, PassiveEffectType.WindsRegeneration, false);
                    }
                    
                    return explainedNumber.ResultNumber;
                }
            }
        }

        public List<LoreObject> KnownLores
        {
            get
            {
                List<LoreObject> list = new List<LoreObject>();
                EnsureKnownLores();
                foreach (var item in _knownLores)
                {
                    list.Add(LoreObject.GetLore(item));
                }
                return list;
            }
        }

        public List<string> AllAbilites
        {
            get
            {
                var list = new List<string>();
                if (_baseCharacter != null)
                {
                    list.AddRange(_baseCharacter.GetAbilities());
                    if (list.Count <= 0 && _baseCharacter.OriginalCharacter != null && _baseCharacter.OriginalCharacter.IsTemplate)
                    {
                        list.AddRange(_baseCharacter.OriginalCharacter.GetAbilities());
                    }
                }
                list.AddRange(AcquiredAbilities);
                
                return list;
            }
        }

        public List<string> AllAttributes
        {
            get
            {
                var list = new List<string>();
                if (_baseCharacter != null)
                {
                    list.AddRange(_baseCharacter.GetAttributes());
                    if (list.Count <= 0 && _baseCharacter.OriginalCharacter != null && _baseCharacter.OriginalCharacter.IsTemplate)
                    {
                        list.AddRange(_baseCharacter.OriginalCharacter.GetAttributes());
                    }
                }
                list.AddRange(AcquiredAttributes);
                return list;
            }
        }

        public List<string> SelectedAbilities
        {
            get
            {
                if (_selectedAbilities.Count > 0) return _selectedAbilities;
                else return AllAbilites;
            }
        }

        public List<string> GetAllPrayers()
        {
            var list = new List<string>();
            foreach (var ability in AllAbilites)
            {
                if(list.Contains(ability)) continue;    //shouldn't happen, yet better save then sorry
                
                var t  = AbilityFactory.GetTemplate(ability);
                if (t!=null&&t.AbilityType == AbilityType.Prayer)
                {
                    list.Add(ability);
                }
            }

            return list;
        }

        public ReligionObject DominantReligion
        {
            get
            {
                if(ReligionDevotionLevels.Count == 0 || ReligionDevotionLevels.Values.Sum() == 0) return null;
                var dominantTuple = ReligionDevotionLevels.MaxBy(x => x.Value);
                return MBObjectManager.Instance.GetObject<ReligionObject>(dominantTuple.Key);
            }
        }

        public void AddSelectedAbility(string abilityId)
        {
            if(!_selectedAbilities.Contains(abilityId)) _selectedAbilities.Add(abilityId);
        }

        public void RemoveSelectedAbility(string abilityId)
        {
            if (_selectedAbilities.Contains(abilityId)) _selectedAbilities.Remove(abilityId);
        }

        public void ToggleSelectedAbility(string abilityId)
        {
            if (IsAbilitySelected(abilityId)) RemoveSelectedAbility(abilityId);
            else if(AllAbilites.Contains(abilityId)) AddSelectedAbility(abilityId);
        }

        public bool IsAbilitySelected(string abilityId)
        {
            return _selectedAbilities.Contains(abilityId);
        }

        public HeroExtendedInfo(CharacterObject character)
        {
            _baseCharacter = character;
        }

        public void AddKnownLore(string loreId)
        {
            if (LoreObject.GetLore(loreId) != null && !_knownLores.Contains(loreId)) _knownLores.Add(loreId);
        }

        public void RemoveAbility(string abilityID)
        {
            if (AcquiredAbilities.Contains(abilityID))
            {
                AcquiredAbilities.Remove(abilityID);
            }
        }
        public void RemoveAllPrayers()
        {
            var prayers = AllAbilites.Where(x => AbilityFactory.GetTemplate(x).AbilityType == AbilityType.Prayer);


            foreach (var prayer in prayers)
            {
                AcquiredAbilities.Remove(prayer);
            }
        }
        
        public void RemoveKnownLore(string loreId)
        {
            if (LoreObject.GetLore(loreId) != null && _knownLores.Contains(loreId))
            {
                foreach (var abilityID in AllAbilites)
                {
                    var ability = AbilityFactory.GetTemplate(abilityID);
                    if (ability.BelongsToLoreID!=loreId)continue;
                    _selectedAbilities.Remove(abilityID);
                }
                _knownLores.Remove(loreId);
            }
        }
        
        public void RemoveAllSpells()
        {
            var allSpells = AbilityFactory.GetAllSpellNamesAsList();

            foreach (var spell in allSpells)
            {
                AcquiredAbilities.Remove(spell);
            }

        }

        public bool HasAnyKnownLore()
        {
            return !_knownLores.IsEmpty();
        }
        public bool HasKnownLore(string loreId)
        {
            return _knownLores.Contains(loreId);
        }

        private void EnsureKnownLores()
        {
            List<AbilityTemplate> list = new List<AbilityTemplate>();
            foreach(var abilityId in AllAbilites)
            {
                var ability = AbilityFactory.GetTemplate(abilityId);
                if (ability != null && ability.IsSpell) list.Add(ability);
            }
            foreach(var item in list)
            {
                if (!HasKnownLore(item.BelongsToLoreID)) AddKnownLore(item.BelongsToLoreID);
            }
        }
    }
}
