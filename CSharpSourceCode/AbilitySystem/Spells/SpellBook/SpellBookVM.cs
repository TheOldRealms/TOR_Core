using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.SpellBook
{
    public class SpellBookVM : ViewModel
    {
        private Action _closeAction;
        private HeroViewModel _currentCharacter;
        private List<Hero> _heroes;
        private Hero _currentHero;
        private MBBindingList<StatItemVM> _stats;
        private MBBindingList<LoreObjectVM> _lores;
        private LoreObjectVM _currentLore;
        private int _currentHeroIndex = 0;
        private bool _isTrainerMode;
        private string _trainerCulture;
        private string _spellBookTitle;
        private string _spellcastingStats;
        private string _doneLbl;

        public SpellBookVM(Action closeAction, List<Hero> heroes, bool isTrainerMode, string trainerCulture)
        {
            _closeAction = closeAction;
            _stats = [];
            _lores = [];
            _heroes = heroes;
            _isTrainerMode = isTrainerMode;
            _trainerCulture = trainerCulture;
            Initialize();
            RefreshValues();
        }

        private void Initialize()
        {
            _currentHero = _heroes[_currentHeroIndex];
            CurrentCharacter = new HeroViewModel();
            CurrentCharacter.FillFrom(_currentHero);
            CurrentCharacter.SetEquipment(EquipmentIndex.ArmorItemEndSlot, default);
            CurrentCharacter.SetEquipment(EquipmentIndex.HorseHarness, default);
            CurrentCharacter.SetEquipment(EquipmentIndex.NumAllWeaponSlots, default);

            var info = _currentHero.GetExtendedInfo();
            StatItems.Clear();
            StatItems.Add(new StatItemVM(TORTextHelper.GetText("tor_spellbook_hero_name", "Hero name: "), _currentHero.Name.ToString()));
            var spellLevelText = GameTexts.FindText("tor_spellcasting_level", info.SpellCastingLevel.ToString());
            StatItems.Add(new StatItemVM(TORTextHelper.GetText("tor_spellbook_casting_level", "Spell casting level: "), spellLevelText.ToString()));
            StatItems.Add(new StatItemVM(TORTextHelper.GetText("tor_spellbook_max_wom", "Maximum Winds of Magic: "), ((int)info.MaxWindsOfMagic).ToString() + CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText()));
            StatItems.Add(new StatItemVM(TORTextHelper.GetText("tor_spellbook_current_wom", "Current Winds of Magic: "), ((int)info.GetCustomResourceValue("WindsOfMagic")).ToString() + CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText()));
            var rechargeRateText = TORTextHelper.GetTextObject("tor_spellbook_recharge_rate", "{RATE}{ICON}/ hour");
            rechargeRateText.SetTextVariable("RATE", info.WindsOfMagicRechargeRate.ToString("0.00"));
            rechargeRateText.SetTextVariable("ICON", CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText());
            StatItems.Add(new StatItemVM(TORTextHelper.GetText("tor_spellbook_wom_recharge", "Winds of Magic recharge rate: "), rechargeRateText.ToString()));
            string lorestext = "";
            for (int i = 0; i < info.KnownLores.Count; i++)
            {
                lorestext += info.KnownLores[i].Name;
                if (i != info.KnownLores.Count - 1) lorestext += ", ";

                if (i > 0 && i % 3 == 2)
                {
                    lorestext += "\n";
                }
            }
            StatItems.Add(new StatItemVM(TORTextHelper.GetText("tor_spellbook_known_lores", "Known Magic Schools: "), lorestext));

            LoreObjects.Clear();
            var lores = LoreObject.GetAll();
            foreach (var lore in lores)
            {
                if (!_isTrainerMode)
                {
                    if (info.KnownLores.Contains(lore)) LoreObjects.Add(new LoreObjectVM(this, lore, _currentHero));
                }
                else if (!lore.DisabledForCultures.Contains(_trainerCulture))
                {
                    LoreObjects.Add(new LoreObjectVM(this, lore, _currentHero, _isTrainerMode));
                }
                //permitting damsels to see LoreOfHeavens
                else if (_isTrainerMode && CharacterObject.OneToOneConversationCharacter != null && _trainerCulture == TORConstants.Cultures.BRETONNIA && _currentHero.HasAttribute(Attributes.PRIEST_LADY) && _currentHero.HasKnownLore(lore.StringId))
                {
                    LoreObjects.Add(new LoreObjectVM(this, lore, _currentHero, _isTrainerMode));
                }

                //necrarchs can't access trainers who know all the lores they can learn, so they can learn spells from irrelevant trainers as long as the player has learned the lore
                //only the player necrarch benefits, other companions must be trained through an appropriate trainer
                else if (_isTrainerMode && Hero.MainHero.HasCareer(TORCareers.Necrarch) && _currentHero == Hero.MainHero && Hero.MainHero.HasKnownLore(lore.StringId) && CharacterObject.OneToOneConversationCharacter != null)
                {
                    LoreObjects.Add(new LoreObjectVM(this, lore, _currentHero, _isTrainerMode));
                }

                //permitting spellsingers to see HighMagic and DarkMagic lores
                else if (_isTrainerMode && CharacterObject.OneToOneConversationCharacter != null  && _trainerCulture == TORConstants.Cultures.ASRAI && _currentHero.Culture.StringId == TORConstants.Cultures.ASRAI && _currentHero.CharacterObject.IsElf() && _currentHero.HasKnownLore(lore.StringId))
                {
                    LoreObjects.Add(new LoreObjectVM(this, lore, _currentHero, _isTrainerMode));
                }
            }
            if (CurrentLore == null || !LoreObjects.Contains(CurrentLore))
            {
                CurrentLore = LoreObjects[0];
            }
        }

        public override void RefreshValues()
        {
            SpellBookTitle = TORTextHelper.GetText("tor_spellbook_title", "SpellBook");
            SpellcastingStats = TORTextHelper.GetText("tor_spellcasting_stat", "Spellcasting Stats");
            DoneLbl = TORTextHelper.GetText("tor_spellbook_button", "Done");
            base.RefreshValues();
        }

        private void ExecuteClose()
        {
            _closeAction();
        }

        private void ExecuteSelectPreviousHero()
        {
            if (_heroes.Count > 1)
            {
                _currentHeroIndex -= 1;
                if (_currentHeroIndex < 0)
                {
                    _currentHeroIndex = _heroes.Count - 1;
                }
                Initialize();
            }
        }

        private void ExecuteSelectNextHero()
        {
            if (_heroes.Count > 1)
            {
                _currentHeroIndex += 1;
                if (_currentHeroIndex > _heroes.Count - 1)
                {
                    _currentHeroIndex = 0;
                }
                Initialize();
            }
        }

        internal void OnLoreObjectSelected(LoreObjectVM loreObjectVM)
        {
            CurrentLore.IsSelected = false;
            CurrentLore = loreObjectVM;
        }

        [DataSourceProperty]
        public string SpellBookTitle
        {
            get
            {
                return _spellBookTitle;
            }
            set
            {
                if (value != _spellBookTitle)
                {
                    _spellBookTitle = value;
                    base.OnPropertyChangedWithValue(value, "SpellBookTitle");
                }
            }
        }

        [DataSourceProperty]
        public string SpellcastingStats
        {
            get
            {
                return _spellcastingStats;
            }
            set
            {
                if (value != _spellcastingStats)
                {
                    _spellcastingStats = value;
                    base.OnPropertyChangedWithValue(value, "SpellcastingStats");
                }
            }
        }

        [DataSourceProperty]
        public string DoneLbl
        {
            get
            {
                return _doneLbl;
            }
            set
            {
                if (value != _doneLbl)
                {
                    _doneLbl = value;
                    base.OnPropertyChangedWithValue(value, "DoneLbl");
                }
            }
        }

        [DataSourceProperty]
        public HeroViewModel CurrentCharacter
        {
            get
            {
                return this._currentCharacter;
            }
            set
            {
                if (value != this._currentCharacter)
                {
                    this._currentCharacter = value;
                    base.OnPropertyChangedWithValue(value, "CurrentCharacter");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<StatItemVM> StatItems
        {
            get
            {
                return this._stats;
            }
            set
            {
                if (value != this._stats)
                {
                    this._stats = value;
                    base.OnPropertyChangedWithValue(value, "StatItems");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<LoreObjectVM> LoreObjects
        {
            get
            {
                return this._lores;
            }
            set
            {
                if (value != this._lores)
                {
                    this._lores = value;
                    base.OnPropertyChangedWithValue(value, "LoreObjects");
                }
            }
        }

        [DataSourceProperty]
        public LoreObjectVM CurrentLore
        {
            get
            {
                return this._currentLore;
            }
            set
            {
                if (value != this._currentLore)
                {
                    this._currentLore = value;
                    base.OnPropertyChangedWithValue(value, "CurrentLore");
                    CurrentLore.IsSelected = true;
                }
            }
        }

        [DataSourceProperty]
        public bool IsTrainerMode
        {
            get
            {
                return this._isTrainerMode;
            }
            set
            {
                if (value != this._isTrainerMode)
                {
                    this._isTrainerMode = value;
                    base.OnPropertyChangedWithValue(value, "IsTrainerMode");
                }
            }
        }
    }
}