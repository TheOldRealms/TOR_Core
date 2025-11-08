using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TOR_Core.Quests
{
    public class SpecializeLoreQuest : QuestBase
    {
        [SaveableField(1)]
        private int _numberOfCasts = 0;
        [SaveableField(2)]
        private JournalLog _task1 = null;
        [SaveableField(3)]
        private JournalLog _task2 = null;
        [SaveableField(4)]
        private TextObject _title = new TextObject("{=tor_specialize_lore_quest_title_str}Practice Spellcasting");

        public override TextObject Title => _title;
        public override bool IsRemainingTimeHidden => false;
        public override bool IsSpecialQuest => true;
        public bool Task1Complete => _task1.HasBeenCompleted();

        public SpecializeLoreQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
        {
            SetLogs();
        }

        protected override void HourlyTick() { }
        protected override void InitializeQuestOnGameLoad() { }

        protected override void SetDialogs() { }

        private void SetLogs()
        {
            _task1 = AddDiscreteLog(new TextObject("{=tor_specialize_lore_quest_task1_str}Use magic 5 times."), new TextObject("{=tor_specialize_lore_quest_task1_counter_str}Number of casts"), _numberOfCasts, 5);
        }

        public void IncrementCast()
        {
            _numberOfCasts++;
            if (!_task1.HasBeenCompleted()) _task1.UpdateCurrentProgress(_numberOfCasts);
            CheckCondition();
        }

        private void CheckCondition()
        {
            if (_task1.HasBeenCompleted() && _task2 == null)
            {
                _task2 = AddLog(new TextObject("{=tor_specialize_lore_quest_task1_finish_str}Visit a spell trainer to specialize in a lore."));
            }
        }
    }
}
