using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem.SpellBook;

namespace TOR_Core.AbilitySystem.Spells.Prayers
{
    //TODO maybe better create common base class with LoreObject
    public class PrayerLoreObjectVM : ViewModel
    {
        private MBBindingList<PrayerItemVM> _prayers;

        public PrayerLoreObjectVM(BattlePrayersVM parent, List<string> prayerIDs, Hero hero)
        {
            _prayers = new MBBindingList<PrayerItemVM>();
            var prayerTemplates = new List<AbilityTemplate>();

            foreach (var prayerID in prayerIDs)
            {
                var template = AbilityFactory.GetTemplate(prayerID);
                if (template != null)
                {
                    prayerTemplates.Add(template);
                }
            }

            foreach (var prayerAbility in prayerTemplates)
            {
                _prayers.Add(new PrayerItemVM(prayerAbility, hero));
            }

            RefreshValues();
        }

        [DataSourceProperty]
        public MBBindingList<PrayerItemVM> PrayerList
        {
            get
            {
                return _prayers;
            }
            set
            {
                if (value != _prayers)
                {
                    _prayers = value;
                    OnPropertyChangedWithValue(value, "PrayerList");
                }
            }
        }
    }
}