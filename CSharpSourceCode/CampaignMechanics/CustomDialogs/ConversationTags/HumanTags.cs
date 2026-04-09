using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CustomDialogs.ConversationTags
{
    public class IsBretonnianTag : ConversationTag
    {
        public const string Id = "IsBretonnianTag";
        public override string StringId => nameof(IsBretonnianTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.Culture?.StringId == TORConstants.Cultures.BRETONNIA;
        }
    }

    public class IsEmpireTag : ConversationTag
    {
        public const string Id = "IsEmpireTag";
        public override string StringId => nameof(IsEmpireTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.Culture?.StringId == TORConstants.Cultures.EMPIRE;
        }
    }

    public class PlayerIsEmpireTag : ConversationTag
    {
        public const string Id = "PlayerIsEmpireTag";
        public override string StringId => nameof(PlayerIsEmpireTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return Hero.MainHero.Culture?.StringId == TORConstants.Cultures.EMPIRE;
        }
    }

    public class PlayerIsBretonnianTag : ConversationTag
    {
        public const string Id = "PlayerIsBretonnianTag";
        public override string StringId => nameof(PlayerIsBretonnianTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return Hero.MainHero.Culture?.StringId == TORConstants.Cultures.BRETONNIA;
        }
    }

    public class IsWarriorPriestTag : ConversationTag
    {
        public const string Id = "IsWarriorPriestTag";
        public override string StringId => nameof(IsWarriorPriestTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return character.IsHero && character.HeroObject.HasCareer(TORCareers.WarriorPriest);
        }
    }

    public class PlayerIsWarriorPriestTag : ConversationTag
    {
        public const string Id = "PlayerIsWarriorPriestTag";
        public override string StringId => nameof(PlayerIsWarriorPriestTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return Hero.MainHero.HasCareer(TORCareers.WarriorPriest);
        }
    }

    public class PlayerIsGrailKnightTag : ConversationTag
    {
        public const string Id = "PlayerIsGrailKnightTag";
        public override string StringId => nameof(PlayerIsGrailKnightTag);

        public override bool IsApplicableTo(CharacterObject character)
        {
            return Hero.MainHero.HasCareer(TORCareers.GrailKnight);
        }
    }
}
