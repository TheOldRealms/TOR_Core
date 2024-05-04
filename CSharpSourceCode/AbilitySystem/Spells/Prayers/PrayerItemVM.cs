using TaleWorlds.CampaignSystem;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Spells.Prayers
{
    public class PrayerItemVM : AbilityItemVM
    {
        public PrayerItemVM(AbilityTemplate template, Hero hero) : base(template, hero)
        {
            RefreshValues();
        }
        
        public override void RefreshValues()
        {
            IsKnown = Hero.HasAbility(Template.StringID);
            base.RefreshValues();
        }
    }
}