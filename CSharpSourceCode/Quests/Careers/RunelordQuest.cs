using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.CampaignMechanics.Menagery;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Quests;

public class RunelordQuest : QuestBase
{
    [SaveableField(1)]
    private JournalLog _task1 = null;
    [SaveableField(2)]
    private JournalLog _task2 = null;
    [SaveableField(3)]
    private JournalLog _task3 = null;
    [SaveableField(4)]
    private JournalLog _task4 = null;
    [SaveableField(5)]
    private int _currentContracts = 0;

    [SaveableField(6)]
    private int _currentAbilities = 0;
    [SaveableField(7)]
    private int _currentReputation = 0;

    public const int RUNELORDQUEST2REQUIREDCRAFTINGORDERS = 20;
    public const int RUNELORDQUEST2REQUIREDKNOWNRUNES = 22;
    public const int RUNELORDQUEST2REQUIREDLEARNEDABILITIES = 3;

    public RunelordQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
    {
        InitializeQuest();
    }

    private void InitializeQuest()
    {
        var currentKnownRunes = Hero.MainHero.GetExtendedInfo().KnownEnchantmentBlueprints.Count;//Sly : the requirement of learning all of the "current" dwarf enchantments is set to the maximum and the player's already known runes are counted as normal so the player sees in the quest description, eg "7/22" instead of "0/15" when starting the quest.

        var dwarfBehavior = Campaign.Current.GetCampaignBehavior<OathGoldBehavior>();

        if (dwarfBehavior == null) return;

        var count = 0;
        foreach (var abilityID in Hero.MainHero.GetExtendedInfo().AcquiredAbilities)
        {
            var ability = AbilityFactory.GetAllTemplates().FirstOrDefault(x => x.StringID == abilityID);
            if (ability == null) continue;

            if (ability.BelongsToLoreID == "RuneMagic")
            {
                count++;
            }
        }

        _currentAbilities = count;

        var reputation = dwarfBehavior.RuneSmithReputation;
        _currentContracts = dwarfBehavior.CraftingOrdersCompleted - RunesmithQuest.RUNESMITHQUEST1REQUIREDCRAFTINGORDERS;     //already done crafted orders of the original quest

        _task1 = AddDiscreteLog(TORTextHelper.GetTextObject("tor_runelord_quest_log_abilities", "Learn {REQUIRED} Rune abilities of the Anvil of Doom.").SetTextVariable("REQUIRED", RUNELORDQUEST2REQUIREDLEARNEDABILITIES), TORTextHelper.GetTextObject("tor_runelord_quest_task_abilities", "Rune Magic"), _currentAbilities, RUNELORDQUEST2REQUIREDLEARNEDABILITIES);
        _task2 = AddDiscreteLog(TORTextHelper.GetTextObject("tor_runelord_quest_log_runes", "Learn {REQUIRED} runes.").SetTextVariable("REQUIRED", RUNELORDQUEST2REQUIREDKNOWNRUNES), TORTextHelper.GetTextObject("tor_runelord_quest_task_runes", "Runes"), currentKnownRunes, RUNELORDQUEST2REQUIREDKNOWNRUNES);
        _task3 = AddDiscreteLog(TORTextHelper.GetTextObject("tor_runelord_quest_log_contracts", "Finish up {REQUIRED} smithing contracts.").SetTextVariable("REQUIRED", RUNELORDQUEST2REQUIREDCRAFTINGORDERS), TORTextHelper.GetTextObject("tor_runelord_quest_task_contracts", "Contracts"), _currentContracts, RUNELORDQUEST2REQUIREDCRAFTINGORDERS);
        _task4 = AddDiscreteLog(TORTextHelper.GetTextObject("tor_runelord_quest_log_reputation", "Reach Respected with the Runesmith Guild."), TORTextHelper.GetTextObject("tor_runelord_quest_task_reputation", "Reputation"), reputation, OathGoldBehavior.MAXIMUMVALUE);


    }

    protected override void RegisterEvents()
    {
        base.RegisterEvents();
        CampaignEvents.OnCraftingOrderCompletedEvent.AddNonSerializedListener(this, CraftingOrderCompleted);
        TORCampaignEvents.Instance.EnchantmentLearned += RuneLearned;
        TORCampaignEvents.Instance.AbilityLearned += AbilityLearned;
        TORCampaignEvents.Instance.OathLevelChanged += OathLevelChanged;
    }

    private void OathLevelChanged(object sender, OathLevelChangedEventArgs e)
    {
        if (e.Guild != "runesmith")
            return;

        var value = e.NewValue;

        _currentReputation = value;

        _task4.UpdateCurrentProgress(_currentReputation);
    }

    private void AbilityLearned(object sender, AbilitylearnedEventArgs e)
    {
        var hero = e.Hero;

        if (hero != Hero.MainHero)
        {
            return;
        }

        var ability = AbilityFactory.GetAllTemplates().FirstOrDefault(x => x.StringID == e.AbilityId);

        if (ability == null)
            return;

        if (ability.BelongsToLoreID == "RuneMagic")
        {
            _currentAbilities++;
        }

        _task1.UpdateCurrentProgress(_currentAbilities);
    }

    private void RuneLearned(object sender, EnchantmentLearnedEventArgs enchantmentLearnedEventArgs)
    {
        var currentProgress = _task2.CurrentProgress;

        _task2.UpdateCurrentProgress(currentProgress + 1);
        //UpdateQuestTaskStage(_task2,currentProgress+1);

        UpdateQuest();
    }

    public override string SpecialQuestType => "RunelordQuest";

    private void UpdateQuest()
    {
        if (AreAllTasksFinished())
        {
            AddLog(TORTextHelper.GetTextObject("tor_runelord_quest_log_complete", "Talk to the rune smith"));

        }
    }

    private bool AreAllTasksFinished()
    {
        return JournalEntries.All(entry => entry.HasBeenCompleted());
    }


    private void CraftingOrderCompleted(Town town, CraftingOrder order, ItemObject resultItem, Hero hero)
    {
        var currentProgress = _task3.CurrentProgress;
        _task3.UpdateCurrentProgress(currentProgress + 1);
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

    public override TextObject Title => TORTextHelper.GetTextObject("tor_runelord_quest_title", "Runelord Quest");

    public override bool IsRemainingTimeHidden => true;


    ~RunelordQuest()
    {
        TORCampaignEvents.Instance.EnchantmentLearned -= RuneLearned;
        TORCampaignEvents.Instance.AbilityLearned -= AbilityLearned;
        TORCampaignEvents.Instance.OathLevelChanged -= OathLevelChanged;
    }
}