using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
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
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.ServeAsAMerc
{
    public class ServeAsAHirelingCampaignBehavior : CampaignBehaviorBase
    {

        private bool _enlisted;
        private Hero _enlistingLord;

        public bool IsEnlisted()
        {
            return _enlisted;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, ServeAsAMercDialog);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, EnlistingLordPartyEntersSettlement);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, EnlistingLordPartyLeavesSettlement);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, controlPlayerLoot);                //Those events are never executed when the player lose a battle!
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, mapEventEnded);
        }
        // INIT PHASE
        private void ServeAsAMercDialog(CampaignGameStarter campaignGameStarter)
        {
            TextObject infotext = new TextObject("Following {ENLISTINGLORDNAME}", null);
            campaignGameStarter.AddPlayerLine("convincelord", "lord_talk_speak_diplomacy_2", "payedsword", "I am hereby offering my sword.", null, EnlistPlayer);
            campaignGameStarter.AddDialogLine("payedsword", "payedsword", "end", "As you wish.", null, null, 200, null);
            campaignGameStarter.AddWaitGameMenu("hireling_menu", infotext.Value, new OnInitDelegate(this.party_wait_talk_to_other_members_on_init), new OnConditionDelegate(this.wait_on_condition),
                    null, new OnTickDelegate(this.wait_on_tick), GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 0f, GameMenu.MenuFlags.None, null);
            TextObject textObject = new TextObject("Desert", null);
            TextObject textObject25 = new TextObject("{=FLT0000044}Abandon Party", null);
            
            
            campaignGameStarter.AddGameMenuOption("hireling_menu", "party_wait_leave", textObject25.ToString(), delegate (MenuCallbackArgs args)
            {
                TextObject text = new TextObject("{=FLT0000045}This will damage your reputation with the {FACTION}", null);
                string faction_name = (_enlistingLord != null) ? _enlistingLord.MapFaction.Name.ToString() : "DATA CORRUPTION ERROR";
                text.SetTextVariable("FACTION", faction_name);
                args.Tooltip = text;
                args.optionLeaveType = GameMenuOption.LeaveType.Escape;
                return true;
            }, delegate (MenuCallbackArgs args)
            {
                TextObject titleText = new TextObject("{=FLT0000044}Abandon Party", null);
                TextObject text = new TextObject("{=FLT0000046}Are you sure you want to abandon the party>  This will harm your relations with the entire faction.", null);
                TextObject affrimativeText = new TextObject("{=FLT0000047}Yes", null);
                TextObject negativeText = new TextObject("{=FLT0000048}No", null);
                InformationManager.ShowInquiry(new InquiryData(titleText.ToString(), text.ToString(), true, true, affrimativeText.ToString(), negativeText.ToString(), delegate ()
                {
                    // ChangeFactionRelation(_enlistingLord.MapFaction, -100000);
                    ChangeCrimeRatingAction.Apply(_enlistingLord.MapFaction, 55f, true);
                    foreach (Clan clan in _enlistingLord.Clan.Kingdom.Clans)
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
                    LeaveLordPartyAction(true);
                    GameMenu.ExitToLast();
                }, delegate ()
                {
                    GameMenu.ActivateGameMenu("hireling_menu");
                }, "", 0f, null, null, null), false, false);
            }, true, -1, false, null);


            TextObject hirelingBattleTextMenu = new TextObject("This is a test of Hireling BattleMenu", null);
            campaignGameStarter.AddGameMenu("hireling_battle_menu", hirelingBattleTextMenu.Value, new OnInitDelegate(this.party_wait_talk_to_other_members_on_init), GameOverlays.MenuOverlayType.Encounter, GameMenu.MenuFlags.None, null);
            campaignGameStarter.AddGameMenuOption("hireling_battle_menu", "hireling_attack", "Attack", 
                game_menu_attack_hideout_parties_on_condition,
                delegate (MenuCallbackArgs args)
                {
                    while (Campaign.Current.CurrentMenuContext != null)
                    {
                        GameMenu.ExitToLast();
                    }
                    StartBattleAction.Apply(PartyBase.MainParty, _enlistingLord.PartyBelongedTo.MapEvent.DefenderSide.LeaderParty);
                    TORCommon.Say("ATTACK MENU 2");
                }
                , false, 4, false);
        }

        public void LeaveLordPartyAction(bool keepgear)
        {
            Hero.MainHero.RemoveAttribute("enlisted");
            PlayerEncounter.Finish(true);
            UndoDiplomacy();
            showPlayerParty();
            _enlisted = false;
            _enlistingLord = null;
        }

        // Token: 0x0600375D RID: 14173 RVA: 0x000FA5A4 File Offset: 0x000F87A4
        private bool game_menu_attack_hideout_parties_on_condition(MenuCallbackArgs args)
        {
            TORCommon.Say("ATTACK MENU");
            return true;
        }

        // Token: 0x0600375E RID: 14174 RVA: 0x000FA630 File Offset: 0x000F8830
        private void game_menu_encounter_attack_on_consequence(MenuCallbackArgs args)
        {

           
        }


        // Token: 0x0600015C RID: 348 RVA: 0x000119BC File Offset: 0x0000FBBC
        private bool wait_on_condition(MenuCallbackArgs args)
        {
            TORCommon.Say("WAIT ON CONDITION");
            return true;
        }
        // Token: 0x06000159 RID: 345 RVA: 0x000112C1 File Offset: 0x0000F4C1
        private void wait_on_tick(MenuCallbackArgs args, CampaignTime time)
        {
            TORCommon.Say("WAIT ON TICK");

            this.updatePartyMenu(args);
        }
        // Token: 0x0600015E RID: 350 RVA: 0x000119DC File Offset: 0x0000FBDC
        private void updatePartyMenu(MenuCallbackArgs args)
        {
            bool flag = _enlistingLord == null || !_enlisted;
            if (flag)
            {
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }
            }
            else
            {

                args.MenuContext.SetBackgroundMeshName(_enlistingLord.MapFaction.Culture.EncounterBackgroundMesh);
                TextObject text = args.MenuContext.GameMenu.GetText();
                text.SetTextVariable("PARTY_LEADER", _enlistingLord.EncyclopediaLinkWithName);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {

        }

        private void party_wait_talk_to_other_members_on_init(MenuCallbackArgs args)
        {

        }


        // BATTLE HANDLERS


        private void controlPlayerLoot(MapEvent mapEvent)
        {
            if (mapEvent.PlayerSide == mapEvent.WinningSide && Hero.MainHero.IsEnlisted())
            {

                PlayerEncounter.Current.RosterToReceiveLootItems.Clear();
                PlayerEncounter.Current.RosterToReceiveLootMembers.Clear();
                PlayerEncounter.Current.RosterToReceiveLootPrisoners.Clear();
            }

        }



        // SETTLEMENTS HANDLERS

        private void EnlistingLordPartyLeavesSettlement(MobileParty mobileParty, Settlement settlement)
        {
            if (!_enlisted) return;
            if (_enlistingLord.PartyBelongedTo == mobileParty)
            {

                LeaveSettlementAction.ApplyForParty(PartyBase.MainParty.MobileParty);
            }
        }

        private void EnlistingLordPartyEntersSettlement(MobileParty mobileParty, Settlement settlement, Hero arg3)
        {
            if (!_enlisted) return;
            if (_enlistingLord!=null&&_enlistingLord.PartyBelongedTo == mobileParty)
            {


                bool isTown = settlement.IsTown;
                if (isTown)
                {
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                }

                EnterSettlementAction.ApplyForParty(PartyBase.MainParty.MobileParty, settlement);
            }
        }


        // CAMPAIGN HANDLERS





        // MAP EVENT AND ENCOUNTERS
        private void mapEventEnded(MapEvent mapEvent)
        {
            if (!mapEvent.IsPlayerMapEvent) return;
            _enlisted = true;
            GameMenu.ActivateGameMenu("hireling_menu");
        }



        // ON TICKS HANDLERS
        private void OnTick(float dt)
        {
            if (_enlisted && _enlistingLord != null)
            {
                HidePlayerParty();
                PartyBase.MainParty.MobileParty.Position2D = _enlistingLord.PartyBelongedTo.Position2D;
                if (_enlistingLord.PartyBelongedTo.MapEvent != null && MobileParty.MainParty.MapEvent == null)
                {
                    TORCommon.Say("Lord starts encounter");
                    var mapEvent = _enlistingLord.PartyBelongedTo.MapEvent;
                    var flagIsEnlistingLordAttacker = false;
                    foreach (var party in mapEvent.AttackerSide.Parties)
                    {
                        if (party.Party == _enlistingLord.PartyBelongedTo.Party)
                        {
                            TORCommon.Say("Lord is attacking");
                            flagIsEnlistingLordAttacker = true;
                            break;
                        }
                    }
                    GameMenu.ActivateGameMenu("hireling_battle_menu");

                    //while (Campaign.Current.CurrentMenuContext != null) { 
                      //      GameMenu.ExitToLast();
                        //    GameMenu.ActivateGameMenu("hireling_battle_menu");
                        //}

                   
                   // if (flagIsEnlistingLordAttacker)
                    //{
                      //  StartBattleAction.Apply(PartyBase.MainParty, mapEvent.DefenderSide.LeaderParty);
                    //}
                    //else { 
                      //  StartBattleAction.Apply(Campaign.Current.MainParty.Party, mapEvent.AttackerSide.LeaderParty);
                    //}

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
            _enlistingLord = CharacterObject.OneToOneConversationCharacter.HeroObject;
            ChangeKingdomAction.ApplyByJoinFactionAsMercenary(Hero.MainHero.Clan, _enlistingLord.Clan.Kingdom, 25, false);
            MBTextManager.SetTextVariable("ENLISTINGLORDNAME", _enlistingLord.EncyclopediaLinkWithName);



            while (Campaign.Current.CurrentMenuContext != null)
                GameMenu.ExitToLast();
            _enlisted = true;
            GameMenu.ActivateGameMenu("hireling_menu");


        }

        private void showPlayerParty()
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
    }
}