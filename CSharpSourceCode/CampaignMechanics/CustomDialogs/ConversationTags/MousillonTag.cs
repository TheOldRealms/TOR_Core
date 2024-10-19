
using TOR_Core.Utilities;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
    public class MousillonTag : ConversationTag
    {
        public const string Id = "MousillonTag";
        public override string StringId => nameof (MousillonTag);
        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.Culture.StringId == TORConstants.Cultures.MOUSILLON;
        }
    }
}