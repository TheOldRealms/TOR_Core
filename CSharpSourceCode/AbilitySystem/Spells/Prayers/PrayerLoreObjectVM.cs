using TaleWorlds.Library;
using TOR_Core.AbilitySystem.SpellBook;

namespace TOR_Core.AbilitySystem.Spells.Prayers
{
    //TODO maybe better create common base class with LoreObject
    public class PrayerLoreObjectVM
    {
        private string _name;
        private LoreObject _lore;
        private BattlePrayersVM _parent;
        private MBBindingList<SpellItemVM> prayers;
        private bool _isVisible;
        private bool _isSelected;
        private string _spriteName;
        
        
    }
}