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

        public static TORSkillEffects Instance { get; private set; }
        public static SkillEffect GunReloadSpeed => Instance._gunReloadSpeed;
        public static SkillEffect GunAccuracy => Instance._gunAccuracy;

        public TORSkillEffects()
        {
            Instance = this;
            if (!(Game.Current.GameType is Campaign)) return;
            _gunReloadSpeed = Game.Current.ObjectManager.RegisterPresumedObject<SkillEffect>(new SkillEffect("GunReloadSpeed"));
            _gunAccuracy = Game.Current.ObjectManager.RegisterPresumedObject<SkillEffect>(new SkillEffect("GunAccuracy"));

            _gunReloadSpeed.Initialize(new TextObject("{=!}Gunpowder firearms reload speed: +{a0} %", null), new SkillObject[]
            {
                TORSkills.GunPowder
            }, SkillEffect.PerkRole.Personal, 0.07f, SkillEffect.PerkRole.None, 0f, SkillEffect.EffectIncrementType.AddFactor, 0f, 0f);
            _gunAccuracy.Initialize(new TextObject("{=!}Gunpowder firearms accuracy: +{a0} %", null), new SkillObject[]
            {
                TORSkills.GunPowder
            }, SkillEffect.PerkRole.Personal, 0.05f, SkillEffect.PerkRole.None, 0f, SkillEffect.EffectIncrementType.AddFactor, 0f, 0f);
        }
    }
}
