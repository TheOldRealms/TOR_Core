using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.CustomDialogs.ConversationTags
{
    public class VampireMaleTag : ConversationTag
    {
        public const string Id = "VampireMaleTag";
        public override string StringId => nameof(VampireMaleTag);
        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.IsVampire() && !character.IsFemale;
        }
    }

    public class VampireFemaleTag : ConversationTag
    {
        public const string Id = "VampireFemaleTag";
        public override string StringId => nameof(VampireFemaleTag);
        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.IsVampire() && character.IsFemale;
        }
    }
}
