using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Quests.Careers
{
    public class OrcBossQuest2 : QuestBase
    {
        // Quest requirements constants (from task notes)
        private const int RequiredWeaponSkillLevels = 225;  // or 250
        private const int RequiredBattlesWon = 300;
        private const int RequiredArenaFights = 50;
        private const int RequiredBrawlsWon = 100;
        private const int RequiredCitiesCaptured = 5;
        private const int RequiredLordDuels = 15;
        private const int RequiredTeefTransferred = 500000;

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
        private JournalLog _taskBrawlsWon = null;
        [SaveableField(7)]
        private JournalLog _taskCitiesCaptured = null;
        [SaveableField(8)]
        private JournalLog _taskLordDuels = null;
        [SaveableField(9)]
        private JournalLog _taskTeefTransferred = null;

        [SaveableField(10)]
        private int _currentOneHandedSkillLevel = 0;
        [SaveableField(11)]
        private int _currentTwoHandedSkillLevel = 0;
        [SaveableField(12)]
        private int _currentPolearmSkillLevel = 0;
        [SaveableField(13)]
        private int _currentBattlesWon = 0;
        [SaveableField(14)]
        private int _currentArenaFights = 0;
        [SaveableField(15)]
        private int _currentBrawlsWon = 0;
        [SaveableField(16)]
        private int _currentCitiesCaptured = 0;
        [SaveableField(17)]
        private int _currentLordDuels = 0;
        [SaveableField(18)]
        private int _currentTeefTransferred = 0;
        [SaveableField(19)]
        private bool _readyToComplete = false;

        public OrcBossQuest2(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
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
            _currentBrawlsWon = 0;
            _currentCitiesCaptured = 0;
            _currentLordDuels = 0;
            _currentTeefTransferred = 0;

            // Create journal logs for each task
            _taskOneHandedSkill = AddDiscreteLog(
                new TextObject("{=tor_orc_boss_quest2_log_onehanded}Reach {REQUIRED} levels in One-Handed skill")
                    .SetTextVariable("REQUIRED", RequiredWeaponSkillLevels),
                new TextObject("{=tor_orc_boss_quest2_task_onehanded}One-Handed Skill"),
                _currentOneHandedSkillLevel,
                RequiredWeaponSkillLevels);

            _taskTwoHandedSkill = AddDiscreteLog(
                new TextObject("{=tor_orc_boss_quest2_log_twohanded}Reach {REQUIRED} levels in Two-Handed skill")
                    .SetTextVariable("REQUIRED", RequiredWeaponSkillLevels),
                new TextObject("{=tor_orc_boss_quest2_task_twohanded}Two-Handed Skill"),
                _currentTwoHandedSkillLevel,
                RequiredWeaponSkillLevels);

            _taskPolearmSkill = AddDiscreteLog(
                new TextObject("{=tor_orc_boss_quest2_log_polearm}Reach {REQUIRED} levels in Polearm skill")
                    .SetTextVariable("REQUIRED", RequiredWeaponSkillLevels),
                new TextObject("{=tor_orc_boss_quest2_task_polearm}Polearm Skill"),
                _currentPolearmSkillLevel,
                RequiredWeaponSkillLevels);

            _taskBattlesWon = AddDiscreteLog(
                new TextObject("{=tor_orc_boss_quest2_log_battles}Win {REQUIRED} battles")
                    .SetTextVariable("REQUIRED", RequiredBattlesWon),
                new TextObject("{=tor_orc_boss_quest2_task_battles}Battles Won"),
                _currentBattlesWon,
                RequiredBattlesWon);

            _taskArenaFights = AddDiscreteLog(
                new TextObject("{=tor_orc_boss_quest2_log_arena}Win {REQUIRED} arena fights")
                    .SetTextVariable("REQUIRED", RequiredArenaFights),
                new TextObject("{=tor_orc_boss_quest2_task_arena}Arena Fights"),
                _currentArenaFights,
                RequiredArenaFights);

            _taskBrawlsWon = AddDiscreteLog(
                new TextObject("{=tor_orc_boss_quest2_log_brawls}Win {REQUIRED} brawls")
                    .SetTextVariable("REQUIRED", RequiredBrawlsWon),
                new TextObject("{=tor_orc_boss_quest2_task_brawls}Brawls Won"),
                _currentBrawlsWon,
                RequiredBrawlsWon);

            _taskCitiesCaptured = AddDiscreteLog(
                new TextObject("{=tor_orc_boss_quest2_log_cities}Capture {REQUIRED} cities or castles")
                    .SetTextVariable("REQUIRED", RequiredCitiesCaptured),
                new TextObject("{=tor_orc_boss_quest2_task_cities}Cities Captured"),
                _currentCitiesCaptured,
                RequiredCitiesCaptured);

            _taskLordDuels = AddDiscreteLog(
                new TextObject("{=tor_orc_boss_quest2_log_duels}Beat {REQUIRED} Lords in duels")
                    .SetTextVariable("REQUIRED", RequiredLordDuels),
                new TextObject("{=tor_orc_boss_quest2_task_duels}Lord Duels Won"),
                _currentLordDuels,
                RequiredLordDuels);

            _taskTeefTransferred = AddDiscreteLog(
                new TextObject("{=tor_orc_boss_quest2_log_teef}Transfer {REQUIRED} worth of loot to teef")
                    .SetTextVariable("REQUIRED", RequiredTeefTransferred),
                new TextObject("{=tor_orc_boss_quest2_task_teef}Teef Transferred"),
                _currentTeefTransferred,
                RequiredTeefTransferred);
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            // Skill tracking
            CampaignEvents.HeroGainedSkill.AddNonSerializedListener(this, OnSkillIncreased);

            // Battle tracking
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnMapEventEnded);

            // Settlement captured tracking
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);

            // Custom TOR events
            TORCampaignEvents.Instance.BrawlWon += OnBrawlWon;
            TORCampaignEvents.Instance.ArenaFightWon += OnArenaFightWon;
            TORCampaignEvents.Instance.TeefTransferred += OnTeefTransferred;
            TORCampaignEvents.Instance.LordDuelWon += OnLordDuelWon;
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

        private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            // Check if the player captured a town or castle
            if (capturerHero == Hero.MainHero && (settlement.IsTown || settlement.IsCastle))
            {
                _currentCitiesCaptured++;
                _taskCitiesCaptured.UpdateCurrentProgress(_currentCitiesCaptured);
                UpdateQuest();
            }
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

        private void OnLordDuelWon(object sender, LordDuelWonEventArgs e)
        {
            if (e.Hero != Hero.MainHero) return;

            _currentLordDuels++;
            _taskLordDuels.UpdateCurrentProgress(_currentLordDuels);
            UpdateQuest();
        }

        public override bool IsSpecialQuest => true;

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
            // Award the PlayerOrcBigBoss attribute
            Hero.MainHero.AddAttribute("PlayerOrcBigBoss");

            // TODO: Unlock the third layer of the Orc Boss career if it exists
        }

        protected override void SetDialogs()
        {
            // No dialogues needed for this quest
        }

        protected override void InitializeQuestOnGameLoad()
        {
            // Re-register events on game load
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

        public override TextObject Title => new TextObject("{=tor_orc_boss_quest2_title}Da Path of Da Big Boss");

        public override bool IsRemainingTimeHidden => true;

        ~OrcBossQuest2()
        {
            TORCampaignEvents.Instance.BrawlWon -= OnBrawlWon;
            TORCampaignEvents.Instance.ArenaFightWon -= OnArenaFightWon;
            TORCampaignEvents.Instance.TeefTransferred -= OnTeefTransferred;
            TORCampaignEvents.Instance.LordDuelWon -= OnLordDuelWon;
        }
    }
}