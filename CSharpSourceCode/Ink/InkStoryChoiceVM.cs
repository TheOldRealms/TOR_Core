using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TOR_Core.Ink
{
    public class InkStoryChoiceVM : ViewModel
    {
        private string _text = string.Empty;
        private int _index = 0;
        private Action<int> _onSelected;

        public InkStoryChoiceVM(int choiceIndex, string choiceText, Action<int> onSelected)
        {
            _index = choiceIndex;
            _text = choiceText;
            _onSelected = onSelected;
        }

        private void ExecuteSelect()
        {
            if (_onSelected != null) _onSelected(_index);
        }

        [DataSourceProperty]
        public string ChoiceText
        {
            get
            {
                return _text;
            }
            set
            {
                if (value != _text)
                {
                    _text = value;
                    OnPropertyChangedWithValue(value, "ChoiceText");
                }
            }
        }
    }
}
