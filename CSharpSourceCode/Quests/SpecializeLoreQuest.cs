using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

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
        private TextObject _title = TORTextHelper.GetTextObject("tor_quest_specialize_lore_title", "Practice Spellcasting");

        public override TextObject Title => _title;
        public override bool IsRemainingTimeHidden => false;
        public override string SpecialQuestType => "SpecializeLoreQuest"; 
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
            _task1 = AddDiscreteLog(TORTextHelper.GetTextObject("tor_specialize_lore_quest_task1", "Use magic 5 times."), TORTextHelper.GetTextObject("tor_specialize_lore_quest_task1_counter", "Number of casts"), _numberOfCasts, 5);
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
                _task2 = AddLog(TORTextHelper.GetTextObject("tor_specialize_lore_quest_task1_finish", "Visit a spell trainer to specialize in a lore."));
            }
        }
    }
}