using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Ink;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CustomEvents
{
    public class CustomEventsCampaignBehavior : CampaignBehaviorBase
    {
        private const float RareChance = 0.5f;
        private const float SpecialChance = 1f;
        private const float UncommonChance = 3f;
        private const float CommonChance = 5f;
        private const float AbundantChance = 7f;
        private const int CoolDown = 168;

        List<CustomEvent> _events = new List<CustomEvent>();
        Dictionary<string, double> _triggerTimes = new Dictionary<string, double>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
            TORCampaignEvents.Instance.ChaosUprisingStarted += OnChaosUprisingStarted;
        }

        private void OnChaosUprisingStarted(object sender, ChaosUprisingStartedEventArgs e)
        {
            MBInformationManager.AddQuickInformation(new TextObject($"Chaos corruption reaches a critical level in {e.Settlement.Name.ToString()} and rebellion breaks out."));
        }

        private void DailyTick()
        {
            if (CampaignTime.Now.GetDayOfYear == 1) InkStoryManager.OpenStory("MorrsliebWaxes");
        }

        private void OnSessionStart(CampaignGameStarter starter)
        {
            _events.Clear();
            foreach (var item in InkStoryManager.AllStories.Where(x => !x.IsDevelopmentVersion && x.Frequency != CustomEventFrequency.Invalid && x.Frequency != CustomEventFrequency.Special))
            {
                _events.Add(new CustomEvent(item.StringId, item.Frequency, item.Cooldown, StandardMovingCheck, () => InkStoryManager.OpenStory(item.StringId)));
            }
            _events.Add(new CustomEvent("Duel", CustomEventFrequency.Uncommon, 900, () => StandardMovingCheck() && !Hero.MainHero.HasAttribute("DefeatedVittorio"), () => InkStoryManager.OpenStory("Duel")));
            _events.Add(new CustomEvent("CampFireLearning", CustomEventFrequency.Abundant, 300, () => StandardMovingCheck() && CampaignTime.Now.IsNightTime, () => InkStoryManager.OpenStory("CampFireLearning")));
            _events.Add(new CustomEvent("Minstrel", CustomEventFrequency.Common, 1000,
                () => StandardMovingCheck() &&
                !CampaignTime.Now.IsNightTime &&
                TORCommon.FindNearestSettlement(MobileParty.MainParty, 100f, x => x.IsTown)?.Culture.StringId == TORConstants.Cultures.BRETONNIA, () => InkStoryManager.OpenStory("Minstrel")));
        }

        private bool StandardMovingCheck()
        {
            return MobileParty.MainParty.IsMoving && 
                MobileParty.MainParty.Army == null && 
                !Hero.MainHero.IsPrisoner && 
                MobileParty.MainParty.MemberRoster.TotalManCount > 10 && 
                MobileParty.MainParty.CurrentSettlement == null && 
                MobileParty.MainParty.BesiegedSettlement == null;
        }

        private void HourlyTick(MobileParty party)
        {
            if (party != MobileParty.MainParty) return;
            if (!_triggerTimes.ContainsKey("Global")) _triggerTimes.Add("Global", 9999);
            if (GetRandomFrequency(out CustomEventFrequency chosenFrequency) && HasCooldownExpired())
            {
                var chosenEvent = _events.GetRandomElementWithPredicate(x => x.Frequency == chosenFrequency && x.DoesConditionHold() && x.StringId != InkStoryManager.LastStoryId && !_triggerTimes.ContainsKey(x.StringId));
                if (chosenEvent == null) chosenEvent = _events.GetRandomElementWithPredicate(x => x.Frequency == chosenFrequency && x.DoesConditionHold() && x.Cooldown < GetElapsedTimeSinceLastTrigger(x));
                if (chosenEvent != null)
                {
                    chosenEvent.Trigger();
                    if (_triggerTimes.ContainsKey(chosenEvent.StringId)) _triggerTimes[chosenEvent.StringId] = CampaignTime.Now.ToHours;
                    else _triggerTimes.Add(chosenEvent.StringId, CampaignTime.Now.ToHours);
                    _triggerTimes["Global"] = CampaignTime.Now.ToHours;
                }
            }
        }

        private int GetElapsedTimeSinceLastTrigger(CustomEvent x)
        {
            if (_triggerTimes.ContainsKey(x.StringId)) return (int)(CampaignTime.Now.ToHours - _triggerTimes[x.StringId]);
            else return 999;
        }

        private bool HasCooldownExpired() => CampaignTime.Now.ToHours - _triggerTimes["Global"] > CoolDown;

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

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_triggerTimes", ref _triggerTimes);
        }

        ~CustomEventsCampaignBehavior()
        {
            TORCampaignEvents.Instance.ChaosUprisingStarted -= OnChaosUprisingStarted;
        }
    }
}
