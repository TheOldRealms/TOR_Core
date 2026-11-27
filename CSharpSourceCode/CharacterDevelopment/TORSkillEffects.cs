using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.BattleMechanics.DualWield;
using TOR_Core.Extensions;

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
        private SkillEffect _ambushSpottingBonus;
        private SkillEffect _berserkSwingSpeed;
        private SkillEffect _berserkMovementSpeed;

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
        public static SkillEffect AmbushSpottingBonus => Instance._ambushSpottingBonus;
        public static SkillEffect BerserkSwingSpeed => Instance._berserkSwingSpeed;
        public static SkillEffect BerserkMovementSpeed => Instance._berserkMovementSpeed;

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
            _ambushSpottingBonus = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("AmbushSpottingBonus"));
            _berserkSwingSpeed = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("BerserkSwingSpeed"));
            _berserkMovementSpeed = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("BerserkMovementSpeed"));

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
            
            //wardsave is reducing the damage multiplier below 1.00 and is therefore an addition of a negative
            _faithWardSave.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_ward_save", "Ward save: +{a0} %"),
            TORSkills.Faith, PartyRole.Personal, -0.0008f, EffectIncrementType.AddFactor);

            _blessingDuration.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_blessing_duration", "Blessing duration increase: +{a0} %"),
            TORSkills.Faith, PartyRole.PartyLeader, 0.01f, EffectIncrementType.AddFactor, 0f, 0f);

            _prayerEffectiveness.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_prayer_effectiveness", "Prayer effectiveness(Only Priests): +{a0} %"),
            TORSkills.Faith, PartyRole.Personal, 0.00025f, EffectIncrementType.AddFactor, 0f, 0f);

            _prayerDuration.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_prayer_duration", "Prayer duration increase(Only Priests): +{a0} %"),
            TORSkills.Faith, PartyRole.Personal, 0.00025f, EffectIncrementType.AddFactor, 0f, 0f);

            _ambushSpottingBonus.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_ambush_spotting", "Ambush detection: +{a0} % (combined from leader and Scout, higher dominates)"),
            DefaultSkills.Scouting, PartyRole.Scout, 0.0015f, EffectIncrementType.AddFactor);

            // Berserk display effects. Coefficients MUST stay in sync with
            // BerserkBonusModel.SwingSpeedPerLevel / MovementSpeedPerLevel so
            // the "Personal: X" tooltip reads the same number the runtime applies.
            _berserkSwingSpeed.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_berserk_swing_speed", "Paired-weapons swing speed: +{a0} %"),
            TORSkills.Berserk, PartyRole.Personal, BerserkBonusModel.SwingSpeedPerLevel, EffectIncrementType.AddFactor, 0f, 0f);

            _berserkMovementSpeed.Initialize(TORTextHelper.GetTextObject("tor_skill_effect_berserk_movement_speed", "Paired-weapons movement speed: +{a0} %"),
            TORSkills.Berserk, PartyRole.Personal, BerserkBonusModel.MovementSpeedPerLevel, EffectIncrementType.AddFactor, 0f, 0f);
        }
    }
}