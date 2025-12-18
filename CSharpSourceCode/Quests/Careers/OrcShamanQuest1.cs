using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Ink;
using TOR_Core.Utilities;

namespace TOR_Core.Quests.Careers
{
    public class OrcShamanQuest1 : QuestBase
    {
        // Quest requirements constants
        private const int RequiredSpellcraftSkillLevels = 150;
        private const int RequiredFaithSkillLevels = 100;
        private const int RequiredEnchantsLearned = 5;
        private const int RequiredTeefTransferred = 125000;
        private const int RequiredShrinesLooted = 15;

        [SaveableField(1)]
        private JournalLog _taskPrayAtShrine = null;
        [SaveableField(2)]
        private JournalLog _taskSpellcraft = null;
        [SaveableField(3)]
        private JournalLog _taskFaith = null;
        [SaveableField(4)]
        private JournalLog _taskEnchantsLearned = null;
        [SaveableField(5)]
        private JournalLog _taskTeefTransferred = null;
        [SaveableField(6)]
        private JournalLog _taskShrinesLooted = null;

        [SaveableField(7)]
        private bool _hasPrayedAtShrine = false;
        [SaveableField(8)]
        private int _currentSpellcraftLevel = 0;
        [SaveableField(9)]
        private int _currentFaithLevel = 0;
        [SaveableField(10)]
        private int _currentEnchantsLearned = 0;
        [SaveableField(11)]
        private int _currentTeefTransferred = 0;
        [SaveableField(12)]
        private int _currentShrinesLooted = 0;
        [SaveableField(13)]
        private bool _readyToComplete = false;

        public OrcShamanQuest1(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
        {
            InitializeQuest();
        }

        private void InitializeQuest()
        {
            // Start with only the shrine prayer task visible
            _taskPrayAtShrine = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_shaman_quest1_log_pray", "Pray at a Shrine of Gork or Mork"),
                TORTextHelper.GetTextObject("tor_orc_shaman_quest1_task_pray", "Shrine Prayer"),
                0,
                1);

            // Don't add other tasks yet - they'll be added after shrine prayer is complete
        }

        private void RevealMainQuestTasks()
        {
            // Calculate current skill levels
            _currentSpellcraftLevel = Hero.MainHero?.GetSkillValue(TORSkills.Spellcraft) ?? 0;
            _currentFaithLevel = Hero.MainHero?.GetSkillValue(TORSkills.Faith) ?? 0;

            // TODO: Get actual values from behaviors when they're implemented
            _currentEnchantsLearned = 0;
            _currentTeefTransferred = 0;

            // Now add all the main quest tasks
            _taskSpellcraft = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_shaman_quest1_log_spellcraft", "Reach {REQUIRED} levels in Spellcraft skill")
                    .SetTextVariable("REQUIRED", RequiredSpellcraftSkillLevels),
                TORTextHelper.GetTextObject("tor_orc_shaman_quest1_task_spellcraft", "Spellcraft Skill"),
                _currentSpellcraftLevel,
                RequiredSpellcraftSkillLevels);

            _taskFaith = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_shaman_quest1_log_faith", "Reach {REQUIRED} levels in Faith skill")
                    .SetTextVariable("REQUIRED", RequiredFaithSkillLevels),
                TORTextHelper.GetTextObject("tor_orc_shaman_quest1_task_faith", "Faith Skill"),
                _currentFaithLevel,
                RequiredFaithSkillLevels);

