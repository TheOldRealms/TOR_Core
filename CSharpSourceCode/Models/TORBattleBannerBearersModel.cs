using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORBattleBannerBearersModel : SandboxBattleBannerBearersModel
    {
        public override bool CanAgentBecomeBannerBearer(Agent agent)
        {
            CharacterObject characterObject;
            return agent.IsHuman && !agent.IsMainAgent && agent.IsAIControlled && (characterObject = (agent.Character as CharacterObject)) != null && !characterObject.IsHero;
        }

        public override int GetDesiredNumberOfBannerBearersForFormation(Formation formation)
        {
            if (!CanFormationDeployBannerBearers(formation))
            {
                return 0;
            }
            return TORConfig.NumberOfTroopsPerFormationWithStandard;
        }
    }
}