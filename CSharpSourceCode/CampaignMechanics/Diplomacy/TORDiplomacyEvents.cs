using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Actions.Alliance;

namespace TOR_Core.CampaignMechanics.Diplomacy
{
    public class TORDiplomacyEvents
    {
        private static readonly Lazy<TORDiplomacyEvents> lazy = new Lazy<TORDiplomacyEvents>(() => new TORDiplomacyEvents());
        public static TORDiplomacyEvents Instance { get => lazy.Value; }

        private readonly List<IMbEventBase> _listeners;

        protected readonly MbEvent<AllianceEvent> _allianceBroken = new MbEvent<AllianceEvent>();
        protected readonly MbEvent<AllianceEvent> _allianceFormed = new MbEvent<AllianceEvent>();

        private TORDiplomacyEvents()
        {
            _listeners = new List<IMbEventBase>
            {
                _allianceBroken,
                _allianceFormed,
            };
        }

        public static IMbEvent<AllianceEvent> AllianceFormed => Instance._allianceFormed;

        public static IMbEvent<AllianceEvent> AllianceBroken => Instance._allianceBroken;

        public void OnAllianceFormed(AllianceEvent allianceEvent)
        {
            Instance._allianceFormed.Invoke(allianceEvent);
        }

        public void OnAllianceBroken(AllianceEvent allianceEvent)
        {
            Instance._allianceBroken.Invoke(allianceEvent);
        }

        public static void RemoveListeners(object o)
        {
            Instance.RemoveListenersInternal(o);
        }

        protected void RemoveListenersInternal(object obj)
        {
            foreach (var listener in _listeners)
                listener.ClearListeners(obj);
        }
    }
}
