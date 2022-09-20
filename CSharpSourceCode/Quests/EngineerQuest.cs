using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TOR_Core.Quests
{
    public class EngineerQuest : QuestBase
    {
        [SaveableField(1)] private int _destroyedParty = 0;
        [SaveableField(2)] private JournalLog _task1 = null;
        [SaveableField(3)] private JournalLog _task2 = null;
        [SaveableField(4)] private MobileParty _targetParty = null;
        [SaveableField(5)] private TextObject _cultistPartyDisplayName = null;
        [SaveableField(6)] private TextObject _cultistPartyLeaderName;
        [SaveableField(8)] private string _cultistPartyTemplateId = "";
        private string _cultistLeaderTemplateId = "";
        [SaveableField(5)] private TextObject _rogueEngineerDisplayName = null;
        [SaveableField(7)] private string _rogueEngineerLeaderName = "";
        [SaveableField(8)] private string _rogueEngineerPartyTemplateId  = "";
        private string _rogueEngineerLeaderTemplateId = "";
        
        [SaveableField(9)] private string _cultistfactionID = "";
        [SaveableField(10)] private string _spawnLocationOwnerId = "";
        [SaveableField(11)] private readonly TextObject _title=null;
        [SaveableField(12)] private TextObject _missionLogText1=null;
        [SaveableField(13)] private TextObject _missionLogTextShort1=null;
        [SaveableField(14)] private TextObject _missionLogText2=null;
        [SaveableField(15)] private TextObject _missionLogTextShort2=null;
        [SaveableField(16)] private TextObject _defeatDialogLine = null;
        [SaveableField(17)] private bool _failstate;
        private bool _initAfterReload;
        private bool _skipImprisonment;
        
        private string _engineerfactionID;
        public EngineerQuest(string questId,Hero questGiver, CampaignTime duration, int rewardGold, string questTitle,string CultistPartyName, string cultistLeaderName,
            string _cultistPartyTemplate,string cultistfactionID, string rogueEngineerName, string rogueEngineerPartyTemplate,string rogueEngineerFactionId): base(
            questId, questGiver, duration, rewardGold)
        {
            _title = new TextObject(questTitle);
            _cultistPartyLeaderName = new TextObject(cultistLeaderName);
            _cultistPartyDisplayName = new TextObject(CultistPartyName);
            _cultistPartyTemplateId = _cultistPartyTemplate;
            _cultistfactionID = cultistfactionID;

            _rogueEngineerLeaderName = rogueEngineerName;
            _rogueEngineerDisplayName= new TextObject(rogueEngineerPartyTemplate);
            _rogueEngineerPartyTemplateId = rogueEngineerPartyTemplate;
            _engineerfactionID = rogueEngineerFactionId;

            SetLogs();
        }

        public MobileParty TargetParty => _targetParty;
        public string QuestEnemyLeaderName => _cultistPartyLeaderName.ToString();
        public bool FailState => _failstate;
        public override bool IsSpecialQuest => true;
        public override TextObject Title => _title;
        public override bool IsRemainingTimeHidden => false;
       

        private void SetLogs()
        {
            _task1 = AddDiscreteLog(new TextObject("Catch the runaway thiefs"), new TextObject("Number of casts"), 0, 1);
            _task1 = AddDiscreteLog(new TextObject("Catch the runaway thiefs"), new TextObject("Number of casts"), 0, 1);
            _task2 = AddDiscreteLog(new TextObject("Catch the runaway thiefs"), new TextObject("Number of casts"), 0, 1);

            
            _task1 = _missionLogTextShort1!=null ? 
                AddDiscreteLog(_missionLogText1, _missionLogTextShort1, _destroyedParty, 1) : 
                AddLog(_missionLogText1);
        }
        
        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this,QuestBattleEnded);
            CampaignEvents.SetupPreConversationEvent.AddNonSerializedListener(this,SkipDialog);
            CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, KillLeaderFromQuestPartyAfterDialog);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this,QuestBattleEndedWithFail);
        }
        
        private void RegisterQuestSpecificElementsOnGameLoad()
        {
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this,SetMarkerAfterLoad);
        }
        
        private void SetMarkerAfterLoad(MobileParty mobileParty)
        {
            if (_initAfterReload) return;
            if (_task1.HasBeenCompleted()) return;
            
            var home = _targetParty.HomeSettlement;
            var hero = _targetParty.LeaderHero;
            var clan = _targetParty.ActualClan;
            var pos = _targetParty.Position2D;
            var name = _targetParty.Name;
            _targetParty.ChangePartyLeader(null);
            _targetParty.ResetTargetParty();
            _targetParty.RemoveParty();
            
            SpawnQuestParty(hero, home, clan,name);
            _targetParty.SetPartyUsedByQuest(true);
            _targetParty.Party.Visuals.SetMapIconAsDirty();
            _initAfterReload = true;
        }
        
        private void QuestBattleEnded(MapEvent mapEvent)
        {
            if (!mapEvent.IsPlayerMapEvent || !mapEvent.IsFieldBattle) return;
            if (mapEvent.PartiesOnSide(mapEvent.PlayerSide.GetOppositeSide()).Any(party => party.Party.MobileParty == _targetParty))
            {
                _skipImprisonment = true;
                TaskSuccessful();
            }
        }
        
        private void QuestBattleEndedWithFail(MapEvent mapEvent)
        {
            if (mapEvent.Winner == null) return;
            if (!mapEvent.IsPlayerMapEvent|| mapEvent.InvolvedParties.All(party => party.MobileParty != _targetParty)) return;
            if (mapEvent.Winner.MissionSide == mapEvent.PlayerSide) return;
            CompleteQuestWithFail();
            AddDiscreteLog( new TextObject("I failed... I was beaten. I need to return to the Master Engineer with the news."),
                new TextObject("Return to the Master Engineer in Nuln"), 
                _destroyedParty, 1); 
            _targetParty.RemoveParty();
        }
        
        private void SkipDialog()
        {
            if (!_targetParty.IsActive) return;
            if (!_skipImprisonment) return;
            if (Campaign.Current.CurrentConversationContext != ConversationContext.CapturedLord) return;
            Campaign.Current.ConversationManager.EndConversation();
            Campaign.Current.ConversationManager.AddDialogLineMultiAgent("start", "start", "rogueengineer_playerafterbattle", _defeatDialogLine, ()=> _skipImprisonment, RemoveSkip, 0,1, 200, null);
            Campaign.Current.ConversationManager.ClearCurrentOptions();
        }
        
        private void RemoveSkip()
        {
            _skipImprisonment = false;
        }
        
        private void KillLeaderFromQuestPartyAfterDialog(PartyBase obj)
        {
            if (obj.MobileParty == _targetParty)
            {
                KillCharacterAction.ApplyByRemove(obj.Owner,false);
            }
        }
        
        protected override void OnStartQuest()
        {
            
            SpawnQuestParty(_cultistPartyLeaderName,_cultistPartyDisplayName,null);
        }

        public void TaskSuccessful()
        {
            _task1.UpdateCurrentProgress(1);

            if (_task1.HasBeenCompleted() && _task2 == null)
            {
                _task2 = _missionLogTextShort2!=null ? 
                    AddDiscreteLog(_missionLogText2, _missionLogTextShort2, _destroyedParty, 1) : 
                    AddLog(_missionLogText2);
            }
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
            if (!_task1.HasBeenCompleted())
            {
              //  RegisterQuestSpecificElementsOnGameLoad();
            }
        }
        
        public override void OnFailed()
        {
            base.OnFailed();
            _failstate = true;
        }

        public static EngineerQuest GetCurrentActiveIfExists()
        {
            EngineerQuest returnvalue = null;
            if (Campaign.Current.QuestManager.Quests.Any(x => x is EngineerQuest && x.IsOngoing))
            {
                returnvalue = Campaign.Current.QuestManager.Quests.FirstOrDefault(x => x is EngineerQuest && x.IsOngoing) as EngineerQuest;
            }
            return returnvalue;
        }


        private void SpawnQuestParty(Hero hero, Settlement spawnSettlement, Clan clan, TextObject name)
        {
            var party = QuestPartyComponent.CreateParty(spawnSettlement, hero, clan);
            party.Party.Visuals.SetVisualVisible(true);
            party.Party.Visuals.SetMapIconAsDirty();
            party.SetCustomName(name); 
            party.SetPartyUsedByQuest(true);
            AddTrackedObject(party);
            _targetParty = party;
        }
        
        
        
        private void SpawnQuestParty(TextObject heroNameOverride=null, TextObject partyNameOverride=null, TextObject spawnLocationOverride=null)
        {
            //this is intended as a quick fix, if we dont  want a full random spawning
            var settlement = spawnLocationOverride == null ? 
                Settlement.All.FirstOrDefault(x => x.IsHideout && x.Culture.StringId == _spawnLocationOwnerId) : 
                Settlement.All.FirstOrDefault(x => x.Name ==spawnLocationOverride);
            
            var leaderTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(_cultistLeaderTemplateId);
            var faction =  Campaign.Current.Factions.FirstOrDefault(x => x.StringId.ToString() == _cultistfactionID);
            var factionClan = (Clan)faction;
            var hero = HeroCreator.CreateSpecialHero(leaderTemplate, settlement, factionClan , null, 45);
            if(heroNameOverride!=null)hero.SetName(heroNameOverride, heroNameOverride);
            var party = QuestPartyComponent.CreateParty(settlement, hero, factionClan, _cultistPartyTemplateId);
            if(partyNameOverride!=null)party.SetCustomName(partyNameOverride);
            party.Aggressiveness = 0f;
            party.IgnoreByOtherPartiesTill(CampaignTime.Never);
            party.SetPartyUsedByQuest(true);
            _targetParty = party;

        }
        
    }
    
    
    
    public class RogueEngineerQuestTypeDefiner : SaveableTypeDefiner
    {
        public RogueEngineerQuestTypeDefiner() : base(701792)
        {

        }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(EngineerQuest), 1);
        }
    }
}