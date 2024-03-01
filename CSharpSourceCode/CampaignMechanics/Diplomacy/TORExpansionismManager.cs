using JetBrains.Annotations;

using System;
using System.Collections.Generic;

using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Diplomacy
{
    internal sealed class TORExpansionismManager
    {
        [SaveableField(1)][UsedImplicitly] private Dictionary<IFaction, float> _expansionism;

        private static readonly Lazy<TORExpansionismManager> lazy = new Lazy<TORExpansionismManager>(() => new TORExpansionismManager());
        public static TORExpansionismManager Instance { get => lazy.Value; }
        public float SiegeExpansionism => 20;
        public float ExpansionismDecayPerDay => 1;
        public float MinimumExpansionismPerFief => 3;
        public float CriticalExpansionism => 100;

        public TORExpansionismManager()
        {
            _expansionism = new Dictionary<IFaction, float>();
        }

        public float GetMinimumExpansionism(Kingdom kingdom)
        {
            return kingdom.Fiefs.Count * MinimumExpansionismPerFief;
        }

        public float GetExpansionism(IFaction faction)
        {
            return _expansionism.TryGetValue(faction, out var result) ? result : 0f;
        }

        public void AddSiegeScore(IFaction faction)
        {
            _expansionism.TryGetValue(faction, out var value);
            _expansionism[faction] = Math.Max(value, GetMinimumExpansionism(faction) - MinimumExpansionismPerFief) + SiegeExpansionism;
        }

        private static float GetMinimumExpansionism(IFaction faction)
        {
            return faction.IsKingdomFaction ? (faction as Kingdom).GetMinimumExpansionism() : default;
        }

        public void ApplyExpansionismDecay(IFaction faction)
        {
            if (_expansionism.TryGetValue(faction, out var value))
                _expansionism[faction] = Math.Max(value - ExpansionismDecayPerDay, GetMinimumExpansionism(faction));
            else
                _expansionism[faction] = GetMinimumExpansionism(faction);
        }
    }
}