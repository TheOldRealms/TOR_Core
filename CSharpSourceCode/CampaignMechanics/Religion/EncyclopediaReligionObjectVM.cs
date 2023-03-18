using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace TOR_Core.CampaignMechanics.Religion
{
    [EncyclopediaViewModel(typeof(ReligionObject))]
    public class EncyclopediaReligionObjectVM : EncyclopediaContentPageVM
    {
        private string _titleText;
        private string _descriptionText;
        private ReligionObject _religionObject;

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
    }
}
