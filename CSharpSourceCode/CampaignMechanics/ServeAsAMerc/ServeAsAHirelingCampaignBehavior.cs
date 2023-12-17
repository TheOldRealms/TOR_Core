using System;
using System.Collections.Generic;
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
using TaleWorlds.Library;
using TaleWorlds.Localization;
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
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this,ServeAsAMercDialog);
            CampaignEvents.TickEvent.AddNonSerializedListener(this,OnTick);
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this,EnlistingLordPartyEncounter);
            CampaignEvents.OnHeroJoinedPartyEvent.AddNonSerializedListener(this,EnlistingLordPartyJoinsBattle);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, EnlistingLordPartyEntersSettlement);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, EnlistingLordPartyLeavesSettlement);
        }


         private void EnlistingLordPartyLeavesSettlement(MobileParty mobileParty, Settlement settlement)
         {
             if(!_enlisted) return;
             if (_enlistingLord.PartyBelongedTo == mobileParty)
             {
                 
                 LeaveSettlementAction.ApplyForParty(PartyBase.MainParty.MobileParty);
             }
         }
         private void EnlistingLordPartyEntersSettlement(MobileParty mobileParty, Settlement settlement, Hero arg3)
         {
             if(!_enlisted) return;
             if (_enlistingLord.PartyBelongedTo == mobileParty)
             {
               
                
                    bool isTown = settlement.IsTown;
                    if (isTown)
                    {
                        Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                    }
                
                EnterSettlementAction.ApplyForParty(PartyBase.MainParty.MobileParty,settlement);
             }
         }
         
         private void EnlistingLordPartyJoinsBattle(Hero hero, MobileParty otherEventParty)
         {
             if(!_enlisted) return;
             if (_enlistingLord == hero)
             {
                 
                 EncounterManager.StartPartyEncounter(PartyBase.MainParty, otherEventParty.Party);
             }
         }

         private void EnlistingLordPartyEncounter(MapEvent mapEvent, PartyBase attacker, PartyBase defender)
         {
             if(!_enlisted) return;
             if (!_enlisted) return;
            if (attacker != _enlistingLord.PartyBelongedTo.Party && defender != _enlistingLord.PartyBelongedTo.Party) return;
            else
            {
                PartyBase.MainParty.MapEventSide = _enlistingLord.PartyBelongedTo.MapEventSide;
                GameMenu.ExitToLast();

            }
            if (attacker == _enlistingLord.PartyBelongedTo.Party) {
                EncounterManager.StartPartyEncounter(PartyBase.MainParty, defender);


            } else if (defender == _enlistingLord.PartyBelongedTo.Party)
            {
                EncounterManager.StartPartyEncounter(attacker, PartyBase.MainParty);
            }
        }


         private void OnTick(float dt)
         {
             if (_enlisted && _enlistingLord != null)
             {
                 HidePlayerParty();
                 PartyBase.MainParty.MobileParty.Position2D = _enlistingLord.PartyBelongedTo.Position2D;
             }
         }

        public static void LeaveLordPartyAction(bool keepgear)
        {
            bool flag2 = PlayerEncounter.Current != null;
            if (flag2)
            {
                PlayerEncounter.Finish(true);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {

        }

        private void party_wait_talk_to_other_members_on_init(MenuCallbackArgs args)
        {
     
        }

        private void ServeAsAMercDialog(CampaignGameStarter campaignGameStarter)
        {


            campaignGameStarter.AddPlayerLine("convincelord", "lord_talk_speak_diplomacy_2", "payedsword", "I am hereby offering my sword.", null, EnlistPlayer);
            campaignGameStarter.AddDialogLine("payedsword", "payedsword", "end", "As you wish.", null,null,200,null);
            TextObject infotext = new TextObject("Following {ENLISTINGLORDNAME}", null);        
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
        }

        // Token: 0x0600015C RID: 348 RVA: 0x000119BC File Offset: 0x0000FBBC
        private bool wait_on_condition(MenuCallbackArgs args)
        {
            return true;
        }
        // Token: 0x06000159 RID: 345 RVA: 0x000112C1 File Offset: 0x0000F4C1
        private void wait_on_tick(MenuCallbackArgs args, CampaignTime time)
        {
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

        private void EnlistPlayer()
         {
             HidePlayerParty();
             DisbandParty();
            _enlistingLord = CharacterObject.OneToOneConversationCharacter.HeroObject;
             ChangeKingdomAction.ApplyByJoinFactionAsMercenary(Hero.MainHero.Clan, _enlistingLord.Clan.Kingdom,25,false);
             MBTextManager.SetTextVariable("ENLISTINGLORDNAME", _enlistingLord.EncyclopediaLinkWithName);



            while (Campaign.Current.CurrentMenuContext != null)
                 GameMenu.ExitToLast();

             //GameMenu.ActivateGameMenu("party_wait");
             
             _enlisted = true;
            GameMenu.ActivateGameMenu("hireling_menu");


        }

        private void HidePlayerParty()
         {

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
             foreach (TroopRosterElement troopRosterElement in (List<TroopRosterElement>) MobileParty.MainParty.MemberRoster.GetTroopRoster())
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