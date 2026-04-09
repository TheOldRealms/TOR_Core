using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation.Tags;
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

    public class PlayerIsEonirTag : ConversationTag
    {
        public const string Id = "PlayerIsEonirTag";
        public override string StringId => nameof(PlayerIsEonirTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return Hero.MainHero.CharacterObject.IsElf() && Hero.MainHero.Culture.StringId == TORConstants.Cultures.EONIR;
        }
    }
}