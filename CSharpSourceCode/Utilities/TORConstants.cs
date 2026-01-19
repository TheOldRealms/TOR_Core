using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.Utilities
{
    public static class TORConstants
    {
        public const int TotalNumberOfUniqueLoadingScreenImages = 12;
        public const int MIRACLE_CHANCE = 5;
        public const int MAXIMUM_DEVOTION_LEVEL = 100;
        public const int DEVOTED_TRESHOLD = 50;
        public const int FANATIC_TRESHOLD = 75;
        public const int DEFAULT_BLESSING_DURATION = 72;
        public const int DEFAULT_WARDING_DURATION = 72;
        public const int DEFAULT_PRAYING_DEVOTION_INCREASE = 5;
        public const int DEFAULT_WARDING_DEVOTION_INCREASE = 5;
        public const int DEFAULT_CURSE_WOUND_STRENGTH = 1;
        public const int DEFAULT_PRAYING_FAITH_XP = 10;
        public const int DEFAULT_CURSE_RADIUS = 25;
        public const int BOUNTY_QUEST_CHANCE = 25;
        public const int SKELETON_VOICE_INDEX_START = 24;
        public const int SKELETON_VOICES_COUNT = 1;
        public const int VAMPIRE_VOICE_INDEX_START = 25;
        public const int VAMPIRE_VOICES_COUNT = 2;
        public const int EMPIRE_VOICE_INDEX_START = 27;
        public const int EMPIRE_VOICES_COUNT = 2;
        public const int BRETONNIA_VOICE_INDEX_START = 29;
        public const int BRETONNIA_VOICES_COUNT = 3;
        public const int ELVEN_VOICE_INDEX_START = 32;
        public const int ELVEN_VOICES_COUNT = 1;
        public const int TREESPIRIT_VOICE_INDEX_START = 33;
        public const int TREESPIRIT_VOICES_COUNT = 1;
        public const float SHRINE_PRAYING_DURATION = 6f;

        public readonly struct Cultures
        {
            public const string EMPIRE = "empire";
            public const string HERRIMAULT = "desert_bandits";
            public const string BRETONNIA = "vlandia";
            public const string SYLVANIA = "khuzait";
            public const string MOUSILLON = "mousillon";
            public const string ASRAI = "battania";
            public const string DRUCHII = "druchii";
            public const string BEASTMEN = "steppe_bandits";
            public const string CHAOS = "chaos_culture";
            public const string EONIR = "eonir";
            public const string DAWI = "sturgia";
            public const string GREENSKIN = "aserai";

            public static readonly List<string> All =
            [
                EMPIRE,BRETONNIA,SYLVANIA,MOUSILLON,ASRAI,EONIR,DAWI,GREENSKIN
            ];
        }

        public readonly struct Factions
        {
            // Empire Provinces
            public const string REIKLAND = "reikland";
            public const string MIDDENLAND = "middenland";
            public const string OSTLAND = "ostland";
            public const string OSTERMARK = "ostermark";
            public const string STIRLAND = "stirland";
            public const string HOCHLAND = "hochland";
            public const string AVERLAND = "averland";
            public const string WISSENLAND = "wissenland";
            public const string TALABECLAND = "talabecland";
            public const string NORDLAND = "nordland";
            public const string MOOT = "moot";

            // Bretonnia Duchies
            public const string COURONNE = "couronne";
            public const string AQUITAINE = "aquitaine";
            public const string ARTOIS = "artois";
            public const string BORDELEAUX = "bordeleaux";
            public const string GISOREUX = "gisoreux";
            public const string MONTFORT = "montfort";
            public const string PARRAVON = "parravon";
            public const string QUENELLES = "quenelles";
            public const string CARCASSONNE = "carcassonne";
            public const string BASTONNE = "bastonne";
            public const string BRIONNE = "brionne";
            public const string ANGUILLE = "anguille";
            public const string LYONESSE = "lyonesse";

            // Vampire Counts
            public const string SYLVANIA = "sylvania";
            public const string MOUSILLON = "mousillon";
            public const string NECRACHS = "necrachs";
            public const string BLOODDRAGONS = "blooddragons";

            // Dwarf Holds
            public const string KARAK_KADRIN = "karak_kadrin";
            public const string KARAK_NORN = "karak_norn";
            public const string KARAK_HIRN = "karak_hirn";
            public const string KARAK_IZOR = "karak_izor";
            public const string KARAK_AZGARAZ = "karak_azgaraz";
            public const string KARAK_KAFERKAMMAZ = "karak_kaferkammaz";
            public const string KARAK_ZIFLIN = "karak_ziflin";
            public const string KARAK_ZHUFBAR = "karak_zhufbar";
            public const string KARAK_GANTUK = "karak_gantuk";
            public const string KARAK_EKSFILAZ = "karak_eksfilaz";
            public const string KARAK_ANGAZHAR = "karak_angazhar";

            // Elf Kingdoms
            public const string ATHEL_LOREN = "athel_loren";
            public const string LAURELORN = "laurelorn";

            // Greenskin Tribes
            public const string BAD_AXES = "bad_axes";
            public const string BLACK_PIT = "black_pit";
            public const string BLACK_SUNZ = "black_sunz";
            public const string BLOODY_SPEARZ = "bloody_spearz";
            public const string BRASSKEEP = "brasskeep";
            public const string CROOKED_EYE = "crooked_eye";
            public const string DEFF_GRINDAZ = "deff_grindaz";
            public const string IRON_TRIBE = "iron_tribe";
            public const string MASSIF_CHOPPAS = "massif_choppas";
            public const string NECK_SNAPPERS = "neck_snappers";
            public const string RED_EYE = "red_eye";
            public const string SKULL_SMASHERZ = "skull_smasherz";

            // Other
            public const string REAVAZ = "reavaz";
            public const string WASTELAND = "wasteland";

            public static readonly List<string> AllEmpire =
            [
                REIKLAND, MIDDENLAND, OSTLAND, OSTERMARK, STIRLAND, HOCHLAND,
                AVERLAND, WISSENLAND, TALABECLAND, NORDLAND, MOOT
            ];

            public static readonly List<string> AllBretonnia =
            [
                COURONNE, AQUITAINE, ARTOIS, BORDELEAUX, GISOREUX, MONTFORT,
                PARRAVON, QUENELLES, CARCASSONNE, BASTONNE, BRIONNE, ANGUILLE, LYONESSE
            ];

            public static readonly List<string> AllVampire =
            [
                SYLVANIA, MOUSILLON, NECRACHS, BLOODDRAGONS
            ];

            public static readonly List<string> AllDwarfs =
            [
                KARAK_KADRIN, KARAK_NORN, KARAK_HIRN, KARAK_IZOR, KARAK_AZGARAZ,
                KARAK_KAFERKAMMAZ, KARAK_ZIFLIN, KARAK_ZHUFBAR, KARAK_GANTUK,
                KARAK_EKSFILAZ, KARAK_ANGAZHAR
            ];

            public static readonly List<string> AllElves =
            [
                ATHEL_LOREN, LAURELORN
            ];

            public static readonly List<string> AllGreenskins =
            [
                BAD_AXES, BLACK_PIT, BLACK_SUNZ, BLOODY_SPEARZ, BRASSKEEP,
                CROOKED_EYE, DEFF_GRINDAZ, IRON_TRIBE, MASSIF_CHOPPAS,
                NECK_SNAPPERS, RED_EYE, SKULL_SMASHERZ
            ];

            public static readonly List<string> All =
            [
                // Empire
                REIKLAND, MIDDENLAND, OSTLAND, OSTERMARK, STIRLAND, HOCHLAND,
                AVERLAND, WISSENLAND, TALABECLAND, NORDLAND, MOOT, WASTELAND,
                // Bretonnia
                COURONNE, AQUITAINE, ARTOIS, BORDELEAUX, GISOREUX, MONTFORT,
                PARRAVON, QUENELLES, CARCASSONNE, BASTONNE, BRIONNE, ANGUILLE, LYONESSE,
                // Vampires
                SYLVANIA, MOUSILLON, NECRACHS, BLOODDRAGONS,
                // Dwarfs
                KARAK_KADRIN, KARAK_NORN, KARAK_HIRN, KARAK_IZOR, KARAK_AZGARAZ,
                KARAK_KAFERKAMMAZ, KARAK_ZIFLIN, KARAK_ZHUFBAR, KARAK_GANTUK,
                KARAK_EKSFILAZ, KARAK_ANGAZHAR,
                // Elves
                ATHEL_LOREN, LAURELORN,
                // Greenskins
                BAD_AXES, BLACK_PIT, BLACK_SUNZ, BLOODY_SPEARZ, BRASSKEEP,
                CROOKED_EYE, DEFF_GRINDAZ, IRON_TRIBE, MASSIF_CHOPPAS,
                NECK_SNAPPERS, RED_EYE, SKULL_SMASHERZ, REAVAZ
 
            ];
        }

        /// <summary>
        /// Maps settlement prefix codes (e.g., "RL", "ST", "AV") to their rightful faction StringIds.
        /// Used for territorial integrity calculations in war scoring.
        /// </summary>
        public static class SettlementPrefixToFaction
        {
            private static readonly Dictionary<string, string> _prefixMap = new()
            {
                // Empire Provinces
                { "RL", Factions.REIKLAND },
                { "ML", Factions.MIDDENLAND },
                { "OL", Factions.OSTLAND },
                { "OM", Factions.OSTERMARK },
                { "ST", Factions.STIRLAND },
                { "HL", Factions.HOCHLAND },
                { "AV", Factions.AVERLAND },
                { "WI", Factions.WISSENLAND },
                { "TB", Factions.TALABECLAND },
                { "NL", Factions.NORDLAND },
                { "MT", Factions.MOOT },

                // Bretonnia Duchies
                { "CO", Factions.COURONNE },
                { "AQ", Factions.AQUITAINE },
                { "AS", Factions.ARTOIS },
                { "BL", Factions.BORDELEAUX },
                { "GX", Factions.GISOREUX },
                { "MO", Factions.MONTFORT },
                { "PA", Factions.PARRAVON },
                { "QU", Factions.QUENELLES },
                { "CC", Factions.CARCASSONNE },
                { "BA", Factions.BASTONNE },
                { "BE", Factions.BRIONNE },
                { "LA", Factions.ANGUILLE },
                { "LY", Factions.LYONESSE },

                // Vampire Counts
                { "SY", Factions.SYLVANIA },
                { "MS", Factions.MOUSILLON },
                // Note: Necrachs and Blooddragons share "BK" prefix with Brasskeep
                // BK1 = Blooddragons, BK2 = Brasskeep - cannot map cleanly

                // Dwarf Holds
                { "KK", Factions.KARAK_KADRIN },
                { "NO", Factions.KARAK_NORN },
                { "KH", Factions.KARAK_HIRN },
                { "KI", Factions.KARAK_IZOR },
                { "AZ", Factions.KARAK_AZGARAZ },
                { "KF", Factions.KARAK_KAFERKAMMAZ },
                { "ZI", Factions.KARAK_ZIFLIN },
                { "ZH", Factions.KARAK_ZHUFBAR },
                { "KG", Factions.KARAK_GANTUK },
                { "EZ", Factions.KARAK_EKSFILAZ },
                { "AN", Factions.KARAK_ANGAZHAR },

                // Elf Kingdoms
                { "AL", Factions.ATHEL_LOREN },
                { "LL", Factions.LAURELORN },

                // Greenskin Tribes
                { "BX", Factions.BAD_AXES },
                { "BP", Factions.BLACK_PIT },
                { "BZ", Factions.BLACK_SUNZ },
                { "BS", Factions.BLOODY_SPEARZ },
                { "BK", Factions.BRASSKEEP },
                { "CE", Factions.CROOKED_EYE },
                { "DG", Factions.DEFF_GRINDAZ },
                { "IT", Factions.IRON_TRIBE },
                { "MC", Factions.MASSIF_CHOPPAS },
                { "NS", Factions.NECK_SNAPPERS },
                { "RE", Factions.RED_EYE },
                { "SM", Factions.SKULL_SMASHERZ },

                // Other Greenskin
                { "RZ", Factions.REAVAZ },

                // Other
                { "WA", Factions.WASTELAND },
            };

            /// <summary>
            /// Gets the faction StringId that a settlement with the given prefix belongs to.
            /// </summary>
            /// <param name="settlementId">The settlement ID (e.g., "town_RL1", "castle_ST2")</param>
            /// <returns>The faction StringId that historically owns this settlement, or null if not found</returns>
            public static string GetRightfulOwner(string settlementId)
            {
                if (string.IsNullOrEmpty(settlementId))
                    return null;

                // Extract the prefix (two letters after "town_" or "castle_")
                // Format: town_XX# or castle_XX#
                string prefix = null;

                if (settlementId.StartsWith("town_") && settlementId.Length >= 7)
                {
                    prefix = settlementId.Substring(5, 2).ToUpper();
                }
                else if (settlementId.StartsWith("castle_") && settlementId.Length >= 9)
                {
                    prefix = settlementId.Substring(7, 2).ToUpper();
                }

                if (prefix != null && _prefixMap.TryGetValue(prefix, out string factionId))
                {
                    return factionId;
                }

                return null;
            }

            /// <summary>
            /// Checks if a settlement belongs originally to a specific faction based on its prefix.
            /// </summary>
            public static bool SettlementBelongsOriginallyToFaction(Settlement settlement, Kingdom faction)
            {
                var id = "";
                var kingdom="";
                if (settlement != null)
                {
                    id = settlement.StringId;
                }
                var rightfulOwner = GetRightfulOwner(id);
                return rightfulOwner != null && rightfulOwner == faction.StringId;
            }

            /// <summary>
            /// Gets the culture StringId for a faction StringId.
            /// </summary>
            public static string GetFactionCulture(string factionId)
            {
                if (string.IsNullOrEmpty(factionId))
                    return null;

                // Empire provinces
                if (Factions.AllEmpire.Contains(factionId))
                    return Cultures.EMPIRE;

                // Bretonnia duchies
                if (Factions.AllBretonnia.Contains(factionId))
                    return Cultures.BRETONNIA;

                // Vampire Counts - mixed cultures
                if (factionId == Factions.SYLVANIA)
                    return Cultures.SYLVANIA;
                if (factionId == Factions.MOUSILLON)
                    return Cultures.MOUSILLON;
                if (factionId == Factions.NECRACHS || factionId == Factions.BLOODDRAGONS)
                    return Cultures.SYLVANIA; // Generic vampire culture

                // Dwarf Holds
                if (Factions.AllDwarfs.Contains(factionId))
                    return Cultures.DAWI;

                // Elf Kingdoms
                if (factionId == Factions.ATHEL_LOREN)
                    return Cultures.ASRAI;
                if (factionId == Factions.LAURELORN)
                    return Cultures.EONIR;

                // Greenskin Tribes
                if (Factions.AllGreenskins.Contains(factionId) || factionId == Factions.REAVAZ)
                    return Cultures.GREENSKIN;

                // Wasteland
                if (factionId == Factions.WASTELAND)
                    return Cultures.EMPIRE;

                return null;
            }
        }

    }
}