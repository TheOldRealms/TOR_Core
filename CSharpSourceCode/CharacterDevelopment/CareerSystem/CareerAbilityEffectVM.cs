using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerAbilityEffectVM : ViewModel
    {
        private string _text;

        public CareerAbilityEffectVM(TextObject text)
        {
            _text = text.ToString();
        }

        [DataSourceProperty]
        public string LineText
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
                    OnPropertyChangedWithValue(value, "LineText");
                }
            }
        }
    }
}