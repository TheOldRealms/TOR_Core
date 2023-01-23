﻿using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.StatusEffect
{
    public class StatusEffect
    {
        public Agent ApplierAgent = null;
        public int CurrentDuration = 0;
        public StatusEffectTemplate Template { get; }

        public StatusEffect(StatusEffectTemplate template)
        {
            Template = template;
        }
    }
}