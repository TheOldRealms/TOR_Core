using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;

namespace TOR_Core.Utilities
{
    public static class TORSpellBlowHelper
    {
        private static Dictionary<Tuple<int, int>, Queue<SpellBlowInfo>> TriggeredEffects = new Dictionary<Tuple<int, int>, Queue<SpellBlowInfo>>();
        public static void EnqueueSpellBlowInfo(int victimAgentIndex, int attackAgentIndex, string triggeredEffectId, DamageType damageType, string originSpellTemplateId)
        {
            if (victimAgentIndex == -1 || attackAgentIndex == -1)
                return;

            var coord = new Tuple<int, int>(victimAgentIndex, attackAgentIndex);

            if (TriggeredEffects.ContainsKey(coord))
            {
                SpellBlowInfo info = new SpellBlowInfo();
                info.TriggeredEffectId = triggeredEffectId;
                info.DamageType = damageType;
                info.DamagerIndex = attackAgentIndex;
                info.OriginAbilityTemplateId = originSpellTemplateId;
                TriggeredEffects[coord].Enqueue(info);
                return;
            }

            var spellItem = new SpellBlowInfo();
            spellItem.TriggeredEffectId = triggeredEffectId;
            spellItem.DamageType = damageType;
            spellItem.OriginAbilityTemplateId = originSpellTemplateId;
            spellItem.DamagerIndex = attackAgentIndex;
            Queue<SpellBlowInfo> queue = new Queue<SpellBlowInfo>();
            queue.Enqueue(spellItem);
            TriggeredEffects.Add(coord, queue);
        }

        public static SpellBlowInfo GetSpellBlowInfo(int victimAgentIndex, int attackAgentIndex)
        {
            var coord = new Tuple<int, int>(victimAgentIndex, attackAgentIndex);
            if (!TriggeredEffects.ContainsKey(coord)) return new SpellBlowInfo();

            var item = TriggeredEffects[coord].Dequeue();

            if (!TriggeredEffects[coord].IsEmpty())
            {
                return item;
            }
            //this should be null 
            return item;
        }

        public static void Clear()
        {
            TriggeredEffects.Clear();
        }

        public static bool IsSpellBlow(Blow b)
        {
            return b.StrikeType == StrikeType.Thrust && b.AttackType == AgentAttackType.Kick && b.DamageCalculated && b.BlowFlag.HasFlag(BlowFlags.NoSound) && b.VictimBodyPart == BoneBodyPartType.Chest;
        }
        
        public static bool IsSpellBlow(KillingBlow b)
        {
            return  b.AttackType == AgentAttackType.Kick &&  b.WeaponItemKind ==-1 &&b.VictimBodyPart == BoneBodyPartType.Chest;
        }
    }
    public struct SpellBlowInfo
    {
        public int DamagerIndex;
        public string TriggeredEffectId;
        public DamageType DamageType;
        public string OriginAbilityTemplateId;
    }
}
