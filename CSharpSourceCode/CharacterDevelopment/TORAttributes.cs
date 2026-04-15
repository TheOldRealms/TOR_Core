using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

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