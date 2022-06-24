using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace TOR_Core.CampaignMechanics.RaidingParties
{
    public interface IRaidingParty
    {
        void SetBehavior(MobileParty party, PartyThinkParams partyThinkParams);
    }
}
