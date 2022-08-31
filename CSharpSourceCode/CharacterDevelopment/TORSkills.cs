using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TOR_Core.CharacterDevelopment
{
    public class TORSkills
    {
        private SkillObject _faith;
        private SkillObject _gunPowder;
        private SkillObject _spellCraft;

        public static TORSkills Instance { get; private set; }
        public static SkillObject Faith => Instance._faith;
        public static SkillObject GunPowder => Instance._gunPowder;
        public static SkillObject SpellCraft => Instance._spellCraft;

        public TORSkills()
        {
            Instance = this;
            _faith = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("Faith"));
            _gunPowder = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("Gunpowder"));
            _spellCraft = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("Spellcraft"));
            _faith.Initialize(new TextObject("{=!}Faith", null), new TextObject("{=!}Faith is lorem ipsum...", null), SkillObject.SkillTypeEnum.Personal).SetAttribute(TORAttributes.Wisdom);
            _gunPowder.Initialize(new TextObject("{=!}Gunpowder", null), new TextObject("{=!}Gunpowder is lorem ipsum...", null), SkillObject.SkillTypeEnum.Personal).SetAttribute(TORAttributes.Wisdom);
            _spellCraft.Initialize(new TextObject("{=!}Spellcraft", null), new TextObject("{=!}Spellcraft is lorem ipsum...", null), SkillObject.SkillTypeEnum.Personal).SetAttribute(TORAttributes.Wisdom);
        }
    }
}
