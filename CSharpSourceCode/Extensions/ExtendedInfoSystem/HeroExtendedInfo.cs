using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.Career;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    public class HeroExtendedInfo
    {
        [SaveableField(0)] public List<string> AcquiredAbilities = new List<string>();
        [SaveableField(1)] public List<string> AcquiredAttributes = new List<string>();
        [SaveableField(2)] public float CurrentWindsOfMagic = 0;
        [SaveableField(3)] public int Corruption = 0; //between 0 and 100, 0 = pure af, 100 = fallen to chaos
        [SaveableField(4)] public SpellCastingLevel SpellCastingLevel = SpellCastingLevel.None;
        [SaveableField(5)] private CharacterObject _baseCharacter;
        [SaveableField(6)] private List<string> _knownLores = new List<string>();
        [SaveableField(7)] private List<string> _selectedAbilities = new List<string>();
        [SaveableField(8)] public CareerId _AcquiredCareer = CareerId.None;
        [SaveableField(9)] public List<string> AcquiredAbilitiesTORSkillPointIds = new List<string>();
        [SaveableField(10)] public int AvailableTorSkillPoints;         //might not be completely important. Could be deducted from Player Level.
        public CharacterObject BaseCharacter => _baseCharacter;

        public float MaxWindsOfMagic
        {
            get
            {
                if (!(Game.Current.GameType is Campaign)) return 50;
                else
                {
                    if (BaseCharacter.HeroObject != null && BaseCharacter.HeroObject != Hero.MainHero && BaseCharacter.HeroObject.Occupation == Occupation.Lord && BaseCharacter.HeroObject.IsSpellCaster()) return 100f;
                    ExplainedNumber explainedNumber = new ExplainedNumber(10f, false, null);
                    
                    
                    
                    //TODO is not shown in the GUI
                    if (BaseCharacter.HeroObject != null && BaseCharacter.HeroObject == Campaign.Current.MainParty.LeaderHero)
                    {
                        var extraWind = Campaign.Current.GetCampaignBehavior<CareerCampaignBase>().GetExtraWindPoints();

                        if (extraWind > 0)
                        {
                            explainedNumber.Add(extraWind,new TextObject("Career Perks"));
                        }
                    }
                    
                    SkillHelper.AddSkillBonusForCharacter(TORSkills.SpellCraft, TORSkillEffects.MaxWinds, BaseCharacter, ref explainedNumber);
                  
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
        
        public List<string> AllCareerSkillPoints
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
                list.AddRange(AcquiredAbilitiesTORSkillPointIds);
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
    public class HeroExtendedInfoInfoDefiner : SaveableTypeDefiner
    {
        public HeroExtendedInfoInfoDefiner() : base(1_543_132) { }
        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(HeroExtendedInfo), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<string, HeroExtendedInfo>));
        }
    }
}
