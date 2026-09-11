using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement
{
    [HarmonyPatch]
    public static class AiPatrollingBehaviorPatch
    {
        /// <summary>
        /// Provides a null leader hero guard that is missing from the ai behavior. It's present when necessary on all of the other native ones.
        /// </summary>
        /// <remarks>
        /// Added on 1.4.7.
        /// A crash would occur when a leaderless raider clan party would try to evaluate if it should patrol.
        /// </remarks>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AiPatrollingBehavior), "AiHourlyTick")]
        public static bool NullLeaderPatch(MobileParty mobileParty, PartyThinkParams p)
        {
            if (mobileParty.LeaderHero == null) return false;

            return true;
        }
    }
}
