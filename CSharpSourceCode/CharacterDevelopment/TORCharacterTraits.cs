using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TOR_Core.CharacterDevelopment
{
    public class TORCharacterTraits
    {
        private TraitObject _spellCasterSkills;

        public static TORCharacterTraits Instance { get; private set; }
        public static TraitObject SpellCasterSkills => Instance._spellCasterSkills;

        public TORCharacterTraits()
        {
            Instance = this;
            _spellCasterSkills = Game.Current.ObjectManager.RegisterPresumedObject(new TraitObject("SpellCasterSkills"));
            _spellCasterSkills.Initialize(new TextObject("{=!}Spellcaster", null), new TextObject("{=!}Spellcaster Description", null), true, 0, 10);
        }
    }
}
