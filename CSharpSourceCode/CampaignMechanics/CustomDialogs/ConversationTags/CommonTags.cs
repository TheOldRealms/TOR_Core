using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation.Tags;

namespace TOR_Core.CampaignMechanics.CustomDialogs.ConversationTags
{
    public class PlayerIsRenownedTag : ConversationTag
    {
        public const string Id = "PlayerIsRenownedTag";
        public override string StringId => nameof(PlayerIsRenownedTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.IsHero && Clan.PlayerClan.Tier > 2;
        }
    }
}
