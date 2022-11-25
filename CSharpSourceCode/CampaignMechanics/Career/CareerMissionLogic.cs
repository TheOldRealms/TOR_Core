using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Career
{
    public class CareerMissionLogic :MissionLogic
    {

        private CareerCampaignBase _careerCampaignBase;
        private bool offline;
        public override void EarlyStart()
        {
            base.EarlyStart();
            if (Game.Current.GameType is Campaign)
            {
                _careerCampaignBase = Campaign.Current.GetCampaignBehavior<CareerCampaignBase>();
            }
            else
                offline = true;
        }



        

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if (offline) return;
            if (affectedAgent.Character==Campaign.Current.MainParty.LeaderHero.CharacterObject)
            {
                TORCommon.Say(""+affectedAgent.Health);
            }
        }
    }
}