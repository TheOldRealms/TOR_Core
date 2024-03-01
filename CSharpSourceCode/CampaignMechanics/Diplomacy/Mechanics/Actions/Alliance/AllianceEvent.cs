using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Actions.Alliance
{
    public readonly struct AllianceEvent
    {
        public AllianceEvent(Kingdom kingdom, Kingdom otherKingdom)
        {
            Kingdom = kingdom;
            OtherKingdom = otherKingdom;
        }

        public Kingdom Kingdom { get; }
        public Kingdom OtherKingdom { get; }
    }
}