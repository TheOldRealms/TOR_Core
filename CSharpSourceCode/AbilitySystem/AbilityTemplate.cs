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

namespace TOR_Core.AbilitySystem
{
    [Serializable]
    public class AbilityTemplate
    {
        [XmlAttribute]
        public string StringID = "";
        [XmlAttribute]
        public string Name = "";
        [XmlAttribute]
        public string SpriteName  = "";
        [XmlAttribute]
        public int CoolDown = 10;
        [XmlAttribute]
        public int WindsOfMagicCost = 0; //spell only
        [XmlAttribute]
        public float BaseMisCastChance = 0.3f; //spell only
        [XmlAttribute]
        public float Duration = 3;
        [XmlAttribute]
        public float Radius = 0.8f;
        [XmlAttribute]
        public AbilityType AbilityType = AbilityType.Spell;
        [XmlAttribute]
        public AbilityEffectType AbilityEffectType = AbilityEffectType.Missile;
        [XmlAttribute]
        public float BaseMovementSpeed = 35f;
        [XmlAttribute]
        public float TickInterval = 1f;
        [XmlAttribute]
        public TriggerType TriggerType = TriggerType.OnCollision;
        [XmlAttribute]
        public string TriggeredEffectID = "";
        [XmlAttribute]
        public bool HasLight = true;
        [XmlAttribute]
        public float LightIntensity = 5f;
        [XmlAttribute]
        public float LightRadius = 5f;
        public Vec3 LightColorRGB = new Vec3(255, 170, 0);
        [XmlAttribute]
        public float LightFlickeringMagnitude = 1;
        [XmlAttribute]
        public float LightFlickeringInterval = 0.2f;
        [XmlAttribute]
        public bool ShadowCastEnabled = true;
        [XmlAttribute]
        public string ParticleEffectPrefab = "";
        [XmlAttribute]
        public float ParticleEffectSizeModifier = 1;
        [XmlAttribute]
        public string SoundEffectToPlay = "";
        [XmlAttribute]
        public bool ShouldSoundLoopOverDuration = true;
        [XmlAttribute]
        public CastType CastType = CastType.Instant;
        [XmlAttribute]
        public float CastTime = 0;
        [XmlAttribute]
        public string AnimationActionName = "";
        [XmlAttribute]
        public AbilityTargetType AbilityTargetType = AbilityTargetType.EnemiesInAOE;
        [XmlAttribute]
        public float Offset = 1.0f;
        [XmlAttribute]
        public CrosshairType CrosshairType = CrosshairType.Self;
        [XmlAttribute]
        public float MinDistance = 1.0f;
        [XmlAttribute]
        public float MaxDistance = 1.0f;
        [XmlAttribute]
        public float TargetCapturingRadius = 0;
        [XmlAttribute]
        public int SpellTier = 0; //spell only, max 3 (0-1-2-3)
        [XmlAttribute]
        public string BelongsToLoreID = ""; //spell only
        [XmlAttribute]
        public string TooltipDescription = "default";
        [XmlAttribute]
        public float MaxRandomDeviation = 0;
        [XmlAttribute]
        public bool ShouldRotateVisuals = false;
        [XmlAttribute]
        public float VisualsRotationVelocity = 0f;

        public SeekerParameters SeekerParameters;
        
        public AbilityTemplate() { }
        public AbilityTemplate(string id) => StringID = id;

        public bool IsSpell => AbilityType == AbilityType.Spell;
        public TriggeredEffectTemplate AssociatedTriggeredEffectTemplate => TriggeredEffectManager.GetTemplateWithId(TriggeredEffectID);
        public bool DoesDamage => AssociatedTriggeredEffectTemplate?.DamageType != DamageType.Invalid && AssociatedTriggeredEffectTemplate?.DamageAmount > 0;
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

        public MBBindingList<StatItemVM> GetStats(Hero hero, AbilityTemplate spellTemplate)
        {
            MBBindingList<StatItemVM> list = new MBBindingList<StatItemVM>();
            if (IsSpell)
            {
                list.Add(new StatItemVM("Spell Name: ", Name));
                list.Add(new StatItemVM("Winds of Magic cost: ", hero.GetEffectiveWindsCostForSpell(spellTemplate) + TORCommon.GetWindsIconAsText()));
                list.Add(new StatItemVM("Spell Tier: ", ((SpellCastingLevel)SpellTier).ToString()));
                list.Add(new StatItemVM("Spell Type: ", AbilityEffectType.ToString()));
                list.Add(new StatItemVM("Cooldown: ", CoolDown.ToString()+" seconds"));
            }
            return list;
        }

        public AbilityTemplate Clone(string newId)
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
                TriggeredEffectID = TriggeredEffectID,
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
                VisualsRotationVelocity = VisualsRotationVelocity
            };
        }
    }
}
