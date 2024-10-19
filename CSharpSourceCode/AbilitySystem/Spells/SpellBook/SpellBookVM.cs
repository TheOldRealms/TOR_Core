using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.Utilities;
using TOR_Core.Extensions;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CampaignMechanics.CustomResources;

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
            StatItems.Add(new StatItemVM("Hero name: ", _currentHero.Name.ToString()));
            StatItems.Add(new StatItemVM("Spell casting level: ", info.SpellCastingLevel.ToString()));
            StatItems.Add(new StatItemVM("Maximum Winds of Magic: ", ((int)info.MaxWindsOfMagic).ToString() + CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText()));
            StatItems.Add(new StatItemVM("Current Winds of Magic: ", ((int)info.GetCustomResourceValue("WindsOfMagic")).ToString() + CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText()));
            StatItems.Add(new StatItemVM("Winds of Magic recharge rate: ", info.WindsOfMagicRechargeRate.ToString("0.00") + CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText() + "/ hour"));
            string lorestext = "";
            for(int i = 0; i < info.KnownLores.Count; i++)
            {
                lorestext += info.KnownLores[i].Name;
                if (i != info.KnownLores.Count - 1) lorestext += ", ";

                if (i>0&&i%2==1)
                {
                    lorestext += "\n";
                }
            }
            StatItems.Add(new StatItemVM("Known Magic Schools: ", lorestext));

            LoreObjects.Clear();
            var lores = LoreObject.GetAll();
            foreach(var lore in lores)
            {
                if (!_isTrainerMode)
                {
                    if (info.KnownLores.Contains(lore)) LoreObjects.Add(new LoreObjectVM(this, lore, _currentHero));
                }
                else if(!lore.DisabledForCultures.Contains(_trainerCulture))
                {
                    LoreObjects.Add(new LoreObjectVM(this, lore, _currentHero, _isTrainerMode));
                }
                else if (_isTrainerMode && Hero.MainHero.HasCareer(TORCareers.GrailDamsel)&& Hero.MainHero.HasKnownLore(lore.ID) && CharacterObject.OneToOneConversationCharacter != null && _trainerCulture == TORConstants.Cultures.BRETONNIA)
                {
                    LoreObjects.Add(new LoreObjectVM(this, lore, _currentHero, _isTrainerMode));
                }
                else if (_isTrainerMode && Hero.MainHero.HasCareer(TORCareers.Necrarch) && Hero.MainHero.HasKnownLore(lore.ID) && CharacterObject.OneToOneConversationCharacter != null)
                {
                    LoreObjects.Add(new LoreObjectVM(this, lore, _currentHero, _isTrainerMode));
                }
                else if (_isTrainerMode && Hero.MainHero.HasCareer(TORCareers.Spellsinger) && Hero.MainHero.HasKnownLore(lore.ID) &&
                         CharacterObject.OneToOneConversationCharacter != null)
                {
                    LoreObjects.Add(new LoreObjectVM(this, lore, _currentHero, _isTrainerMode));
                }
            }
            CurrentLore = LoreObjects[0];
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
        }

        private void ExecuteClose()
        {
            _closeAction();
        }

        private void ExecuteSelectPreviousHero()
        {
            if(_heroes.Count > 1)
            {
                _currentHeroIndex -= 1;
                if(_currentHeroIndex < 0)
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
