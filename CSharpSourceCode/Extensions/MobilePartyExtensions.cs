using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CampaignMechanics.TORCustomSettlement.Component;
using TOR_Core.CampaignMechanics.UniqueSpawns;
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
        public static bool IsUniqueSpawn(this MobileParty party)
        {
            return party.PartyComponent is UniqueSpawnPartyComponent;
        }
        public static UniqueSpawnPartyComponent GetUniqueSpawnComponent(this MobileParty party)
        {
            return party.PartyComponent as UniqueSpawnPartyComponent;
        }

        public static MobilePartyExtendedInfo GetPartyInfo(this MobileParty party)
        {
            return ExtendedInfoManager.Instance.GetPartyInfoFor(party.StringId);
        }

        public static void AddBlessingToParty(this MobileParty party, string blessingId)
        {
            var model = Campaign.Current.Models.GetFaithModel();
            model?.AddBlessingToParty(party, blessingId);
        }

        //Sly : I wonder if this would be more efficient with out variables to allow us to check for a blessing, and if there is one then you get it back from the same method call rather than needing to fetch the info again and find the blessing details from it
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
            return TORCommon.FindSettlementsAroundPosition(party.Position.ToVec2(), threshold).Count > 0;
        }

        public static bool IsAffectedByCurse(this MobileParty party)
        {
            if (!party.IsLordParty) return false;

            if (party.IsMainParty)
            {
                if (party.LeaderHero.IsEnlisted())
                    return false;   // optional, might be better to make here a more deliberate check for enlisting lord check
            }

            // Mousillon and Sylvania cultures are immune to cursed sites
            if (party.LeaderHero?.Culture?.StringId == TORConstants.Cultures.MOUSILLON ||
                party.LeaderHero?.Culture?.StringId == TORConstants.Cultures.SYLVANIA)
            {
                return false;
            }

            var settlementFound = TORCommon.FindNearestSettlement(party, TORConstants.DEFAULT_CURSE_RADIUS, x => x.SettlementComponent is CursedSiteComponent);
            if (settlementFound == null) return false;

            if (settlementFound.SettlementComponent is not CursedSiteComponent cursedSite) return false;

            if (party.LeaderHero?.GetDominantReligion() == cursedSite.Religion) return false;

            return true;
        }

        public static List<ItemRosterElement> GetArtilleryItems(this MobileParty party)
        {
            List<ItemRosterElement> list = [.. party.ItemRoster.Where(x => x.EquipmentElement.Item.StringId.Contains("artillery")).ToList()];
            return list;
        }

        /// <summary>
        /// Uses the party's engineer role hero to calculate fieldable artillery if the hero is an Engineer or is tagged as capable of placing it.
        /// </summary>
        public static int GetMaxNumberOfArtillery(this MobileParty party)
        {
            if (!party.IsLordParty) return 0;

            if (party == MobileParty.MainParty)
            {
                //a game save occurring after the player is taken prisoner may end up with LeaderHero not yet being null, but no heroes will be in the party so MaxBy finds an empty enumerable
                if (party.LeaderHero == null || Hero.MainHero.IsPrisoner) return 0;


                var engineer = party.EffectiveEngineer;
                if (!engineer.IsArtilleryHero()) return 0;

                var engineering = party.EffectiveEngineer.GetSkillValue(DefaultSkills.Engineering);
                return (int)(engineering / 50);
            }
            else
            {
                return 3;
            }
        }

        public static List<Hero> GetMemberHeroes(this MobileParty party)
        {
            List<Hero> heroes = [];
            var roster = party?.MemberRoster?.GetTroopRoster();
            if (roster != null)
            {
                foreach (var member in roster)
                {
                    if (member.Character?.HeroObject != null)
                    {
                        heroes.Add(member.Character.HeroObject);
                    }
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
            foreach (var hero in party.GetMemberHeroes())
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

        public static bool HasAnvilOfDoom(this MobileParty party)
        {
            var roster = party.ItemRoster;

            if (roster.Any(elem => elem.EquipmentElement.Item.StringId == "tor_dw_anvil_of_doom"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a party's distance to arbitrary locations in elven forests is sufficiently close to consider the party "inside" the forest.
        /// </summary>
        /// <remarks>
        /// Uses 2 circles to cover Athel Lorn and 1 circle for Laurelorn. Originally intended for dryad binding by spellsingers and enchanting ingredients.
        /// </remarks>
        public static bool InElfForest(this MobileParty party)
        {
            if (party.InAthelLoren() || party.InLaurelorn())
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if a party is inside the Athel Lorn forests (asrai territory).
        /// </summary>
        /// <remarks>
        /// Uses 2 locations to approximate the boundaries via radii.
        /// </remarks>
        public static bool InAthelLoren(this MobileParty party)
        {
            var position = party.Position;

            //Points and radii squared defining approximate boundary for Athel Loren
            var ALNorthHalf = new Vec2(1209.99f, 916.42f); //Athel Lorn north half : midpoint (1209.99, 916.42); r^2 = 1255.75
            var ALSouthHalf = new Vec2(1207.75f, 796.74f); //Athel Lorn south half : midpoint (1207.75, 796.74); r^2 = 1419.44
            //Sly : part of athel loren is missed by the circles which need additional ones
            if (position.DistanceSquared(ALNorthHalf) < 4384.25 || position.DistanceSquared(ALSouthHalf) < 5482.29)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if a party is inside the Laurelorn forest (eonir territory).
        /// </summary>
        public static bool InLaurelorn(this MobileParty party)
        {
            var position = party.Position;

            //Point and radius squared defining approximate boundary for Laurelorn forest
            //Sly : this needs to be updated to include the peninsula to the NW of Tor Lithanel as it's needed to grant enchanting materials for fights near the forest.
            var LLAll = new Vec2(1259.60f, 1317.35f); //Laurelorn : midpoint (1259.60, 1317.35); r^2 = 672.65

            if (position.DistanceSquared(LLAll) < 1877.1218)
            {
                return true;
            }
            return false;
        }
    }
}