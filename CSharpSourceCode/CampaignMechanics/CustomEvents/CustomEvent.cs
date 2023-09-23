using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.CustomEvents
{
    public class CustomEvent
    {
        public string StringId { get; private set; }
        public CustomEventFrequency Frequency { get; private set; }
        public int Cooldown { get; private set; }
        private Func<bool> _condition;
        private Action _consequence;
        public CustomEvent(string stringId, CustomEventFrequency frequency, int cooldown, Func<bool> condition, Action consequence)
        {
            StringId = stringId;
            Frequency = frequency;
            Cooldown = cooldown;
            _condition = condition;
            _consequence = consequence;
        }

        public bool DoesConditionHold()
        {
            if (_condition != null) return _condition();
            else return false;
        }

        public void Trigger()
        {
            if (_consequence != null) _consequence();
        }
    }

    public enum CustomEventFrequency
    {
        Invalid = 0,
        Rare = 1,
        Uncommon,
        Common,
        Abundant,
        Special
    }
}
