using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.SpellCasting
{
    /// <summary>
    /// Tracks all damage and effects from a single spell cast.
    /// Created when ability starts, collected when ability ends.
    /// Expected DOT/HOT values are pre-calculated when triggered effects are applied.
    /// </summary>
    public class SpellCastSession
    {
        private readonly HashSet<int> _agentsDamaged = new();
        private readonly HashSet<int> _agentsHealed = new();
        private readonly HashSet<int> _agentsKilled = new();
        private readonly HashSet<int> _agentsAffectedByStatusEffects = new();

        public int CastID { get; }
        public Agent Caster { get; }
        public Hero CasterHero { get; }
        public AbilityTemplate AbilityTemplate { get; }
        public DamageType PrimaryDamageType { get; private set; }

        public int TotalDamageDealt { get; private set; }
        public int TotalDamageAbsorbed { get; private set; }
        public int TotalHealingDone { get; private set; }
        public int TickCount { get; private set; }

        public int AgentsDamagedCount => _agentsDamaged.Count;
        public int AgentsHealedCount => _agentsHealed.Count;
        public int AgentsKilledCount => _agentsKilled.Count;
        public int AgentsAffectedByStatusEffectsCount => _agentsAffectedByStatusEffects.Count;
        public int StatusEffectsApplied { get; private set; }

        /// <summary>
        /// Time when the session can be collected (after status effects expire).
        /// </summary>
        public float EarliestCollectTime { get; private set; }

        public SpellCastSession(int castId, Agent caster, AbilityTemplate abilityTemplate)
        {
            CastID = castId;
            Caster = caster;
            AbilityTemplate = abilityTemplate;
            PrimaryDamageType = DamageType.Physical;

            // Store Hero reference for XP granting (Agent may become invalid after battle)
            if (Game.Current.GameType is Campaign && caster?.IsHero == true)
            {
                CasterHero = caster.GetHero();
            }
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
        /// Books a kill in this session.
        /// </summary>
        public void BookKill(Agent victim)
        {
            if (victim != null)
            {
                _agentsKilled.Add(victim.Index);
            }
        }

        /// <summary>
        /// Books a status effect application in this session.
        /// </summary>
        public void BookStatusEffect(Agent target)
        {
            StatusEffectsApplied++;
            if (target != null)
            {
                _agentsAffectedByStatusEffects.Add(target.Index);
            }
        }

        /// <summary>
        /// Records that a tick has occurred (for lasting effects).
        /// </summary>
        public void RecordTick()
        {
            TickCount++;
        }

        /// <summary>
        /// Extends the earliest collect time to wait for status effects to expire.
        /// </summary>
        public void ExtendCollectTime(float duration)
        {
            float newTime = Mission.Current.CurrentTime + duration + 1f; // +1 second buffer
            if (newTime > EarliestCollectTime)
            {
                EarliestCollectTime = newTime;
            }
        }

        /// <summary>
        /// Returns true if the session is ready to be collected (status effects have expired).
        /// </summary>
        public bool IsReadyToCollect => Mission.Current == null || Mission.Current.CurrentTime >= EarliestCollectTime;

        /// <summary>
        /// Returns true if this session has any meaningful data to display or grant XP for.
        /// </summary>
        public bool HasData => TotalDamageDealt > 0 || TotalHealingDone > 0 || StatusEffectsApplied > 0;
    }
}
