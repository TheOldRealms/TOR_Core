using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    /// <summary>
    /// Shared helper methods for diplomacy calculations (war, alliance, trade).
    /// Centralizes distance calculations and personality trait modifiers.
    /// </summary>
    public static class DiplomacyHelpers
    {
        // Distance thresholds for different diplomatic actions
        public const float MaxWarDistance = 300f;
        public const float MaxAllianceDistance = 500f;
        public const float MaxTradeDistance = 600f;

        // Personality trait modifier step (trait level -2 to +2 maps to 0.25 to 1.75)
        private const float TraitModifierStep = 0.375f;

        /// <summary>
        /// Gets the approximate distance between two kingdoms based on their mid settlements.
        /// </summary>
        public static float GetKingdomDistance(Kingdom kingdom1, Kingdom kingdom2)
        {
            if (kingdom1?.FactionMidSettlement == null || kingdom2?.FactionMidSettlement == null)
                return float.MaxValue;

            return kingdom1.FactionMidSettlement.Position.Distance(kingdom2.FactionMidSettlement.Position);
        }

        /// <summary>
        /// Checks if two kingdoms are within war declaration distance.
        /// </summary>
        public static bool IsWithinWarDistance(Kingdom kingdom1, Kingdom kingdom2)
        {
            return GetKingdomDistance(kingdom1, kingdom2) <= MaxWarDistance;
        }

        /// <summary>
        /// Checks if two kingdoms are within alliance distance.
        /// </summary>
        public static bool IsWithinAllianceDistance(Kingdom kingdom1, Kingdom kingdom2)
        {
            return GetKingdomDistance(kingdom1, kingdom2) <= MaxAllianceDistance;
        }

        /// <summary>
        /// Checks if two kingdoms are within trade agreement distance.
        /// </summary>
        public static bool IsWithinTradeDistance(Kingdom kingdom1, Kingdom kingdom2)
        {
            return GetKingdomDistance(kingdom1, kingdom2) <= MaxTradeDistance;
        }

        /// <summary>
        /// Gets a trait-based modifier for diplomacy scoring.
        /// Maps trait level (-2 to +2) to multiplier (0.25 to 1.75).
        /// Higher trait level = higher modifier.
        /// </summary>
        public static float GetTraitModifier(Hero leader, TraitObject trait)
        {
            if (leader == null || trait == null) return 1f;
            int traitLevel = leader.GetTraitLevel(trait);
            return 1f + (traitLevel * TraitModifierStep);
        }

        /// <summary>
        /// Gets an inverse trait-based modifier for diplomacy scoring.
        /// Maps trait level (-2 to +2) to multiplier (1.75 to 0.25).
        /// Higher trait level = lower modifier.
        /// </summary>
        public static float GetInverseTraitModifier(Hero leader, TraitObject trait)
        {
            if (leader == null || trait == null) return 1f;
            int traitLevel = leader.GetTraitLevel(trait);
            return 1f - (traitLevel * TraitModifierStep);
        }

        /// <summary>
        /// Gets the dominant pantheon for a kingdom based on its leader's religion or culture.
        /// </summary>
        public static Pantheon GetKingdomPantheon(Kingdom kingdom)
        {
            if (kingdom == null) return Pantheon.Human;

            var leaderReligion = kingdom.Leader?.GetDominantReligion();
            if (leaderReligion != null)
                return leaderReligion.Pantheon;

            return ReligionObjectHelper.GetPantheon(kingdom.Culture?.StringId);
        }

        /// <summary>
        /// Checks if two kingdoms share the same culture.
        /// </summary>
        public static bool AreSameCulture(Kingdom kingdom1, Kingdom kingdom2)
        {
            var culture1 = kingdom1?.Culture?.StringId;
            var culture2 = kingdom2?.Culture?.StringId;
            return !string.IsNullOrEmpty(culture1) && culture1 == culture2;
        }

        /// <summary>
        /// Gets the culture compatibility between two kingdoms.
        /// Returns: -1 (hostile) to +1 (friendly), 0 = neutral
        /// </summary>
        public static float GetCultureCompatibility(Kingdom kingdom1, Kingdom kingdom2)
        {
            var culture1 = kingdom1?.Culture?.StringId;
            var culture2 = kingdom2?.Culture?.StringId;

            if (string.IsNullOrEmpty(culture1) || string.IsNullOrEmpty(culture2))
                return 0f;

            return ReligionObjectHelper.CalculateCultureCompatibility(culture1, culture2);
        }

        /// <summary>
        /// Checks if two kingdoms share the same religion.
        /// </summary>
        public static bool AreSameReligion(Kingdom kingdom1, Kingdom kingdom2)
        {
            var religion1 = kingdom1?.Leader?.GetDominantReligion();
            var religion2 = kingdom2?.Leader?.GetDominantReligion();
            return religion1 != null && religion1 == religion2;
        }

        /// <summary>
        /// Checks if two kingdoms have hostile religions.
        /// </summary>
        public static bool AreReligionsHostile(Kingdom kingdom1, Kingdom kingdom2)
        {
            var religion1 = kingdom1?.Leader?.GetDominantReligion();
            var religion2 = kingdom2?.Leader?.GetDominantReligion();

            if (religion1 == null || religion2 == null)
                return false;

            return religion1.HostileReligions != null && religion1.HostileReligions.Contains(religion2);
        }

        /// <summary>
        /// Gets the religion compatibility between two kingdoms.
        /// Returns: -1 (hostile) to +1 (friendly), 0 = neutral
        /// </summary>
        public static float GetReligionCompatibility(Kingdom kingdom1, Kingdom kingdom2)
        {
            var religion1 = kingdom1?.Leader?.GetDominantReligion();
            var religion2 = kingdom2?.Leader?.GetDominantReligion();

            if (religion1 == null || religion2 == null)
                return 0f;

            return ReligionObjectHelper.CalculateReligionCompatibility(religion1, religion2);
        }

        /// <summary>
        /// Gets the pantheon compatibility between two kingdoms.
        /// Returns: -1 (hostile) to +1 (friendly), 0 = neutral
        /// </summary>
        public static float GetPantheonCompatibility(Kingdom kingdom1, Kingdom kingdom2)
        {
            var religion1 = kingdom1?.Leader?.GetDominantReligion();
            var religion2 = kingdom2?.Leader?.GetDominantReligion();

            if (religion1 == null || religion2 == null)
                return 0f;

            return ReligionObjectHelper.GetPantheonCompatibility(religion1.Pantheon, religion2.Pantheon);
        }

        /// <summary>
        /// Calculates the average relation between a clan's members and a kingdom's clan leaders.
        /// </summary>
        public static float CalculateClanToKingdomRelation(Clan clan, Kingdom kingdom)
        {
            if (clan == null || kingdom == null)
                return 0f;

            var clanHeroes = clan.Heroes.Where(h => h.IsAlive && !h.IsChild).ToList();
            var kingdomLeaders = kingdom.Clans
                .Where(c => c.Leader != null && c.Leader.IsAlive)
                .Select(c => c.Leader)
                .ToList();

            if (!clanHeroes.Any() || !kingdomLeaders.Any())
                return 0f;

            float totalRelation = 0f;
            int count = 0;

            foreach (var clanHero in clanHeroes)
            {
                foreach (var kingdomLeader in kingdomLeaders)
                {
                    totalRelation += clanHero.GetRelation(kingdomLeader);
                    count++;
                }
            }

            return count > 0 ? totalRelation / count : 0f;
        }

        /// <summary>
        /// Calculates the average relation between two kingdoms based on all clan leaders.
        /// </summary>
        public static float CalculateKingdomToKingdomRelation(Kingdom kingdom1, Kingdom kingdom2)
        {
            if (kingdom1 == null || kingdom2 == null)
                return 0f;

            var leaders1 = kingdom1.Clans
                .Where(c => c.Leader != null && c.Leader.IsAlive)
                .Select(c => c.Leader)
                .ToList();

            var leaders2 = kingdom2.Clans
                .Where(c => c.Leader != null && c.Leader.IsAlive)
                .Select(c => c.Leader)
                .ToList();

            if (!leaders1.Any() || !leaders2.Any())
                return 0f;

            float totalRelation = 0f;
            int count = 0;

            foreach (var leader1 in leaders1)
            {
                foreach (var leader2 in leaders2)
                {
                    totalRelation += leader1.GetRelation(leader2);
                    count++;
                }
            }

            return count > 0 ? totalRelation / count : 0f;
        }

        /// <summary>
        /// Gets the lore-based rivalry level between two kingdoms.
        /// Returns: 0 = no rivalry, 1.0 = standard rivalry, 1.5 = major rivalry.
        /// Rivalries are symmetric (A vs B = B vs A).
        /// Used for war scoring (bonus), peace scoring (penalty), alliance scoring (penalty), trade scoring (penalty).
        /// </summary>
        public static float GetLoreRivalryLevel(Kingdom kingdom1, Kingdom kingdom2)
        {
            if (kingdom1 == null || kingdom2 == null)
                return 0f;

            var culture1 = kingdom1.Culture?.StringId;
            var culture2 = kingdom2.Culture?.StringId;
            var faction1 = kingdom1.StringId;
            var faction2 = kingdom2.StringId;

            // === MAJOR RIVALRIES (1.5) ===

            // War of the Beard - Dwarfs vs Wood Elves (ancient grudge)
            // Includes both Asrai culture and Laurelorn faction
            if ((culture1 == TORConstants.Cultures.DAWI && culture2 == TORConstants.Cultures.ASRAI) ||
                (culture1 == TORConstants.Cultures.ASRAI && culture2 == TORConstants.Cultures.DAWI))
            {
                return 1.5f;
            }

            // Dawi vs Laurelorn specifically (Wood Elves faction)
            if ((culture1 == TORConstants.Cultures.DAWI && faction2 == TORConstants.Factions.LAURELORN) ||
                (faction1 == TORConstants.Factions.LAURELORN && culture2 == TORConstants.Cultures.DAWI))
            {
                return 1.5f;
            }

            // Middenland vs Reikland - Ulric vs Sigmar theological conflict
            if ((faction1 == TORConstants.Factions.MIDDENLAND && faction2 == TORConstants.Factions.REIKLAND) ||
                (faction1 == TORConstants.Factions.REIKLAND && faction2 == TORConstants.Factions.MIDDENLAND))
            {
                return 1.5f;
            }

            // === STANDARD RIVALRIES (1.0) ===

            // Nordland vs Laurelorn - territorial forest dispute
            if ((faction1 == TORConstants.Factions.NORDLAND && faction2 == TORConstants.Factions.LAURELORN) ||
                (faction1 == TORConstants.Factions.LAURELORN && faction2 == TORConstants.Factions.NORDLAND))
            {
                return 1.0f;
            }

            // Grey Mountains border disputes - Bretonnian vs Empire mountain passes
            if (IsGreyMountainsRivalry(faction1, faction2))
            {
                return 1.0f;
            }

            // Gisoreux vs Wasteland (Marienburg) - trade/border rivalry
            if ((faction1 == TORConstants.Factions.GISOREUX && faction2 == TORConstants.Factions.WASTELAND) ||
                (faction1 == TORConstants.Factions.WASTELAND && faction2 == TORConstants.Factions.GISOREUX))
            {
                return 1.0f;
            }

            // Montfort vs non-humans - xenophobic duchy
            if (IsMontfortXenophobiaRivalry(faction1, faction2, kingdom1, kingdom2))
            {
                return 1.0f;
            }

            return 0f;
        }

        /// <summary>
        /// Checks if the two factions are part of the Grey Mountains border rivalry.
        /// Parravon and Montfort (Bretonnia) vs Wissenland and Reikland (Empire).
        /// </summary>
        private static bool IsGreyMountainsRivalry(string faction1, string faction2)
        {
            bool isBretonnianMountain1 = faction1 == TORConstants.Factions.PARRAVON || faction1 == TORConstants.Factions.MONTFORT;
            bool isBretonnianMountain2 = faction2 == TORConstants.Factions.PARRAVON || faction2 == TORConstants.Factions.MONTFORT;
            bool isEmpireMountain1 = faction1 == TORConstants.Factions.WISSENLAND || faction1 == TORConstants.Factions.REIKLAND;
            bool isEmpireMountain2 = faction2 == TORConstants.Factions.WISSENLAND || faction2 == TORConstants.Factions.REIKLAND;

            return (isBretonnianMountain1 && isEmpireMountain2) || (isEmpireMountain1 && isBretonnianMountain2);
        }

        /// <summary>
        /// Checks if Montfort's xenophobia creates a rivalry with a non-human faction.
        /// </summary>
        private static bool IsMontfortXenophobiaRivalry(string faction1, string faction2, Kingdom kingdom1, Kingdom kingdom2)
        {
            if (faction1 == TORConstants.Factions.MONTFORT && kingdom2?.Leader?.CharacterObject != null)
            {
                return !kingdom2.Leader.CharacterObject.IsHuman();
            }
            if (faction2 == TORConstants.Factions.MONTFORT && kingdom1?.Leader?.CharacterObject != null)
            {
                return !kingdom1.Leader.CharacterObject.IsHuman();
            }
            return false;
        }

        /// <summary>
        /// Gets the lore-based affinity level between two kingdoms.
        /// Returns: 0 = no special affinity, 0.75 = moderate affinity, 1.0 = good affinity, 1.5 = strong affinity.
        /// Affinities are symmetric (A with B = B with A).
        /// Used for war scoring (penalty), peace scoring (bonus), alliance scoring (bonus), trade scoring (bonus).
        /// </summary>
        public static float GetLoreAffinityLevel(Kingdom kingdom1, Kingdom kingdom2)
        {
            if (kingdom1 == null || kingdom2 == null)
                return 0f;

            var culture1 = kingdom1.Culture?.StringId;
            var culture2 = kingdom2.Culture?.StringId;
            var faction1 = kingdom1.StringId;
            var faction2 = kingdom2.StringId;

            // === STRONG AFFINITIES (1.5) ===

            // Dwarfs + Empire - Strong historical alliance against Greenskins and Chaos
            if ((culture1 == TORConstants.Cultures.DAWI && culture2 == TORConstants.Cultures.EMPIRE) ||
                (culture1 == TORConstants.Cultures.EMPIRE && culture2 == TORConstants.Cultures.DAWI))
            {
                return 1.5f;
            }

            // === GOOD AFFINITIES (1.0) ===

            // Eonir (Laurelorn Wood Elves) - Natural traders with most factions
            // Exceptions: Dwarfs (War of the Beard), Nordland (territorial rivalry)
            if (culture1 == TORConstants.Cultures.EONIR || culture2 == TORConstants.Cultures.EONIR)
            {
                var otherCulture = culture1 == TORConstants.Cultures.EONIR ? culture2 : culture1;
                var otherFaction = culture1 == TORConstants.Cultures.EONIR ? faction2 : faction1;

                // No affinity with Dwarfs (War of the Beard)
                if (otherCulture == TORConstants.Cultures.DAWI)
                    return 0f;

                // No affinity with Nordland (territorial rivalry)
                if (otherFaction == TORConstants.Factions.NORDLAND)
                    return 0f;

                return 1.0f;
            }

            // === MODERATE AFFINITIES (0.75) ===

            // Bretonnia + Asrai (Wood Elves) - Complex but peaceful coexistence with Athel Loren
            if ((culture1 == TORConstants.Cultures.BRETONNIA && culture2 == TORConstants.Cultures.ASRAI) ||
                (culture1 == TORConstants.Cultures.ASRAI && culture2 == TORConstants.Cultures.BRETONNIA))
            {
                return 0.75f;
            }

            // === SMALL AFFINITIES (0.5) ===

            // Montfort prefers humans (xenophobic but friendly to fellow humans)
            if (IsMontfortHumanAffinity(faction1, faction2, kingdom1, kingdom2))
            {
                return 0.5f;
            }

            return 0f;
        }

        /// <summary>
        /// Checks if Montfort has affinity with another human faction.
        /// </summary>
        private static bool IsMontfortHumanAffinity(string faction1, string faction2, Kingdom kingdom1, Kingdom kingdom2)
        {
            if (faction1 == TORConstants.Factions.MONTFORT && kingdom2?.Leader?.CharacterObject != null)
            {
                return kingdom2.Leader.CharacterObject.IsHuman();
            }
            if (faction2 == TORConstants.Factions.MONTFORT && kingdom1?.Leader?.CharacterObject != null)
            {
                return kingdom1.Leader.CharacterObject.IsHuman();
            }
            return false;
        }
    }
}
