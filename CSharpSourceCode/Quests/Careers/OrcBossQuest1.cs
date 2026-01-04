using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;
using TOR_Core.Ink;
using TOR_Core.Utilities;

namespace TOR_Core.Quests.Careers
{
    public class OrcBossQuest1 : QuestBase
    {
        // Quest requirements constants
        private const int RequiredWeaponSkillLevels = 150;
        private const int RequiredBattlesWon = 50;
        private const int RequiredArenaFights = 20;
        private const int RequiredTeefTransferred = 150000;
        private const int RequiredBrawlsWon = 50;

        [SaveableField(1)]
        private JournalLog _taskOneHandedSkill = null;
        [SaveableField(2)]
        private JournalLog _taskTwoHandedSkill = null;
        [SaveableField(3)]
        private JournalLog _taskPolearmSkill = null;
        [SaveableField(4)]
        private JournalLog _taskBattlesWon = null;
        [SaveableField(5)]
        private JournalLog _taskArenaFights = null;
        [SaveableField(6)]
        private JournalLog _taskTeefTransferred = null;
        [SaveableField(7)]
        private JournalLog _taskBrawlsWon = null;

        [SaveableField(8)]
        private int _currentOneHandedSkillLevel = 0;
        [SaveableField(9)]
        private int _currentTwoHandedSkillLevel = 0;
        [SaveableField(10)]
        private int _currentPolearmSkillLevel = 0;
        [SaveableField(11)]
        private int _currentBattlesWon = 0;
        [SaveableField(12)]
        private int _currentArenaFights = 0;
        [SaveableField(13)]
        private int _currentTeefTransferred = 0;
        [SaveableField(14)]
        private int _currentBrawlsWon = 0;
        [SaveableField(15)]
        private bool _readyToComplete = false;

        public OrcBossQuest1(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
        {
            InitializeQuest();
        }

        private void InitializeQuest()
        {
            // Calculate current weapon skill levels
            _currentOneHandedSkillLevel = Hero.MainHero?.GetSkillValue(DefaultSkills.OneHanded) ?? 0;
            _currentTwoHandedSkillLevel = Hero.MainHero?.GetSkillValue(DefaultSkills.TwoHanded) ?? 0;
            _currentPolearmSkillLevel = Hero.MainHero?.GetSkillValue(DefaultSkills.Polearm) ?? 0;

            // TODO: Get actual values from behaviors when they're implemented
            _currentBattlesWon = 0;
            _currentArenaFights = 0;
            _currentTeefTransferred = 0;
            _currentBrawlsWon = 0;

            // Create journal logs for each task
            _taskOneHandedSkill = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_boss_quest1_log_onehanded", "Reach {REQUIRED} levels in One-Handed skill")
                    .SetTextVariable("REQUIRED", RequiredWeaponSkillLevels),
                TORTextHelper.GetTextObject("tor_orc_boss_quest1_task_onehanded", "One-Handed Skill"),
                _currentOneHandedSkillLevel,
                RequiredWeaponSkillLevels);

            _taskTwoHandedSkill = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_boss_quest1_log_twohanded", "Reach {REQUIRED} levels in Two-Handed skill")
                    .SetTextVariable("REQUIRED", RequiredWeaponSkillLevels),
                TORTextHelper.GetTextObject("tor_orc_boss_quest1_task_twohanded", "Two-Handed Skill"),
                _currentTwoHandedSkillLevel,
                RequiredWeaponSkillLevels);

            _taskPolearmSkill = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_boss_quest1_log_polearm", "Reach {REQUIRED} levels in Polearm skill")
                    .SetTextVariable("REQUIRED", RequiredWeaponSkillLevels),
                TORTextHelper.GetTextObject("tor_orc_boss_quest1_task_polearm", "Polearm Skill"),
                _currentPolearmSkillLevel,
                RequiredWeaponSkillLevels);

