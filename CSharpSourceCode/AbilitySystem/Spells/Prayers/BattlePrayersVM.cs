using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Spells.Prayers
{
    public class BattlePrayersVM : ViewModel
    {
        private MBBindingList<StatItemVM> _stats;
        private Action _closeAction;
        private LoreObjectVM _currentLore;

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

            info.GetAllPrayers();
            
            

        }
        
        public override void RefreshValues()
        {
            base.RefreshValues();
        }
        
        private void ExecuteClose()
        {
            _closeAction();
        }
        
        
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
    }
}