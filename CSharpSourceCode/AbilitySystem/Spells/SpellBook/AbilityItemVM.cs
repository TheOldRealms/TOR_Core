using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.SpellBook
{
    public abstract class AbilityItemVM : ViewModel
    {
        private readonly Hero _hero;

        private readonly AbilityTemplate _abilityTemplate;
        private string _abilityName;
        private string _abilitySpriteName;

        private BasicTooltipViewModel _abilityHint;

        private bool _isDisabled;
        private bool _isKnown;
        private bool _isSelected;
        private string _disabledReason;

        private MBBindingList<StatItemVM> _statItems;


        public AbilityItemVM(AbilityTemplate template, Hero hero)
        {
            _abilityTemplate = template;
            _hero = hero;
            AbilityName = new TextObject(_abilityTemplate.Name).ToString();
            AbilitySpriteName = template.SpriteName;
            AbilityStatItems = template.GetStats(_hero, template);
            AbilityHint = new BasicTooltipViewModel(GetHintText);
        }

        private string GetHintText()
        {
            return new TextObject(_abilityTemplate.TooltipDescription).ToString();
        }

        protected AbilityTemplate Template => this._abilityTemplate;
        protected Hero Hero => this._hero;


        public override void RefreshValues()
        {
            base.RefreshValues();
            IsSelected = Hero.GetExtendedInfo().IsAbilitySelected(Template.StringID);
            IsKnown = Hero.HasAbility(Template.StringID);
            IsDisabled = !IsKnown;
        }

        [DataSourceProperty]
        public bool IsDisabled
        {
            get
            {
                return this._isDisabled;
            }
            set
            {
                if (value != this._isDisabled)
                {
                    this._isDisabled = value;
                    base.OnPropertyChangedWithValue(value, "IsDisabled");
                }
            }
        }

        [DataSourceProperty]
        public bool IsKnown
        {
            get
            {
                return this._isKnown;
            }
            set
            {
                if (value != this._isKnown)
                {
                    this._isKnown = value;
                    base.OnPropertyChangedWithValue(value, "IsKnown");
                }
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get
            {
                return this._isSelected;
            }
            set
            {
                if (value != this._isSelected)
                {
                    this._isSelected = value;
                    base.OnPropertyChangedWithValue(value, "IsSelected");
                }
            }
        }
        protected virtual void ExecuteSelectAbility()
        {
            Hero.GetExtendedInfo().ToggleSelectedAbility(Template.StringID);
            RefreshValues();
        }

        [DataSourceProperty]
        public BasicTooltipViewModel AbilityHint
        {
            get
            {
                return this._abilityHint;
            }
            set
            {
                if (value != this._abilityHint)
                {
                    this._abilityHint = value;
                    base.OnPropertyChangedWithValue(value, "AbilityHint");
                }
            }
        }

        [DataSourceProperty]
        public string DisabledReason
        {
            get
            {
                return this._disabledReason;
            }
            set
            {
                if (value != this._disabledReason)
                {
                    this._disabledReason = value;
                    base.OnPropertyChangedWithValue(value, "DisabledReason");
                }
            }
        }


        [DataSourceProperty]
        public string AbilitySpriteName
        {
            get
            {
                return this._abilitySpriteName;
            }
            set
            {
                if (value != this._abilitySpriteName)
                {
                    this._abilitySpriteName = value;
                    base.OnPropertyChangedWithValue(value, "AbilitySpriteName");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<StatItemVM> AbilityStatItems
        {
            get
            {
                return this._statItems;
            }
            set
            {
                if (value != this._statItems)
                {
                    this._statItems = value;
                    base.OnPropertyChangedWithValue(value, "AbilityStatItems");
                }
            }
        }

        [DataSourceProperty]
        public string AbilityName
        {
            get
            {
                return this._abilityName;
            }
            set
            {
                if (value != this._abilityName)
                {
                    this._abilityName = value;
                    base.OnPropertyChangedWithValue(value, "AbilityName");
                }
            }
        }

    }
}