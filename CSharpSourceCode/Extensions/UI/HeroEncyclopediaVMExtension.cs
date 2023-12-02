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
using TaleWorlds.Localization;
using TaleWorlds.TwoDimension;
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
					var religionText = new TextObject("{=tor_religion_follower_none_str}Not a follower of any religion").ToString();
					if (_hero.HasAnyReligion())
					{
						var devotionLevelText = GameTexts.FindText ("tor_religion_devotionlevel", _hero.GetDevotionLevelForReligion (_hero.GetDominantReligion()).ToString());
						var religionNameText = GameTexts.FindText ("tor_religion_name_of_god", _hero.GetDominantReligion().StringId);
						var link = HyperlinkTexts.GetSettlementHyperlinkText (_hero.GetDominantReligion().EncyclopediaLink, religionNameText);
						MBTextManager.SetTextVariable ("TOR_DEVOTION_LEVEL",devotionLevelText);
						MBTextManager.SetTextVariable ("TOR_RELIGION",link);
						religionText = GameTexts.FindText ("tor_religion_text_frame").ToString();
					}

					var label = new TextObject("{=tor_religion_label}Religion") + ": ";
					heroVM.Stats.Add(new StringPairItemVM(label, religionText));
				}
			}
        }
	}
}
