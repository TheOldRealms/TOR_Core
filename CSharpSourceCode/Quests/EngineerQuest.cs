using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using static TaleWorlds.CampaignSystem.Campaign;

namespace TOR_Core.Quests
{
    public class EngineerQuest : QuestBase
    {
        [SaveableField(1)] private EngineerQuestStates _currentActiveLog = EngineerQuestStates.Cultisthunt;
        [SaveableField(2)] private JournalLog _task1 = null;
        [SaveableField(3)] private JournalLog _task2 = null;
        [SaveableField(4)] private JournalLog _task3 = null;
        [SaveableField(5)] private JournalLog _task4 = null;
        [SaveableField(6)] private MobileParty _targetParty = null;
        [SaveableField(7)] private bool _failstate;
        private bool _skipImprisonment;
        private string RogueEngineerLeaderName;
        private const string QuestName = "Runaway Parts";
        private const string CultistFactionId = "forest_bandits";
        private string CultistPartyDisplayName;
        private string CultistPartyLeaderName;
        private const string CultistPartyTemplateId = "broken_wheel";
        private const string CultistLeaderTemplateId = "tor_bw_cultist_lord_0";
        private const string EngineerFactionId = "mountain_bandits";
        private string RogueEngineerDisplayName;
       
        private const string RogueEngineerPartyTemplateId = "empire_deserters_boss_party";
        private const string RogueEngineerLeaderTemplateId = "tor_engineerquesthero";
        public EngineerQuestStates CurrentActiveLog => (EngineerQuestStates)_currentActiveLog;
        private List<JournalLog> _logs;

        private delegate void InializeVisuals(MobileParty party);

        public EngineerQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId,
            questGiver, duration, rewardGold)
        {
            InitializeQuest();
        }

        public MobileParty TargetParty => _targetParty;
        public string QuestEnemyLeaderName => CultistPartyLeaderName.ToString();
        public bool FailState => _failstate;
        public override bool IsSpecialQuest => true;
        public override TextObject Title => new TextObject(QuestName);
        public override bool IsRemainingTimeHidden => false;

        private void LoadAllLogs()
        {
            _logs = new List<JournalLog>();
            var log0 = new JournalLog(CampaignTime.Now, new TextObject("{=tor_engineer_quest_log0_str}The Master Engineer has tasked me with hunting down thieving runaways, I should find them and bring back what they stole."), new TextObject("{=tor_engineer_quest_task0_str}Track down runaway thieves"), 0, 1, LogType.Discreate);
            var log1 = new JournalLog(CampaignTime.Now, new TextObject("{=tor_engineer_quest_log1_str}I found the thieves, but they did not have the stolen components. I should return to the Master Engineer with the news."), new TextObject("{=tor_engineer_quest_task1_str}Return to the Master Engineer in Nuln"), 0, 0, LogType.Discreate);
            var log2 = new JournalLog(CampaignTime.Now, new TextObject("{=tor_engineer_quest_log2_str}It would appear a traitorous Engineer has the stolen parts, the Master Engineer has asked for my help in finding him."), new TextObject("{=tor_engineer_quest_task2_str}Track down Goswin and retrieve the stolen components."), 0, 1, LogType.Discreate);
            var log3 = new JournalLog(CampaignTime.Now, new TextObject("{=tor_engineer_quest_log3_str}I have slain Oswin and retrieved the stolen components, I should return to the Master Engineer and let him know."), new TextObject("{=tor_engineer_quest_task3_str}Return to the Master Engineer in Nuln"), 0, 1, LogType.Discreate);
            _logs.Add(log0);
            _logs.Add(log1);
            _logs.Add(log2);
            _logs.Add(log3);
        }

        public string GetRogueEngineerTemplateID()
        {
            return RogueEngineerLeaderTemplateId;
        }

        private void InitializeQuest()
        {
            LoadAllLogs();
            _task1 = AddDiscreteLog(_logs[0].LogText, _logs[0].TaskName, 0, 1);
            RogueEngineerLeaderName = new TextObject ("{ROGUE_ENGINEER_NAME}").ToString();
            RogueEngineerDisplayName = GameTexts.FindText ("str_quest_tor_engineer","rogueEngineerParty").ToString();
            CultistPartyDisplayName = new TextObject("{tor_quest_engineer_cultist_party_str} Runaway Thieves").ToString();
            CultistPartyLeaderName = new TextObject("{tor_quest_engineer_cultist_party_leader_str} Runaway Thieves Leader").ToString();
            _currentActiveLog = 0;
        }

