using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.CustomDialogs
{
    public class CareerSwitchCampaignBehavior : CampaignBehaviorBase
    {
        private bool inquiryDeclined;
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnStart);
        }

        private void OnStart(CampaignGameStarter starter)
        {

            //Blood Knight
            BloodKnightLines(starter);

            //Carstein
            VampireCountDialogLines(starter);
            
            //BlackGrailKnight
            BlackGrailKnightDialogLines(starter);
            
            // Necrarch
            NecrachDialogLines(starter);
        }

        private void BlackGrailKnightDialogLines(CampaignGameStarter starter)
        {
            
            
            starter.AddPlayerLine("request_blackGrail", "lord_talk_speak_diplomacy_2", "black_grail_root", "{=tor_blackgrail_request_str}You keep saying the Lady is not real. What exactly do you mean?", IsEligibleForBlackGrailKnight, null);
            starter.AddDialogLine("black_grail_request_answer1", "black_grail_root", "black_grail_request_answer_line2", "{=tor_blackgrail_answer_line1_str}Not every Bretonne is ready for the truth. Are you willing to cleanse yourself from the Lie that we Bretonnes dogmatically follow?", null, null);
            starter.AddDialogLine("black_grail_request_answer2", "black_grail_request_answer_line2", "black_grail_request_answer_line3", "{=tor_blackgrail_answer_line2_str}I can tell you once you take the path I am wandering on, there is no salvation, and before we open the eyes of all Bretonnes, you will be an enemy of the King.", null, null);
            starter.AddDialogLine("black_grail_request_answer3", "black_grail_request_answer_line3", "black_grail_request_answer_areyouready", "{=tor_blackgrail_answer_ready_str}So I ask you again, are you really willing to to know the truth about the lady?", null, null);
            
            starter.AddPlayerLine("blackgrail_iamready", "black_grail_request_answer_areyouready", "black_grail_player_prompt", "{=tor_blackgrail_answer_player_ready_str}I am ready, mylord.", null, () => DisplayPrompt(TORCareers.BlackGrailKnight,OnBlackGrailReceived));
            starter.AddPlayerLine("blackgrail_iamnotready", "black_grail_request_answer_areyouready", "black_grail_player_notready", "{=tor_blackgrail_answer_player_notready_str}On a second thought, I need to think about it.", null, null);
            
            starter.AddDialogLine("black_grail_player_prompt", "black_grail_player_prompt", "black_grail_player_ready", "...", null, null);
            
            starter.AddPlayerLine("bloodkiss_inqury_declined", "black_grail_player_ready", "black_grail_player_notready", "{=tor_blackgrail_receive_str}On a second thought, I need to think about it.", ()=> inquiryDeclined, null);
            starter.AddDialogLine("blackgrail_receive", "black_grail_player_ready", "black_grail_player_playcutscene", "{=tor_blackgrail_receive_str}The truth is: The Bretonnes, proud humans follow the whisperings of an Elf. The Lady is nothing more than a Elven Witch, not a goddess or saint. Our whole Kingdom is build upon the Lie of Lady. I will clean you from these lies, drink from this grail, and gain the true powers of Bretonnia!", ()=> !inquiryDeclined, null);
            starter.AddDialogLine("bloodkiss_cutscene", "black_grail_player_playcutscene", "close_window", "{=tor_blackgrail_receive_end_str}Rise my young Knight. Let us clean this world from this non sense.", null, null);
            
            starter.AddDialogLine("bloodkiss_dontbotherme", "black_grail_player_notready", "close_window", "{=tor_blackgrail_dontbotherme_str}Then think about it someplace else. We are done here.", null, null);
            
            
            bool IsEligibleForBlackGrailKnight()
            {
                var partner = Hero.OneToOneConversationHero;
                if(partner.MapFaction.IsKingdomFaction && 
                   partner.MapFaction.StringId == "mousillon" && 
                   Clan.PlayerClan.Kingdom == partner.MapFaction && 
                   !Clan.PlayerClan.IsUnderMercenaryService &&
                   Clan.PlayerClan.Tier >= 2)
                {
                    return Hero.MainHero.GetCareer() == TORCareers.GrailKnight;
                }
                return false;
            }
            
            void OnBlackGrailReceived()
            {
                Hero.MainHero.AddCareer(TORCareers.BlackGrailKnight);
            }
        }

        private void BloodKnightLines(CampaignGameStarter starter)
        {
            starter.AddPlayerLine("request_bloodkiss", "lord_talk_speak_diplomacy_2", "bloodkiss_blooddragon_root", "{=tor_bloodkiss_request_blooddragon_str}Grandmaster, I wish to join your ranks of the Blood dragons as a Blood Knight.", IsEligibleForBloodKissBloodDragon, null);

            starter.AddDialogLine("bloodkiss_request_answer", "bloodkiss_blooddragon_root", "bloodkiss_blooddragon_answer_line2", "{=tor_bloodkiss_blooddragon_answer_line2_str}Ah, indeed, a Warrior of incredible prowess stands before me, daring to seek the Blood Kiss from none other than Walach Harkon. While you are besmirched by your mortality, your valor and devotion to the ideals of knighthood are truly commendable, and I find myself intrigued by your unwavering pursuit of perfection.", null, null);
            starter.AddDialogLine("bloodkiss_request_answer2", "bloodkiss_blooddragon_answer_line2", "bloodkiss_blooddragon_answer_line3", "{=tor_bloodkiss_blooddragon_answer_line3_str}To be worthy of the Blood Kiss is no small feat, and yet, I sense in you a rare dedication to the principles that guide the Order of the Blood Dragons. Your thirst for excellence and your unwavering discipline resonate strongly. The vows you have sworn, the code of chivalry you uphold – they mirror the very foundations upon which the Blood Dragons stand.", null, null);
            starter.AddDialogLine("bloodkiss_request_answer3", "bloodkiss_blooddragon_answer_line3", "bloodkiss_blooddragon_areyouready", "{=tor_bloodkiss_blooddragon_answer_ready_str}Know this, knight, the Blood Kiss is not a gift to be granted lightly. It is an eternal bond, a oath of fealty to kindred and I. All pathetic mortal senses of mercy and weakness will be purged from you (MAGIC ABILITIES WILL BE REMOVED!). Your desires for power and dominance shall be sated forevermore.", null, null);
            
            starter.AddPlayerLine("bloodkiss_iamready", "bloodkiss_blooddragon_areyouready", "bloodkiss_blooddragon_player_prompt", "{=tor_bloodkiss_blooddragon_player_ready_str}I am ready, Grandmaster.", null, null);
            starter.AddPlayerLine("bloodkiss_iamnotready", "bloodkiss_blooddragon_areyouready", "bloodkiss_blooddragon_player_notready", "{=tor_bloodkiss_blooddragon_player_notready_str}On a second thought, I need to think about it.", null, null);

            starter.AddDialogLine("bloodkiss_blooddragon_player_prompt", "bloodkiss_blooddragon_player_prompt", "bloodkiss_blooddragon_player_ready", "...", null, () => DisplayPrompt(TORCareers.BloodKnight,OnBloodKissBloodDragonRecieved));
            

            starter.AddPlayerLine("bloodkiss_inqury_declined2", "bloodkiss_blooddragon_player_ready", "bloodkiss_blooddragon_player_notready", "{=tor_blackgrail_receive_str}On a second thought, I need to think about it.", ()=> inquiryDeclined, null);

            starter.AddDialogLine("bloodkiss_recieve", "bloodkiss_blooddragon_player_ready", "bloodkiss_blooddragon_playcutscene", "{=tor_bloodkiss_blooddragon_receive_str}Embrace the Bloodkiss, become one with the night, and ascend to greatness alongside the Blood Dragons.", ()=> !inquiryDeclined, null);
            starter.AddDialogLine("bloodkiss_cutscene", "bloodkiss_blooddragon_playcutscene", "close_window", "{=tor_bloodkiss_blooddragon_player_receive_end_str}Rise, young blood dragon.", null, null);
            
            starter.AddDialogLine("bloodkiss_dontbotherme", "bloodkiss_blooddragon_player_notready", "close_window", "{=tor_bloodkiss_player_dontbotherme_str}Then think about it someplace else, thou disturbeth mine plans. ", null, null);

            
            void OnBloodKissBloodDragonRecieved()
            {
                MBInformationManager.ShowSceneNotification(new BloodKissSceneNotificationItem());
                Hero.MainHero.CharacterObject.Race = FaceGen.GetRaceOrDefault("vampire");
                Hero.MainHero.AddCareer(TORCareers.BloodKnight);
            }
            
            bool IsEligibleForBloodKissBloodDragon()
            {
                var partner = Hero.OneToOneConversationHero;
                if(partner.MapFaction.IsKingdomFaction && 
                   partner.MapFaction.StringId == "blooddragons" && 
                   Clan.PlayerClan.Kingdom == partner.MapFaction && 
                   !Clan.PlayerClan.IsUnderMercenaryService &&
                   Clan.PlayerClan.Tier >= 2)
                {

                    return CanBeVampire();

                }
                return false;
            }
        }

        private void VampireCountDialogLines(CampaignGameStarter starter)
        {
            starter.AddPlayerLine("request_bloodkiss", "lord_talk_speak_diplomacy_2", "bloodkiss_root", "{=tor_bloodkiss_request_str}My lord von Carstein, I wish to shed my mortal shell and serve you eternally.", IsEligibleForBloodKissCarstein, null);

            starter.AddDialogLine("bloodkiss_request_answer", "bloodkiss_root", "bloodkiss_answer_line2", "{=tor_bloodkiss_answer_line2_str}Thou hast served me and mine loyally, I shall grant thee the Blood kiss if thou so desireth.", null, null);
            starter.AddDialogLine("bloodkiss_request_answer2", "bloodkiss_answer_line2", "bloodkiss_areyouready", "{=tor_bloodkiss_answer_line3_str}However, this gift does not come without cost, to be granted the blood kiss thou canst never go back. Art thou sure?", null, null);
            starter.AddPlayerLine("bloodkiss_iamready", "bloodkiss_areyouready", "bloodkiss_player_prompt", "{=tor_bloodkiss_player_ready_str}I am ready my lord.", null, null);
            starter.AddPlayerLine("bloodkiss_iamnotready", "bloodkiss_areyouready", "bloodkiss_player_notready", "{=tor_bloodkiss_player_notready_str}On a second thought, I need to think about it.", null, null);
            
            starter.AddDialogLine("bloodkiss_player_prompt", "bloodkiss_player_prompt", "bloodkiss_player_ready", "...", null, () => DisplayPrompt(TORCareers.MinorVampire,OnBloodKissRecieved));
            
            starter.AddPlayerLine("bloodkiss_inqury_declined2", "bloodkiss_player_ready", "bloodkiss_player_notready", "{=tor_blackgrail_receive_str}On a second thought, I need to think about it.", ()=> inquiryDeclined, null);
            
            starter.AddDialogLine("bloodkiss_recieve", "bloodkiss_player_ready", "bloodkiss_playcutscene", "{=tor_bloodkiss_receive_str}Prepare thine self for thine dark awakening.", ()=> !inquiryDeclined, null);
            starter.AddDialogLine("bloodkiss_cutscene", "bloodkiss_playcutscene", "close_window", "{=tor_bloodkiss_player_receive_end_str}Rise, von Carstein scion.", null, null);
           
            starter.AddDialogLine("bloodkiss_dontbotherme", "bloodkiss_player_notready", "close_window", "{=tor_bloodkiss_player_dontbotherme_str}Then think about it someplace else, thou disturbeth mine plans. ", null, null);
            
            bool IsEligibleForBloodKissCarstein()
            {
                var partner = Hero.OneToOneConversationHero;
                if(partner.MapFaction.IsKingdomFaction && 
                   partner.MapFaction.StringId == "sylvania" && 
                   Clan.PlayerClan.Kingdom == partner.MapFaction && 
                   !Clan.PlayerClan.IsUnderMercenaryService &&
                   Clan.PlayerClan.Tier >= 4)
                {

                    return CanBeVampire();


                }
                return false;
            }


            

            
            void OnBloodKissRecieved()
            {
                MBInformationManager.ShowSceneNotification(new BloodKissSceneNotificationItem());
                Hero.MainHero.CharacterObject.Race = FaceGen.GetRaceOrDefault("vampire");
                Hero.MainHero.AddCareer(TORCareers.MinorVampire);
            }
        }
        
        private void NecrachDialogLines(CampaignGameStarter starter)
        {
            starter.AddPlayerLine("request_bloodkiss", "lord_talk_speak_diplomacy_2", "bloodkiss_necrach_root", "{=tor_bloodkiss_necrach_request_str}My lord. Zacharias. The Everliving. The disciple of Melkihor the Ancient. The most powerful of necrarchs. I wish to shed my mortal shell and serve you eternally. -", IsEligibleForBloodKissNecrach, null);

            starter.AddDialogLine("bloodkiss_request_answer", "bloodkiss_necrach_root", "bloodkiss_necrach_answer_line2", "{=tor_bloodkiss_answer_line2_str}You has served me and mine loyally, as my teacher did with me long ago. I will share the gift of immortality with you", null, null);
            starter.AddDialogLine("bloodkiss_request_answer2", "bloodkiss_necrach_answer_line2", "bloodkiss_necrach_areyouready", "{=tor_bloodkiss_answer_line3_str}Are you sure you are ready to leave your entire mortal life behind and enter forever into the lap of eternal night?", null, null);
            starter.AddPlayerLine("bloodkiss_iamready", "bloodkiss_necrach_areyouready", "bloodkiss_necrach_player_prompt", "{=tor_bloodkiss_player_ready_str}I am ready my lord.", null, null);
            starter.AddPlayerLine("bloodkiss_iamnotready", "bloodkiss_necrach_areyouready", "bloodkiss_player_notready", "{=tor_bloodkiss_player_notready_str}On a second thought, I need to think about it.", null, null);
            
            starter.AddDialogLine("bloodkiss_necrach_player_prompt", "bloodkiss_necrach_player_prompt", "bloodkiss_nechrach_player_ready", "...", null, () => DisplayPrompt(TORCareers.Necrarch,OnNecrachBloodKissRecieved));
            
            starter.AddPlayerLine("bloodkiss_inqury_declined2", "bloodkiss_player_ready", "bloodkiss_player_notready", "{=tor_blackgrail_receive_str}On a second thought, I need to think about it.", ()=> inquiryDeclined, null);
            
            starter.AddDialogLine("bloodkiss_recieve", "bloodkiss_nechrach_player_ready", "bloodkiss_playcutscene", "{=tor_bloodkiss_receive_str}Prepare thine self for thine dark awakening.", ()=> !inquiryDeclined, null);
            starter.AddDialogLine("bloodkiss_cutscene", "bloodkiss_playcutscene", "close_window", "{=tor_bloodkiss_player_receive_end_str}Behold! The new addition to the brotherhood of the necrarchs.", null, null);
           
            starter.AddDialogLine("bloodkiss_dontbotherme", "bloodkiss_player_notready", "close_window", "{=tor_bloodkiss_player_dontbotherme_str}Then think about it someplace else, thou disturbeth mine plans. ", null, null);
            
            bool IsEligibleForBloodKissNecrach()
            {
                var partner = Hero.OneToOneConversationHero;
                if(partner.MapFaction.IsKingdomFaction && 
                   partner.MapFaction.StringId == "necrachs" && 
                   Clan.PlayerClan.Kingdom == partner.MapFaction && 
                   !Clan.PlayerClan.IsUnderMercenaryService &&
                   Clan.PlayerClan.Tier >= 4)
                {
                    return CanBeVampire();
                }
                return false;
            }
            
            void OnNecrachBloodKissRecieved()
            {
                MBInformationManager.ShowSceneNotification(new BloodKissSceneNotificationItem());
                Hero.MainHero.AddCareer(TORCareers.Necrarch);
            }
        }
        
        
        
        private void DisplayPrompt(CareerObject careerObject, Action switchCareer)
        {
            var title = GameTexts.FindText("career_switch_title", careerObject.StringId);
            var explaination = GameTexts.FindText("career_switch_explaination", careerObject.StringId);
            inquiryDeclined = false;
            var inquiry = new InquiryData(title.ToString(),
                explaination.ToString(),
                true, 
                true, 
                "Accept", "Decline",
                 switchCareer,
                () => inquiryDeclined=true);
            InformationManager.ShowInquiry(inquiry);
        } 
        
        private bool CanBeVampire()
        {
            if (Hero.MainHero.CharacterObject.IsElf())
            {
                return false;
            }
            
            var career = Hero.MainHero.GetCareer();

            if (CareerHelper.IsPriestCareer(career))
                return false;
                
            
            return !Hero.MainHero.IsVampire();
        }
        
        public override void SyncData(IDataStore dataStore) { }
    }
}
