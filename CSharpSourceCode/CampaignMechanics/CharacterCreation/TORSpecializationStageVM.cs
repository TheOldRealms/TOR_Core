using System;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.CharacterCreation
{
    /// <summary>
    /// Simple ViewModel for TORSpecializationStage
    /// Shows title and description without depending on narrative menus
    /// </summary>
    public class TORSpecializationStageVM : ViewModel
    {
        private string _titleText;
        private string _descriptionText;
        private string _affirmativeText;
        private string _negativeText;
        private bool _canAdvance;

        private readonly Action _onNextStage;
        private readonly Action _onPreviousStage;

        public TORSpecializationStageVM(
            string title,
            string description,
            Action onNextStage,
            TextObject affirmativeText,
            Action onPreviousStage,
            TextObject negativeText)
        {
            _titleText = title;
            _descriptionText = description;
            _onNextStage = onNextStage;
            _onPreviousStage = onPreviousStage;
            _affirmativeText = affirmativeText?.ToString() ?? "Continue";
            _negativeText = negativeText?.ToString() ?? "Back";
            _canAdvance = true; // Always allow advancing for MVP
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
        }
    }
}
