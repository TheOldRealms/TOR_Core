using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.SpellBook
{
    public class SpellItemVM : ViewModel
    {
        private string _spellSpriteName;
        private string _spellName;
        private Hero _hero;
        private AbilityTemplate _spellTemplate;
        private MBBindingList<StatItemVM> _statItems;
        private bool _isDisabled;
        private bool _isKnown;
        private string _disabledReason;
        private BasicTooltipViewModel _spellHint;
        private bool _isTrainerMode;
        private bool _canLearn = false;
        private string _learnText;
        private int _goldCost;
        private bool _isSelected;

        public SpellItemVM(AbilityTemplate template, Hero currentHero, bool isTrainerMode = false)
        {
            _spellTemplate = template;
            _hero = currentHero;
            _isTrainerMode = isTrainerMode;
            SpellName = new TextObject(_spellTemplate.Name).ToString();
            SpellSpriteName = template.SpriteName;
            SpellStatItems = template.GetStats(_hero, _spellTemplate);
            SpellHint = new BasicTooltipViewModel(GetHintText);
            RefreshValues();
        }

        private string GetHintText()
        {
            return new TextObject (_spellTemplate.TooltipDescription).ToString();
        }

        private void ExecuteLearnSpell()
        {
            // Deduct gold from the party leader if possible. Needed because
            // companions in a party do not actually own any gold.
            var sugarDaddy = _hero.IsPartyLeader ? _hero
                : _hero.PartyBelongedTo != null ? _hero.PartyBelongedTo.Owner
                    : _hero;
            if(sugarDaddy.Gold >= _goldCost)
            {
                sugarDaddy.ChangeHeroGold(-_goldCost);
                _hero.AddAbility(_spellTemplate.StringID);
                MBInformationManager.AddQuickInformation(new TextObject("Successfully learned spell: " + _spellTemplate.Name));
            }
            else
            {
                MBInformationManager.AddQuickInformation(new TextObject("Not enough gold"));
            }
            RefreshValues();
        }

        private void ExecuteSelectSpell()
        {
            if(!_isTrainerMode) _hero.GetExtendedInfo().ToggleSelectedAbility(_spellTemplate.StringID);
            RefreshValues();
        }

        public override void RefreshValues()
        {
            _goldCost = _spellTemplate.GoldCost;
            var model = Campaign.Current.Models.GetAbilityModel();
            var info = _hero.GetExtendedInfo();
            if (model != null)
            {
                _goldCost = model.GetSpellGoldCostForHero(_hero, _spellTemplate);
            }
            LearnText = "Learn " + _goldCost + "<img src=\"General\\Icons\\Coin@2x\"/>";
            IsKnown = _hero.HasAbility(_spellTemplate.StringID);
            IsSelected = !_isTrainerMode && info.IsAbilitySelected(_spellTemplate.StringID);
            IsDisabled = !IsKnown;
            if (IsDisabled)
            {
                CanLearn = _isTrainerMode && _spellTemplate.SpellTier <= (int)info.SpellCastingLevel && _hero.HasKnownLore(_spellTemplate.BelongsToLoreID);
                if (!info.KnownLores.Any(x=>x.ID == _spellTemplate.BelongsToLoreID))
                {
                    DisabledReason = "Unfamiliar lore";
                }
                else if(_spellTemplate.SpellTier > (int)info.SpellCastingLevel)
                {
                    DisabledReason = "Insufficient caster level";
                }
                else
                {
                    DisabledReason = "Can learn";
                    CanLearn = _isTrainerMode && _spellTemplate.SpellTier <= (int)info.SpellCastingLevel && _hero.HasKnownLore(_spellTemplate.BelongsToLoreID);
                }
            }
            base.RefreshValues();
        }

        [DataSourceProperty]
        public string SpellName
        {
            get
            {
                return this._spellName;
            }
            set
            {
                if (value != this._spellName)
                {
                    this._spellName = value;
                    base.OnPropertyChangedWithValue(value, "SpellName");
                }
            }
        }

        [DataSourceProperty]
        public string SpellSpriteName
        {
            get
            {
                return this._spellSpriteName;
            }
            set
            {
                if (value != this._spellSpriteName)
                {
                    this._spellSpriteName = value;
                    base.OnPropertyChangedWithValue(value, "SpellSpriteName");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<StatItemVM> SpellStatItems
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
                    base.OnPropertyChangedWithValue(value, "SpellStatItems");
                }
            }
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
        public BasicTooltipViewModel SpellHint
        {
            get
            {
                return this._spellHint;
            }
            set
            {
                if (value != this._spellHint)
                {
                    this._spellHint = value;
                    base.OnPropertyChangedWithValue(value, "SpellHint");
                }
            }
        }

        [DataSourceProperty]
        public bool CanLearn
        {
            get
            {
                return this._canLearn;
            }
            set
            {
                if (value != this._canLearn)
                {
                    this._canLearn = value;
                    base.OnPropertyChangedWithValue(value, "CanLearn");
                }
            }
        }

        [DataSourceProperty]
        public string LearnText
        {
            get
            {
                return this._learnText;
            }
            set
            {
                if (value != this._learnText)
                {
                    this._learnText = value;
                    base.OnPropertyChangedWithValue(value, "LearnText");
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
    }
}
