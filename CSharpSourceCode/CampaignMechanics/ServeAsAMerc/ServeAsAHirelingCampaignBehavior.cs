using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.ServeAsAMerc
{
    public class ServeAsAHirelingCampaignBehavior : CampaignBehaviorBase
    {

        private bool _hirelingEnlisted;
        private Hero _hirelingEnlistingLord;
        private bool _hirelingEnlistingLordIsAttacking;
        private bool _hirelingLordIsFightingWithoutPlayer;
        private float _ratioPartyAgainstEnemyStrength = 0;
        private int _percentageOfBalanceRequiredToAvoidFight = 30;
        private bool debugSkipBattles = false;

        private bool _startBattle;
        private bool _siegeBattleMissionStarted;

        public bool IsEnlisted()
        {
            return _hirelingEnlisted;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, ServeAsAMercDialog);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, EnlistingLordPartyEntersSettlement);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnPartyLeavesSettlement);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, controlPlayerLoot);                //Those events are never executed when the player lose a battle!
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, mapEventEnded);
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, BattleMenuOpened);
            CampaignEvents.GameMenuOptionSelectedEvent.AddNonSerializedListener(this, ContinueTimeAfterLeftSettlementWhileEnlisted);
        }

       

        private void ContinueTimeAfterLeftSettlementWhileEnlisted(GameMenuOption obj)
        {
            if (_hirelingEnlisted && obj.IdString =="town_leave")
            {
                GameMenu.ActivateGameMenu("hireling_menu");
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
            }
        }
        

        private void BattleMenuOpened(MenuCallbackArgs obj)
        {
            if (_startBattle && obj.MenuContext.GameMenu.StringId == "encounter" && !debugSkipBattles)
            {
                _startBattle = false;
                
                MenuHelper.EncounterAttackConsequence(obj);
            }
            if (debugSkipBattles && _hirelingEnlistingLordIsAttacking)
            {
                _startBattle = false;
            }

        }


        private void DesertEnlistingParty(string menuToReturn)
        {
            TextObject titleText = new TextObject("{=FLT0000044}Abandon Party", null);
            TextObject text = new TextObject("{=FLT0000046}Are you sure you want to abandon the party>  This will harm your relations with the entire faction.", null);
            TextObject affrimativeText = new TextObject("{=FLT0000047}Yes", null);
            TextObject negativeText = new TextObject("{=FLT0000048}No", null);
            InformationManager.ShowInquiry(new InquiryData(titleText.ToString(), text.ToString(), true, true, affrimativeText.ToString(), negativeText.ToString(), delegate ()
            {
                ChangeCrimeRatingAction.Apply(_hirelingEnlistingLord.MapFaction, 55f, true);
                foreach (Clan clan in _hirelingEnlistingLord.Clan.Kingdom.Clans)
                {
                    bool flag2 = !clan.IsUnderMercenaryService;
                    if (flag2)
                    {
                        ChangeRelationAction.ApplyPlayerRelation(clan.Leader, -20, true, true);
                        foreach (Hero lord in clan.Heroes)
                        {
                            bool isLord = lord.IsLord;
                            if (isLord)
                            {
                                //  Test.ChangeLordRelation(lord, -100000);
                            }
                        }
                    }
                }
                LeaveLordPartyAction();
                GameMenu.ExitToLast();
            }, delegate ()
            {
                GameMenu.ActivateGameMenu(menuToReturn);
            }, "", 0f, null, null, null), false, false);
        }
        
        // INIT PHASE
        private void ServeAsAMercDialog(CampaignGameStarter campaignGameStarter)
        {
            TextObject infotext = new TextObject("Following {ENLISTINGLORDNAME}", null);
            campaignGameStarter.AddPlayerLine("convincelord", "lord_talk_speak_diplomacy_2", "payedsword", "I am hereby offering my sword.", null, EnlistPlayer);
            campaignGameStarter.AddDialogLine("payedsword", "payedsword", "end", "As you wish.", null, null, 200, null);
            campaignGameStarter.AddWaitGameMenu("hireling_menu", infotext.Value, new OnInitDelegate(this.party_wait_talk_to_other_members_on_init), new OnConditionDelegate(this.wait_on_condition),
                    null, new OnTickDelegate(this.wait_on_tick), GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 0f, GameMenu.MenuFlags.None, null);
            TextObject textObjectHirelingMenuDesert = new TextObject("Desert", null);
            TextObject textObjectHirelingEnterSettlement = new TextObject("Enter the settlement", null);


            campaignGameStarter.AddGameMenuOption("hireling_menu", "party_wait_leave", textObjectHirelingMenuDesert.ToString(), delegate (MenuCallbackArgs args)
            {
                TextObject text = new TextObject("{=FLT0000045}This will damage your reputation with the {FACTION}", null);
                string faction_name = (_hirelingEnlistingLord != null) ? _hirelingEnlistingLord.MapFaction.Name.ToString() : "DATA CORRUPTION ERROR";
                text.SetTextVariable("FACTION", faction_name);
                args.Tooltip = text;
                args.optionLeaveType = GameMenuOption.LeaveType.Escape;
                return true;
            }, delegate (MenuCallbackArgs args)
            {
                DesertEnlistingParty("hireling_menu");
            }, true, -1, false, null);

            campaignGameStarter.AddGameMenuOption("hireling_menu", "enter_town", textObjectHirelingEnterSettlement.ToString(), delegate (MenuCallbackArgs args)
            {

                args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                TORCommon.Say((_hirelingEnlistingLord.PartyBelongedTo.CurrentSettlement != null).ToString());

                return _hirelingEnlistingLord.PartyBelongedTo.CurrentSettlement != null;
            }, delegate (MenuCallbackArgs args)
            {

                while (Campaign.Current.CurrentMenuContext != null)
                    GameMenu.ExitToLast();
                EncounterManager.StartSettlementEncounter(MobileParty.MainParty.Party.MobileParty, _hirelingEnlistingLord.PartyBelongedTo.CurrentSettlement);
                EnterSettlementAction.ApplyForParty(MobileParty.MainParty.Party.MobileParty, _hirelingEnlistingLord.CurrentSettlement);
            }, true, -1, false, null);


            TextObject hirelingBattleTextMenu = new TextObject("This is a test of Hireling BattleMenu", null);
            campaignGameStarter.AddGameMenu("hireling_battle_menu", hirelingBattleTextMenu.Value, new OnInitDelegate(this.party_wait_talk_to_other_members_on_init), GameOverlays.MenuOverlayType.Encounter, GameMenu.MenuFlags.None, null);
            campaignGameStarter.AddGameMenuOption("hireling_battle_menu", "hireling_join_battle", "Join battle",
                hireling_battle_menu_join_battle_on_condition,
                delegate (MenuCallbackArgs args)
                {

                    while (Campaign.Current.CurrentMenuContext != null)
                    {
                        GameMenu.ExitToLast();
                    }
                    if (_hirelingEnlistingLord.PartyBelongedTo.MapEvent != null)
                    {
                        if (_hirelingEnlistingLordIsAttacking)
                        {
                            var mapEvent = _hirelingEnlistingLord.PartyBelongedTo.MapEvent;
                            TORCommon.Say("attack!"); 
                            StartBattleAction.Apply(PartyBase.MainParty, mapEvent.DefenderSide.LeaderParty);
                            
                            MobileParty.MainParty.CurrentSettlement= _hirelingEnlistingLord.PartyBelongedTo.MapEvent.MapEventSettlement;

                            if (mapEvent.IsSiegeAssault)
                            {
                                Game.Current.AfterTick += InitializeSiegeBattle;    //deliberate waiting until all information is copied over, atleast that's what I assume is happening?
                                _siegeBattleMissionStarted = true;
                            }
                            
                        }
                        else
                        {
                            TORCommon.Say("defend!");
                            StartBattleAction.Apply(_hirelingEnlistingLord.PartyBelongedTo.MapEvent.AttackerSide.LeaderParty, PartyBase.MainParty);
                        }
                        _startBattle = true;
                    }
                }
                , false, 4, false);

            campaignGameStarter.AddGameMenuOption("hireling_battle_menu", "hireling_avoid_combat", "Avoid Combat",
               hireling_battle_menu_avoid_combat_on_condition,
               delegate (MenuCallbackArgs args)
               {
                   _hirelingLordIsFightingWithoutPlayer = true;
                   _startBattle = false;
                   args.MenuContext.GameMenu.StartWait();


               }
               , false, 4, false);

            campaignGameStarter.AddGameMenuOption("hireling_battle_menu", "hireling_flee", "Desert",
               hireling_battle_menu_desert_on_condition,
               delegate (MenuCallbackArgs args)
               {
                   DesertEnlistingParty("hireling_battle_menu");

               }
               , false, 4, false);
        }

        public void LeaveLordPartyAction(bool keepGear = true)
        {
            _hirelingEnlisted = false;
            _hirelingEnlistingLord = null;
            PlayerEncounter.Finish(true);
            UndoDiplomacy();
            ShowPlayerParty();

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
        
        private bool hireling_battle_menu_avoid_battle_on_condition(MenuCallbackArgs args)
        {
            var maxHitPointsHero = Hero.MainHero.MaxHitPoints;
            var hitPointsHero = Hero.MainHero.HitPoints;

            return hitPointsHero < maxHitPointsHero * 0.2 || _ratioPartyAgainstEnemyStrength < 0.30; // || ennemy is 30% of our balance;
        }
        
        private bool hireling_battle_menu_desert_on_condition(MenuCallbackArgs args)
        {
            return true;
        }

        private bool hireling_battle_menu_avoid_combat_on_condition(MenuCallbackArgs args)
        {
            return hireling_battle_menu_avoid_battle_on_condition(args);
        }


        // Token: 0x0600015C RID: 348 RVA: 0x000119BC File Offset: 0x0000FBBC   
        private bool wait_on_condition(MenuCallbackArgs args)
        {
            TORCommon.Say("WAIT ON CONDITION");
            return true;
        }
        
        private void wait_on_tick(MenuCallbackArgs args, CampaignTime time)
        {
            this.updatePartyMenu(args);
        }
        
        private void updatePartyMenu(MenuCallbackArgs args)
        {
            bool flag = _hirelingEnlistingLord == null || !_hirelingEnlisted;
            if (flag)
            {
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }
            }
            else
            {

                args.MenuContext.SetBackgroundMeshName(_hirelingEnlistingLord.MapFaction.Culture.EncounterBackgroundMesh);
                TextObject text = args.MenuContext.GameMenu.GetText();
                text.SetTextVariable("PARTY_LEADER", _hirelingEnlistingLord.EncyclopediaLinkWithName);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData<bool>("_enlisted", ref _hirelingEnlisted);
            dataStore.SyncData<Hero>("_enlistingLord", ref _hirelingEnlistingLord);
        }

        private void party_wait_talk_to_other_members_on_init(MenuCallbackArgs args)
        {

        }


        // BATTLE HANDLERS


        private void controlPlayerLoot(MapEvent mapEvent)
        {
            if (mapEvent.PlayerSide == mapEvent.WinningSide && IsEnlisted())
            {

                PlayerEncounter.Current.RosterToReceiveLootItems.Clear();
                PlayerEncounter.Current.RosterToReceiveLootMembers.Clear();
                PlayerEncounter.Current.RosterToReceiveLootPrisoners.Clear();
            }

        }



        // SETTLEMENTS HANDLERS

        private void OnPartyLeavesSettlement(MobileParty mobileParty, Settlement settlement)
        {

            if (!_hirelingEnlisted || _hirelingEnlistingLord == null) return;
           
            if (_hirelingEnlistingLord.PartyBelongedTo == mobileParty || (MobileParty.MainParty == mobileParty && mobileParty.CurrentSettlement == null))
            {


                while (Campaign.Current.CurrentMenuContext != null)
                    GameMenu.ExitToLast();
                GameMenu.ActivateGameMenu("hireling_menu");
                if (PartyBase.MainParty.MobileParty.CurrentSettlement != null)
                    LeaveSettlementAction.ApplyForParty(PartyBase.MainParty.MobileParty);
            }
        }

        private void EnlistingLordPartyEntersSettlement(MobileParty mobileParty, Settlement settlement, Hero arg3)
        {
            if (!_hirelingEnlisted) return;
            if (_hirelingEnlistingLord != null && _hirelingEnlistingLord.PartyBelongedTo == mobileParty)
            {


                bool isTown = settlement.IsTown;
                if (isTown)
                {

                    while (Campaign.Current.CurrentMenuContext != null)
                        GameMenu.ExitToLast();
                    GameMenu.ActivateGameMenu("hireling_menu");
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                }

                //EnterSettlementAction.ApplyForParty(PartyBase.MainParty.MobileParty, settlement);
            }
        }


        // CAMPAIGN HANDLERS





        // MAP EVENT AND ENCOUNTERS

        // If the enlisted lord is fighting (in a map event) without us, we should fight with him

        // If the player is concerned by the end of a map event, we should start displaying the menu.
        private void mapEventEnded(MapEvent mapEvent)
        {
            if (_hirelingEnlistingLord != null && !mapEvent.IsPlayerMapEvent && getEnlistingLordisInMapEvent(mapEvent))
            {
                GameMenu.SwitchToMenu("hireling_menu");
                _hirelingLordIsFightingWithoutPlayer = false;
            };
            if (mapEvent.IsPlayerMapEvent)
            {
                GameMenu.SwitchToMenu("hireling_menu");
            }
            // TODO: Refining this part
        }



        // ON TICKS HANDLERS
        private void OnTick(float dt)
        {
            if (_hirelingLordIsFightingWithoutPlayer)
            {
                if (!MobileParty.MainParty.ShouldBeIgnored)
                {
                    // This part has not been tested, but it should work.
                    MobileParty.MainParty.IgnoreForHours(1);
                }

            }

            if (_hirelingEnlisted && _hirelingEnlistingLord != null)
            {


                HidePlayerParty();
                PartyBase.MainParty.MobileParty.Position2D = _hirelingEnlistingLord.PartyBelongedTo.Position2D;
                if (_hirelingEnlistingLord.PartyBelongedTo.MapEvent != null && MobileParty.MainParty.MapEvent == null)
                {
                    var mapEvent = _hirelingEnlistingLord.PartyBelongedTo.MapEvent;

                    
                    // TODO: CHECK THE DEFENDING PART
                    _hirelingEnlistingLordIsAttacking = false;

                    TORCommon.Say("Lord starts encounter");
                    foreach (var party in mapEvent.AttackerSide.Parties)
                    {
                        if (party.Party == _hirelingEnlistingLord.PartyBelongedTo.Party)
                        {
                            TORCommon.Say("Lord is attacking");
                            float partyStrength = 0;
                            float enemyStrength = 0;
                            mapEvent.GetStrengthsRelativeToParty(BattleSideEnum.Attacker, out partyStrength, out enemyStrength);
                            _ratioPartyAgainstEnemyStrength = enemyStrength / partyStrength;

                            _hirelingEnlistingLordIsAttacking = true;
                            break;
                        }
                    }
                    
                    if (!_hirelingLordIsFightingWithoutPlayer)
                    {
                        GameMenu.ActivateGameMenu("hireling_battle_menu");
                    }
                }
                
            }
        }


        // ACTIONS

        private void UndoDiplomacy()
        {
            ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(Hero.MainHero.Clan, false);
        }


        private void EnlistPlayer()
        {
            HidePlayerParty();
            DisbandParty();
            Hero.MainHero.AddAttribute("enlisted");
            _hirelingEnlistingLord = CharacterObject.OneToOneConversationCharacter.HeroObject;
            ChangeKingdomAction.ApplyByJoinFactionAsMercenary(Hero.MainHero.Clan, _hirelingEnlistingLord.Clan.Kingdom, 25, false);
            MBTextManager.SetTextVariable("ENLISTINGLORDNAME", _hirelingEnlistingLord.EncyclopediaLinkWithName);

            
            while (Campaign.Current.CurrentMenuContext != null)
                GameMenu.ExitToLast();
            _hirelingEnlisted = true;
            GameMenu.ActivateGameMenu("hireling_menu");


        }

        private void ShowPlayerParty()
        {
            // Currently not working
            PartyBase.MainParty.MobileParty.IsVisible = true;
        }

        private void HidePlayerParty()
        {
            // Currently not working
            PartyBase.MainParty.MobileParty.IsVisible = false;


            /*if (((PartyVisual) ).HumanAgentVisuals != null)
                ((PartyVisual) PartyBase.MainParty.Visuals).HumanAgentVisuals.GetEntity().SetVisibilityExcludeParents(false);
            if (((PartyVisual) PartyBase.MainParty.Visuals).MountAgentVisuals == null)
                return;
            ((PartyVisual) PartyBase.MainParty.Visuals).MountAgentVisuals.GetEntity().SetVisibilityExcludeParents(false);*/
        }


        private void DisbandParty()
        {
            if (MobileParty.MainParty.MemberRoster.TotalManCount <= 1)
                return;
            List<TroopRosterElement> troopRosterElementList = new List<TroopRosterElement>();
            foreach (TroopRosterElement troopRosterElement in (List<TroopRosterElement>)MobileParty.MainParty.MemberRoster.GetTroopRoster())
            {
                if (troopRosterElement.Character != Hero.MainHero.CharacterObject && troopRosterElement.Character.HeroObject == null)
                    troopRosterElementList.Add(troopRosterElement);
            }
            if (troopRosterElementList.Count == 0)
                return;
            foreach (TroopRosterElement troopRosterElement in troopRosterElementList)
            {
                //Test.followingHero.PartyBelongedTo.MemberRoster.AddToCounts(troopRosterElement.Character, troopRosterElement.Number);
                MobileParty.MainParty.MemberRoster.AddToCounts(troopRosterElement.Character, -1 * troopRosterElement.Number);
            }
        }


        // GETTERS 
        public bool getEnlistingLordisInMapEvent(MapEvent mapEvent)
        {

            var lordIsInMapEvent = false;
            if (_hirelingEnlistingLord == null)
                return false;

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