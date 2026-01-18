using System.Collections.Generic;

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

    }
}