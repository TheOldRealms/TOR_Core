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
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    public class HeroExtendedInfo(CharacterObject character)
    {
        [SaveableField(0)] public List<string> AcquiredAbilities = [];
        [SaveableField(1)] public List<string> AcquiredAttributes = [];
        [SaveableField(2)] public Dictionary<string, float> CustomResources = [];
        [SaveableField(3)] public Dictionary<string, int> ReligionDevotionLevels = [];
        [SaveableField(4)] public SpellCastingLevel SpellCastingLevel = SpellCastingLevel.None;
        [SaveableField(5)] private CharacterObject _baseCharacter = character;
        [SaveableField(6)] private List<string> _knownLores = [];
        [SaveableField(7)] private List<string> _selectedAbilities = [];
        [SaveableField(8)] public string CareerID = string.Empty;
        [SaveableField(9)] public List<string> CareerChoices = [];

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
                if (Game.Current.GameType is not Campaign) return 50;
                TORAbilityModel  model = Campaign.Current.Models.GetAbilityModel();
                return model.GetMaximumWindsOfMagic(this.BaseCharacter);
            }
        }

        public float WindsOfMagicRechargeRate
        {
            get
            {
                if (Game.Current.GameType is not Campaign) return 0.2f;
                else
                {
                    if (Game.Current.GameType is not Campaign) return 50;
                    TORAbilityModel  model = Campaign.Current.Models.GetAbilityModel();
                    return model.GetWindsRechargeRate(this.BaseCharacter);
                }
            }
        }

        public List<LoreObject> KnownLores
        {
            get
            {
                List<LoreObject> list = [];
                EnsureKnownLores();
                foreach (var item in _knownLores)
                {
                    list.Add(LoreObject.GetLore(item));
                }
                return list;
            }
        }

        public List<string> AllAbilities
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
                else return AllAbilities;
            }
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
            else if(AllAbilities.Contains(abilityId)) AddSelectedAbility(abilityId);
        }

        public bool IsAbilitySelected(string abilityId)
        {
            return _selectedAbilities.Contains(abilityId);
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
            var prayers = AllAbilities.Where(x => AbilityFactory.GetTemplate(x).AbilityType == AbilityType.Prayer);


            foreach (var prayer in prayers)
            {
                AcquiredAbilities.Remove(prayer);
            }
        }
        
        public void RemoveKnownLore(string loreId)
        {
            if (LoreObject.GetLore(loreId) != null && _knownLores.Contains(loreId))
            {
                foreach (var abilityID in AllAbilities)
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
        
        public int GetKnownLoreCount()
        {
            return _knownLores.Count;
        }

        private void EnsureKnownLores()
        {
            List<AbilityTemplate> list = [];
            foreach(var abilityId in AllAbilities)
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
