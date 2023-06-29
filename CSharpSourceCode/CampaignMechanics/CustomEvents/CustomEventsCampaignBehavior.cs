using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TOR_Core.Ink;

namespace TOR_Core.CampaignMechanics.CustomEvents
{
    public class CustomEventsCampaignBehavior : CampaignBehaviorBase
    {
        private const float RareChance = 0.5f;
        private const float UncommonChance = 1f;
        private const float CommonChance = 3f;
        private const float AbundantChance = 10f;

        List<CustomEvent> _events = new List<CustomEvent>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyTick);
        }

        private void OnSessionStart(CampaignGameStarter starter)
        {
            _events.Clear();
            _events.Add(new CustomEvent("OverturnedCart", CustomEventFrequency.Common, () => MobileParty.MainParty.IsMoving, () => InkStoryManager.OpenStory("OverturnedCart")));
        }

        private void HourlyTick(MobileParty party)
        {
            if (party != MobileParty.MainParty) return;
            if(GetRandomFrequency(out CustomEventFrequency chosenFrequency))
            {
                var chosenEvent = _events.GetRandomElementWithPredicate(x => x.Frequency == chosenFrequency && x.DoesConditionHold());
                if (chosenEvent != null) chosenEvent.Trigger();
            }
        }

        private bool GetRandomFrequency(out CustomEventFrequency chosenFrequency)
        {
            float roll = MBRandom.RandomInt(0, 1000) / 10;
            chosenFrequency = CustomEventFrequency.Invalid;
            if (roll > AbundantChance) return false;
            else if (roll < RareChance) chosenFrequency = CustomEventFrequency.Rare;
            else if (roll < UncommonChance) chosenFrequency = CustomEventFrequency.Uncommon;
            else if (roll < CommonChance) chosenFrequency = CustomEventFrequency.Common;
            else if (roll <= AbundantChance) chosenFrequency = CustomEventFrequency.Abundant;
            return true;
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
