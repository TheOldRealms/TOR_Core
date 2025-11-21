using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CharacterCreation
{
    /// <summary>
    /// ViewModel for individual specialization option (lore, bloodline, or priesthood)
    /// </summary>
    public class SpecializationOptionVM : ViewModel
    {
        private string _name;
        private string _description;
        private string _positiveEffect;
        private string _negativeEffect;
        private bool _isSelected;
        private string _iconSprite;
        private object _data;
        private readonly Action<SpecializationOptionVM> _onSelect;

        public SpecializationOptionVM(string name, string description, object data, Action<SpecializationOptionVM> onSelect, string iconSprite = "",
            string positiveEffect = "", string negativeEffect = "")
        {
            _name = name;
            _description = description;
            _positiveEffect = positiveEffect ?? "";
            _negativeEffect = negativeEffect ?? "";
            _iconSprite = iconSprite ?? "traits_magic_icon"; // Default placeholder
            _data = data;
            _onSelect = onSelect;
            _isSelected = false;
        }

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChangedWithValue(value, nameof(Name));
                }
            }
        }

        [DataSourceProperty]
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChangedWithValue(value, nameof(Description));
                }
            }
        }

        [DataSourceProperty]
        public string PositiveEffect
        {
            get => _positiveEffect;
            set
            {
                if (_positiveEffect != value)
                {
                    _positiveEffect = value;
                    OnPropertyChangedWithValue(value, nameof(PositiveEffect));
                    OnPropertyChangedWithValue(!string.IsNullOrEmpty(value), nameof(HasPositiveEffect));
                }
            }
        }

        [DataSourceProperty]
        public string NegativeEffect
        {
            get => _negativeEffect;
            set
            {
                if (_negativeEffect != value)
                {
                    _negativeEffect = value;
                    OnPropertyChangedWithValue(value, nameof(NegativeEffect));
                    OnPropertyChangedWithValue(!string.IsNullOrEmpty(value), nameof(HasNegativeEffect));
                }
            }
        }

        [DataSourceProperty]
        public bool HasPositiveEffect => !string.IsNullOrEmpty(_positiveEffect);

        [DataSourceProperty]
        public bool HasNegativeEffect => !string.IsNullOrEmpty(_negativeEffect);

        [DataSourceProperty]
        public string IconSprite
        {
            get => _iconSprite;
            set
            {
                if (_iconSprite != value)
                {
                    _iconSprite = value;
                    OnPropertyChangedWithValue(value, nameof(IconSprite));
                }
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsSelected));
                }
            }
        }

        public object Data => _data;

        public void ExecuteSelect()
        {
            _onSelect?.Invoke(this);
        }
    }

    /// <summary>
    /// ViewModel for TORSpecializationStage
    /// Shows title, description, and selectable options with character preview
    /// </summary>
    public class TORSpecializationStageVM : ViewModel
    {
        private string _titleText;
        private string _descriptionText;
        private string _affirmativeText;
        private string _negativeText;
        private bool _canAdvance;
        private bool _hasSelection;
        private string _selectedDescription;
        private string _selectedPositiveEffect;
        private string _selectedNegativeEffect;
        private bool _hasPositiveEffect;
        private bool _hasNegativeEffect;
        private MBBindingList<SpecializationOptionVM> _options;
        private SpecializationOptionVM _selectedOption;
        private CharacterViewModel _currentCharacter;
        private TORSpecializationGainedPropertiesVM _gainedPropertiesController;

        private readonly Action _onNextStage;
        private readonly Action _onPreviousStage;
        private readonly Action<SpecializationOptionVM> _onOptionSelected;

        public TORSpecializationStageVM(CharacterCreationManager characterCreationManager, string title, string description,
            Action onNextStage, TextObject affirmativeText, Action onPreviousStage,
            TextObject negativeText, Action<SpecializationOptionVM> onOptionSelected = null)
        {
            TitleText = title;
            DescriptionText = description;
            _onNextStage = onNextStage;
            _onPreviousStage = onPreviousStage;
            _onOptionSelected = onOptionSelected;
            AffirmativeText = affirmativeText?.ToString() ?? "Continue";
            _negativeText = negativeText?.ToString() ?? "Back";
            _canAdvance = false; // Disabled until an option is selected
            _options = new MBBindingList<SpecializationOptionVM>();

            _currentCharacter = new CharacterViewModel();
            // Set the customized body properties from CharacterObject (preserves face editor changes)
            // var bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);


            _currentCharacter.FillFrom(CharacterObject.PlayerCharacter);
            _currentCharacter.OnPropertyChangedWithValue("BodyProperties",
                CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1).ToString());
            //_currentCharacter.FillFrom(Hero.MainHero);

            // Initialize custom gained properties controller to show specialization bonuses/penalties
            _gainedPropertiesController = new TORSpecializationGainedPropertiesVM(characterCreationManager);
        }

        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set
            {
                if (_titleText != value)
                {
                    _titleText = value;
                    OnPropertyChangedWithValue(value, nameof(TitleText));
                }
            }
        }

        [DataSourceProperty]
        public string DescriptionText
        {
            get => _descriptionText;
            set
            {
                if (_descriptionText != value)
                {
                    _descriptionText = value;
                    OnPropertyChangedWithValue(value, nameof(DescriptionText));
                }
            }
        }

        [DataSourceProperty]
        public string AffirmativeText
        {
            get => _affirmativeText;
            set
            {
                if (_affirmativeText != value)
                {
                    _affirmativeText = value;
                    OnPropertyChangedWithValue(value, nameof(AffirmativeText));
                }
            }
        }

        [DataSourceProperty]
        public string NegativeText
        {
            get => _negativeText;
            set
            {
                if (_negativeText != value)
                {
                    _negativeText = value;
                    OnPropertyChangedWithValue(value, nameof(NegativeText));
                }
            }
        }

        [DataSourceProperty]
        public bool CanAdvance
        {
            get => _canAdvance;
            set
            {
                if (_canAdvance != value)
                {
                    _canAdvance = value;
                    OnPropertyChangedWithValue(value, nameof(CanAdvance));
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<SpecializationOptionVM> Options
        {
            get => _options;
            set
            {
                if (_options != value)
                {
                    _options = value;
                    OnPropertyChangedWithValue(value, nameof(Options));
                }
            }
        }

        [DataSourceProperty]
        public CharacterViewModel CurrentCharacter
        {
            get => _currentCharacter;
            set
            {
                if (_currentCharacter != value)
                {
                    _currentCharacter = value;
                    OnPropertyChangedWithValue(value, nameof(CurrentCharacter));
                }
            }
        }

        [DataSourceProperty]
        public SpecializationOptionVM SelectedOption
        {
            get => _selectedOption;
            set
            {
                if (_selectedOption != value)
                {
                    _selectedOption = value;
                    OnPropertyChangedWithValue(value, nameof(SelectedOption));
                    HasSelection = value != null;
                }
            }
        }

        [DataSourceProperty]
        public bool HasSelection
        {
            get => _hasSelection;
            set
            {
                if (_hasSelection != value)
                {
                    _hasSelection = value;
                    OnPropertyChangedWithValue(value, nameof(HasSelection));
                }
            }
        }

        [DataSourceProperty]
        public string SelectedDescription
        {
            get => _selectedDescription;
            set
            {
                if (_selectedDescription != value)
                {
                    _selectedDescription = value;
                    OnPropertyChangedWithValue(value, nameof(SelectedDescription));
                }
            }
        }

        [DataSourceProperty]
        public string SelectedPositiveEffect
        {
            get => _selectedPositiveEffect;
            set
            {
                if (_selectedPositiveEffect != value)
                {
                    _selectedPositiveEffect = value;
                    OnPropertyChangedWithValue(value, nameof(SelectedPositiveEffect));
                }
            }
        }

        [DataSourceProperty]
        public string SelectedNegativeEffect
        {
            get => _selectedNegativeEffect;
            set
            {
                if (_selectedNegativeEffect != value)
                {
                    _selectedNegativeEffect = value;
                    OnPropertyChangedWithValue(value, nameof(SelectedNegativeEffect));
                }
            }
        }

        [DataSourceProperty]
        public bool HasPositiveEffect
        {
            get => _hasPositiveEffect;
            set
            {
                if (_hasPositiveEffect != value)
                {
                    _hasPositiveEffect = value;
                    OnPropertyChangedWithValue(value, nameof(HasPositiveEffect));
                }
            }
        }

        [DataSourceProperty]
        public bool HasNegativeEffect
        {
            get => _hasNegativeEffect;
            set
            {
                if (_hasNegativeEffect != value)
                {
                    _hasNegativeEffect = value;
                    OnPropertyChangedWithValue(value, nameof(HasNegativeEffect));
                }
            }
        }

        [DataSourceProperty]
        public TORSpecializationGainedPropertiesVM GainedPropertiesController
        {
            get => _gainedPropertiesController;
            set
            {
                if (_gainedPropertiesController != value)
                {
                    _gainedPropertiesController = value;
                    OnPropertyChangedWithValue(value, nameof(GainedPropertiesController));
                }
            }
        }

        /// <summary>
        /// Add a selectable option to the list
        /// </summary>
        public void AddOption(string name, string description, object data, string iconSprite = "", string positiveEffect = "",
            string negativeEffect = "")
        {
            var option = new SpecializationOptionVM(name, description, data, OnOptionSelected, iconSprite, positiveEffect, negativeEffect);
            _options.Add(option);

            // Pre-select the first option
            if (_options.Count == 1)
            {
                OnOptionSelected(option);
            }
        }

        /// <summary>
        /// Update character equipment for preview
        /// </summary>
        public void UpdateCharacterEquipment(Equipment equipment)
        {
            if (_currentCharacter != null && equipment != null)
            {
                _currentCharacter.SetEquipment(equipment);
            }
        }

        /// <summary>
        /// Called when an option is selected
        /// </summary>
        private void OnOptionSelected(SpecializationOptionVM selectedOption)
        {
            // Deselect all other options
            foreach (var option in _options)
            {
                option.IsSelected = (option == selectedOption);
            }

            SelectedOption = selectedOption; // Update the property for detail panel binding
            CanAdvance = true; // Enable Continue button

            // Update detail panel properties
            SelectedDescription = selectedOption.Description;
            SelectedPositiveEffect = selectedOption.PositiveEffect;
            SelectedNegativeEffect = selectedOption.NegativeEffect;
            HasPositiveEffect = selectedOption.HasPositiveEffect;
            HasNegativeEffect = selectedOption.HasNegativeEffect;

            // Notify callback for equipment preview
            _onOptionSelected?.Invoke(selectedOption);

            // Update custom gained properties display to show specialization bonuses/penalties
            _gainedPropertiesController?.UpdateFromOption(selectedOption.Data as SpecializationOption);
        }

        /// <summary>
        /// Get the currently selected option data
        /// </summary>
        public object GetSelectedData()
        {
            return _selectedOption?.Data;
        }

        public void OnNextStage()
        {
            _onNextStage?.Invoke();
        }

        public void OnPreviousStage()
        {
            _onPreviousStage?.Invoke();
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            _currentCharacter?.OnFinalize();
            _gainedPropertiesController?.OnFinalize();
        }
    }

    /// <summary>
    /// Custom gained properties display for specialization stage
    /// Shows full attribute/skill table like native, highlighting specialization bonuses/penalties
    /// </summary>
    public class TORSpecializationGainedPropertiesVM : ViewModel
    {
        // All attributes start at 2 in character creation (not 1)
        public const int BaseAttributeValue = 2;

        private MBBindingList<SpecializationAttributeGroupVM> _gainGroups;
        private CharacterCreationManager _manager;

        public TORSpecializationGainedPropertiesVM(CharacterCreationManager manager)
        {
            _manager = manager;
            _gainGroups = new MBBindingList<SpecializationAttributeGroupVM>();
            InitializeAttributeGroups();
        }

        [DataSourceProperty]
        public MBBindingList<SpecializationAttributeGroupVM> GainGroups
        {
            get => _gainGroups;
            set
            {
                if (_gainGroups != value)
                {
                    _gainGroups = value;
                    OnPropertyChangedWithValue(value, nameof(GainGroups));
                }
            }
        }

        private void InitializeAttributeGroups()
        {
            // Use the Hero's current values which include ALL bonuses from previous stages
            // This is more reliable than trying to sum up SelectedOptions
            var skillBonuses = new Dictionary<SkillObject, int>();

            TORCommon.Log($"[Specialization] Initializing gained properties display", NLog.LogLevel.Info);
            TORCommon.Log($"[Specialization] Total selected options: {_manager.SelectedOptions.Count}", NLog.LogLevel.Info);

            // Calculate skill bonuses from previous stages
            foreach (var selectedOption in _manager.SelectedOptions)
            {
                var option = selectedOption.Value;
                TORCommon.Log($"[Specialization] Option: {option.StringId}, Focus: {option.Args.FocusToAdd}, Skills: {option.Args.AffectedSkills.Count}", NLog.LogLevel.Info);

                // Add skill bonuses
                if (option.Args.FocusToAdd > 0)
                {
                    foreach (var skill in option.Args.AffectedSkills)
                    {
                        if (!skillBonuses.ContainsKey(skill))
                            skillBonuses[skill] = 0;

                        skillBonuses[skill] += option.Args.FocusToAdd;
                        TORCommon.Log($"[Specialization]   Skill {skill.StringId}: +{option.Args.FocusToAdd} (total: {skillBonuses[skill]})", NLog.LogLevel.Info);
                    }
                }
            }

            // Create all attribute groups - get current attribute values from Hero
            foreach (var attribute in Attributes.All)
            {
                // Get the hero's current attribute value (includes all bonuses applied so far)
                int currentAttributeValue = Hero.MainHero?.GetAttributeValue(attribute) ?? BaseAttributeValue;
                // The "bonus" is current value minus starting base value
                int attributeBonus = currentAttributeValue - BaseAttributeValue;

                TORCommon.Log($"[Specialization] {attribute.Name}: current={currentAttributeValue}, bonus={attributeBonus}", NLog.LogLevel.Info);

                _gainGroups.Add(new SpecializationAttributeGroupVM(attribute, attributeBonus, skillBonuses));
            }
        }

        public void UpdateFromOption(SpecializationOption option)
        {
            // Reset all changes
            foreach (var group in _gainGroups)
            {
                group.ResetChanges();
            }

            if (option == null) return;

            // Calculate skill changes
            var skillChanges = new Dictionary<string, int>();
            if (option.SkillsToIncrease != null)
            {
                foreach (var skillIdRaw in option.SkillsToIncrease)
                {
                    bool isDecrease = skillIdRaw.StartsWith("-");
                    string skillId = isDecrease ? skillIdRaw.Substring(1) : skillIdRaw;
                    int amount = isDecrease ? -1 : 1;

                    if (skillChanges.ContainsKey(skillId))
                        skillChanges[skillId] += amount;
                    else
                        skillChanges[skillId] = amount;
                }
            }

            // Apply attribute change
            if (!string.IsNullOrEmpty(option.AttributeToIncrease))
            {
                bool isDecrease = option.AttributeToIncrease.StartsWith("-");
                string attributeId = isDecrease ? option.AttributeToIncrease.Substring(1) : option.AttributeToIncrease;
                int amount = isDecrease ? -1 : 1;

                var group = _gainGroups.FirstOrDefault(g => g.Attribute.StringId == attributeId);
                if (group != null)
                {
                    group.SetAttributeChange(amount);
                }
            }

            // Apply skill changes to appropriate groups
            foreach (var kvp in skillChanges)
            {
                var skill = Skills.All.FirstOrDefault(s => s.StringId == kvp.Key);
                if (skill != null)
                {
                    // Find the group that contains this skill
                    var group = _gainGroups.FirstOrDefault(g => g.Skills.Any(s => s.SkillId == skill.StringId));
                    group?.SetSkillChange(skill, kvp.Value);
                }
            }
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            foreach (var group in _gainGroups)
            {
                group.OnFinalize();
            }
            _gainGroups?.Clear();
        }
    }

    /// <summary>
    /// ViewModel for an attribute group (like Vigor, Control, etc.) with its associated skills
    /// </summary>
    public class SpecializationAttributeGroupVM : ViewModel
    {
        private CharacterAttribute _attribute;
        private SpecializationAttributeVM _attributeVM;
        private MBBindingList<SpecializationSkillItemVM> _skills;

        public SpecializationAttributeGroupVM(CharacterAttribute attribute, int attributeBonus, Dictionary<SkillObject, int> skillBonuses)
        {
            _attribute = attribute;
            _attributeVM = new SpecializationAttributeVM(attribute, attributeBonus);
            _skills = new MBBindingList<SpecializationSkillItemVM>();

            // Add all skills for this attribute with their bonuses from previous stages
            var skillsForAttribute = GetSkillsForAttribute(attribute);
            foreach (var skill in skillsForAttribute)
            {
                int skillBonus = skillBonuses.ContainsKey(skill) ? skillBonuses[skill] : 0;
                _skills.Add(new SpecializationSkillItemVM(skill, skillBonus));
            }
        }

        private List<SkillObject> GetSkillsForAttribute(CharacterAttribute attribute)
        {
            var skills = new List<SkillObject>();

            // Map attributes to their skills based on native game structure
            if (attribute == DefaultCharacterAttributes.Vigor)
            {
                skills.Add(DefaultSkills.OneHanded);
                skills.Add(DefaultSkills.TwoHanded);
                skills.Add(DefaultSkills.Polearm);
            }
            else if (attribute == DefaultCharacterAttributes.Control)
            {
                skills.Add(DefaultSkills.Bow);
                skills.Add(DefaultSkills.Crossbow);
                skills.Add(DefaultSkills.Throwing);
            }
            else if (attribute == DefaultCharacterAttributes.Endurance)
            {
                skills.Add(DefaultSkills.Riding);
                skills.Add(DefaultSkills.Athletics);
                skills.Add(DefaultSkills.Crafting);
            }
            else if (attribute == DefaultCharacterAttributes.Cunning)
            {
                skills.Add(DefaultSkills.Scouting);
                skills.Add(DefaultSkills.Tactics);
                skills.Add(DefaultSkills.Roguery);
            }
            else if (attribute == DefaultCharacterAttributes.Social)
            {
                skills.Add(DefaultSkills.Charm);
                skills.Add(DefaultSkills.Leadership);
                skills.Add(DefaultSkills.Trade);
            }
            else if (attribute == DefaultCharacterAttributes.Intelligence)
            {
                skills.Add(DefaultSkills.Steward);
                skills.Add(DefaultSkills.Medicine);
                skills.Add(DefaultSkills.Engineering);
            }
            else if (attribute == TORAttributes.Discipline)
            {
                skills.Add(TORSkills.Faith);
                skills.Add(TORSkills.SpellCraft);
                skills.Add(TORSkills.GunPowder);
            }

            return skills;
        }

        [DataSourceProperty]
        public CharacterAttribute Attribute => _attribute;

        [DataSourceProperty]
        public SpecializationAttributeVM AttributeVM
        {
            get => _attributeVM;
            set
            {
                if (_attributeVM != value)
                {
                    _attributeVM = value;
                    OnPropertyChangedWithValue(value, nameof(AttributeVM));
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<SpecializationSkillItemVM> Skills
        {
            get => _skills;
            set
            {
                if (_skills != value)
                {
                    _skills = value;
                    OnPropertyChangedWithValue(value, nameof(Skills));
                }
            }
        }

        public void SetAttributeChange(int amount)
        {
            _attributeVM.SetChange(amount);
        }

        public void SetSkillChange(SkillObject skill, int amount)
        {
            var skillVM = _skills.FirstOrDefault(s => s.SkillId == skill.StringId);
            skillVM?.SetChange(amount);
        }

        public void ResetChanges()
        {
            _attributeVM.SetChange(0);
            foreach (var skill in _skills)
            {
                skill.SetChange(0);
            }
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            _skills?.Clear();
        }
    }

    /// <summary>
    /// ViewModel for a single attribute
    /// </summary>
    public class SpecializationAttributeVM : ViewModel
    {
        private CharacterAttribute _attribute;
        private int _change;
        private int _currentValue;
        private int _baseValue; // Value before this specialization
        private string _nameText;
        private bool _hasIncreasedInCurrentStage;

        public SpecializationAttributeVM(CharacterAttribute attribute, int previousStageBonus)
        {
            _attribute = attribute;
            _nameText = attribute.Name.ToString();
            _change = 0;
            // Base value is the starting value (BASE_ATTRIBUTE_VALUE) plus bonuses from previous narrative stages
            _baseValue = TORSpecializationGainedPropertiesVM.BaseAttributeValue + previousStageBonus;
            _currentValue = _baseValue;
            _hasIncreasedInCurrentStage = false;
        }

        [DataSourceProperty]
        public string NameText
        {
            get => _nameText;
            set
            {
                if (_nameText != value)
                {
                    _nameText = value;
                    OnPropertyChangedWithValue(value, nameof(NameText));
                }
            }
        }

        [DataSourceProperty]
        public int CurrentValue
        {
            get => _currentValue;
            set
            {
                if (_currentValue != value)
                {
                    _currentValue = value;
                    OnPropertyChangedWithValue(value, nameof(CurrentValue));
                    OnPropertyChangedWithValue(value.ToString(), nameof(CurrentValueText));
                }
            }
        }

        [DataSourceProperty]
        public string CurrentValueText => _currentValue.ToString();

        [DataSourceProperty]
        public bool HasIncreasedInCurrentStage
        {
            get => _hasIncreasedInCurrentStage;
            set
            {
                if (_hasIncreasedInCurrentStage != value)
                {
                    _hasIncreasedInCurrentStage = value;
                    OnPropertyChangedWithValue(value, nameof(HasIncreasedInCurrentStage));
                }
            }
        }

        public void SetChange(int amount)
        {
            _change = amount;
            _currentValue = _baseValue + amount;
            HasIncreasedInCurrentStage = amount != 0;
            OnPropertyChangedWithValue(_currentValue, nameof(CurrentValue));
            OnPropertyChangedWithValue(_currentValue.ToString(), nameof(CurrentValueText));
        }
    }

    /// <summary>
    /// ViewModel for a single skill focus change in specialization
    /// </summary>
    public class SpecializationSkillItemVM : ViewModel
    {
        private SkillObject _skill;
        private int _focusChange;
        private int _currentFocus;
        private int _baseFocus; // Focus before this specialization
        private string _skillId;
        private bool _hasIncreasedInCurrentStage;
        private MBBindingList<FocusIconVM> _focusPointGainList;

        public SpecializationSkillItemVM(SkillObject skill, int previousStageBonus)
        {
            _skill = skill;
            _focusChange = 0;
            _skillId = skill.StringId;
            // Base focus is the bonuses from previous narrative stages (no starting focus)
            _baseFocus = previousStageBonus;
            _currentFocus = _baseFocus;
            _hasIncreasedInCurrentStage = false;

            // Initialize focus point list (max 5 focus points)
            _focusPointGainList = new MBBindingList<FocusIconVM>();
            for (int i = 0; i < 5; i++)
            {
                bool isOld = i < _baseFocus;
                bool isNew = false;
                _focusPointGainList.Add(new FocusIconVM(isOld, isNew));
            }
        }

        [DataSourceProperty]
        public string SkillId
        {
            get => _skillId;
            set
            {
                if (_skillId != value)
                {
                    _skillId = value;
                    OnPropertyChangedWithValue(value, nameof(SkillId));
                }
            }
        }

        [DataSourceProperty]
        public int CurrentFocus
        {
            get => _currentFocus;
            set
            {
                if (_currentFocus != value)
                {
                    _currentFocus = value;
                    OnPropertyChangedWithValue(value, nameof(CurrentFocus));
                    OnPropertyChangedWithValue(value.ToString(), nameof(CurrentFocusText));
                }
            }
        }

        [DataSourceProperty]
        public string CurrentFocusText => _currentFocus.ToString();

        [DataSourceProperty]
        public MBBindingList<FocusIconVM> FocusPointGainList
        {
            get => _focusPointGainList;
            set
            {
                if (_focusPointGainList != value)
                {
                    _focusPointGainList = value;
                    OnPropertyChangedWithValue(value, nameof(FocusPointGainList));
                }
            }
        }

        [DataSourceProperty]
        public bool HasIncreasedInCurrentStage
        {
            get => _hasIncreasedInCurrentStage;
            set
            {
                if (_hasIncreasedInCurrentStage != value)
                {
                    _hasIncreasedInCurrentStage = value;
                    OnPropertyChangedWithValue(value, nameof(HasIncreasedInCurrentStage));
                }
            }
        }

        public void SetChange(int amount)
        {
            _focusChange = amount;
            _currentFocus = _baseFocus + amount;
            HasIncreasedInCurrentStage = amount != 0;
            OnPropertyChangedWithValue(_currentFocus, nameof(CurrentFocus));

            // Update focus bars: old (dark green) vs new (light green)
            for (int i = 0; i < _focusPointGainList.Count && i < 5; i++)
            {
                bool isOld = i < _baseFocus; // Previously existing focus
                bool isNew = i >= _baseFocus && i < _currentFocus; // Newly added focus

                _focusPointGainList[i].IsOld = isOld;
                _focusPointGainList[i].IsNew = isNew;
            }
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            _focusPointGainList?.Clear();
        }
    }

    /// <summary>
    /// ViewModel for a single focus point bar
    /// </summary>
    public class FocusIconVM : ViewModel
    {
        private bool _isOld; // Dark green (previously existing)
        private bool _isNew; // Light green (newly added this stage)

        public FocusIconVM(bool isOld, bool isNew)
        {
            _isOld = isOld;
            _isNew = isNew;
        }

        [DataSourceProperty]
        public bool IsOld
        {
            get => _isOld;
            set
            {
                if (_isOld != value)
                {
                    _isOld = value;
                    OnPropertyChangedWithValue(value, nameof(IsOld));
                }
            }
        }

        [DataSourceProperty]
        public bool IsNew
        {
            get => _isNew;
            set
            {
                if (_isNew != value)
                {
                    _isNew = value;
                    OnPropertyChangedWithValue(value, nameof(IsNew));
                }
            }
        }
    }
}