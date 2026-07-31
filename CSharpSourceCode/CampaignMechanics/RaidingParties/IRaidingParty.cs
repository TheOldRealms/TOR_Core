using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.RaidingParties
{
    public interface IRaidingParty
    {
        void HourlyTickAI(PartyThinkParams thinkParams);
    }
}