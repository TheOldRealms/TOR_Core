using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Quests.Careers
{
    public class OrcShamanQuest2 : QuestBase
    {
        // Quest requirements constants
        private const int RequiredSpellcraftSkillLevels = 225;
        private const int RequiredFaithSkillLevels = 150;
        private const int RequiredEnchantsLearned = 10;
        private const int RequiredTeefTransferred = 250000;
        private const int RequiredCitiesCaptured = 5;
        private const int RequiredShrinesLooted = 30;

        [SaveableField(1)]
        private JournalLog _taskSpellcraft = null;
        [SaveableField(2)]
        private JournalLog _taskFaith = null;
        [SaveableField(3)]
        private JournalLog _taskEnchantsLearned = null;
        [SaveableField(4)]
        private JournalLog _taskTeefTransferred = null;
        [SaveableField(5)]
        private JournalLog _taskCitiesCaptured = null;
        [SaveableField(6)]
        private JournalLog _taskShrinesLooted = null;

        [SaveableField(7)]
        private int _currentSpellcraftLevel = 0;
        [SaveableField(8)]
        private int _currentFaithLevel = 0;
        [SaveableField(9)]
        private int _currentEnchantsLearned = 0;
        [SaveableField(10)]
        private int _currentTeefTransferred = 0;
        [SaveableField(11)]
        private int _currentCitiesCaptured = 0;
        [SaveableField(12)]
        private int _currentShrinesLooted = 0;
        [SaveableField(13)]
        private bool _readyToComplete = false;

        public OrcShamanQuest2(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
        {
            InitializeQuest();
        }

        private void InitializeQuest()
        {
            // Calculate current skill levels
            _currentSpellcraftLevel = Hero.MainHero?.GetSkillValue(TORSkills.Spellcraft) ?? 0;
            _currentFaithLevel = Hero.MainHero?.GetSkillValue(TORSkills.Faith) ?? 0;

            // TODO: Get actual values from behaviors when they're implemented
            _currentEnchantsLearned = 0;
            _currentTeefTransferred = 0;
            _currentCitiesCaptured = 0;

            // Create journal logs for each task
            _taskSpellcraft = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_shaman_quest2_log_spellcraft", "Reach {REQUIRED} levels in Spellcraft skill")
                    .SetTextVariable("REQUIRED", RequiredSpellcraftSkillLevels),
                TORTextHelper.GetTextObject("tor_orc_shaman_quest2_task_spellcraft", "Spellcraft Skill"),
                _currentSpellcraftLevel,
                RequiredSpellcraftSkillLevels);

            _taskFaith = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_shaman_quest2_log_faith", "Reach {REQUIRED} levels in Faith skill")
                    .SetTextVariable("REQUIRED", RequiredFaithSkillLevels),
                TORTextHelper.GetTextObject("tor_orc_shaman_quest2_task_faith", "Faith Skill"),
                _currentFaithLevel,
                RequiredFaithSkillLevels);

            _taskEnchantsLearned = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_shaman_quest2_log_enchants", "Learn {REQUIRED} Orc enchantments total")
                    .SetTextVariable("REQUIRED", RequiredEnchantsLearned),
                TORTextHelper.GetTextObject("tor_orc_shaman_quest2_task_enchants", "Enchantments Learned"),
                _currentEnchantsLearned,
                RequiredEnchantsLearned);

            _taskTeefTransferred = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_shaman_quest2_log_teef", "Transfer {REQUIRED} worth of loot to teef total")
                    .SetTextVariable("REQUIRED", RequiredTeefTransferred),
                TORTextHelper.GetTextObject("tor_orc_shaman_quest2_task_teef", "Teef Transferred"),
                _currentTeefTransferred,
                RequiredTeefTransferred);

            _taskCitiesCaptured = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_shaman_quest2_log_cities", "Capture {REQUIRED} cities or castles")
                    .SetTextVariable("REQUIRED", RequiredCitiesCaptured),
                TORTextHelper.GetTextObject("tor_orc_shaman_quest2_task_cities", "Cities Captured"),
                _currentCitiesCaptured,
                RequiredCitiesCaptured);

            _taskShrinesLooted = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_shaman_quest2_log_shrines", "Loot {REQUIRED} shrines total")
                    .SetTextVariable("REQUIRED", RequiredShrinesLooted),
                TORTextHelper.GetTextObject("tor_orc_shaman_quest2_task_shrines", "Shrines Looted"),
                _currentShrinesLooted,
                RequiredShrinesLooted);
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            // Skill tracking
            CampaignEvents.HeroGainedSkill.AddNonSerializedListener(this, OnSkillIncreased);

            // Settlement capture tracking
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);

            // Custom TOR events
            TORCampaignEvents.Instance.TeefTransferred += OnTeefTransferred;
            TORCampaignEvents.Instance.EnchantmentLearned += OnEnchantmentLearned;
            TORCampaignEvents.Instance.ShrineLooted += OnShrineLooted;
        }

        private void OnSkillIncreased(Hero hero, SkillObject skill, int skillValueBefore, bool arg4)
        {
            if (hero != Hero.MainHero) return;

            if (skill == TORSkills.Spellcraft)
            {
                _currentSpellcraftLevel = Hero.MainHero.GetSkillValue(TORSkills.Spellcraft);
                _taskSpellcraft.UpdateCurrentProgress(_currentSpellcraftLevel);
                UpdateQuest();
            }
            else if (skill == TORSkills.Faith)
            {
                _currentFaithLevel = Hero.MainHero.GetSkillValue(TORSkills.Faith);
                _taskFaith.UpdateCurrentProgress(_currentFaithLevel);
                UpdateQuest();
            }
        }

        private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            // Check if the main hero captured a city or castle
            if (capturerHero == Hero.MainHero && (settlement.IsTown || settlement.IsCastle))
            {
                _currentCitiesCaptured++;
                _taskCitiesCaptured.UpdateCurrentProgress(_currentCitiesCaptured);
                UpdateQuest();
            }
        }

        private void OnTeefTransferred(object sender, TeefTransferredEventArgs e)
        {
            if (e.Hero != Hero.MainHero) return;

            _currentTeefTransferred += e.Amount;
            _taskTeefTransferred.UpdateCurrentProgress(_currentTeefTransferred);
            UpdateQuest();
        }

        private void OnEnchantmentLearned(object sender, EnchantmentLearnedEventArgs e)
        {
            if (e.Hero != Hero.MainHero) return;

            _currentEnchantsLearned++;
            _taskEnchantsLearned.UpdateCurrentProgress(_currentEnchantsLearned);
            UpdateQuest();
        }

        private void OnShrineLooted(object sender, ShrineLootedEventArgs e)
        {
            if (e.Hero != Hero.MainHero) return;

            _currentShrinesLooted++;
            _taskShrinesLooted.UpdateCurrentProgress(_currentShrinesLooted);
            UpdateQuest();
        }

        public override string SpecialQuestType => "OrcShamanQuest2";

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
            // Award the PlayerOrcShaman tier 3 attribute
            Hero.MainHero.AddAttribute("PlayerOrcShamanTier3");

            // TODO: Add final tier completion event/story if needed
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

        public override TextObject Title => TORTextHelper.GetTextObject("tor_orc_shaman_quest2_title", "Great Shaman Lord");

        public override bool IsRemainingTimeHidden => true;

        ~OrcShamanQuest2()
        {
            TORCampaignEvents.Instance.TeefTransferred -= OnTeefTransferred;
            TORCampaignEvents.Instance.EnchantmentLearned -= OnEnchantmentLearned;
            TORCampaignEvents.Instance.ShrineLooted -= OnShrineLooted;
        }
    }
}
