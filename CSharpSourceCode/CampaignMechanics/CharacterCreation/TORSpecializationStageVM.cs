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
          private bool _isSelected;
          private object _data;
          private readonly Action<SpecializationOptionVM> _onSelect;

          public SpecializationOptionVM(string name, string description, object data, Action<SpecializationOptionVM> onSelect)
          {
              _name = name;
              _description = description;
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
              TOR_Core.Utilities.TORCommon.Log($"[SpecializationOptionVM] Option selected: {_name}", NLog.LogLevel.Info);
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
          private MBBindingList<SpecializationOptionVM> _options;
          private SpecializationOptionVM _selectedOption;
          private CharacterViewModel _currentCharacter;

          private readonly Action _onNextStage;
          private readonly Action _onPreviousStage;
          private readonly Action<SpecializationOptionVM> _onOptionSelected;

          public TORSpecializationStageVM(
              string title,
              string description,
              Action onNextStage,
              TextObject affirmativeText,
              Action onPreviousStage,
              TextObject negativeText,
              Action<SpecializationOptionVM> onOptionSelected = null)
          {
              _titleText = title;
              _descriptionText = description;
              _onNextStage = onNextStage;
              _onPreviousStage = onPreviousStage;
              _onOptionSelected = onOptionSelected;
              _affirmativeText = affirmativeText?.ToString() ?? "Continue";
              _negativeText = negativeText?.ToString() ?? "Back";
              _canAdvance = false; // Disabled until an option is selected
              _options = new MBBindingList<SpecializationOptionVM>();

              // Create CharacterViewModel for character display (better suited for character creation)
              _currentCharacter = new CharacterViewModel();
              // Set the customized body properties from CharacterObject (preserves face editor changes)
             // var bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);
              
              
              _currentCharacter.FillFrom(CharacterObject.PlayerCharacter);
              _currentCharacter.OnPropertyChangedWithValue("BodyProperties",CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1).ToString());
              //_currentCharacter.FillFrom(Hero.MainHero);
              
              TOR_Core.Utilities.TORCommon.Log("[TORSpecializationStageVM] Set customized BodyProperties to preserve face/body from editor", NLog.LogLevel.Info);

              TOR_Core.Utilities.TORCommon.Log($"[TORSpecializationStageVM] Created with Title='{_titleText}', Desc='{_descriptionText}', Affirmative='{_affirmativeText}', Negative='{_negativeText}'", NLog.LogLevel.Info);
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

          /// <summary>
          /// Add a selectable option to the list
          /// </summary>
          public void AddOption(string name, string description, object data)
          {
              var option = new SpecializationOptionVM(name, description, data, OnOptionSelected);
              _options.Add(option);
              TOR_Core.Utilities.TORCommon.Log($"[TORSpecializationStageVM] Added option: {name}", NLog.LogLevel.Info);
          }

          /// <summary>
          /// Set customized body properties from face editor (preserves player's face customization)
          /// </summary>
          public void SetCustomizedBodyProperties(BodyProperties bodyProperties)
          {
              if (_currentCharacter != null)
              {
                  // Apply the stored body properties to the character preview

                  _currentCharacter.BodyProperties = bodyProperties.ToString();
                  
                  TOR_Core.Utilities.TORCommon.Log($"[TORSpecializationStageVM] Applied customized BodyProperties: {bodyProperties}", NLog.LogLevel.Info);
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
                  TOR_Core.Utilities.TORCommon.Log("[TORSpecializationStageVM] Updated character equipment", NLog.LogLevel.Info);
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

              _selectedOption = selectedOption;
              CanAdvance = true; // Enable Continue button

              // Notify callback for equipment preview
              _onOptionSelected?.Invoke(selectedOption);

              TOR_Core.Utilities.TORCommon.Log($"[TORSpecializationStageVM] Selected option: {selectedOption.Name}, CanAdvance={CanAdvance}", NLog.LogLevel.Info);
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
              TOR_Core.Utilities.TORCommon.Log($"[TORSpecializationStageVM] OnNextStage called, selected option: {_selectedOption?.Name ?? "none"}", NLog.LogLevel.Info);
              _onNextStage?.Invoke();
          }

          public void OnPreviousStage()
          {
              TOR_Core.Utilities.TORCommon.Log("[TORSpecializationStageVM] OnPreviousStage called from button", NLog.LogLevel.Info);
              _onPreviousStage?.Invoke();
          }

          public override void OnFinalize()
          {
              base.OnFinalize();
              _currentCharacter?.OnFinalize();
          }
      }
  }
