using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
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
            starter.AddPlayerLine("request_bloodkiss", "lord_talk_speak_diplomacy_2", "bloodkiss_root", "My lord, I wish to serve you with undying loyalty in eternal life. Grant me the Blood Kiss.", IsEligibleForBloodKiss, null);
            starter.AddDialogLine("bloodkiss_request_answer", "bloodkiss_root", "bloodkiss_answer_line2", "You have served me well. I will grant you the gift of the Blood Kiss.", null, null);
            starter.AddDialogLine("bloodkiss_request_answer2", "bloodkiss_answer_line2", "bloodkiss_areyouready", "Are you ready?", null, null);
            starter.AddPlayerLine("bloodkiss_iamready", "bloodkiss_areyouready", "lord_talk_speak_diplomacy_2", "I am ready.", null, null);
            starter.AddPlayerLine("bloodkiss_iamnotready", "bloodkiss_areyouready", "lord_talk_speak_diplomacy_2", "On a second thought, I would rather not.", null, null);
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
