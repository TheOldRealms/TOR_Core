using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORFaithModel : GameModel
    {
        public int CalculateBlessingDurationForParty(MobileParty party)
        {
            ExplainedNumber result = new ExplainedNumber(TORConstants.DEFAULT_BLESSING_DURATION);
            SkillHelper.AddSkillBonusForParty(TORSkills.Faith, TORSkillEffects.BlessingDuration, party, ref result);
            return (int)result.ResultNumber;
        }

        public int CalculateSkillXpForPraying(Hero hero)
        {
            ExplainedNumber result = new ExplainedNumber(TORConstants.DEFAULT_PRAYING_FAITH_XP);
            return (int)result.ResultNumber;
        }

        public int CalculateDevotionIncreaseForPraying(Hero hero)
        {
            ExplainedNumber result = new ExplainedNumber(TORConstants.DEFAULT_PRAYING_DEVOTION_INCREASE);
            PerkHelper.AddPerkBonusForCharacter(TORPerks.Faith.Devotee, hero.CharacterObject, false, ref result);
            return (int)result.ResultNumber;
        }

        public int CalculateCursedRegionDamagePerHour(MobileParty party)
        {
            ExplainedNumber result = new ExplainedNumber(TORConstants.DEFAULT_CURSE_WOUND_STRENGTH);
            PerkHelper.AddPerkBonusForCharacter(TORPerks.Faith.Superstitious, party.LeaderHero.CharacterObject, true, ref result);
            return (int)result.ResultNumber;
        }
    }
}
