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
    public class TORSkills
    {
        private SkillObject _faith;
        private SkillObject _gunPowder;
        private SkillObject _spellCraft;

        public static TORSkills Instance { get; private set; }
        public static SkillObject Faith => Instance._faith;
        public static SkillObject GunPowder => Instance._gunPowder;
        public static SkillObject Spellcraft => Instance._spellCraft;

        public TORSkills()
        {
            Instance = this;
            _faith = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("Faith"));
            _gunPowder = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("Gunpowder"));
            _spellCraft = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("Spellcraft"));
            _faith.Initialize(TORTextHelper.GetTextObject("tor_skill_faith", "Faith"), TORTextHelper.GetTextObject("tor_skill_faith_description", "Faith signifies your beliefs and your conviction in your chosen religion."), [TORAttributes.Discipline]);
            _gunPowder.Initialize(TORTextHelper.GetTextObject("tor_skill_gunpowder", "Gunpowder"), TORTextHelper.GetTextObject("tor_skill_gunpowder_description", "Gunpowder skill governs your ability to handle firearms and artillery."), [TORAttributes.Discipline]);
            _spellCraft.Initialize(TORTextHelper.GetTextObject("tor_skill_spellcraft", "Spellcraft"), TORTextHelper.GetTextObject("tor_skill_spellcraft_description", "Spellcraft is your ability to use magic."), [TORAttributes.Discipline]);
        }
    }
}