            _taskBattlesWon = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_boss_quest1_log_battles", "Win {REQUIRED} battles")
                    .SetTextVariable("REQUIRED", RequiredBattlesWon),
                TORTextHelper.GetTextObject("tor_orc_boss_quest1_task_battles", "Battles Won"),
                _currentBattlesWon,
                RequiredBattlesWon);

            _taskArenaFights = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_boss_quest1_log_arena", "Win {REQUIRED} arena fights")
                    .SetTextVariable("REQUIRED", RequiredArenaFights),
                TORTextHelper.GetTextObject("tor_orc_boss_quest1_task_arena", "Arena Fights"),
                _currentArenaFights,
                RequiredArenaFights);

            _taskTeefTransferred = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_boss_quest1_log_teef", "Transfer {REQUIRED} worth of loot to teef")
                    .SetTextVariable("REQUIRED", RequiredTeefTransferred),
                TORTextHelper.GetTextObject("tor_orc_boss_quest1_task_teef", "Teef Transferred"),
                _currentTeefTransferred,
                RequiredTeefTransferred);

            _taskBrawlsWon = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_boss_quest1_log_brawls", "Win {REQUIRED} brawls")
                    .SetTextVariable("REQUIRED", RequiredBrawlsWon),
                TORTextHelper.GetTextObject("tor_orc_boss_quest1_task_brawls", "Brawls Won"),
                _currentBrawlsWon,
                RequiredBrawlsWon);
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            // Skill tracking
            CampaignEvents.HeroGainedSkill.AddNonSerializedListener(this, OnSkillIncreased);

            // Battle tracking
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnMapEventEnded);

            // Custom TOR events
            TORCampaignEvents.Instance.BrawlWon += OnBrawlWon;
            TORCampaignEvents.Instance.ArenaFightWon += OnArenaFightWon;
            TORCampaignEvents.Instance.TeefTransferred += OnTeefTransferred;
        }

        private void OnSkillIncreased(Hero hero, SkillObject skill, int skillValueBefore, bool arg4)
        {
            if (hero != Hero.MainHero) return;

            if (skill == DefaultSkills.OneHanded)
            {
                _currentOneHandedSkillLevel = Hero.MainHero.GetSkillValue(DefaultSkills.OneHanded);
                _taskOneHandedSkill.UpdateCurrentProgress(_currentOneHandedSkillLevel);
                UpdateQuest();
            }
            else if (skill == DefaultSkills.TwoHanded)
            {
                _currentTwoHandedSkillLevel = Hero.MainHero.GetSkillValue(DefaultSkills.TwoHanded);
                _taskTwoHandedSkill.UpdateCurrentProgress(_currentTwoHandedSkillLevel);
                UpdateQuest();
            }
            else if (skill == DefaultSkills.Polearm)
            {
                _currentPolearmSkillLevel = Hero.MainHero.GetSkillValue(DefaultSkills.Polearm);
                _taskPolearmSkill.UpdateCurrentProgress(_currentPolearmSkillLevel);
                UpdateQuest();
            }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            _currentBattlesWon++;
            _taskBattlesWon.UpdateCurrentProgress(_currentBattlesWon);
            UpdateQuest();
        }

        private void OnBrawlWon(object sender, BrawlWonEventArgs e)
        {
            if (e.Hero != Hero.MainHero) return;

            _currentBrawlsWon++;
            _taskBrawlsWon.UpdateCurrentProgress(_currentBrawlsWon);
            UpdateQuest();
        }

        private void OnArenaFightWon(object sender, ArenaFightWonEventArgs e)
        {
            if (e.Hero != Hero.MainHero) return;

            _currentArenaFights++;
            _taskArenaFights.UpdateCurrentProgress(_currentArenaFights);
            UpdateQuest();
        }

        private void OnTeefTransferred(object sender, TeefTransferredEventArgs e)
        {
            if (e.Hero != Hero.MainHero) return;

            _currentTeefTransferred += e.Amount;
            _taskTeefTransferred.UpdateCurrentProgress(_currentTeefTransferred);
            UpdateQuest();
        }

        public override string SpecialQuestType => "OrcBossQuest1";

        private void UpdateQuest()
        {
            if (AreAllTasksFinished() && !_readyToComplete)
            {
                _readyToComplete = true;
            }
        }

        private bool AreAllTasksFinished()
        {
            return JournalEntries.All(entry => entry.HasBeenCompleted());
        }

        protected override void OnCompleteWithSuccess()
        {
            // Award the PlayerOrcBoss attribute
            Hero.MainHero.AddAttribute("PlayerOrcBoss");

            // Open the transition story to OrcBossQuest2
            InkStoryManager.OpenStory("OrcBossQuest2");
        }

        protected override void SetDialogs()
        {
            // No dialogues needed for this quest
        }

        protected override void InitializeQuestOnGameLoad()
        {
            // Re-register events on game load
            //Sly : do events need to be registered here or RegisterEvents is sufficient? ie. does EngineerQuest have a potential issue because it doesn't do that.
            //Or, is it not necessary because QuestBase has InitializeQuestOnLoadWithQuestManager which calls RegisterEvents, InitializeQuestOnGameLoad, and AddDialogs for each quest?
        }

        protected override void HourlyTick()
        {
            // Check if ready to complete and finalize quest when safe
            if (_readyToComplete)
            {
                CompleteQuestWithSuccess();
            }
            else
            {
                UpdateQuest();
            }
        }

        public override TextObject Title => TORTextHelper.GetTextObject("tor_orc_boss_quest1_title", "Da Path of Da Boss");

        public override bool IsRemainingTimeHidden => true;

        ~OrcBossQuest1()
        {
            TORCampaignEvents.Instance.BrawlWon -= OnBrawlWon;
            TORCampaignEvents.Instance.ArenaFightWon -= OnArenaFightWon;
            TORCampaignEvents.Instance.TeefTransferred -= OnTeefTransferred;
        }
    }
}