using System;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Utilities;
using TOR_Core.Extensions;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;

namespace TOR_Core.AbilitySystem
{
    public interface ITemplate
    {
        string StringID { get; }
        ITemplate Clone(string newId);
    }

    [Serializable]
    public class AbilityTemplate : ITemplate
    {
        [XmlAttribute]
        public string StringID { get; set; } = "";
        [XmlAttribute]
        public string Name { get; set; } = "";
        [XmlAttribute]
        public string SpriteName { get; set; } = "";
        [XmlAttribute]
        public int CoolDown { get; set; } = 10;
        [XmlAttribute]
        public int WindsOfMagicCost { get; set; } = 0; //spell only
        [XmlAttribute]
        public float BaseMisCastChance { get; set; } = 0.3f; //spell only
        [XmlAttribute]
        public float Duration { get; set; } = 3;
        [XmlAttribute]
        public float Radius { get; set; } = 0.8f;
        [XmlAttribute]
        public AbilityType AbilityType { get; set; } = AbilityType.Spell;
        [XmlAttribute]
        public AbilityEffectType AbilityEffectType { get; set; } = AbilityEffectType.Missile;
        [XmlAttribute]
        public float BaseMovementSpeed { get; set; } = 35f;
        [XmlAttribute]
        public float TickInterval { get; set; } = 1f;
        [XmlAttribute]
        public TriggerType TriggerType { get; set; } = TriggerType.OnCollision;
        [XmlElement("TriggeredEffect")]
        public List<string> TriggeredEffects { get; set; } = new List<string>();
        [XmlAttribute]
        public bool HasLight { get; set; } = true;
        [XmlAttribute]
        public float LightIntensity { get; set; } = 5f;
        [XmlAttribute]
        public float LightRadius { get; set; } = 5f;
        [XmlElement]
        public Vec3 LightColorRGB { get; set; } = new Vec3(255, 170, 0);
        [XmlAttribute]
        public float LightFlickeringMagnitude { get; set; } = 1;
        [XmlAttribute]
        public float LightFlickeringInterval { get; set; } = 0.2f;
        [XmlAttribute]
        public bool ShadowCastEnabled { get; set; } = true;
        [XmlAttribute]
        public string ParticleEffectPrefab { get; set; } = "";
        [XmlAttribute]
        public float ParticleEffectSizeModifier { get; set; } = 1;
        [XmlAttribute]
        public string SoundEffectToPlay { get; set; } = "";
        [XmlAttribute]
        public bool ShouldSoundLoopOverDuration { get; set; } = true;
        [XmlAttribute]
        public CastType CastType { get; set; } = CastType.Instant;
        [XmlAttribute]
        public float CastTime { get; set; } = 0;
        [XmlAttribute]
        public string AnimationActionName { get; set; } = "";
        [XmlAttribute]
        public AbilityTargetType AbilityTargetType { get; set; } = AbilityTargetType.EnemiesInAOE;
        [XmlAttribute]
        public float Offset { get; set; } = 1.0f;
        [XmlAttribute]
        public CrosshairType CrosshairType { get; set; } = CrosshairType.Self;
        [XmlAttribute]
        public float MinDistance { get; set; } = 1.0f;
        [XmlAttribute]
        public float MaxDistance { get; set; } = 1.0f;
        [XmlAttribute]
        public float TargetCapturingRadius { get; set; } = 0;
        [XmlAttribute]
        public int SpellTier { get; set; } = 0; //spell only, max 3 (0-1-2-3)
        [XmlAttribute]
        public string BelongsToLoreID { get; set; } = ""; //spell only
        [XmlAttribute]
        public string TooltipDescription { get; set; } = "default";
        [XmlAttribute]
        public float MaxRandomDeviation { get; set; } = 0;
        [XmlAttribute]
        public bool ShouldRotateVisuals { get; set; } = false;
        [XmlAttribute]
        public bool DoNotAlignParticleEffectPrefab { get; set; } = false;
        [XmlElement] 
        public SeekerParameters SeekerParameters { get; set; } = null;
        [XmlIgnore] 
        public float ScaleVariable1 { get; set; } = 0f;
        public float VisualsRotationVelocity { get; set; } = 0f;
        
