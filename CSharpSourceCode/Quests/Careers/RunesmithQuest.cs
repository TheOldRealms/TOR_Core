using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.CampaignMechanics.Menagery;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Quests;

public class RunesmithQuest : QuestBase
{
    [SaveableField(1)]
    private JournalLog _task1 = null;
    [SaveableField(2)]
    private JournalLog _task2 = null;
    [SaveableField(3)]
    private int _currentContracts = 0;

    public const int RUNESMITHQUEST1REQUIREDCRAFTINGORDERS = 15;
    public const int RUNESMITHQUEST1REQUIREDKNOWNRUNES = 7;

    public RunesmithQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
    {
        InitializeQuest();
    }

    private void InitializeQuest()
    {
        var currentKnownRunes = Hero.MainHero.GetExtendedInfo().KnownEnchantmentBlueprints.Count;

        var dwarfBehavior = Campaign.Current.GetCampaignBehavior<OathGoldBehavior>();
        var completed = 0;


        if (dwarfBehavior != null)
        {
            _currentContracts = dwarfBehavior.CraftingOrdersCompleted;
        }

        _task1 = AddDiscreteLog(TORTextHelper.GetTextObject("tor_runesmith_quest_log_contracts", "Finish {REQUIRED} smithing contracts.").SetTextVariable("REQUIRED", RUNESMITHQUEST1REQUIREDCRAFTINGORDERS), TORTextHelper.GetTextObject("tor_runesmith_quest_task_contracts", "Contracts"), _currentContracts, RUNESMITHQUEST1REQUIREDCRAFTINGORDERS);
        _task2 = AddDiscreteLog(TORTextHelper.GetTextObject("tor_runesmith_quest_log_runes", "Learn {REQUIRED} runes.").SetTextVariable("REQUIRED", RUNESMITHQUEST1REQUIREDKNOWNRUNES), TORTextHelper.GetTextObject("tor_runesmith_quest_task_runes", "Runes"), currentKnownRunes, RUNESMITHQUEST1REQUIREDKNOWNRUNES);

    }

    protected override void RegisterEvents()
    {
        base.RegisterEvents();
        CampaignEvents.OnCraftingOrderCompletedEvent.AddNonSerializedListener(this, CraftingOrderCompleted);
        TORCampaignEvents.Instance.EnchantmentLearned += RuneLearned;
    }

    private void RuneLearned(object sender, EnchantmentLearnedEventArgs enchantmentLearnedEventArgs)
    {
        var currentProgress = _task2.CurrentProgress;

        _task2.UpdateCurrentProgress(currentProgress + 1);

        UpdateQuest();
    }

    public override string SpecialQuestType => "RunesmithQuest";

    private void UpdateQuest()
    {
        if (AreAllTasksFinished())
        {
            AddLog(TORTextHelper.GetTextObject("tor_runesmith_quest_log_complete", "Talk to the rune smith"));

        }
    }

    private bool AreAllTasksFinished()
    {
        return JournalEntries.All(entry => entry.HasBeenCompleted());
    }


    private void CraftingOrderCompleted(Town town, CraftingOrder order, ItemObject resultItem, Hero hero)
    {
        var currentProgress = _task1.CurrentProgress;
        _task1.UpdateCurrentProgress(currentProgress + 1);
        UpdateQuest();
    }


    protected override void SetDialogs()
    {

    }


    protected override void InitializeQuestOnGameLoad()
    {

    }

    protected override void HourlyTick()
    {
    }

    public override TextObject Title => TORTextHelper.GetTextObject("tor_runesmith_quest_title", "Runesmith Quest");

    public override bool IsRemainingTimeHidden => true;


    ~RunesmithQuest()
    {
        TORCampaignEvents.Instance.EnchantmentLearned -= RuneLearned;
    }

}