using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TOR_Core.CharacterDevelopment
{
    public class TORAttributes
    {
        private CharacterAttribute _wisdom;

        public static TORAttributes Instance { get; private set; }

        public static CharacterAttribute Wisdom => Instance._wisdom;

        public TORAttributes()
        {
            Instance = this;
            _wisdom = Game.Current.ObjectManager.RegisterPresumedObject(new CharacterAttribute("wisdom"));
            _wisdom.Initialize(new TextObject("{=!}Wisdom", null), new TextObject("{=!}Wisdom represents lorem ipsum...", null), new TextObject("{=!}WIS", null));
        }
    }
}
