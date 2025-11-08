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
        private CharacterAttribute _discipline;

        public static TORAttributes Instance { get; private set; }

        public static CharacterAttribute Discipline => Instance._discipline;

        public TORAttributes()
        {
            Instance = this;
            _discipline = Game.Current.ObjectManager.RegisterPresumedObject(new CharacterAttribute("discipline"));
            _discipline.Initialize(new TextObject("{=tor_attribute_discipline_str}Discipline", null), new TextObject("{=tor_attribute_discipline_description_str}Discipline is the ability to refine your skill in certain skills which require practice or focus.", null), new TextObject("{=tor_attribute_discipline_abbreviation_str}DIS", null));
        }
    }
}
