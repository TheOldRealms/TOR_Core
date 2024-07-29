using TaleWorlds.CampaignSystem.Conversation.Tags;
using TaleWorlds.CampaignSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CustomDialogs.ConversationTags
{
    public class EonirTag : ConversationTag
    {
        public const string Id = "EonirTag";
        public override string StringId => nameof(EonirTag);
        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.IsElf() && character.Culture.StringId == TORConstants.Cultures.EONIR;
        }
    }
}