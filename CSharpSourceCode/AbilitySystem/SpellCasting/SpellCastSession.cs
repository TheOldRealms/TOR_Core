using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.DamageSystem;

namespace TOR_Core.AbilitySystem.SpellCasting
{
    /// <summary>
    /// Tracks all damage and effects from a single spell cast.
    /// Created when ability starts, collected when ability ends or when all status effects expire.
    /// </summary>
    public class SpellCastSession
    {
        private readonly HashSet<int> _agentsDamaged = new();
        private readonly HashSet<int> _agentsHealed = new();

        public int CastID { get; }
        public Agent Caster { get; }
        public AbilityTemplate AbilityTemplate { get; }
        public DamageType PrimaryDamageType { get; private set; }

        public int TotalDamageDealt { get; private set; }
        public int TotalDamageAbsorbed { get; private set; }
        public int TotalHealingDone { get; private set; }
        public int TickCount { get; private set; }

        public int AgentsDamagedCount => _agentsDamaged.Count;
        public int AgentsHealedCount => _agentsHealed.Count;

        /// <summary>
        /// Number of active status effects associated with this session.
        /// Session cannot be collected until this is 0 and ability has ended.
        /// </summary>
        public int PendingStatusEffects { get; private set; }

        /// <summary>
        /// Whether the ability entity has ended (OnRemoved was called).
        /// </summary>
        public bool AbilityEnded { get; set; }

        public SpellCastSession(int castId, Agent caster, AbilityTemplate abilityTemplate)
        {
            CastID = castId;
            Caster = caster;
            AbilityTemplate = abilityTemplate;
            PrimaryDamageType = DamageType.Physical;
        }

        /// <summary>
        /// Books damage dealt to an agent in this session.
        /// </summary>
        public void BookDamage(Agent victim, int damageDealt, int damageAbsorbed, DamageType damageType)
        {
            if (victim != null)
            {
                _agentsDamaged.Add(victim.Index);
            }

            TotalDamageDealt += damageDealt;
            TotalDamageAbsorbed += damageAbsorbed;
            PrimaryDamageType = damageType;
        }

        /// <summary>
        /// Books healing done to an agent in this session.
        /// </summary>
        public void BookHealing(Agent target, int healingDone)
        {
            if (target != null)
            {
                _agentsHealed.Add(target.Index);
            }

            TotalHealingDone += healingDone;
        }

        /// <summary>
        /// Records that a tick has occurred (for lasting effects).
        /// </summary>
        public void RecordTick()
        {
            TickCount++;
        }

        /// <summary>
        /// Increments the pending status effects counter.
        /// </summary>
        public void AddPendingStatusEffect()
        {
            PendingStatusEffects++;
        }

        /// <summary>
        /// Decrements the pending status effects counter.
        /// </summary>
        public void RemovePendingStatusEffect()
        {
            if (PendingStatusEffects > 0)
                PendingStatusEffects--;
        }

        /// <summary>
        /// Returns true if this session is ready to be collected.
        /// </summary>
        public bool IsReadyToCollect => AbilityEnded && PendingStatusEffects == 0;

        /// <summary>
        /// Returns true if this session has any meaningful data to display.
        /// </summary>
        public bool HasData => TotalDamageDealt > 0 || TotalHealingDone > 0;
    }
}
