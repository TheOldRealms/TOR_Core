using SandBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

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
			return (int)(formation.GetCountOfUnitsWithCondition(x => x.IsActive() && !x.IsDetachedFromFormation && x.CurrentlyUsedGameObject == null) / 15);
		}
	}
}
