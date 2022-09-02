using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TOR_Core.CharacterDevelopment
{
    public class TORSkillEffects
    {
        private SkillEffect _gunReloadSpeed;
        private SkillEffect _gunAccuracy;
        private SkillEffect _spellEffectiveness;
        private SkillEffect _spellDuration;

        public static TORSkillEffects Instance { get; private set; }
        public static SkillEffect GunReloadSpeed => Instance._gunReloadSpeed;
        public static SkillEffect GunAccuracy => Instance._gunAccuracy;
        public static SkillEffect SpellEffectiveness => Instance._spellEffectiveness;
        public static SkillEffect SpellDuration => Instance._spellDuration;

        public TORSkillEffects()
        {
            Instance = this;
            _gunReloadSpeed = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("GunReloadSpeed"));
            _gunAccuracy = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("GunAccuracy"));
            _spellEffectiveness = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("SpellEffectiveness"));
            _spellDuration = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("SpellDuration"));

            _gunReloadSpeed.Initialize(new TextObject("{=!}Gunpowder firearms reload speed: +{a0} %", null), new SkillObject[]
            {
                TORSkills.GunPowder
            }, SkillEffect.PerkRole.Personal, 0.07f, SkillEffect.PerkRole.None, 0f, SkillEffect.EffectIncrementType.AddFactor, 0f, 0f);
            _gunAccuracy.Initialize(new TextObject("{=!}Gunpowder firearms accuracy: +{a0} %", null), new SkillObject[]
            {
                TORSkills.GunPowder
            }, SkillEffect.PerkRole.Personal, 0.05f, SkillEffect.PerkRole.None, 0f, SkillEffect.EffectIncrementType.AddFactor, 0f, 0f);
            _spellEffectiveness.Initialize(new TextObject("{=!}Spell effectiveness: +{a0} %", null), new SkillObject[]
            {
                TORSkills.SpellCraft
            }, SkillEffect.PerkRole.Personal, 0.05f, SkillEffect.PerkRole.None, 0f, SkillEffect.EffectIncrementType.AddFactor, 0f, 0f);
            _spellDuration.Initialize(new TextObject("{=!}Spell duration: +{a0} %", null), new SkillObject[]
            {
                TORSkills.SpellCraft
            }, SkillEffect.PerkRole.Personal, 0.05f, SkillEffect.PerkRole.None, 0f, SkillEffect.EffectIncrementType.AddFactor, 0f, 0f);
        }
    }
}
