using System;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.StatusEffect
{
    public class StatusEffect : IDisposable, IEquatable<StatusEffect>
    {
        public Agent ApplierAgent = null;
        public float CurrentDuration = 0;
        public StatusEffectTemplate Template { get; }

        public StatusEffect(StatusEffectTemplate template, Agent applierAgent)
        {
            Template = template;
            ApplierAgent = applierAgent;
        }

        public void Dispose()
        {
            ApplierAgent = null;
        }

        public bool Equals(StatusEffect other) => Template.StringID == other.Template.StringID;
    }
}