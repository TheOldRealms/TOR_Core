using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
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

        public int CalculateSkillXpForPraying(Hero hero, int blessingDuration=1)
        {
            ExplainedNumber result = new ExplainedNumber(TORConstants.DEFAULT_PRAYING_FAITH_XP);
            result.AddFactor(blessingDuration-1);
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

        public void AddBlessingToParty(MobileParty party, string cultID)
        {
            var religion = ReligionObject.All.FirstOrDefault(x => x.StringId == cultID);
            if (religion == null) return;
            AddBlessingToParty(party, religion);
        }
        
        public void AddBlessingToParty(MobileParty party, ReligionObject religion)
        {
            if (religion == null) return;
            var cultID = religion.StringId;
            if (party == null || !party.IsActive || !party.IsLordParty) return;
            
            var duration = CalculateBlessingDurationForParty(party);
            
            if (party == MobileParty.MainParty && party.LeaderHero == Hero.MainHero && !Hero.MainHero.IsPrisoner && cultID == "cult_of_sigmar" && Hero.MainHero.HasCareerChoice("SigmarsProclaimerPassive4"))
            {
                var choice = TORCareerChoices.GetChoice("SigmarsProclaimerPassive4");
                if(choice?.Passive == null)return;
                foreach (var hero in party.GetMemberHeroes())
                {
                    var value =(int) choice.Passive.EffectMagnitude;
                    hero.Heal(value,false);
                }
            }
            
            if (cultID== "cult_of_ulric" && Hero.MainHero.HasCareerChoice("TeachingsOfTheWinterFatherPassive2"))
            {
                var choice = TORCareerChoices.GetChoice("TeachingsOfTheWinterFatherPassive2");
                if (choice == null || choice.Passive == null) return;
                Hero.MainHero.Heal(Hero.MainHero.MaxHitPoints,false);
            }
            
            party.LeaderHero.AddReligiousInfluence(religion, duration);
            party.LeaderHero.AddSkillXp(TORSkills.Faith, CalculateSkillXpForPraying(Hero.MainHero, duration));
            
            ExtendedInfoManager.Instance.AddBlessingToParty(party.StringId, cultID, duration);
        }

        
    }
}
