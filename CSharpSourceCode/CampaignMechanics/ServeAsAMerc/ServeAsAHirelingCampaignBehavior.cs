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
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

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
                 
                 //EnterSettlementAction.ApplyForParty(PartyBase.MainParty.MobileParty,settlement);
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
             if (attacker == _enlistingLord.PartyBelongedTo.Party || defender == _enlistingLord.PartyBelongedTo.Party)
             {
                 EncounterManager.StartPartyEncounter(PartyBase.MainParty, defender);


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
         
         

         public override void SyncData(IDataStore dataStore)
         {
   
         }
         
         

         private void ServeAsAMercDialog(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddPlayerLine("convincelord", "lord_talk_speak_diplomacy_2", "payedsword", "I am hereby offering my sword.", null, EnlistPlayer);
            campaignGameStarter.AddDialogLine("payedsword", "payedsword", "end", "As you wish.", null,null,200,null);
            
        }
         

         private void EnlistPlayer()
         {
             HidePlayerParty();
             DisbandParty();
             _enlistingLord = CharacterObject.OneToOneConversationCharacter.HeroObject;
             ChangeKingdomAction.ApplyByJoinFactionAsMercenary(Hero.MainHero.Clan, _enlistingLord.Clan.Kingdom,25,false);
             
             
             while (Campaign.Current.CurrentMenuContext != null)
                 GameMenu.ExitToLast();

             //GameMenu.ActivateGameMenu("party_wait");
             
             _enlisted = true;
             

         }
         
         private void HidePlayerParty()
         {

     //        PartyBase.MainParty.MobileParty.IsVisible = false;
             
             
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