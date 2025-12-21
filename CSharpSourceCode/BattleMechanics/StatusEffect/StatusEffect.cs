using System;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.StatusEffect
{
    public class StatusEffect : IDisposable, IEquatable<StatusEffect>
    {
        public Agent ApplierAgent = null;
        public float CurrentDuration = 0;
        public StatusEffectTemplate Template { get; }
        public int CastId { get; set; } = -1;

        public StatusEffect(StatusEffectTemplate template, Agent applierAgent, int castId = -1)
        {
            Template = template;
            ApplierAgent = applierAgent;
            CastId = castId;
        }

        public void Dispose()
        {
            ApplierAgent = null;
        }

        public bool Equals(StatusEffect other) => Template.StringID == other.Template.StringID;
    }
}