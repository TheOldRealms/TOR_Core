using TaleWorlds.Core;
using TOR_Core.Extensions;

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
            _discipline.Initialize(TORTextHelper.GetTextObject("tor_attribute_discipline", "Discipline"), TORTextHelper.GetTextObject("tor_attribute_discipline_description", "Discipline represents mental fortitude and self-control."), TORTextHelper.GetTextObject("tor_attribute_discipline_abbreviation", "DIS"));
        }
    }
}