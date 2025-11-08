using HarmonyLib;
using Helpers;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORFaithModel : GameModel
    {
        public int CalculateBlessingDurationForParty(MobileParty party)
        {
            ExplainedNumber result = new ExplainedNumber(TORConstants.DEFAULT_BLESSING_DURATION);
            SkillHelper.AddSkillBonusForParty(TORSkillEffects.BlessingDuration, party, ref result);
            return (int)result.ResultNumber;
        }

        public int CalculateSkillXpForPraying(Hero hero, int blessingDuration=1)
        {
            ExplainedNumber result = new ExplainedNumber(TORConstants.DEFAULT_PRAYING_FAITH_XP);

            if (hero.PartyBelongedTo!=null && hero.PartyBelongedTo.HasAnyActiveBlessing())
            {//when refreshing an active blessing, xp is only granted for the extension
                var t = hero.PartyBelongedTo.GetPartyInfo();
                blessingDuration = blessingDuration - t.CurrentBlessingRemainingDuration;
            }
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

        public void AddBlessedWaterToMainParty(DevotionLevel level, int durationValue)
        {
            var amount = 0;
            var party = MobileParty.MainParty;
            var info = party.GetPartyInfo();

            var blessingDuration = durationValue;
            if (info != null) blessingDuration -= info.CurrentBlessingRemainingDuration;
            if (blessingDuration <= 0) return; //you'd need to lose faith skill level between blessings for this to be true
     
            var timeValue = (float)blessingDuration/(5*CampaignTime.HoursInDay);
            
            switch (level)
            {
                case DevotionLevel.None:
                    amount=0;
                    break;
                case DevotionLevel.Follower:
                    amount=1;
                    break;
                case DevotionLevel.Devoted:
                    amount=2;
                    break;
                case DevotionLevel.Fanatic:
                    amount=3;
                    break;
            }
            
            var blessedWater = TorEnchantingIngredients.BlessedWater;
            var toReceive = (int)(amount*timeValue);
            if (toReceive > 0) PartyBase.MainParty.MobileParty.ItemRoster.AddToCounts(blessedWater, toReceive);
        }

        public void AddBlessingToParty(MobileParty party, ReligionObject religion)
        {
            if (religion == null) return;
            if (party == null || !party.IsActive || !party.IsLordParty || party.LeaderHero == null) return;
            
            var cultID = religion.StringId;
            var leaderHero = party.LeaderHero;
            var duration = CalculateBlessingDurationForParty(party);

            if (party == MobileParty.MainParty && leaderHero == Hero.MainHero && !Hero.MainHero.IsPrisoner)
            {
                AddBlessedWaterToMainParty(leaderHero.GetDevotionLevelForReligion(religion), duration);

                if (cultID == "cult_of_sigmar" && leaderHero.HasCareerChoice("SigmarsProclaimerPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("SigmarsProclaimerPassive4");
                    if (choice?.Passive == null) return;
                    foreach (var hero in party.GetMemberHeroes())
                    {
                        var value = (int)choice.Passive.EffectMagnitude;
                        hero.Heal(value, false);
                    }
                }

                if (cultID == "cult_of_ulric" && leaderHero.HasCareerChoice("TeachingsOfTheWinterFatherPassive2"))
                {
                    var choice = TORCareerChoices.GetChoice("TeachingsOfTheWinterFatherPassive2");
                    if (choice == null || choice.Passive == null) return;
                    Hero.MainHero.Heal(Hero.MainHero.MaxHitPoints, false);
                }

                if (cultID == "cult_of_lady" && leaderHero.HasCareerChoice("TalesOfGilesPassive3"))
                {
                    var roster = party.MemberRoster;
                    for (int i = 0; i < roster.Count; i++)
                    {
                        var character = roster.GetCharacterAtIndex(i);
                        if (!character.IsRegular) continue;

                        int wounded = roster.GetElementWoundedNumber(i);
                        if (wounded > 0)
                        {
                            roster.AddToCountsAtIndex(i, 0, -wounded, 0, true);
                        }
                    }
                }
            }

            //because ai parties entering shrines will automatically gain a blessing if possible, a player can run an army through a shrine like a religious car wash
            leaderHero.AddReligiousInfluence(religion, duration);
            leaderHero.AddSkillXp(TORSkills.Faith, CalculateSkillXpForPraying(leaderHero, duration));            
            
            ExtendedInfoManager.Instance.AddBlessingToParty(party, cultID, duration);
        }
    }
}
