using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Invasions;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions
{
    public static class MobilePartyExtensions
    {
        public static bool IsRaidingParty(this MobileParty party)
        {
            return party.PartyComponent is IRaidingParty;
        }

        public static bool IsInvasionParty(this MobileParty party)
        {
            return party.PartyComponent is IInvasionParty;
        }

        public static MobilePartyExtendedInfo GetPartyInfo(this MobileParty party)
        {
            return ExtendedInfoManager.Instance.GetPartyInfoFor(party.StringId);
        }

        public static void AddBlessingToParty(this MobileParty party, string blessingId)
        { 
            var model = Campaign.Current.Models.GetFaithModel();
            if (model != null)
            {
                model.AddBlessingToParty(party,blessingId);
            }
        }

        public static bool HasAnyActiveBlessing(this MobileParty party)
        {
            var info = party.GetPartyInfo();
            return info != null && info.CurrentBlessingRemainingDuration > 0 && !string.IsNullOrWhiteSpace(info.CurrentBlessingStringId);
        }

        public static bool HasBlessing(this MobileParty party, string id)
        {
            var info = party.GetPartyInfo();
            return info != null && info.CurrentBlessingStringId == id && info.CurrentBlessingRemainingDuration > 0;
        }

        public static bool IsNearASettlement(this MobileParty party, float threshold = 1.5f)
        {
            foreach (Settlement settlement in Settlement.All)
            {
                float distance;
                Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, party, Campaign.MapDiagonal, out distance);
                if (distance < threshold)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsAffectedByCurse(this MobileParty party)
        {
            if (!party.IsLordParty) return false;

            if (party.IsMainParty)
            {
                if (party.LeaderHero.IsEnlisted())
                    return false;
            }
            foreach (Settlement settlement in TORCustomSettlementCampaignBehavior.AllCustomSettlements)
            {
                if(settlement.SettlementComponent is CursedSiteComponent)
                {
                    var comp = settlement.SettlementComponent as CursedSiteComponent;
                    if (party.LeaderHero?.GetDominantReligion() == comp.Religion) continue;
                    float distance;
                    Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, party, Campaign.MapDiagonal, out distance);
                    if (distance < TORConstants.DEFAULT_CURSE_RADIUS)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static List<ItemRosterElement> GetArtilleryItems(this MobileParty party)
        {
            List<ItemRosterElement> list = new List<ItemRosterElement>();
            list.AddRange(party.ItemRoster.Where(x => x.EquipmentElement.Item.StringId.Contains("artillery")).ToList());
            return list;
        }

        public static int GetMaxNumberOfArtillery(this MobileParty party)
        {
            if (party == MobileParty.MainParty)
            {
                if (party.LeaderHero != null)
                {
                    var engineering = party.LeaderHero.GetSkillValue(DefaultSkills.Engineering);
                    return (int)Math.Truncate((decimal)engineering / 50);
                }
                else return 0;
            }
            else if (party.IsLordParty)
            {
                return 3;
            }
            else return 0;
        }

        public static List<Hero> GetMemberHeroes(this MobileParty party)
        {
            List<Hero> heroes = new List<Hero>();
            foreach (var member in party.MemberRoster.GetTroopRoster())
            {
                if (member.Character.HeroObject != null)
                {
                    heroes.Add(member.Character.HeroObject);
                }
            }
            return heroes;
        }

        public static bool HasSpellCasterMember(this MobileParty party)
        {
            return party.GetMemberHeroes().Any(x => x.IsSpellCaster());
        }

        public static List<Hero> GetSpellCasterMemberHeroes(this MobileParty party)
        {
            return party.GetMemberHeroes().Where(x => x.IsSpellCaster()).ToList();
        }

        public static int GetHighestSkillValue(this MobileParty party, SkillObject skill)
        {
            int skillValue = 0;
            foreach(var hero in party.GetMemberHeroes())
            {
                var heroSkillvalue = hero.GetSkillValue(skill);
                if (heroSkillvalue > skillValue) skillValue = heroSkillvalue;
            }
            return skillValue;
        }

        public static int GetHighestAttributeValue(this MobileParty party, CharacterAttribute attribute)
        {
            int value = 0;
            foreach (var hero in party.GetMemberHeroes())
            {
                var heroValue = hero.GetAttributeValue(attribute);
                if (heroValue > value) value = heroValue;
            }
            return value;
        }

        public static bool HasVampire(this MobileParty party)
        {
            foreach (var hero in party.GetMemberHeroes())
            {
                if (hero.IsVampire()) return true;
            }
            return false;
        }

        public static bool HasNecromancer(this MobileParty party)
        {
            foreach (var hero in party.GetMemberHeroes())
            {
                if (hero.IsNecromancer()) return true;
            }
            return false;
        }

        public static bool HasSpellcaster(this MobileParty party)
        {
            foreach (var hero in party.GetMemberHeroes())
            {
                if (hero.IsSpellCaster()) return true;
            }
            return false;
        }

        public static bool HasKnownLore(this MobileParty party, string lorename)
        {
            foreach (var hero in party.GetMemberHeroes())
            {
                if (hero.HasKnownLore(lorename)) return true;
            }
            return false;
        }
    }
}
