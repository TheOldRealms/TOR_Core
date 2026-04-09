using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.CustomDialogs.ConversationTags
{
    public class IsOrcTag : ConversationTag
    {
        public const string Id = "IsOrcTag";
        public override string StringId => nameof(IsOrcTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.IsOrc();
        }
    }

    public class IsGoblinTag : ConversationTag
    {
        public const string Id = "IsGoblinTag";
        public override string StringId => nameof(IsGoblinTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.IsGoblin();
        }
    }

    public class PlayerIsOrcTag : ConversationTag
    {
        public const string Id = "PlayerIsOrcTag";
        public override string StringId => nameof(PlayerIsOrcTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return Hero.MainHero.CharacterObject.IsOrc();
        }
    }
}
