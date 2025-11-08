using TOR_Core.Extensions;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
    public class BloodDragonTag : ConversationTag
    {
        public const string Id = "BloodDragonTag";
        public override string StringId => nameof (BloodDragonTag);
        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.IsVampire() && character.IsBloodDragon();
        }
    }
}