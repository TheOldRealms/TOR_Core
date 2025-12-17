using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment
{
    public class TORSkillEffects
    {
        private SkillEffect _gunReloadSpeed;
        private SkillEffect _gunAccuracy;
        private SkillEffect _spellEffectiveness;
        private SkillEffect _prayerEffectiveness;
        private SkillEffect _spellDuration;
        private SkillEffect _windsRechargeRate;
        private SkillEffect _maxWinds;
        private SkillEffect _faithWardSave;
        private SkillEffect _blessingDuration;
        private SkillEffect _prayerDuration;

        public static TORSkillEffects Instance { get; private set; }
        public static SkillEffect GunReloadSpeed => Instance._gunReloadSpeed;
        public static SkillEffect GunAccuracy => Instance._gunAccuracy;
        public static SkillEffect SpellEffectiveness => Instance._spellEffectiveness;
        public static SkillEffect PrayerEffectiveness => Instance._prayerEffectiveness;
        public static SkillEffect SpellDuration => Instance._spellDuration;

        public static SkillEffect PrayerDuration => Instance._prayerDuration;
        public static SkillEffect WindsRechargeRate => Instance._windsRechargeRate;
        public static SkillEffect MaxWinds => Instance._maxWinds;
        public static SkillEffect FaithWardSave => Instance._faithWardSave;
        public static SkillEffect BlessingDuration => Instance._blessingDuration;

        public TORSkillEffects()
        {
            Instance = this;
            _gunReloadSpeed = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("GunReloadSpeed"));
            _gunAccuracy = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("GunAccuracy"));
            _spellEffectiveness = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("SpellEffectiveness"));
            _spellDuration = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("SpellDuration"));
            _windsRechargeRate = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("WindsRechargeRate"));
            _maxWinds = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("MaxWinds"));
            _faithWardSave = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("FaithWardSave"));
            _blessingDuration = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("BlessingDuration"));
            _prayerEffectiveness = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("PrayerEffectiveness"));
            _prayerDuration = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("PrayerDuration"));

            _gunReloadSpeed.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_gun_reload_speed", "Gunpowder firearms reload speed: +{a0} %"),
            TORSkills.GunPowder, PartyRole.Personal, 0.0007f, EffectIncrementType.AddFactor, 0f, 0f);

            _gunAccuracy.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_gun_accuracy", "Gunpowder firearms accuracy: +{a0} %"),
            TORSkills.GunPowder, PartyRole.Personal, 0.0005f, EffectIncrementType.AddFactor, 0f, 0f);

            _spellEffectiveness.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_spell_effectiveness", "Spell effectiveness: +{a0} %"),
            TORSkills.Spellcraft, PartyRole.Personal, 0.0005f, EffectIncrementType.AddFactor, 0f, 0f);

            _spellDuration.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_spell_duration", "Spell duration: +{a0} %"),
            TORSkills.Spellcraft, PartyRole.Personal, 0.0005f, EffectIncrementType.AddFactor, 0f, 0f);

            _windsRechargeRate.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_winds_recharge_rate", "Winds of magic recharge rate: +{a0} / hour"),
            TORSkills.Spellcraft, PartyRole.Personal, 0.0075f, EffectIncrementType.Add, 0f, 0f);

            _maxWinds.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_max_winds", "Maximum winds of magic: +{a0}"),
            TORSkills.Spellcraft, PartyRole.Personal, 0.3f, EffectIncrementType.Add, 0f, 0f);

            _faithWardSave.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_ward_save", "Ward save: +{a0} %"),
            TORSkills.Faith, PartyRole.Personal, 0.0008f, EffectIncrementType.AddFactor, 0f, 0f);

            _blessingDuration.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_blessing_duration", "Blessing duration increase: +{a0} %"),
            TORSkills.Faith, PartyRole.PartyLeader, 0.01f, EffectIncrementType.AddFactor, 0f, 0f);

            _prayerEffectiveness.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_prayer_effectiveness", "Prayer effectiveness(Only Priests): +{a0} %"),
            TORSkills.Faith, PartyRole.Personal, 0.00025f, EffectIncrementType.AddFactor, 0f, 0f);

            _prayerDuration.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_prayer_duration", "Prayer duration increase(Only Priests): +{a0} %"),
            TORSkills.Faith, PartyRole.Personal, 0.00025f, EffectIncrementType.AddFactor, 0f, 0f);
        }
    }
}