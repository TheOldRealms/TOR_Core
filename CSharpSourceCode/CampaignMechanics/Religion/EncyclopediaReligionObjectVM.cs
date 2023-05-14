using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.CampaignMechanics.Religion
{
    [EncyclopediaViewModel(typeof(ReligionObject))]
    public class EncyclopediaReligionObjectVM : EncyclopediaContentPageVM
    {
        private string _titleText;
        private string _descriptionText;
        private ReligionObject _religionObject;
        private MBBindingList<HeroVM> _followers = new MBBindingList<HeroVM>();
        private MBBindingList<EncyclopediaUnitVM> _troops = new MBBindingList<EncyclopediaUnitVM>();

        public EncyclopediaReligionObjectVM(EncyclopediaPageArgs args) : base(args)
        {
            if(args.Obj != null && args.Obj is ReligionObject)
            {
                _religionObject = args.Obj as ReligionObject;
            }
            RefreshValues();
        }

        public override void RefreshValues()
        {
            if(_religionObject != null)
            {
                TitleText = _religionObject.Name.ToString();
                DescriptionText = _religionObject.LoreText.ToString();
                foreach(var follower in _religionObject.CurrentFollowers)
                {
                    Followers.Add(new HeroVM(follower));
                }
                foreach(var troop in _religionObject.ReligiousTroops)
                {
                    ReligiousTroops.Add(new EncyclopediaUnitVM(troop, false));
                }
            }
        }

        [DataSourceProperty]
        public string TitleText
        {
            get
            {
                return _titleText;
            }
            set
            {
                if (value != _titleText)
                {
                    _titleText = value;
                    OnPropertyChangedWithValue(value, "TitleText");
                }
            }
        }

        [DataSourceProperty]
        public string DescriptionText
        {
            get
            {
                return _descriptionText;
            }
            set
            {
                if (value != _descriptionText)
                {
                    _descriptionText = value;
                    OnPropertyChangedWithValue(value, "DescriptionText");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<HeroVM> Followers
        {
            get
            {
                return _followers;
            }
            set
            {
                if (value != _followers)
                {
                    _followers = value;
                    OnPropertyChangedWithValue(value, "Followers");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<EncyclopediaUnitVM> ReligiousTroops
        {
            get
            {
                return _troops;
            }
            set
            {
                if (value != _troops)
                {
                    _troops = value;
                    OnPropertyChangedWithValue(value, "ReligiousTroops");
                }
            }
        }
    }
}
