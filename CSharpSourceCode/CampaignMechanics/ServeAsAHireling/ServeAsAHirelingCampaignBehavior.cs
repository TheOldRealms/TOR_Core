using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.ServeAsAHireling
{
    public class ServeAsAHirelingCampaignBehavior : CampaignBehaviorBase
    {
        private ServeAsAHirelingActivities _activities;
        private const float MinimumServeDays = 20;
        private const float RatioPartyAgainstEnemyStrength = 0.30f;
        
        private float _durationInDays;
        private bool _hirelingEnlisted;
        private Hero _hirelingEnlistingLord;
        private bool _hirelingEnlistingLordIsAttacking;
        private bool _hirelingLordIsFightingWithoutPlayer;
       
        private readonly bool _debugSkipBattles = false;
        private bool _pauseModeToggle;
        private int _manuallyFoughtBattles;

        private bool _startBattle;
        private bool _siegeBattleMissionStarted;

        private bool _hirelingWaitMenuShown;

        private float _entryServiceTimeStamp;

        private SkillObject _currentTrainedSkill;
        private int _currentActivityIndex;
        
        private bool _enlistInquiryDeclined;

        public float DurationInDays => _durationInDays;

        public int ManuallyFoughtBattles => _manuallyFoughtBattles;


        public bool IsEnlisted()
        {
            return _hirelingEnlisted;
        }

        public Hero EnlistingLord => _hirelingEnlistingLord;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, Initialize);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, EnlistingLordPartyEntersSettlement);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnPartyLeavesSettlement);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, ControlPlayerLoot);                //Those events are never executed when the player lose a battle!
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, MenuOpened);
            CampaignEvents.GameMenuOptionSelectedEvent.AddNonSerializedListener(this, ContinueTimeAfterLeftSettlementWhileEnlisted);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this,WeeklyRenownGain);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this,SkillGain);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this,LeaveKingdomEvent);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
        }

        private void OnRaidCompleted(BattleSideEnum side, RaidEventComponent component)
        {
            if (component.IsPlayerMapEvent)
            {
                GameMenu.ActivateGameMenu("hireling_menu");
            }
        }

        private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase attackingParty)
        {
            if (_hirelingEnlisted)
            {
                if (destroyedParty.LeaderHero == _hirelingEnlistingLord || destroyedParty == MobileParty.MainParty)
                {
                    LeaveLordPartyAction();
                }
            }
        }

        private void LeaveKingdomEvent(Clan clan, Kingdom kingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail arg4, bool arg5)
        {
            if (clan == Clan.PlayerClan && IsEnlisted())
            {
                LeaveLordPartyAction();
            }
        }

        private void SkillGain()
        {
            if (_hirelingEnlisted)
            {
                if (_currentTrainedSkill == null)
                {
                    _currentTrainedSkill = _activities.GetHirelingActivities(Hero.MainHero.GetCareer())[0];
                    _currentActivityIndex = 0;
                }
                
                if (_currentTrainedSkill != null && Hero.MainHero.IsHealthFull())
                {
                    Hero.MainHero.AddSkillXp(_currentTrainedSkill,25);
                }
            }
        }

        private void WeeklyRenownGain()
        {
            if (Hero.MainHero.IsEnlisted())
            {
                var gain = 5;
                var clanTier = Hero.MainHero.Clan.Tier;
                gain += clanTier;
                
                Hero.MainHero.Clan.AddRenown(gain);
            }

        }
        
        private void ContinueTimeAfterLeftSettlementWhileEnlisted(GameMenuOption obj)
        {
            if (_hirelingEnlisted && obj.IdString =="town_leave")
            {
                GameMenu.ActivateGameMenu("hireling_menu");
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
            }
        }

        private float GetEnlistingLordEventStrengthRatio(MapEvent mapEvent)
        {
            var attackerMapEventSide = mapEvent.GetMapEventSide(BattleSideEnum.Attacker);
            BattleSideEnum side;
            if(attackerMapEventSide.Parties.Any(x => x.Party == _hirelingEnlistingLord.PartyBelongedTo.Party))
            {
                side = BattleSideEnum.Attacker;
            }
            else
            {
                side = BattleSideEnum.Defender;
            }
            mapEvent.GetStrengthsRelativeToParty(side, out float enlistingLordStrength, out float enemyStrength);

            if (enemyStrength > 0)
            {
                return enlistingLordStrength / enemyStrength;
            }

            return 1;
        }
        
        private void MenuOpened(MenuCallbackArgs obj)
        {
            if (_startBattle && obj.MenuContext.GameMenu.StringId == "encounter" && !_debugSkipBattles)
            {
                _startBattle = false;
                
                MenuHelper.EncounterAttackConsequence(obj);
            }
            if (_debugSkipBattles && _hirelingEnlistingLordIsAttacking)
            {
                _startBattle = false;
            }
        }
        
        private void LeaveEnlistingParty(string menuToReturn, bool desertion =false)
        {
            if (!desertion)
            {
                desertion = _durationInDays < MinimumServeDays;
            }

            if (desertion)
            {
                var damage = new TextObject("This will harm your relations with the entire faction.");
                GameTexts.SetVariable("HIRELING_DESERT_TEXT",damage);
            }
            else
            {
                GameTexts.SetVariable("HIRELING_DESERT_TEXT","");
            }

            var titleText = new TextObject("{=FLT0000044}Abandon Party");
            var text = new TextObject("{=FLT0000046}Are you sure you want to abandon the party? {HIRELING_DESERT_TEXT}");
            var affirmativeText = new TextObject("{=FLT0000047}Yes");
            var negativeText = new TextObject("{=FLT0000048}No");
            InformationManager.ShowInquiry(new InquiryData(titleText.ToString(), text.ToString(), true, true, affirmativeText.ToString(), negativeText.ToString(), delegate ()
            {
                if (desertion)
                {
                    ChangeCrimeRatingAction.Apply(_hirelingEnlistingLord.MapFaction, 55f);
                    foreach (Clan clan in _hirelingEnlistingLord.Clan.Kingdom.Clans)
                    {
                        bool flag2 = !clan.IsUnderMercenaryService;
                        if (flag2)
                        {
                            ChangeRelationAction.ApplyPlayerRelation(clan.Leader, -10);
                        }   
                    }
                }
                GameMenu.ExitToLast();
                LeaveLordPartyAction();
            }, delegate
            {
                GameMenu.ActivateGameMenu(menuToReturn);
            }));
        }
        
        private void InitializeDialogs(CampaignGameStarter campaignGameStarter)
        {
            var quitText = new TextObject("{HIRELING_QUIT_TEXT}");
            var explainText = new TextObject("{HIRELING_EXPLAIN_TEXT}");
            var positiveDecisionText = new TextObject("{HIRELING_DECISION_TEXT}");
            
            campaignGameStarter.AddPlayerLine("convincelord", "lord_talk_speak_diplomacy_2", "payedsword_quit_sure", "I would like to quit my service.", QuitCondition, null);
            campaignGameStarter.AddDialogLine("payedsword_quit_sure", "payedsword_quit_sure", "payedsword_quit_choice", "Are you sure?", null,null );
            campaignGameStarter.AddPlayerLine("payedsword_quit_choice", "payedsword_quit_choice", "payedsword_quit", "Yes i want to leave", null,null );
            campaignGameStarter.AddPlayerLine("payedsword_quit_choice", "payedsword_quit_choice", "lord_pretalk", "I have to think about this.", null, null);
            campaignGameStarter.AddDialogLine("payedsword_quit", "payedsword_quit", "end", quitText.Value, null, LeaveLordPartyAction);
            
            campaignGameStarter.AddPlayerLine("convincelord", "lord_talk_speak_diplomacy_2", "payedsword_explain", "I am hereby offering my sword.", () => SanityCheck() && !IsEnlisted() && ServeAsAHirelingHelpers.HirelingServiceConditions(), null);
            campaignGameStarter.AddDialogLine("payedsword_explain", "payedsword_explain", "hireling_decide_player", explainText.Value, null, null, 200);
            campaignGameStarter.AddPlayerLine("hireling_decide_player", "hireling_decide_player", "hireling_prompt", "I accept my Lord.", ServeAsAHirelingHelpers.HirelingServiceConditions, () => DisplayPrompt(EnlistPlayer));
            campaignGameStarter.AddPlayerLine("hireling_decide_player", "hireling_decide_player", "lord_pretalk", "I need to think about this", null, null);
            campaignGameStarter.AddDialogLine("hireling_prompt", "hireling_prompt", "hireling_decision", "...", null, null);
            campaignGameStarter.AddPlayerLine("hireling_decision", "hireling_decision", "lord_pretalk", "I need to think about this", () => _enlistInquiryDeclined, null);
            campaignGameStarter.AddDialogLine("hireling_decision", "hireling_decision", "end", positiveDecisionText.Value, null, null);
        }

        private bool SanityCheck()
        {
            return Clan.PlayerClan.MapFaction == Clan.PlayerClan &&
                MobileParty.MainParty.Army == null;
        }

        private bool QuitCondition()
        {
            if (Campaign.Current.ConversationManager.OneToOneConversationHero != _hirelingEnlistingLord)
                return false;
            var culture = Campaign.Current.ConversationManager.OneToOneConversationCharacter.Culture.StringId;
            if (GameTexts.TryGetText("HirelingLordQuit", out var text, culture))
            {
                GameTexts.SetVariable("HIRELING_QUIT_TEXT",text.Value);
            }
            else
            {
                if (GameTexts.TryGetText("HirelingLordQuit", out var defaultText))
                {
                    GameTexts.SetVariable("HIRELING_QUIT_TEXT",defaultText.Value);
                }
            }
            return IsEnlisted() && _durationInDays > MinimumServeDays;
        }
        
        private void DisplayPrompt(Action enlistPlayer)
        {
            var title = GameTexts.FindText("Hireling", "PromptTitle");
            var explaination = GameTexts.FindText("Hireling", "PromptText");
            _enlistInquiryDeclined = false;
            var inquiry = new InquiryData(title.ToString(),
                explaination.ToString(),
                true, 
                true, 
                "Accept", "Decline",
                enlistPlayer,
                () => _enlistInquiryDeclined=true);
            InformationManager.ShowInquiry(inquiry);
        }

        private void SetupHirelingMenu(CampaignGameStarter campaignGameStarter)
        {
            var infotext = new TextObject("{ENLISTING_TEXT}");
            
            campaignGameStarter.AddGameMenuOption("town","town_back_to_hireling", "Back", args => 
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return IsEnlisted();
            }, args => GameMenu.SwitchToMenu("hireling_menu"), true);

            campaignGameStarter.AddWaitGameMenu("hireling_menu", infotext.Value, party_wait_talk_to_other_members_on_init, wait_on_condition,
                null, wait_on_tick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
            
            var textObjectHirelingEnterSettlement = new TextObject("Enter the settlement");
            campaignGameStarter.AddGameMenuOption("hireling_menu", "enter_town", textObjectHirelingEnterSettlement.ToString(), args =>
            {
                if (!IsEnlisted())
                {
                    return false;
                }
                args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                
                return _hirelingEnlistingLord.PartyBelongedTo.CurrentSettlement != null && 
                _hirelingEnlistingLord.PartyBelongedTo.CurrentSettlement == PlayerEncounter.EncounterSettlement &&
                PlayerEncounter.EncounterSettlement.IsTown;
            }, args =>
            {
                GameMenu.SwitchToMenu("town");
            }, true);
            
            var text = new TextObject("{PAUSE_ONOFF_TEXT}");
            campaignGameStarter.AddGameMenuOption("hireling_menu","pause_time_option",text.Value, null, PauseModeToggle);
            var pauseText = GameTexts.FindText("Hireling","PauseTime");
            pauseText.SetTextVariable("PAUSE_ONOFF", "off");
           GameTexts.SetVariable("PAUSE_ONOFF_TEXT", pauseText);
           
           var lordTalkText = GameTexts.FindText("Hireling","TalkToLord");
           campaignGameStarter.AddGameMenuOption("hireling_menu","activity0_option",lordTalkText.Value, null, args => StartDialog());
           
           campaignGameStarter.AddGameMenuOption("hireling_menu","empty","", args => 
           { 
               args.IsEnabled = false; 
               return true;
           }, null);
           
           var activity0 = new TextObject("{HIRELINGACTIVITYTEXT0}");
           var activity1 = new TextObject("{HIRELINGACTIVITYTEXT1}");
           var activity2 = new TextObject("{HIRELINGACTIVITYTEXT2}");
           var activity3 = new TextObject("{HIRELINGACTIVITYTEXT3}");
           var activity4 = new TextObject("{HIRELINGACTIVITYTEXT4}");
           campaignGameStarter.AddGameMenuOption("hireling_menu","activity0_option",activity0.Value, args => HoverActiviy(0,args), args => ToggleActivity(0, args));
           campaignGameStarter.AddGameMenuOption("hireling_menu","activity1_option",activity1.Value, args => HoverActiviy(1,args), args => ToggleActivity(1, args));
           campaignGameStarter.AddGameMenuOption("hireling_menu","activity2_option",activity2.Value, args => HoverActiviy(2,args), args => ToggleActivity(2, args));
           campaignGameStarter.AddGameMenuOption("hireling_menu","activity3_option",activity3.Value, args => HoverActiviy(3,args), args => ToggleActivity(3, args ));
           campaignGameStarter.AddGameMenuOption("hireling_menu","activity4_option",activity4.Value, args => HoverActiviy(4,args), args => ToggleActivity(4, args));
           
           campaignGameStarter.AddGameMenuOption("hireling_menu","empty","", args => { args.IsEnabled = false; return true;
           },null);
           
           campaignGameStarter.AddGameMenuOption("hireling_menu", "party_wait_leave", "Desert", args =>
           {
               var infoText = new TextObject("{=FLT0000045}This will damage your reputation with the {FACTION}. Serve for {MINIMUMSERVEDAYS} days, and speak to your enlisting Lord to avoid consequences");
               string factionName = (_hirelingEnlistingLord != null) ? _hirelingEnlistingLord.MapFaction.Name.ToString() : "DATA CORRUPTION ERROR";
               infoText.SetTextVariable("FACTION", factionName);
               infoText.SetTextVariable("MINIMUMSERVEDAYS", MinimumServeDays);
               args.Tooltip = infoText;
               args.optionLeaveType = GameMenuOption.LeaveType.Escape;
               return true;
           }, args =>
           {
               LeaveEnlistingParty("hireling_menu");
           }, true);
        }
        
        private void StartDialog()
        {
            ConversationCharacterData characterData = new(_hirelingEnlistingLord.CharacterObject, _hirelingEnlistingLord.PartyBelongedTo.Party);
            ConversationCharacterData playerData = new(Hero.MainHero.CharacterObject, Hero.MainHero.PartyBelongedTo.Party);
            Campaign.Current.CurrentConversationContext = ConversationContext.Default;
            Campaign.Current.ConversationManager.OpenMapConversation(playerData,characterData);
        }
        
        private void SetActivities()
        {
            var career = Hero.MainHero.GetCareer();
            for (var i = 0; i < 5; i++)
            {
                if (GameTexts.TryGetText("HirelingActivity" + i, out var text, career.StringId))
                {
                    if (_currentActivityIndex==i)
                    {
                        text = new TextObject($"[{text.Value}]");
                    }
                    GameTexts.SetVariable("HIRELINGACTIVITYTEXT"+i,text);
                } 
            }
        }
        
        private bool HoverActiviy(int i, MenuCallbackArgs args)
        {
            var career = Hero.MainHero.GetCareer();
            var activities = _activities.GetHirelingActivities(career);

            args.Tooltip = activities[i].Name;

            return true;
        }
        
        
        private void ToggleActivity(int i, MenuCallbackArgs args)
        {
        
            var career = Hero.MainHero.GetCareer();
            _currentActivityIndex = i;
            SetActivities();

            var activities = _activities.GetHirelingActivities(career);

            args.Tooltip = activities[i].Name;
           
            _currentTrainedSkill = activities[i];
            args.MenuContext.Refresh();
        }

        private void Initialize(CampaignGameStarter campaignGameStarter)
        {
            _activities = new ServeAsAHirelingActivities();
            
            InitializeDialogs(campaignGameStarter);
            SetupHirelingMenu(campaignGameStarter);
            SetupBattleMenu(campaignGameStarter);
        }

        private void SetupBattleMenu(CampaignGameStarter campaignGameStarter)
        {
            TextObject hirelingBattleTextMenu = new("Your Lord engages in a battle.");
            campaignGameStarter.AddGameMenu("hireling_battle_menu", hirelingBattleTextMenu.Value, party_wait_talk_to_other_members_on_init, GameOverlays.MenuOverlayType.Encounter);

            campaignGameStarter.AddGameMenuOption("hireling_battle_menu", "hireling_join_battle", "Join battle",
                hireling_battle_menu_join_battle_on_condition,
                delegate
                {
                    while (Campaign.Current.CurrentMenuContext != null)
                    {
                        GameMenu.ExitToLast();
                    }
                    if (_hirelingEnlistingLord.PartyBelongedTo.MapEvent != null)
                    {
                        var mapEvent = _hirelingEnlistingLord.PartyBelongedTo.MapEvent;
                        if (_hirelingEnlistingLordIsAttacking)
                        {
                            StartBattleAction.Apply(PartyBase.MainParty, mapEvent.DefenderSide.LeaderParty);
                            
                            MobileParty.MainParty.CurrentSettlement = _hirelingEnlistingLord.PartyBelongedTo.MapEvent.MapEventSettlement;

                            if (mapEvent.IsSiegeAssault)
                            {
                                Game.Current.AfterTick += InitializeSiegeBattle;
                                _siegeBattleMissionStarted = true;
                            }
                        }
                        else
                        {
                            var eventparty = _hirelingEnlistingLord.PartyBelongedTo;
                            if (_hirelingEnlistingLord.PartyBelongedTo.Army != null && _hirelingEnlistingLord.PartyBelongedTo.Army.LeaderParty != eventparty)
                            {
                                eventparty = _hirelingEnlistingLord.PartyBelongedTo.Army.LeaderParty;
                            }
                            
                            StartBattleAction.Apply( PartyBase.MainParty,eventparty.MapEvent.AttackerSide.LeaderParty); //changing the direction fixed the sole defender bug for the player.
                                                                                                                        //It seems the defense has in joining no meaning
                        }
                        _startBattle = true;
                    }
                }
                , false, 4);

            campaignGameStarter.AddGameMenuOption("hireling_battle_menu", "hireling_avoid_combat", "Avoid Combat",
               hireling_battle_menu_avoid_combat_on_condition,
               delegate (MenuCallbackArgs args)
               {
                   _hirelingLordIsFightingWithoutPlayer = true;
                   _startBattle = false;
                   args.MenuContext.GameMenu.StartWait();
               }
               , false, 4);

            campaignGameStarter.AddGameMenuOption("hireling_battle_menu", "hireling_flee", "Flee",
               hireling_battle_menu_desert_on_condition,
               delegate
               {
                   LeaveEnlistingParty("hireling_battle_menu",true);
               }
               , false, 4);
        }

        private void PauseModeToggle(MenuCallbackArgs args)
        {
            _pauseModeToggle = !_pauseModeToggle;

            var onOffText = "Off";
            if (_pauseModeToggle)
            {
                onOffText = "On";
            }
            
            TextObject text2 = GameTexts.FindText("Hireling","PauseTime");
            text2.SetTextVariable("PAUSE_ONOFF", onOffText);
            
            GameTexts.SetVariable("PAUSE_ONOFF_TEXT", text2);
            args.Text = text2;
            args.MenuContext.Refresh();
        }

        public void LeaveLordPartyAction()
        {
            _hirelingEnlisted = false;
            _hirelingEnlistingLord = null;
            _hirelingWaitMenuShown = false;
            //I am not sure why this was needed? Putting it in makes it crash if you leave service while in a town for example.
            //This makes PlayerEncounter.EncounterSettlement null which is accessed via vanilla gamemenu init methods
            //crash does not occur, and finishing encounter is important to not end in a invalid state, where parties try to engange with player but can't
            PlayerEncounter.Finish();
            if (Settlement.CurrentSettlement != null)
            {
                if (PlayerEncounter.EncounterSettlement != null) PlayerEncounter.LeaveSettlement();
                if (MobileParty.MainParty.CurrentSettlement != null) LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
            }
            UndoDiplomacy();
            ShowPlayerParty();
            
            _durationInDays = 0;
            _manuallyFoughtBattles = 0;
        }
        
        private void InitializeSiegeBattle(float tick)
        {
            if (!_hirelingEnlisted) return;
            if(!_siegeBattleMissionStarted) return;
            if(MobileParty.MainParty==null) return;
            var mainPartyMapEvent = MobileParty.MainParty.MapEvent;
            if (mainPartyMapEvent == null || mainPartyMapEvent.StringId == null) return; //wait until the main party event is assigned correctly
            
            StartBattleAction.Apply(PartyBase.MainParty, mainPartyMapEvent.DefenderSide.LeaderParty);
            _siegeBattleMissionStarted = false;
            Game.Current.AfterTick -= InitializeSiegeBattle;    //cleanup,  method is afterwards rendered harmless and will not affect performance 
        }
        
        private bool hireling_battle_menu_join_battle_on_condition(MenuCallbackArgs args)
        {
            var maxHitPointsHero = Hero.MainHero.MaxHitPoints;
            var hitPointsHero = Hero.MainHero.HitPoints;
            return hitPointsHero > maxHitPointsHero * 0.2;
        }
        
        private bool hireling_battle_menu_desert_on_condition(MenuCallbackArgs args)
        {
            return _hirelingEnlistingLord.CurrentSettlement == null;
        }

        private bool hireling_battle_menu_avoid_combat_on_condition(MenuCallbackArgs args)
        {
            var maxHitPointsHero = Hero.MainHero.MaxHitPoints;
            var hitPointsHero = Hero.MainHero.HitPoints;
            
            var lordEvent = _hirelingEnlistingLord.PartyBelongedTo.MapEvent;

            if(lordEvent==null) return false;
            
            var partyStrength = GetEnlistingLordEventStrengthRatio(lordEvent);
           
            var combatstregthThreshold  = partyStrength > RatioPartyAgainstEnemyStrength;// || enemy is 30% of our balance;
            
            return hitPointsHero < maxHitPointsHero * 0.2 || combatstregthThreshold; 
        }
        
        private bool wait_on_condition(MenuCallbackArgs args)
        {
            return true;
        }
        
        private void wait_on_tick(MenuCallbackArgs args, CampaignTime time)
        {
            bool flag = _hirelingEnlistingLord == null || _hirelingEnlistingLord.PartyBelongedTo == null || !_hirelingEnlisted;
            if (flag)
            {
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }
            }
            else
            {
                if(args.MenuContext?.GameMenu == null) return;
                TextObject text1 = args.MenuContext.GameMenu.GetText();
                TextObject text2 = GameTexts.FindText("Hireling","MainText");
                text2.SetTextVariable("ENLISTING_LORD", _hirelingEnlistingLord.Name);
                
                var days = $"{_durationInDays:0.0}";
                text2.SetTextVariable("ENLISTING_DURATION", days);
                text2.SetTextVariable("HIRELING_BATTLE_COUNT", _manuallyFoughtBattles);
                
                var armyInfo = "";
                if(_hirelingEnlistingLord.PartyBelongedTo.Army!=null)
                {
                    armyInfo += "{newLine}";
                    armyInfo += $"is Part of {_hirelingEnlistingLord.PartyBelongedTo.Army.Name}";
                }
                text2.SetTextVariable("ENLISTING_ARMY", armyInfo);
                
                TextObject variable = text2;
                text1.SetTextVariable("ENLISTING_TEXT", variable);
                
                args.MenuContext.SetBackgroundMeshName(_hirelingEnlistingLord.MapFaction.Culture.EncounterBackgroundMesh);
            }
        }
        
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_enlisted", ref _hirelingEnlisted);
            dataStore.SyncData("_enlistingLord", ref _hirelingEnlistingLord);
            dataStore.SyncData("_entryServiceTimeStamp", ref _entryServiceTimeStamp);
            dataStore.SyncData("_manuallyFoughtBattles", ref _manuallyFoughtBattles);
            dataStore.SyncData("_durationInDays", ref _durationInDays);
        }

        private void party_wait_talk_to_other_members_on_init(MenuCallbackArgs args) { }
        
        private void ControlPlayerLoot(MapEvent mapEvent)
        {
            if (mapEvent.PlayerSide == mapEvent.WinningSide && IsEnlisted())
            {

                if (!_hirelingLordIsFightingWithoutPlayer)
                {
                    _manuallyFoughtBattles++;
                }
                
                PlayerEncounter.Current.RosterToReceiveLootItems.Clear();
                PlayerEncounter.Current.RosterToReceiveLootMembers.Clear();
                PlayerEncounter.Current.RosterToReceiveLootPrisoners.Clear();
            }

            _hirelingWaitMenuShown = false;
        }
        
        private void OnPartyLeavesSettlement(MobileParty mobileParty, Settlement settlement)
        {
            if (!IsEnlisted() || _hirelingEnlistingLord == null) return;
           
            if (_hirelingEnlistingLord.PartyBelongedTo == mobileParty || (MobileParty.MainParty == mobileParty && mobileParty.CurrentSettlement == null))
            {
                while (Campaign.Current.CurrentMenuContext != null)
                    GameMenu.ExitToLast();
                if (PartyBase.MainParty.MobileParty.CurrentSettlement != null)
                    LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
                PlayerEncounter.Finish();
                if (PlayerEncounter.EncounterSettlement != null) PlayerEncounter.LeaveSettlement();
                GameMenu.ActivateGameMenu("hireling_menu");
            }
        }

        private void EnlistingLordPartyEntersSettlement(MobileParty mobileParty, Settlement settlement, Hero arg3)
        {
            if (!_hirelingEnlisted || !settlement.IsTown) return;
            if (MobileParty.MainParty.CurrentSettlement == settlement && PlayerEncounter.EncounterSettlement == settlement) return;
            if ( _hirelingEnlistingLord != null && _hirelingEnlistingLord.PartyBelongedTo == mobileParty)
            {
                EnterSettlementAction.ApplyForParty(MobileParty.MainParty, _hirelingEnlistingLord.CurrentSettlement);
                EncounterManager.StartSettlementEncounter(MobileParty.MainParty, settlement);
                GameMenu.SwitchToMenu("hireling_menu");

                if (_pauseModeToggle)
                {
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                }
            }
        }
        
        private void MapEventEnded(MapEvent mapEvent)
        {
            if(_hirelingEnlistingLord == null|| !IsEnlisted()) return; 
            
            if (_hirelingEnlistingLord != null && !mapEvent.IsPlayerMapEvent && GetEnlistingLordisInMapEvent(mapEvent))
            {
                GameMenu.SwitchToMenu("hireling_menu");
                _hirelingLordIsFightingWithoutPlayer = false;
            }
            if (mapEvent.IsPlayerMapEvent)
            {
                GameMenu.SwitchToMenu("hireling_menu");
            }
        }
        
        private void OnTick(float dt)
        {
            if (_hirelingEnlisted && _hirelingEnlistingLord != null && _hirelingEnlistingLord.PartyBelongedTo != null)
            {

                if (_hirelingLordIsFightingWithoutPlayer || _hirelingEnlistingLord.PartyBelongedTo?.BesiegerCamp != null || _hirelingEnlistingLord.PartyBelongedTo.CurrentSettlement != null || (_hirelingEnlistingLord.PartyBelongedTo.MapEvent!=null && _hirelingEnlistingLord.PartyBelongedTo.MapEvent.IsRaid))
                {
                    if (!MobileParty.MainParty.ShouldBeIgnored)
                    {
                        MobileParty.MainParty.IgnoreForHours(1);
                    }
                }
                
                var menu = Campaign.Current.GameMenuManager.GetGameMenu("hireling_menu");
                _durationInDays = Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow - _entryServiceTimeStamp;
                menu.RunOnTick(Campaign.Current.CurrentMenuContext,dt);
                
                if (!_hirelingWaitMenuShown)
                {
                    
                    GameMenu.ActivateGameMenu("hireling_menu");
                    _hirelingWaitMenuShown = true;
                    SetActivities();
                    Campaign.Current.CurrentMenuContext.Refresh();
                }
                
                HidePlayerParty();
                PartyBase.MainParty.MobileParty.Position2D = _hirelingEnlistingLord.PartyBelongedTo.Position2D;
                if (_hirelingEnlistingLord.PartyBelongedTo.MapEvent != null && MobileParty.MainParty.MapEvent == null)
                {
                    var mapEvent = _hirelingEnlistingLord.PartyBelongedTo.MapEvent;
                    _hirelingEnlistingLordIsAttacking = false;
                    
                    foreach (var party in mapEvent.AttackerSide.Parties)
                    {
                        if (party.Party == _hirelingEnlistingLord.PartyBelongedTo.Party)
                        {

                            _hirelingEnlistingLordIsAttacking = true;
                            break;
                        }
                    }
                    
                    if (!_hirelingLordIsFightingWithoutPlayer && mapEvent.DefenderSide.TroopCount > 0)
                    {
                        GameMenu.ActivateGameMenu("hireling_battle_menu");
                    }
                }
                
            } 
            else if(_hirelingEnlisted && _hirelingEnlistingLord?.PartyBelongedTo == null)
            {
                LeaveLordPartyAction();
            }
        }
        
        private void UndoDiplomacy()
        {
            ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(Hero.MainHero.Clan, false);
        }
        
        private void EnlistPlayer()
        {
            _hirelingEnlistingLord = CharacterObject.OneToOneConversationCharacter.HeroObject;
            HidePlayerParty();
            DisbandParty();
            Hero.MainHero.AddAttribute("enlisted");
            
            ChangeKingdomAction.ApplyByJoinFactionAsMercenary(Hero.MainHero.Clan, _hirelingEnlistingLord.Clan.Kingdom, 25, false);
            MBTextManager.SetTextVariable("ENLISTINGLORDNAME", _hirelingEnlistingLord.EncyclopediaLinkWithName);
            
            while (Campaign.Current.CurrentMenuContext != null)
                GameMenu.ExitToLast();
            _hirelingEnlisted = true;

            SetActivities();

             _entryServiceTimeStamp = Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow;
            GameMenu.ActivateGameMenu("hireling_menu");
        }

        private void ShowPlayerParty()
        {
            // Currently not working ??
            PartyBase.MainParty.MobileParty.IsVisible = true;
            PartyBase.MainParty.UpdateVisibilityAndInspected(0);
        }

        private void HidePlayerParty()
        {
            PartyBase.MainParty.MobileParty.IsVisible = false;
            PartyBase.MainParty.UpdateVisibilityAndInspected(0);
        }
        
        private void DisbandParty()
        {
            if (MobileParty.MainParty.MemberRoster.TotalManCount <= 1)
                return;
            List<TroopRosterElement> troopRosterElementList = [];
            foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.MemberRoster.GetTroopRoster())
            {
                if (troopRosterElement.Character != Hero.MainHero.CharacterObject && troopRosterElement.Character.HeroObject == null)
                    troopRosterElementList.Add(troopRosterElement);
            }
            if (troopRosterElementList.Count == 0)
                return;
            foreach (TroopRosterElement troopRosterElement in troopRosterElementList)
            {
                MobileParty.MainParty.MemberRoster.AddToCounts(troopRosterElement.Character, -1 * troopRosterElement.Number);
                EnlistingLord.PartyBelongedTo.MemberRoster.AddToCounts(troopRosterElement.Character, 1 * troopRosterElement.Number);
                
            }
        }
        
        private bool GetEnlistingLordisInMapEvent(MapEvent mapEvent)
        {
            var lordIsInMapEvent = false;
            if (_hirelingEnlistingLord == null)
                return false;

            if (_hirelingEnlistingLord.PartyBelongedTo == null)
            {
                return false;
            }

            foreach (var party in mapEvent.AttackerSide.Parties)
            {
                if (party.Party == _hirelingEnlistingLord.PartyBelongedTo.Party)
                {
                    lordIsInMapEvent = true;
                }
            }
            foreach (var party in mapEvent.DefenderSide.Parties)
            {
                if (party.Party == _hirelingEnlistingLord.PartyBelongedTo.Party)
                {
                    lordIsInMapEvent = true;
                }
            }

            return lordIsInMapEvent;
        }
    }
}