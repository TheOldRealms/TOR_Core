using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.Religion;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(EncyclopediaHeroPageVM))]
    public class HeroEncyclopediaVMExtension : BaseViewModelExtension
    {
		private Hero _hero;

        public HeroEncyclopediaVMExtension(ViewModel vm) : base(vm)
        {
			if(vm is EncyclopediaHeroPageVM)
            {
				var heroVM = vm as EncyclopediaHeroPageVM;
				_hero = heroVM.Obj as Hero;
            }
			RefreshValues();
        }

        public override void RefreshValues()
        {
			//have no idea why, but the vm gets passed in "empty" to the constructor, have to acquire the hero reference here.
			if(_vm is EncyclopediaHeroPageVM)
            {
				var heroVM = _vm as EncyclopediaHeroPageVM;
				if (_hero == null)
				{
					_hero = heroVM.Obj as Hero;
				}
				if (_hero != null)
				{
					var list = ReligionObject.All;
					string religionText = "Not a follower of any religion";
					if (_hero.HasAnyReligion())
					{
						religionText = string.Format("{0} of {1}", _hero.GetDevotionLevelForReligion(_hero.GetDominantReligion()) ,_hero.GetDominantReligion().EncyclopediaLinkWithName.ToString());
					}
					heroVM.Stats.Add(new StringPairItemVM("Religion:", religionText));
				}
			}
        }
	}
}
