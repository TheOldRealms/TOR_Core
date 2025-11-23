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
            // Validate required parameters
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name), "Specialization option name cannot be null or empty");
            if (onSelect == null)
                throw new ArgumentNullException(nameof(onSelect), "OnSelect callback cannot be null");

            _name = name;
            _description = description ?? string.Empty;
            _positiveEffect = positiveEffect ?? string.Empty;
            _negativeEffect = negativeEffect ?? string.Empty;
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment).ToString());
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
                    OnPropertyChangedWithValue(value);
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
            // Calculate bonuses from previous stages by examining SelectedOptions
            // NOTE: Attribute bonuses aren't applied to Hero until finalization, so we must calculate manually
            // All bonuses from stages 1-3 are shown as "old" (dark green)
            // Only specialization bonuses (stage 4) will be shown as "new" (light green) via UpdateFromOption
            var skillBonuses = new Dictionary<SkillObject, int>();
            var attributeBonuses = new Dictionary<CharacterAttribute, int>();

            // Calculate skill AND attribute bonuses from ALL previous stages (1-3)
            foreach (var selectedOption in _manager.SelectedOptions)
            {
                var option = selectedOption.Value;

                // Add skill bonuses
                if (option.Args.FocusToAdd > 0)
                {
                    foreach (var skill in option.Args.AffectedSkills)
                    {
                        if (skillBonuses.TryGetValue(skill, out int currentBonus))
                        {
                            skillBonuses[skill] = currentBonus + option.Args.FocusToAdd;
                        }
                        else
                        {
                            skillBonuses[skill] = option.Args.FocusToAdd;
                        }
                    }
                }

                // Add attribute bonuses (using EffectedAttribute and AttributeLevelToAdd)
                if (option.Args.EffectedAttribute != null && option.Args.AttributeLevelToAdd > 0)
                {
                    var attr = option.Args.EffectedAttribute;
                    if (attributeBonuses.TryGetValue(attr, out int currentBonus))
                    {
                        attributeBonuses[attr] = currentBonus + option.Args.AttributeLevelToAdd;
                    }
                    else
                    {
                        attributeBonuses[attr] = option.Args.AttributeLevelToAdd;
                    }
                }
            }

            // Create all attribute groups with calculated bonuses
            foreach (var attribute in Attributes.All)
            {
                int attributeBonus = attributeBonuses.ContainsKey(attribute) ? attributeBonuses[attribute] : 0;

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

                    if (skillChanges.TryGetValue(skillId, out int currentAmount))
                    {
                        skillChanges[skillId] = currentAmount + amount;
                    }
                    else
                    {
                        skillChanges[skillId] = amount;
                    }
                }
            }

            // Apply attribute changes (can have multiple)
            if (option.AttributesToIncrease != null && option.AttributesToIncrease.Length > 0)
            {
                // Calculate net attribute changes (in case same attribute appears multiple times)
                var attributeChanges = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var attributeRaw in option.AttributesToIncrease)
                {
                    bool isDecrease = attributeRaw.StartsWith("-");
                    string attributeId = isDecrease ? attributeRaw.Substring(1) : attributeRaw;
                    int amount = isDecrease ? -1 : 1;

                    if (attributeChanges.ContainsKey(attributeId))
                        attributeChanges[attributeId] += amount;
                    else
                        attributeChanges[attributeId] = amount;
                }

                // Apply each attribute change to the appropriate group
                foreach (var kvp in attributeChanges)
                {
                    var group = _gainGroups.FirstOrDefault(g => g.Attribute.StringId.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                    if (group != null)
                    {
                        group.SetAttributeChange(kvp.Value);
                    }
                    else
                    {
                        TORCommon.Log($"[UpdateFromOption]   ERROR: Could not find attribute group for '{kvp.Key}'", NLog.LogLevel.Error);
                    }
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
                    if (group != null)
                    {
                        group.SetSkillChange(skill, kvp.Value);
                    }
                    else
                    {
                        TORCommon.Log($"[UpdateFromOption]   ERROR: Could not find group for skill {kvp.Key}", NLog.LogLevel.Error);
                    }
                }
                else
                {
                    TORCommon.Log($"[UpdateFromOption]   ERROR: Could not find skill object for {kvp.Key}", NLog.LogLevel.Error);
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
                skills.Add(TORSkills.GunPowder);
                skills.Add(TORSkills.SpellCraft);
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
        private int _baseValue; // Value before specialization (includes all bonuses from stages 1-3)
        private string _nameText;
        private bool _hasIncreasedInCurrentStage;

        public SpecializationAttributeVM(CharacterAttribute attribute, int previousStageBonus)
        {
            _attribute = attribute;
            _nameText = attribute.Name.ToString() + ":";
            _change = 0;
            // Base value is the starting value (2) plus bonuses from ALL previous stages (1-3)
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

        private bool _hasDecreasedInCurrentStage;

        [DataSourceProperty]
        public bool HasDecreasedInCurrentStage
        {
            get => _hasDecreasedInCurrentStage;
            set
            {
                if (_hasDecreasedInCurrentStage != value)
                {
                    _hasDecreasedInCurrentStage = value;
                    OnPropertyChangedWithValue(value, nameof(HasDecreasedInCurrentStage));
                }
            }
        }

        public void SetChange(int amount)
        {
            _change = amount;
            // Current value = base (all stages 1-3) + specialization change
            _currentValue = _baseValue + amount;
            // Show as increased/decreased based on specialization change
            HasIncreasedInCurrentStage = amount > 0;
            HasDecreasedInCurrentStage = amount < 0;
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
        private int _baseFocus; // Focus from ALL previous stages (1-3), shown as "old"/dark green
        private string _skillId;
        private bool _hasIncreasedInCurrentStage;
        private MBBindingList<FocusIconVM> _focusPointGainList;

        public SpecializationSkillItemVM(SkillObject skill, int previousStageBonus)
        {
            _skill = skill;
            _focusChange = 0;
            _skillId = skill.StringId;
            // Base focus is from ALL previous stages (1-3)
            _baseFocus = previousStageBonus;
            _currentFocus = _baseFocus;
            _hasIncreasedInCurrentStage = false;

            // Initialize focus point list (max 5 focus points)
            _focusPointGainList = new MBBindingList<FocusIconVM>();
            for (int i = 0; i < 5; i++)
            {
                // All bonuses from stages 1-3 show as "old" (dark green)
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

        private bool _hasDecreasedInCurrentStage;

        [DataSourceProperty]
        public bool HasDecreasedInCurrentStage
        {
            get => _hasDecreasedInCurrentStage;
            set
            {
                if (_hasDecreasedInCurrentStage != value)
                {
                    _hasDecreasedInCurrentStage = value;
                    OnPropertyChangedWithValue(value, nameof(HasDecreasedInCurrentStage));
                }
            }
        }

        public void SetChange(int amount)
        {
            _focusChange = amount;
            _currentFocus = _baseFocus + amount;
            HasIncreasedInCurrentStage = amount > 0;
            HasDecreasedInCurrentStage = amount < 0;
            OnPropertyChangedWithValue(_currentFocus, nameof(CurrentFocus));

            // Update focus bars: old (dark green) vs new (light green) vs removed (red)
            for (int i = 0; i < _focusPointGainList.Count && i < 5; i++)
            {
                bool isOld = false;
                bool isNew = false;
                bool isRemoved = false;

                if (amount >= 0)
                {
                    // Increase or no change
                    isOld = i < _baseFocus; // Previously existing focus (dark green)
                    isNew = i >= _baseFocus && i < _currentFocus; // Newly added focus (light green)
                }
                else
                {
                    // Decrease
                    isOld = i < _currentFocus; // Remaining focus (dark green)
                    isRemoved = i >= _currentFocus && i < _baseFocus; // Removed focus (red)
                }

                _focusPointGainList[i].IsOld = isOld;
                _focusPointGainList[i].IsNew = isNew;
                _focusPointGainList[i].IsRemoved = isRemoved;
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
        private bool _isRemoved; // Red (removed this stage)

        public FocusIconVM(bool isOld, bool isNew)
        {
            _isOld = isOld;
            _isNew = isNew;
            _isRemoved = false;
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

        [DataSourceProperty]
        public bool IsRemoved
        {
            get => _isRemoved;
            set
            {
                if (_isRemoved != value)
                {
                    _isRemoved = value;
                    OnPropertyChangedWithValue(value, nameof(IsRemoved));
                }
            }
        }
    }
}