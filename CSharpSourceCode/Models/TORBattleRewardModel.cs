using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace TOR_Core.Models
{
    public class TORBattleRewardModel : DefaultBattleRewardModel
    {
        public override float GetPartySavePrisonerAsMemberShareProbability(PartyBase winnerParty, float lootAmount) => 0f;
    }
}
