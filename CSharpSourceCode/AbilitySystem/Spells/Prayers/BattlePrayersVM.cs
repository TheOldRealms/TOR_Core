using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Religion;
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
            StatItems.Clear();
            var religionID = CareerHelper.GetGodCareerIsDevotedTo(Hero.MainHero.GetCareer());
            var religion = ReligionObject.All.Where(x => x.StringId == religionID).FirstOrDefault();
            StatItems.Add(new StatItemVM("Devoted to : ", religion.DeityName.ToString()));
            var battlePrayers = CareerHelper.GetBattlePrayerList(Hero.MainHero.GetCareer());
            var highest = battlePrayers.Max(x => x.Rank);
            StatItems.Add(new StatItemVM("Prayer level: ", ((PrayerLevel)highest).ToString()));
            var prayers = battlePrayers.ConvertAll(input => input.PrayerID);
            _loreObjectVm = new PrayerLoreObjectVM(this, prayers, Hero.MainHero);
            
            foreach (var prayer in _loreObjectVm.PrayerList)
                if (!prayer.IsKnown)
                    prayer.IsDisabled = true;
        }

        


        [DataSourceProperty] public PrayerLoreObjectVM PrayerLore => _loreObjectVm;

        public override void RefreshValues()
        {
            base.RefreshValues();
        }

        private void ExecuteClose()
        {
            _closeAction();
        }

        [DataSourceProperty] public bool IsSelected => _loreObjectVm != null;

        [DataSourceProperty]
        public MBBindingList<StatItemVM> StatItems
        {
            get => _stats;
            set
            {
                if (value != _stats)
                {
                    _stats = value;
                    OnPropertyChangedWithValue(value, "StatItems");
                }
            }
        }

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    OnPropertyChangedWithValue(value, "IsVisible");
                }
            }
        }
    }
}