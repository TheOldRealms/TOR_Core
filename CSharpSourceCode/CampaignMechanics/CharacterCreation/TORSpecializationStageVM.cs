using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;

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

        private readonly Action _onNextStage;
        private readonly Action _onPreviousStage;
        private readonly Action<SpecializationOptionVM> _onOptionSelected;

        public TORSpecializationStageVM(string title, string description, Action onNextStage, TextObject affirmativeText, Action onPreviousStage,
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
        }
    }
}