        public void ResetQuestinCurrentState()
        {
            if (_currentActiveLog == EngineerQuestStates.Cultisthunt)
            {
                RemoveLog(_task1);
                SpawnQuestParty(CultistLeaderTemplateId, CultistPartyTemplateId, CultistFactionId,
                    CultistPartyLeaderName, CultistPartyDisplayName);
                _task1 = AddDiscreteLog(_logs[(int)EngineerQuestStates.Cultisthunt].LogText,
                    _logs[(int)EngineerQuestStates.Cultisthunt].TaskName, 0, 1);
            }

            if (_currentActiveLog == EngineerQuestStates.RogueEngineerhunt)
            {
                RemoveLog(_task3);
                SpawnQuestParty(RogueEngineerLeaderTemplateId, RogueEngineerPartyTemplateId, EngineerFactionId,
                    RogueEngineerLeaderName, RogueEngineerDisplayName);
                _task3 = AddDiscreteLog(_logs[(int)EngineerQuestStates.RogueEngineerhunt].LogText,
                    _logs[(int)EngineerQuestStates.RogueEngineerhunt].TaskName, 0, 1);
            }
        }

        public override int GetCurrentProgress()
        {
            return (int)_currentActiveLog;
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, QuestBattleEnded);
            CampaignEvents.SetupPreConversationEvent.AddNonSerializedListener(this, SkipDialog);
            CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, KillLeaderFromQuestPartyAfterDialog);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, QuestBattleEndedWithFail);
        }

        private void QuestBattleEnded(MapEvent mapEvent)
        {
            if (!mapEvent.IsPlayerMapEvent || !mapEvent.IsFieldBattle) return;
            if (mapEvent.PartiesOnSide(mapEvent.PlayerSide.GetOppositeSide())
                .Any(party => party.Party.MobileParty == _targetParty))
            {
                _skipImprisonment = true;
                UpdateProgressOnQuest();
            }
        }

        private void QuestBattleEndedWithFail(MapEvent mapEvent)
        {
            if (mapEvent.Winner == null) return;
            if (!mapEvent.IsPlayerMapEvent ||
                mapEvent.InvolvedParties.All(party => party.MobileParty != _targetParty)) return;
            if (mapEvent.Winner.MissionSide == mapEvent.PlayerSide) return;
            if (_failstate) return;
            if (_currentActiveLog == EngineerQuestStates.Cultisthunt)
            {
                RemoveLog(_task1);
                _task1 = AddDiscreteLog(
                    new TextObject("{=tor_engineer_quest_log1_fail_str}I failed... I was beaten. I need to return to the Master Engineer with the news."),
                    new TextObject("Return to the Master Engineer in Nuln"), 0, 1);
            }

            if (_currentActiveLog == EngineerQuestStates.RogueEngineerhunt)
            {
                RemoveLog(_task3);
                _task3 = AddDiscreteLog(
                    new TextObject("{=tor_engineer_quest_log3_fail_str}I failed... I was beaten. I need to return to the Master Engineer with the news."),
                    new TextObject("Return to the Master Engineer in Nuln"), 0, 1);
            }

            _failstate = true;
            _targetParty.RemoveParty();
        }

        private void SkipDialog()
        {
            if (_targetParty == null) return;
            if (!_targetParty.IsActive) return;
            if (!_skipImprisonment) return;
            if (Current.CurrentConversationContext != ConversationContext.CapturedLord) return;
            //Current.ConversationManager.EndConversation();
            Current.ConversationManager.ClearCurrentOptions();
            Current.ConversationManager.AddDialogLineMultiAgent("start", "start", "close_window",
                new TextObject("{=tor_engineer_quest_cultists_skip_str}Your victory here is meaningless...you will never find what we took..."),
                () => _skipImprisonment && _currentActiveLog == EngineerQuestStates.HandInCultisthunt, RemoveSkip, 0, 1,
                200, null);
            
            Current.ConversationManager.AddDialogLineMultiAgent("start", "start", "rogueengineer_playerafterbattle",
                new TextObject("{=tor_engineer_quest_engineer_skip_str}You have no idea what you are interfering with..."),
                () => _skipImprisonment && _currentActiveLog == EngineerQuestStates.HandInRogueEngineerHunt, RemoveSkip,
                0, 1, 200, null);
           
        }

        private void RemoveSkip()
        {
            _skipImprisonment = false;
        }

        private void KillLeaderFromQuestPartyAfterDialog(PartyBase obj)
        {
            if (obj.MobileParty == _targetParty)
            {
                KillCharacterAction.ApplyByRemove(obj.Owner, false);
            }
        }

        protected override void OnStartQuest()
        {
            SpawnQuestParty(CultistLeaderTemplateId, CultistPartyTemplateId, CultistFactionId, CultistPartyLeaderName,
                CultistPartyDisplayName);
        }

        public bool CultistQuestIsActive()
        {
            return _currentActiveLog == EngineerQuestStates.Cultisthunt;
        }

        public bool RogueEngineerQuestPartIsActive()
        {
            return _currentActiveLog == EngineerQuestStates.RogueEngineerhunt;
        }

        public void UpdateProgressOnQuest(EngineerQuestStates state = EngineerQuestStates.None,
            bool withProgress = true)
        {
            if (state != EngineerQuestStates.None) _currentActiveLog = state;
            switch (_currentActiveLog)
            {
                case EngineerQuestStates.Cultisthunt:
                    _task1.UpdateCurrentProgress(1);
                    if (withProgress) _task2 = AddDiscreteLog(_logs[1].LogText, _logs[1].TaskName, 0, 1);
                    break;
                case EngineerQuestStates.HandInCultisthunt:
                    _task2.UpdateCurrentProgress(1);
                    if (withProgress)
                    {
                        SpawnQuestParty(RogueEngineerLeaderTemplateId, RogueEngineerPartyTemplateId, EngineerFactionId,
                            RogueEngineerLeaderName, RogueEngineerDisplayName);
                        _task3 = AddDiscreteLog(_logs[2].LogText, _logs[2].TaskName, 0, 1);
                    }

                    break;
                case EngineerQuestStates.RogueEngineerhunt:
                    _task3.UpdateCurrentProgress(1);
                    if (withProgress)
                    {
                        _task4 = AddDiscreteLog(_logs[3].LogText, _logs[3].TaskName, 0, 1);
                    }

                    break;
                case EngineerQuestStates.HandInRogueEngineerHunt:
                    _task4.UpdateCurrentProgress(1);
                    CompleteQuestWithSuccess();
                    break;
            }

            if (withProgress) _currentActiveLog++;
        }

        public void HandInQuest()
        {
            _task2.UpdateCurrentProgress(1);
            CompleteQuestWithSuccess();
        }

        protected override void SetDialogs()
        {
        }

        protected override void InitializeQuestOnGameLoad()
        {
            LoadAllLogs();
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, Markers);
        }

        private void Markers(MobileParty party) //Currently pointless 
        {
            if (_targetParty != party) return;
            CampaignEvents.HourlyTickPartyEvent.ClearListeners(this);
        }

        public static EngineerQuest GetCurrentActiveIfExists()
        {
            EngineerQuest returnvalue = null;
            if (Current.QuestManager.Quests.Any(x => x is EngineerQuest && x.IsOngoing))
            {
                returnvalue =
                    Current.QuestManager.Quests.FirstOrDefault(x => x is EngineerQuest && x.IsOngoing) as EngineerQuest;
            }

            return returnvalue;
        }

        private void SpawnQuestParty(string partyLeaderTemplate, string partyTemplate, string factionId,
            string heroNameOverride = null, string partyNameOverride = null, Settlement spawnLocationOverride = null)
        {
            var leaderTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(partyLeaderTemplate);
            var faction = Current.Factions.FirstOrDefault(x => x.StringId.ToString() == factionId);
            var factionClan = (Clan)faction;
            //this is intended as a quick fix, if we dont  want a full random spawning
            Settlement settlement = null;
            if (spawnLocationOverride == null)
            {
                if (factionClan.IsBanditFaction)
                {
                    settlement = Settlement.All.FirstOrDefault(x =>
                        x.IsHideout && x.Culture.StringId == faction.Culture.StringId);
                }
                else
                {
                    settlement = Settlement.All.Where(x => x.IsHideout).MinBy(x => x.GetTrackDistanceToMainAgent());
                    ;
                }
            }
            else
            {
                settlement = Settlement.All.FirstOrDefault(x => x.Name == spawnLocationOverride.Name);
            }

            var partyTextObject = new TextObject(partyNameOverride);
            var heroTextObject = new TextObject(heroNameOverride);
            var hero = HeroCreator.CreateSpecialHero(leaderTemplate, settlement, factionClan, null, 45);
            if (heroNameOverride != null) hero.SetName(heroTextObject, heroTextObject);
            var party = QuestPartyComponent.CreateParty(settlement, hero, factionClan, partyTemplate);
            if (partyNameOverride != null) party.SetCustomName(partyTextObject);
            party.Aggressiveness = 0f;
            party.IgnoreByOtherPartiesTill(CampaignTime.Never);
            //  party.SetPartyUsedByQuest(true);
            _targetParty = party;
            List<PartyBase> list = new List<PartyBase>();
            list.Add(_targetParty.Party);
            AddTrackedObject(party);
        }

        protected override void HourlyTick() { }
    }

    public enum EngineerQuestStates
    {
        None = -1,
        Cultisthunt = 0,
        HandInCultisthunt = 1,
        RogueEngineerhunt = 2,
        HandInRogueEngineerHunt = 3
    }
}