using TaleWorlds.CampaignSystem.Conversation.Tags;
using TaleWorlds.CampaignSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CustomDialogs.ConversationTags
{
    public class AsraiTag : ConversationTag
    {
        public const string Id = "AsraiTag";
        public override string StringId => nameof(AsraiTag);
        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.IsElf() && character.Culture.StringId == TORConstants.Cultures.ASRAI;
        }
    }

    public class ElfMaleTag : ConversationTag
    {
        public const string Id = "ElfMaleTag";
        public override string StringId => nameof(ElfMaleTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.IsElf() && !character.IsFemale;
        }
    }

    public class PlayerIsElfTag : ConversationTag
    {
        public const string Id = "PlayerIsElfTag";
        public override string StringId => nameof(PlayerIsElfTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.IsElf() && Hero.MainHero.CharacterObject.IsElf();
        }
    }
}
