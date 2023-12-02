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
            _faith.Initialize(new TextObject("{=tor_skill_faith_str}Faith", null), new TextObject("{=tor_skill_faith_description_str}Faith signifies your beliefs and your conviction in your chosen religion.", null), SkillObject.SkillTypeEnum.Personal).SetAttribute(TORAttributes.Discipline);
            _gunPowder.Initialize(new TextObject("{=tor_skill_gunpowder_str}Gunpowder", null), new TextObject("{=tor_skill_gunpowder_description_str}Gunpowder skill governs your ability to handle firearms and artillery.", null), SkillObject.SkillTypeEnum.Personal).SetAttribute(TORAttributes.Discipline);
            _spellCraft.Initialize(new TextObject("{=tor_skill_spellcraft_str}Spellcraft", null), new TextObject("{=tor_skill_spellcraft_description_str}Spellcraft is your ability to use magic.", null), SkillObject.SkillTypeEnum.Personal).SetAttribute(TORAttributes.Discipline);
        }
    }
}