            _taskEnchantsLearned = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_shaman_quest1_log_enchants", "Learn {REQUIRED} Orc enchantments")
                    .SetTextVariable("REQUIRED", RequiredEnchantsLearned),
                TORTextHelper.GetTextObject("tor_orc_shaman_quest1_task_enchants", "Enchantments Learned"),
                _currentEnchantsLearned,
                RequiredEnchantsLearned);

            _taskTeefTransferred = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_shaman_quest1_log_teef", "Transfer {REQUIRED} worth of loot to teef")
                    .SetTextVariable("REQUIRED", RequiredTeefTransferred),
                TORTextHelper.GetTextObject("tor_orc_shaman_quest1_task_teef", "Teef Transferred"),
                _currentTeefTransferred,
                RequiredTeefTransferred);

            _taskShrinesLooted = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_orc_shaman_quest1_log_shrines", "Loot {REQUIRED} shrines")
                    .SetTextVariable("REQUIRED", RequiredShrinesLooted),
                TORTextHelper.GetTextObject("tor_orc_shaman_quest1_task_shrines", "Shrines Looted"),
                _currentShrinesLooted,
                RequiredShrinesLooted);
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            // Shrine prayer tracking
            TORCampaignEvents.Instance.ShrinePrayer += OnShrinePrayer;

            // Skill tracking (only matters after shrine prayer)
            CampaignEvents.HeroGainedSkill.AddNonSerializedListener(this, OnSkillIncreased);

            // Custom TOR events
            TORCampaignEvents.Instance.TeefTransferred += OnTeefTransferred;
            TORCampaignEvents.Instance.EnchantmentLearned += OnEnchantmentLearned;
            TORCampaignEvents.Instance.ShrineLooted += OnShrineLooted;
        }

        private void OnShrinePrayer(object sender, ShrinePrayerEventArgs e)
        {
            // Only process for main hero praying at Greenskin shrines
            if (e.Hero != Hero.MainHero) return;
            if (e.Religion.Culture.StringId != TORConstants.Cultures.GREENSKIN) return;
            if (_hasPrayedAtShrine) return; // Already completed this step

            // Mark shrine prayer as complete
            _hasPrayedAtShrine = true;
            _taskPrayAtShrine.UpdateCurrentProgress(1);

            // Show the vision story
            InkStoryManager.OpenStory("OrcShamanQuest1InitialVision");

            // Reveal the main quest tasks
            RevealMainQuestTasks();

            UpdateQuest();
        }

        private void OnSkillIncreased(Hero hero, SkillObject skill, int skillValueBefore, bool arg4)
        {
            if (hero != Hero.MainHero) return;
            if (!_hasPrayedAtShrine) return; // Only track after shrine prayer

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

        private void OnTeefTransferred(object sender, TeefTransferredEventArgs e)
        {
            if (e.Hero != Hero.MainHero) return;
            if (!_hasPrayedAtShrine) return; // Only track after shrine prayer

            _currentTeefTransferred += e.Amount;
            _taskTeefTransferred.UpdateCurrentProgress(_currentTeefTransferred);
            UpdateQuest();
        }

        private void OnEnchantmentLearned(object sender, EnchantmentLearnedEventArgs e)
        {
            if (e.Hero != Hero.MainHero) return;
            if (!_hasPrayedAtShrine) return; // Only track after shrine prayer

            _currentEnchantsLearned++;
            _taskEnchantsLearned.UpdateCurrentProgress(_currentEnchantsLearned);
            UpdateQuest();
        }

        private void OnShrineLooted(object sender, ShrineLootedEventArgs e)
        {
            if (e.Hero != Hero.MainHero) return;
            if (!_hasPrayedAtShrine) return; // Only track after shrine prayer

            _currentShrinesLooted++;
            _taskShrinesLooted.UpdateCurrentProgress(_currentShrinesLooted);
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
            // Award the PlayerOrcShaman tier 2 attribute
            Hero.MainHero.AddAttribute("PlayerOrcShamanTier2");

            // Open the transition story which will start OrcShamanQuest2
            InkStoryManager.OpenStory("OrcShamanQuest2InitialVision");
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

        public override TextObject Title => TORTextHelper.GetTextObject("tor_orc_shaman_quest1_title", "Favoured uv Da Godz");

        public override bool IsRemainingTimeHidden => true;

        ~OrcShamanQuest1()
        {
            TORCampaignEvents.Instance.ShrinePrayer -= OnShrinePrayer;
            TORCampaignEvents.Instance.TeefTransferred -= OnTeefTransferred;
            TORCampaignEvents.Instance.EnchantmentLearned -= OnEnchantmentLearned;
            TORCampaignEvents.Instance.ShrineLooted -= OnShrineLooted;
        }
    }
}
