using TaleWorlds.CampaignSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.CustomDialogs
{
    public class BlackGrailCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnStart);
        }

        private void OnStart(CampaignGameStarter starter)
        {
            starter.AddPlayerLine("request_blackGrail", "lord_talk_speak_diplomacy_2", "bloodkiss_blooddragon_root", "{=tor_bloodkiss_request_blooddragon_str}Grandmaster, I wish to join your ranks of the Blood dragons as a Blood Knight.", IsEligibleForBlackGrailKnight, null);

        }
        
        private bool IsEligibleForBlackGrailKnight()
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

        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
    
    
}