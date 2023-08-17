using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TOR_Core.Extensions;
using TOR_Core.Ink;

namespace TOR_Core.CampaignMechanics.CustomEvents
{
    public class CustomEventsCampaignBehavior : CampaignBehaviorBase
    {
        private const float RareChance = 0.5f;
        private const float SpecialChance = 1f;
        private const float UncommonChance = 3f;
        private const float CommonChance = 5f;
        private const float AbundantChance = 7f;
        private const int CoolDown = 12;
        private int _hoursCountSinceLastEvent = 0;

        List<CustomEvent> _events = new List<CustomEvent>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyTick);
        }

        private void OnSessionStart(CampaignGameStarter starter)
        {
            _events.Clear();
            foreach (var item in InkStoryManager.AllStories.Where(x => !x.IsDevelopmentVersion && x.Frequency != CustomEventFrequency.Invalid && x.Frequency != CustomEventFrequency.Special))
            {
                _events.Add(new CustomEvent(item.StringId, item.Frequency, item.Cooldown, StandardMovingCheck, () => InkStoryManager.OpenStory(item.StringId)));
            }
            _events.Add(new CustomEvent("Duel", CustomEventFrequency.Special, 168, () => StandardMovingCheck() && !Hero.MainHero.HasAttribute("DefeatedVittorio"), () => InkStoryManager.OpenStory("Duel")));
        }

        private bool StandardMovingCheck()
        {
            return MobileParty.MainParty.IsMoving && MobileParty.MainParty.Army == null && !Hero.MainHero.IsPrisoner && MobileParty.MainParty.MemberRoster.Count > 10;
        }

        private void HourlyTick(MobileParty party)
        {
            if (party != MobileParty.MainParty) return;
            if (GetRandomFrequency(out CustomEventFrequency chosenFrequency) && HasCooldownExpired())
            {
                var chosenEvent = _events.GetRandomElementWithPredicate(x => x.Frequency == chosenFrequency && x.DoesConditionHold() && x.StringId != InkStoryManager.LastStoryId && x.Cooldown < (CampaignTime.Now.ToHours - x.LastTriggerTime.ToHours));
                if (chosenEvent == null) chosenEvent = _events.GetRandomElementWithPredicate(x => x.Frequency == chosenFrequency && x.DoesConditionHold() && x.Cooldown < (CampaignTime.Now.ToHours - x.LastTriggerTime.ToHours));
                if (chosenEvent != null)
                {
                    chosenEvent.Trigger();
                    chosenEvent.LastTriggerTime = CampaignTime.Now;
                    _hoursCountSinceLastEvent = 0;
                }
            }
            else _hoursCountSinceLastEvent++;
        }

        private bool HasCooldownExpired() => _hoursCountSinceLastEvent > CoolDown;

        private bool GetRandomFrequency(out CustomEventFrequency chosenFrequency)
        {
            float roll = MBRandom.RandomInt(0, 1000) / 10;
            chosenFrequency = CustomEventFrequency.Invalid;
            if (roll > AbundantChance) return false;
            else if (roll < RareChance) chosenFrequency = CustomEventFrequency.Rare;
            else if (roll < SpecialChance) chosenFrequency = CustomEventFrequency.Special;
            else if (roll < UncommonChance) chosenFrequency = CustomEventFrequency.Uncommon;
            else if (roll < CommonChance) chosenFrequency = CustomEventFrequency.Common;
            else if (roll <= AbundantChance) chosenFrequency = CustomEventFrequency.Abundant;
            return true;
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
