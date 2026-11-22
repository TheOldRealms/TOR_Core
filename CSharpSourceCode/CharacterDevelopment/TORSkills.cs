using TaleWorlds.Core;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment
{
    public class TORSkills
    {
        public const string EmptySkillSlotStringId = "TOR_EmptySkillSlot";

        private SkillObject _faith;
        private SkillObject _gunPowder;
        private SkillObject _spellCraft;
        private SkillObject _feralCombat;
        private SkillObject _emptySkillSlot;

        public static TORSkills Instance { get; private set; }
        public static SkillObject Faith => Instance._faith;
        public static SkillObject GunPowder => Instance._gunPowder;
        public static SkillObject Spellcraft => Instance._spellCraft;
        public static SkillObject FeralCombat => Instance._feralCombat;

        /// <summary>
        /// Sentinel skill used by the Character Developer UI extension to pad
        /// each attribute's skill row to a fixed 4-column width. Attached to
        /// no attribute; never earns XP. Identified at bind time by
        /// <see cref="EmptySkillSlotStringId"/>.
        /// </summary>
        public static SkillObject EmptySkillSlot => Instance._emptySkillSlot;

        public TORSkills()
        {
            Instance = this;
            _faith = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("Faith"));
            _gunPowder = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("Gunpowder"));
            _spellCraft = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("Spellcraft"));
            _feralCombat = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("FeralCombat"));
            // Registered with ObjectManager so SkillVM's ctor can call
            // hero.GetSkillValue(sentinel) + hero.HeroDeveloper.GetFocus(sentinel)
            // without NREing on unknown PropertyObject keys. Filtered out
            // of attribute grouping downstream by its empty Attributes array.
            _emptySkillSlot = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject(EmptySkillSlotStringId));
            _faith.Initialize(TORTextHelper.GetTextObject("tor_skill_faith", "Faith"), TORTextHelper.GetTextObject("tor_skill_faith_description", "Faith signifies your beliefs and your conviction in your chosen religion."), [DefaultCharacterAttributes.Social]);
            _gunPowder.Initialize(TORTextHelper.GetTextObject("tor_skill_gunpowder", "Gunpowder"), TORTextHelper.GetTextObject("tor_skill_gunpowder_description", "Gunpowder skill governs your ability to handle firearms and artillery."), [DefaultCharacterAttributes.Control]);
            _spellCraft.Initialize(TORTextHelper.GetTextObject("tor_skill_spellcraft", "Spellcraft"), TORTextHelper.GetTextObject("tor_skill_spellcraft_description", "Spellcraft is your ability to use magic."), [DefaultCharacterAttributes.Intelligence]);
            _feralCombat.Initialize(TORTextHelper.GetTextObject("tor_skill_feral_combat", "Feral Combat"), TORTextHelper.GetTextObject("tor_skill_feral_combat_description", "Feral Combat governs natural weapons, claws, and the paired weapons of those who fight without a shield."), [DefaultCharacterAttributes.Vigor]);
            // Sentinel: initialize with empty strings and no attribute so it
            // never appears in a real skill list. UI extension pads with it.
            _emptySkillSlot.Initialize(TORTextHelper.GetTextObject("tor_skill_empty_slot", " "), TORTextHelper.GetTextObject("tor_skill_empty_slot_description", " "), System.Array.Empty<CharacterAttribute>());
        }
    }
}