        [XmlIgnore]
        public bool IsSpell => AbilityType == AbilityType.Spell;
        [XmlIgnore]
        public List<TriggeredEffectTemplate> AssociatedTriggeredEffectTemplates => TriggeredEffectManager.GetTemplatesWithIds(TriggeredEffects);
        [XmlIgnore]
        public bool DoesDamage => AssociatedTriggeredEffectTemplates.Any(x=> x.DamageType != DamageType.Invalid && x.DamageAmount > 0);
        [XmlIgnore]
        public int GoldCost
        {
            get
            {
                switch (SpellTier)
                {
                    case 1: return 5000;
                    case 2: return 10000;
                    case 3: return 25000;
                    case 4: return 50000;
                    default: return 0;
                }
            }
        }

       
        
        public AbilityTemplate() { }
        public AbilityTemplate(string id) => StringID = id;
        public MBBindingList<StatItemVM> GetStats(Hero hero, AbilityTemplate spellTemplate)
        {
            MBBindingList<StatItemVM> list = new MBBindingList<StatItemVM>();
            if (IsSpell)
            {
                list.Add(new StatItemVM(new TextObject ("{=tor_spell_stat_tag_name_str}Spell Name: ").ToString(), new TextObject(Name).ToString()));
                list.Add(new StatItemVM(new TextObject ("{=tor_spell_stat_tag_wom_cost_str}Winds of Magic cost: ").ToString(), hero.GetEffectiveWindsCostForSpell(spellTemplate) + CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText()));
                list.Add(new StatItemVM(new TextObject ("{=tor_spell_stat_tag_tier_str}Spell Tier: ").ToString(), ((SpellCastingLevel)SpellTier).ToString()));
                list.Add(new StatItemVM(new TextObject ("{=tor_spell_stat_tag_type_str}Spell Type: ").ToString(), AbilityEffectType.ToString()));
                list.Add(new StatItemVM(new TextObject ("{=tor_spell_stat_tag_cooldown_str}Cooldown: ").ToString(), CoolDown.ToString()+" seconds"));
            }
            else if (AbilityType == AbilityType.Prayer)
            {
                list.Add(new StatItemVM(new TextObject ("{=tor_spell_stat_tag_name_str}Prayer Name: ").ToString(), new TextObject(Name).ToString()));
                list.Add(new StatItemVM(new TextObject ("{=tor_spell_stat_tag_tier_str}Prayer Tier: ").ToString(), ((PrayerLevel)SpellTier).ToString()));
                list.Add(new StatItemVM(new TextObject ("{=tor_spell_stat_tag_type_str}Prayer Type: ").ToString(), AbilityEffectType.ToString()));
                list.Add(new StatItemVM(new TextObject ("{=tor_spell_stat_tag_cooldown_str}Cooldown: ").ToString(), CoolDown.ToString()+" seconds"));
            }
            return list;
        }

        public ITemplate Clone(string newId)
        {
            return new AbilityTemplate(newId)
            {
                Name = Name,
                SpriteName  = SpriteName,
                CoolDown = CoolDown,
                WindsOfMagicCost = WindsOfMagicCost,
                BaseMisCastChance = BaseMisCastChance,
                Duration = Duration,
                Radius = Radius,
                AbilityType = AbilityType,
                AbilityEffectType = AbilityEffectType,
                BaseMovementSpeed = BaseMovementSpeed,
                TickInterval = TickInterval,
                TriggerType = TriggerType,
                TriggeredEffects = TriggeredEffects,
                HasLight = HasLight,
                LightIntensity = LightIntensity,
                LightRadius = LightRadius,
                LightColorRGB = LightColorRGB,
                LightFlickeringMagnitude = LightFlickeringMagnitude,
                LightFlickeringInterval = LightFlickeringInterval,
                ShadowCastEnabled = ShadowCastEnabled,
                ParticleEffectPrefab = ParticleEffectPrefab,
                ParticleEffectSizeModifier = ParticleEffectSizeModifier,
                SoundEffectToPlay = SoundEffectToPlay,
                ShouldSoundLoopOverDuration = ShouldSoundLoopOverDuration,
                CastType = CastType,
                CastTime = CastTime,
                AnimationActionName = AnimationActionName,
                AbilityTargetType = AbilityTargetType,
                Offset = Offset,
                CrosshairType = CrosshairType,
                MinDistance = MinDistance,
                MaxDistance = MaxDistance,
                TargetCapturingRadius = TargetCapturingRadius,
                SpellTier = SpellTier,
                BelongsToLoreID = BelongsToLoreID,
                TooltipDescription = TooltipDescription,
                MaxRandomDeviation = MaxRandomDeviation,
                ShouldRotateVisuals = ShouldRotateVisuals,
                VisualsRotationVelocity = VisualsRotationVelocity,
                ScaleVariable1 = ScaleVariable1
            };
        }
    }
}
