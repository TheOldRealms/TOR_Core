using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.CustomDialogs
{
    public class BloodKissCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnStart);
        }

        private void OnStart(CampaignGameStarter starter)
        {
            starter.AddPlayerLine("request_bloodkiss", "lord_talk_speak_diplomacy_2", "bloodkiss_blooddragon_root", "Grandmaster, I wish to join your ranks of the Blood dragons as a Blood Knight.", IsEligibleForBloodKissBloodDragon, null);
            starter.AddPlayerLine("request_bloodkiss", "lord_talk_speak_diplomacy_2", "bloodkiss_root", "My lord von Carstein, I wish to shed my mortal shell and serve you eternally.", IsEligibleForBloodKissCarstein, null);

            //Blooddragons
            starter.AddDialogLine("bloodkiss_request_answer", "bloodkiss_blooddragon_root", "bloodkiss_blooddragon_answer_line2", "Ah, indeed, a Warrior of incredible prowess stands before me, daring to seek the Blood Kiss from none other than Walach Harkon. While you are besmirched by your mortality, your valor and devotion to the ideals of knighthood are truly commendable, and I find myself intrigued by your unwavering pursuit of perfection.", null, null);
            starter.AddDialogLine("bloodkiss_request_answer2", "bloodkiss_blooddragon_answer_line2", "bloodkiss_blooddragon_answer_line3", "To be worthy of the Blood Kiss is no small feat, and yet, I sense in you a rare dedication to the principles that guide the Order of the Blood Dragons. Your thirst for excellence and your unwavering discipline resonate strongly. The vows you have sworn, the code of chivalry you uphold – they mirror the very foundations upon which the Blood Dragons stand.", null, null);
            starter.AddDialogLine("bloodkiss_request_answer3", "bloodkiss_blooddragon_answer_line3", "bloodkiss_blooddragon_areyouready", "Know this, knight, the Blood Kiss is not a gift to be granted lightly. It is an eternal bond, a oath of fealty to kindred and I. All pathetic mortal senses of mercy and weakness will be purged from you (MAGIC ABILITIES WILL BE REMOVED!). Your desires for power and dominance shall be sated forevermore.", null, null);
            
            starter.AddPlayerLine("bloodkiss_iamready", "bloodkiss_blooddragon_areyouready", "bloodkiss_blooddragon_player_ready", "I am ready, Grandmaster.", null, null);
            starter.AddPlayerLine("bloodkiss_iamnotready", "bloodkiss_blooddragon_areyouready", "bloodkiss_player_notready", "On a second thought, I need to think about it.", null, null);

            starter.AddDialogLine("bloodkiss_recieve", "bloodkiss_blooddragon_player_ready", "bloodkiss_blooddragon_playcutscene", "Embrace the Bloodkiss, become one with the night, and ascend to greatness alongside the Blood Dragons.", null, null);
            starter.AddDialogLine("bloodkiss_cutscene", "bloodkiss_blooddragon_playcutscene", "close_window", "Rise, young blood dragon.", null, OnBloodKissBloodDragonRecieved);
            
            //Carstein
            starter.AddDialogLine("bloodkiss_request_answer", "bloodkiss_root", "bloodkiss_answer_line2", "Thou hast served me and mine loyally, I shall grant thee the Blood kiss if thou so desireth.", null, null);
            starter.AddDialogLine("bloodkiss_request_answer2", "bloodkiss_answer_line2", "bloodkiss_areyouready", "However, this gift does not come without cost, to be granted the blood kiss thou canst never go back. Art thou sure?", null, null);
            starter.AddPlayerLine("bloodkiss_iamready", "bloodkiss_areyouready", "bloodkiss_player_ready", "I am ready my lord.", null, null);
            starter.AddPlayerLine("bloodkiss_iamnotready", "bloodkiss_areyouready", "bloodkiss_player_notready", "On a second thought, I need to think about it.", null, null);
            
            starter.AddPlayerLine("bloodkiss_iamready", "bloodkiss_areyouready", "bloodkiss_player_ready", "I am ready my lord.", null, null);
            starter.AddPlayerLine("bloodkiss_iamnotready", "bloodkiss_areyouready", "bloodkiss_player_notready", "On a second thought, I need to think about it.", null, null);

            
            starter.AddDialogLine("bloodkiss_recieve", "bloodkiss_player_ready", "bloodkiss_playcutscene", "Prepare thine self for thine dark awakening.", null, null);
            starter.AddDialogLine("bloodkiss_cutscene", "bloodkiss_playcutscene", "close_window", "Rise, von Carstein scion.", null, OnBloodKissRecieved);
           
            starter.AddDialogLine("bloodkiss_dontbotherme", "bloodkiss_player_notready", "close_window", "Than think about it someplace else, thou disturbeth mine plans. ", null, null);
        }

        
        private void OnBloodKissBloodDragonRecieved()
        {
            MBInformationManager.ShowSceneNotification(new BloodKissSceneNotificationItem());
            Hero.MainHero.CharacterObject.Race = FaceGen.GetRaceOrDefault("vampire");
            Hero.MainHero.AddCareer(TORCareers.BloodKnight);
        }
        
        private void OnBloodKissRecieved()
        {
            MBInformationManager.ShowSceneNotification(new BloodKissSceneNotificationItem());
            Hero.MainHero.CharacterObject.Race = FaceGen.GetRaceOrDefault("vampire");
            Hero.MainHero.AddCareer(TORCareers.MinorVampire);
        }

        private bool IsEligibleForBloodKissBloodDragon()
        {
            var partner = Hero.OneToOneConversationHero;
            if(partner.MapFaction.IsKingdomFaction && 
               partner.MapFaction.StringId == "blooddragons" && 
               Clan.PlayerClan.Kingdom == partner.MapFaction && 
               !Clan.PlayerClan.IsUnderMercenaryService &&
               Clan.PlayerClan.Tier >= 2)
            {
                return Hero.MainHero.GetCareer() == TORCareers.Mercenary && Hero.MainHero.Clan.Kingdom.MapFaction == partner.MapFaction; 
            }
            return false;
        }
        
        private bool IsEligibleForBloodKissCarstein()
        {
            var partner = Hero.OneToOneConversationHero;
            if(partner.MapFaction.IsKingdomFaction && 
                partner.MapFaction.StringId == "sylvania" && 
                Clan.PlayerClan.Kingdom == partner.MapFaction && 
                !Clan.PlayerClan.IsUnderMercenaryService &&
                Clan.PlayerClan.Tier >= 4)
            {
                return !Hero.MainHero.IsVampire()&&Hero.MainHero.HasCareer(TORCareers.Mercenary);
            }
            return false;
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
