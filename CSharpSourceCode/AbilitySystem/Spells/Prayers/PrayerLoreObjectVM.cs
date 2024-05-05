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
    private string _name;
    private LoreObject _lore;
    private BattlePrayersVM _parent;
    private MBBindingList<PrayerItemVM> _prayers;
    private bool _isVisible;
    private bool _isSelected;
    private string _spriteName;

    public PrayerLoreObjectVM(BattlePrayersVM parent, List<string> prayerIDs, Hero hero)
    {
        _parent = parent;
        _prayers = new MBBindingList<PrayerItemVM>();
        var prayerTemplates = new List <AbilityTemplate>();
        
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
        

        //IsVisible = true;
        RefreshValues();
    }
    
    [DataSourceProperty]
    public MBBindingList<PrayerItemVM> PrayerList
    {
        get
        {
            return this._prayers;
        }
        set
        {
            if (value != this._prayers)
            {
                this._prayers = value;
                base.OnPropertyChangedWithValue(value, "PrayerList");
            }
        }
    }
    
    }
}