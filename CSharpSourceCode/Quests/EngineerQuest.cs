using System.Collections.Generic;
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
using static TaleWorlds.CampaignSystem.Campaign;

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
        [SaveableField(7)] private string _cultistPartyTemplateId = "";
        private string _cultistLeaderTemplateId = "";
        [SaveableField(8)] private TextObject _rogueEngineerDisplayName = null;
        [SaveableField(9)] private string _rogueEngineerLeaderName = "";
        [SaveableField(10)] private string _rogueEngineerPartyTemplateId  = "";
        private string _rogueEngineerLeaderTemplateId = "";
        
        [SaveableField(11)] private string _cultistfactionID = "";
        [SaveableField(12)] private readonly TextObject _title=null;
        [SaveableField(13)] private TextObject _missionLogText1=null;
        [SaveableField(14)] private TextObject _missionLogTextShort1=null;
        [SaveableField(15)] private TextObject _missionLogText2=null;
        [SaveableField(16)] private TextObject _missionLogTextShort2=null;
        [SaveableField(17)] private TextObject _defeatDialogLine = null;
        [SaveableField(18)] private bool _failstate;
        private bool _initAfterReload;
        private bool _skipImprisonment;
        
        private string _engineerfactionID;

        private CharacterObject _cultistLeader;


        private string CityID = "town_WI1";


        private delegate void InializeVisuals(MobileParty party);
        
        
        public EngineerQuest(string questId,Hero questGiver, CampaignTime duration, int rewardGold, string questTitle,string CultistPartyName, string cultistLeaderTemplate,
            string _cultistPartyTemplate,string cultistfactionID, string rogueEngineerLeaderTemplateId, string rogueEngineerPartyTemplate,string rogueEngineerFactionId): base(
            questId, questGiver, duration, rewardGold)
        {
            _title = new TextObject(questTitle);

            _cultistLeaderTemplateId = cultistLeaderTemplate;
            _cultistPartyLeaderName = new TextObject("Runaway thief leader");
            _cultistPartyDisplayName = new TextObject(CultistPartyName);
            _cultistPartyTemplateId = _cultistPartyTemplate;
            _cultistfactionID = cultistfactionID;

            _rogueEngineerLeaderTemplateId = rogueEngineerLeaderTemplateId;
            
            _cultistLeader = MBObjectManager.Instance.GetObject<CharacterObject>(_cultistLeaderTemplateId);
            
            var leaderTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(_rogueEngineerLeaderTemplateId);

            _rogueEngineerLeaderName = leaderTemplate.Name.ToString();
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
            _task1 = AddDiscreteLog(new TextObject("Catch the runaway thiefs"), new TextObject("Catched Thiefs"), 0, 1);
            QuestTaskBase task = new QuestTaskBase();

            //var t = (QuestPartyComponent)_targetParty.PartyComponent; 
            //_task1
         
            

            //   _task1 = AddDiscreteLog(new TextObject("Catch the runaway thiefs"), new TextObject("Number of casts"), 0, 1);
            //  _task2 = AddDiscreteLog(new TextObject("Catch the runaway thiefs"), new TextObject("Number of casts"), 0, 1);


            /*_task1 = _missionLogTextShort1!=null ? 
                AddDiscreteLog(_missionLogText1, _missionLogTextShort1, _destroyedParty, 1) : 
                AddLog(_missionLogText1);*/
        }
        
        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this,QuestBattleEnded);
            CampaignEvents.SetupPreConversationEvent.AddNonSerializedListener(this,SkipDialog);
            CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, KillLeaderFromQuestPartyAfterDialog);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this,QuestBattleEndedWithFail);
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
            if (Current.CurrentConversationContext != ConversationContext.CapturedLord) return;
            Current.ConversationManager.EndConversation();
            Current.ConversationManager.AddDialogLineMultiAgent("start", "start", "rogueengineer_playerafterbattle", new TextObject("Your victory here is meaningless...you will never find what we took..."), ()=> _skipImprisonment, RemoveSkip, 0,1, 200, null);
            Current.ConversationManager.ClearCurrentOptions();
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
            Settlement targetSettlement=null;

            var closest =Settlement.All.Where(x => x.IsHideout).MinBy(x => x.GetTrackDistanceToMainAgent());
            
            
            SpawnQuestParty(_cultistPartyLeaderName,_cultistPartyDisplayName,closest);
        }

        public void TaskSuccessful()
        {
            /*_task1.UpdateCurrentProgress(1);

            if (_task1.HasBeenCompleted() && _task2 == null)
            {
                _task2 = _missionLogTextShort2!=null ? 
                    AddDiscreteLog(_missionLogText2, _missionLogTextShort2, _destroyedParty, 1) : 
                    AddLog(_missionLogText2);
            }*/
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
          
            
            
            //base.AddTrackedObject(_targetParty);
           
            /*if (!_task1.HasBeenCompleted())
            {
              //  RegisterQuestSpecificElementsOnGameLoad();
            }*/
            

            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this,Markers);

        }
        

        private void Markers(MobileParty party)
        {
            if (_targetParty != party) return;
            
            /*party.Party.Visuals.SetVisualVisible(true);
            party.Party.Visuals.SetMapIconAsDirty();
            party.SetPartyUsedByQuest(true);
            CampaignEventDispatcher.Instance.OnPartyVisibilityChanged(party.Party);
            base.AddTrackedObject(_targetParty);
            Current.VisualTrackerManager.RegisterObject(party);
            */
           // TORCommon.Say(party.PartyComponent.Name);


            CampaignEvents.HourlyTickPartyEvent.ClearListeners(this);
        }
        protected override void AfterLoad()
        {
            base.AfterLoad();
           
            
            
        }
        

        public override void OnFailed()
        {
            base.OnFailed();
            _failstate = true;
        }

        public static EngineerQuest GetCurrentActiveIfExists()
        {
            EngineerQuest returnvalue = null;
            if (Current.QuestManager.Quests.Any(x => x is EngineerQuest && x.IsOngoing))
            {
                returnvalue = Current.QuestManager.Quests.FirstOrDefault(x => x is EngineerQuest && x.IsOngoing) as EngineerQuest;
            }
            return returnvalue;
        }


        private void SpawnQuestParty(Hero hero, Settlement spawnSettlement, Clan clan, TextObject name)
        {
            var party = QuestPartyComponent.CreateParty(spawnSettlement, hero, clan);
            
            
         //   party.Party.Visuals.SetVisualVisible(true);
         //   party.Party.Visuals.SetMapIconAsDirty();
          //  party.SetCustomName(name); 
         // party.SetPartyUsedByQuest(true);
         AddTrackedObject(party);
         _targetParty = party;

        }
        
        
        
        private void SpawnQuestParty(TextObject heroNameOverride=null, TextObject partyNameOverride=null, Settlement spawnLocationOverride=null)
        {
            
            var leaderTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(_cultistLeaderTemplateId);
            
            var faction =  Current.Factions.FirstOrDefault(x => x.StringId.ToString() == _cultistfactionID);
            var factionClan = (Clan)faction;
            //this is intended as a quick fix, if we dont  want a full random spawning
            var settlement = spawnLocationOverride == null ? 
                Settlement.All.FirstOrDefault(x => x.IsHideout && x.Culture.StringId == leaderTemplate.Culture.StringId) : 
                Settlement.All.FirstOrDefault(x => x.Name ==spawnLocationOverride.Name);
            
            
            
            var hero = HeroCreator.CreateSpecialHero(leaderTemplate, settlement, factionClan , null, 45);
            if(heroNameOverride!=null)hero.SetName(heroNameOverride, heroNameOverride);
            var party = QuestPartyComponent.CreateParty(settlement, hero, factionClan, _cultistPartyTemplateId);
            if(partyNameOverride!=null)party.SetCustomName(partyNameOverride);
            party.Aggressiveness = 0f;
            party.IgnoreByOtherPartiesTill(CampaignTime.Never);
          //  party.SetPartyUsedByQuest(true);
            _targetParty = party;
            List<PartyBase> list = new List<PartyBase>();
            list.Add(_targetParty.Party);
            AddTrackedObject(party);
            
            ToggleTrackedObjects();

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