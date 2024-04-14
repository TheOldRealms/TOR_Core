using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Spells.Prayers
{
    public class BattlePrayersVM : ViewModel
    {
        private MBBindingList<StatItemVM> _stats;
        private Action _closeAction;
        private PrayerLoreObjectVM _loreObjectVm;
        private bool _isVisible;

        public BattlePrayersVM(Action closeAction)
        {
            _closeAction = closeAction;
            _stats = new MBBindingList<StatItemVM>();
            Initialize();
            RefreshValues();
            
        }

        private void Initialize()
        {
            var info = Hero.MainHero.GetExtendedInfo();

            var religion = Hero.MainHero.GetDominantReligion();
            StatItems.Clear();
            StatItems.Add(new StatItemVM("Devoted to : ", religion.Name.ToString()));
            StatItems.Add(new StatItemVM("Prayer level: ", info.SpellCastingLevel.ToString()));

            var battlePrayers = CareerHelper.GetBattlePrayerList(Hero.MainHero.GetCareer());

            

            var prayers = battlePrayers.ConvertAll(input => input.PrayerID);

            _loreObjectVm = new PrayerLoreObjectVM(this, prayers, Hero.MainHero);


            foreach (var prayer in _loreObjectVm.PrayerList)
            {
                if (!prayer.IsKnown)
                {
                    prayer.IsDisabled=true;
                }
            }
            
        }
        
        [DataSourceProperty]
        public PrayerLoreObjectVM PrayerLore
        {
            get
            {
                return this._loreObjectVm;
            }
        }
        
        public override void RefreshValues()
        {
            base.RefreshValues();
        }
        
        private void ExecuteClose()
        {
            _closeAction();
        }
        
        [DataSourceProperty]
        public bool IsSelected
        {
            get { return _loreObjectVm != null; }
            
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
        public bool IsVisible
        {
            get
            {
                return this._isVisible;
            }
            set
            {
                if (value != this._isVisible)
                {
                    this._isVisible = value;
                    base.OnPropertyChangedWithValue(value, "IsVisible");
                }
            }
        }
    }
}