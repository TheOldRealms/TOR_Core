using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.CustomDialogs.ConversationTags
{
    public class IsDwarfTag : ConversationTag
    {
        public const string Id = "IsDwarfTag";
        public override string StringId => nameof(IsDwarfTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.IsDwarf();
        }
    }

    public class PlayerIsDwarfTag : ConversationTag
    {
        public const string Id = "PlayerIsDwarfTag";
        public override string StringId => nameof(PlayerIsDwarfTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return Hero.MainHero.CharacterObject.IsDwarf();
        }
    }
}
