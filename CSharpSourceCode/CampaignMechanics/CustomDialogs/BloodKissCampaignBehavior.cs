using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
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
            starter.AddPlayerLine("request_bloodkiss", "lord_talk_speak_diplomacy_2", "bloodkiss_root", "My lord von Carstein, I wish to shed my mortal shell and serve you eternally.", IsEligibleForBloodKiss, null);
            starter.AddDialogLine("bloodkiss_request_answer", "bloodkiss_root", "bloodkiss_answer_line2", "Thou hast served me and mine loyally, I shall grant thee the Blood kiss if thou so desireth.", null, null);
            starter.AddDialogLine("bloodkiss_request_answer2", "bloodkiss_answer_line2", "bloodkiss_areyouready", "However, this gift does not come without cost, to be granted the blood kiss thou canst never go back. Art thou sure?", null, null);
            starter.AddPlayerLine("bloodkiss_iamready", "bloodkiss_areyouready", "bloodkiss_player_ready", "I am ready my lord.", null, null);
            starter.AddPlayerLine("bloodkiss_iamnotready", "bloodkiss_areyouready", "bloodkiss_player_notready", "On a second thought, I need to think about it.", null, null);
            starter.AddDialogLine("bloodkiss_recieve", "bloodkiss_player_ready", "bloodkiss_playcutscene", "Prepare thine self for thine dark awakening.", null, null);
            starter.AddDialogLine("bloodkiss_cutscene", "bloodkiss_playcutscene", "close_window", "Rise, von Carstein scion.", null, OnBloodKissRecieved);
            starter.AddDialogLine("bloodkiss_dontbotherme", "bloodkiss_player_notready", "close_window", "Than think about it someplace else, thou disturbeth mine plans. ", null, null);
        }

        private void OnBloodKissRecieved()
        {
            MBInformationManager.ShowSceneNotification(new BloodKissSceneNotificationItem());
            Hero.MainHero.CharacterObject.Race = FaceGen.GetRaceOrDefault("vampire");
        }

        private bool IsEligibleForBloodKiss()
        {
            var partner = Hero.OneToOneConversationHero;
            if(partner.MapFaction.IsKingdomFaction && 
                partner.MapFaction.StringId == "sylvania" && 
                Clan.PlayerClan.Kingdom == partner.MapFaction && 
                !Clan.PlayerClan.IsUnderMercenaryService &&
                Clan.PlayerClan.Tier > Campaign.Current.Models.KingdomCreationModel.MinimumClanTierToCreateKingdom)
            {
                return !Hero.MainHero.IsVampire();
            }
            return false;
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
