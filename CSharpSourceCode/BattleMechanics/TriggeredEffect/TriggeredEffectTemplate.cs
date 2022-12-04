using System;
using System.Xml.Serialization;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    [Serializable]
    public class TriggeredEffectTemplate
    {
        [XmlAttribute]
        public string StringID = "";
        [XmlAttribute]
        public string BurstParticleEffectPrefab = "none";
        [XmlAttribute]
        public string SoundEffectId = "none";
        [XmlAttribute]
        public float SoundEffectLength = 2.5f;
        [XmlAttribute]
        public DamageType DamageType = DamageType.Fire;
        [XmlAttribute]
        public int DamageAmount = 50;
        [XmlAttribute]
        public float Radius = 5;
        [XmlAttribute]
        public bool HasShockWave = false;
        [XmlAttribute]
        public TargetType TargetType = TargetType.Enemy;
        [XmlAttribute]
        public string ImbuedStatusEffectID = "none";
        [XmlAttribute]
        public float ImbuedStatusEffectDuration = 5f;
        [XmlAttribute]
        public float DamageVariance = 0.2f;
        [XmlAttribute]
        public string ScriptNameToTrigger = "none";
        [XmlAttribute]
        public string SpawnPrefabName = "none";
        [XmlAttribute]
        public string TroopIdToSummon = "none";
        [XmlAttribute]
        public int NumberToSummon = 0;

        public StatusEffectTemplate AssociatedStatusEffect => StatusEffectManager.GetStatusEffectTemplateWithId(ImbuedStatusEffectID);

        public TriggeredEffectTemplate Clone(string newId)
        {
            return new TriggeredEffectTemplate()
            {
                StringID = newId,
                BurstParticleEffectPrefab = BurstParticleEffectPrefab,
                SoundEffectId = SoundEffectId,
                SoundEffectLength = SoundEffectLength,
                DamageType = DamageType,
                DamageAmount = DamageAmount,
                Radius = Radius,
                HasShockWave = HasShockWave,
                TargetType = TargetType,
                ImbuedStatusEffectID = ImbuedStatusEffectID,
                ImbuedStatusEffectDuration = ImbuedStatusEffectDuration,
                DamageVariance = DamageVariance,
                ScriptNameToTrigger = ScriptNameToTrigger,
                SpawnPrefabName = SpawnPrefabName,
                TroopIdToSummon = TroopIdToSummon,
                NumberToSummon = NumberToSummon
            };
        }
    }
}
