using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
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
        private int _lastTriggeredMorrsliebCycleIndex = -1;

        List<CustomEvent> _events = new List<CustomEvent>();
        Dictionary<string, double> _triggerTimes = new Dictionary<string, double>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTick);
            TORCampaignEvents.Instance.ChaosUprisingStarted += OnChaosUprisingStarted;
        }

        private void OnChaosUprisingStarted(object sender, ChaosUprisingStartedEventArgs e)
        {
            if (e.Settlement.OwnerClan == Clan.PlayerClan)
            {
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            }
            var chaosRebellionText = TORTextHelper.GetTextObject("tor_chaos_rebellion_text", "Chaos corruption reaches a critical level in {SETTLEMENT_NAME} and rebellion breaks out.");
            chaosRebellionText.SetTextVariable("SETTLEMENT_NAME", e.Settlement.Name);
            MBInformationManager.AddQuickInformation(chaosRebellionText);
        }
        private void WeeklyTick()
        {
            int daysInCurrentWeek = Campaign.Current.Models.CampaignTimeModel.DaysInWeek;
            int morrsliebCycleDurationInHours = GetCalendarNormalizedCooldownHours(CampaignTime.HoursInDay * CampaignTime.DaysInYear);
            int currentMorrsliebCycleIndex = (int)(CampaignTime.Now.ToHours / morrsliebCycleDurationInHours);

            if (CampaignTime.Now.GetDayOfYear <= daysInCurrentWeek && _lastTriggeredMorrsliebCycleIndex != currentMorrsliebCycleIndex)
            {
                InkStoryManager.OpenStory("MorrsliebWaxes");
                _lastTriggeredMorrsliebCycleIndex = currentMorrsliebCycleIndex;
            }
        }

        // normalize cooldowns to the vanilla calendar so fastmode wont increase custom event frequency
        private static int GetCalendarNormalizedCooldownHours(int normalModeCooldownHours)
        {
            if (Campaign.Current.Options.AccelerationMode != GameAccelerationMode.Fast)
            {
                return normalModeCooldownHours;
            }

            var campaignTimeModel = Campaign.Current.Models.CampaignTimeModel;
            float calendarCompressionFactor = (7f / campaignTimeModel.DaysInWeek) * (3f / campaignTimeModel.WeeksInSeason);

            return TaleWorlds.Library.MathF.Round(normalModeCooldownHours * calendarCompressionFactor);
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
            if (!TORConfig.DisableMinstrelEvent)
            {
                _events.Add(new CustomEvent("Minstrel", CustomEventFrequency.Common, 1000,
                    () => StandardMovingCheck() &&
                    !CampaignTime.Now.IsNightTime &&
                    TORCommon.FindNearestSettlement(MobileParty.MainParty, 100f, x => x.IsTown)?.Culture.StringId == TORConstants.Cultures.BRETONNIA, () => InkStoryManager.OpenStory("Minstrel")));
            }
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

        private void HourlyTick()
        {
            if (!_triggerTimes.ContainsKey("Global")) _triggerTimes.Add("Global", 9999);
            if (GetRandomFrequency(out CustomEventFrequency chosenFrequency) && HasCooldownExpired())
            {
                var chosenEvent = _events.GetRandomElementWithPredicate(x => x.Frequency == chosenFrequency && x.DoesConditionHold() && x.StringId != InkStoryManager.LastStoryId && !_triggerTimes.ContainsKey(x.StringId));
                if (chosenEvent == null) chosenEvent = _events.GetRandomElementWithPredicate(x => x.Frequency == chosenFrequency && x.DoesConditionHold() && GetCalendarNormalizedCooldownHours(x.Cooldown) < GetElapsedTimeSinceLastTrigger(x)); if (chosenEvent != null)
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

        private bool HasCooldownExpired() => CampaignTime.Now.ToHours - _triggerTimes["Global"] > GetCalendarNormalizedCooldownHours(CoolDown);

        private bool GetRandomFrequency(out CustomEventFrequency chosenFrequency)
        {
            float roll = MBRandom.RandomInt(0, 1000) / 10;//Sly : what distribution does this produce? it's stored in a float, doesn't that map to exactly the same distribution as not dividing and using a smaller interval? This could be (0, 200) and maintain the same relative chances.
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
            dataStore.SyncData("_lastTriggeredMorrsliebCycleIndex", ref _lastTriggeredMorrsliebCycleIndex);
        }

        ~CustomEventsCampaignBehavior()
        {
            TORCampaignEvents.Instance.ChaosUprisingStarted -= OnChaosUprisingStarted;
        }
    }
}