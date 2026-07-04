using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

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
        public List<string> TriggeredEffects { get; set; } = [];
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

        private float _maxDistance = 25f;
        [XmlAttribute]
        public float MaxDistance
        {
            get => MaxDistanceSpecified ? _maxDistance : IsShortDefaultMaxDistance() ? 1f : 25f;
            set {_maxDistance = value; MaxDistanceSpecified = true;}
        }
        [XmlIgnore]
        public bool MaxDistanceSpecified { get; set; }
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
        [XmlAttribute]
        public bool UseGravity { get; set; } = false;
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
        public bool DoesDamage => AssociatedTriggeredEffectTemplates.Any(x => x.DamageType != DamageType.Invalid && x.DamageAmount > 0);
        [XmlIgnore]
        public bool DoesHeal => AssociatedTriggeredEffectTemplates.Any(x => x.DamageAmount < 0);
        [XmlIgnore]
        public int GoldCost
        {
            get
            {
                return SpellTier switch
                {
                    1 => 5000,
                    2 => 10000,
                    3 => 25000,
                    4 => 50000,
                    _ => 0,
                };
            }
        }

        private bool IsShortDefaultMaxDistance()
        {
            return AbilityEffectType == AbilityEffectType.Wind ||
                   AbilityEffectType == AbilityEffectType.Summoning ||
                   AbilityEffectType == AbilityEffectType.ArtilleryPlacement ||
                   AbilityEffectType == AbilityEffectType.ItemPlacement ||
                   AbilityEffectType == AbilityEffectType.TimeWarpEffect ||
                   AbilityEffectType == AbilityEffectType.CareerAbilityEffect ||
                   AbilityEffectType == AbilityEffectType.TacticalReposition;
        }

        public AbilityTemplate() { }
        public AbilityTemplate(string id) => StringID = id;
        public MBBindingList<StatItemVM> GetStats(Hero hero, AbilityTemplate spellTemplate)
        {
            MBBindingList<StatItemVM> list = new MBBindingList<StatItemVM>();
            if (IsSpell)
            {
                list.Add(new StatItemVM(TORTextHelper.GetText("tor_spell_stat_name", "Spell Name: "), new TextObject(Name).ToString()));
                list.Add(new StatItemVM(TORTextHelper.GetText("tor_spell_stat_wom_cost", "Winds of Magic cost: "), hero.GetEffectiveWindsCostForSpell(spellTemplate) + CustomResourceManager.GetResourceObject("WindsOfMagic").GetCustomResourceIconAsText()));
                var spellTierText = GameTexts.FindText("tor_spellcasting_level", ((SpellCastingLevel)SpellTier).ToString());
                list.Add(new StatItemVM(TORTextHelper.GetText("tor_spell_stat_tier", "Spell Tier: "), spellTierText.ToString()));
                var spellTypeText = GameTexts.FindText("tor_ability_effect_type", AbilityEffectType.ToString());
                list.Add(new StatItemVM(TORTextHelper.GetText("tor_spell_stat_type", "Spell Type: "), spellTypeText.ToString()));
                if (GameTexts.TryGetText("tor_cooldown_seconds", out var spellCooldownText))
                {
                    spellCooldownText.SetTextVariable("COOLDOWN", CoolDown);
                }
                else
                {
                    spellCooldownText = new TextObject(CoolDown.ToString() + " seconds");
                }

                list.Add(new StatItemVM(TORTextHelper.GetText("tor_spell_stat_cooldown", "Cooldown: "), spellCooldownText.ToString()));
            }
            else if (AbilityType == AbilityType.Prayer)
            {
                list.Add(new StatItemVM(TORTextHelper.GetText("tor_prayer_stat_name", "Prayer Name: "), new TextObject(Name).ToString()));
                var prayerTierText = GameTexts.FindText("tor_prayer_level", ((PrayerLevel)SpellTier).ToString());
                list.Add(new StatItemVM(TORTextHelper.GetText("tor_prayer_stat_tier", "Prayer Tier: "), prayerTierText.ToString()));
                var prayerTypeText = GameTexts.FindText("tor_ability_effect_type", AbilityEffectType.ToString());
                list.Add(new StatItemVM(TORTextHelper.GetText("tor_prayer_stat_type", "Prayer Type: "), prayerTypeText.ToString()));
                if (GameTexts.TryGetText("tor_cooldown_seconds", out var prayerCooldownText))
                {
                    prayerCooldownText.SetTextVariable("COOLDOWN", CoolDown);
                }
                else
                {
                    prayerCooldownText = new TextObject(CoolDown.ToString() + " seconds");
                }
                list.Add(new StatItemVM(TORTextHelper.GetText("tor_prayer_stat_cooldown", "Cooldown: "), prayerCooldownText.ToString()));
            }
            return list;
        }

        public ITemplate Clone(string newId)
        {
            return new AbilityTemplate(newId)
            {
                Name = Name,
                SpriteName = SpriteName,
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
                _maxDistance = _maxDistance,
                MaxDistanceSpecified = MaxDistanceSpecified,
                TargetCapturingRadius = TargetCapturingRadius,
                SpellTier = SpellTier,
                BelongsToLoreID = BelongsToLoreID,
                TooltipDescription = TooltipDescription,
                MaxRandomDeviation = MaxRandomDeviation,
                ShouldRotateVisuals = ShouldRotateVisuals,
                VisualsRotationVelocity = VisualsRotationVelocity,
                ScaleVariable1 = ScaleVariable1,
                UseGravity = UseGravity
            };
        }
    }
}