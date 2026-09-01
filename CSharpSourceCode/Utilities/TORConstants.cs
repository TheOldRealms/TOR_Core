using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.Utilities
{
    public static class TORConstants
    {
        public const int TotalNumberOfUniqueLoadingScreenImages = 12;
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
            public const string GREENSKIN_BANDIT = "greenskin_bandit";
            public const string GOBLIN_BANDIT = "looters";
            public const string CHAOS_CULTIST = "forest_bandits";
            public const string EMPIRE_DESERTERS = "mountain_bandits";

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
            public const string WASTELAND = "wasteland";
            
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
            public const string REAVAZ = "reavaz";


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

        public readonly struct CharacterAttributes
        {
            //Comments are examples, not exhaustive lists.

            //Player specific
            public const string PLAYER_RUNESMITH = "PlayerRunesmith";//quest completed for career tier 2
            public const string PLAYER_RUNELORD = "PlayerRunelord";//quest completed for career tier 3
            //These are quest related attributes. If the career tier is locked behind completing the quest, are these not fancy names for their CareerTier equivalents?
            public const string PLAYER_ORC_BOSS = "PlayerOrcBoss";
            public const string PLAYER_ORC_BIG_BOSS = "PlayerOrcBigBoss";
            public const string PLAYER_ORC_SHAMAN_TIER_2 = "PlayerOrcShamanTier2";
            public const string PLAYER_ORC_SHAMAN_TIER_3 = "PlayerOrcShamanTier3";
            public const string GIFT_OF_NURGLE = "GiftOfNurgle";

            //Ability system
            public const string ABILITY_USER = "AbilityUser";
            public const string CAN_PLACE_ARTILLERY = "CanPlaceArtillery";

            //Special hero types - generally used for detecting specific types of npc heroes
            public const string SPELLCASTER = "SpellCaster";
            public const string RUNESMITH = "Runesmith";
            public const string ILL_FATED = "IllFated";//moussilon knights, similar to grail knights
            public const string LEGENDARY_LORD = "LegendaryLord";//Specific famous heroes who need plot armour
            public const string WARBOSS = "Warboss";//greenskin mechanic
            public const string PRIEST_TRAINER = "PriestTrainer";//Blesses player, teaches equipment blessing, etc.
            public const string SKILL_TRAINER = "SkillTrainer";
            public const string ENGINEER_COMPANION = "EngineerCompanion";
            public const string NECROMANCER = "Necromancer";
            public const string VAMPIRE = "Vampire";//what is this actually doing?
            public const string WIGHT_KING = "WightKing";
            public const string BRETONNIAN_KNIGHT = "BretonnianKnight";
            public const string AI_COMPANION = "AiCompanion";//is this still a thing?
            public const string SHAMAN_BOSS = "ShamanBoss";
            public const string GLADE_CAPTAIN = "GladeCaptain";
            public const string BIG_BOSS = "BigBoss";
            public const string EVERCHOSEN = "Everchosen";//chaos archaon
            public const string BLOOD_DRAGON = "BloodDragon";//they are a subtype of vampire that can't be differentiated by race
            public const string SLAYER_LORD = "SlayerLord";
            public const string BERGERAC = "Bergerac";//bretonnian minor clan, but why does this pass through an attribute rather than making use of native code that handles recruitment for minor clans who are supposed to pull from their special troops?
            public const string PEASANT_KNIGHT = "PeasantKnight";//same as bergerac
            public const string BRASS_KEEP = "BrassKeep";

            //Agent-relevant
            public const string TOUGH = "Tough";
            public const string BULWARK = "Bulwark";
            public const string BULWARK_2 = "Bulwark2";
            public const string BULWARK_3 = "Bulwark3";
            public const string ETHEREAL = "Ethereal";
            public const string ETHEREAL_2 = "Ethereal2";
            public const string MONSTER_SLAYER = "MonsterSlayer";
            public const string MONSTER_SLAYER_2 = "MonsterSlayer2";
            public const string PIERCING = "Piercing";
            public const string PIERCING_2 = "Piercing2";
            public const string POISONOUS = "Poisonous";
            public const string POISONOUS_2 = "Poisonous2";
            public const string REGENERATION = "Regeneration";
            public const string REGENERATION_2 = "Regeneration2";
            public const string REGENERATION_3 = "Regeneration3";
            public const string SWIFT = "Swift";
            public const string SWIFT_2 = "Swift2";
            public const string SWIFT_3 = "Swift3";
            public const string UNDEAD_SLAYER = "UndeadSlayer";
            public const string UNDEAD_SLAYER_2 = "UndeadSlayer2";
            public const string UNBREAKABLE = "Unbreakable";
            public const string TUBTHUMPING = "Tubthumping";
            public const string SURVIVOR = "Survivor";
            public const string UNSTOPPABLE = "Unstoppable";
            public const string HORSE_STEADY = "HorseSteady";
            public const string HORSE_LINK = "HorseLink";
            public const string SHIELD_PENETRATION = "ShieldPenetration";
            public const string THE_HUNGER = "TheHunger";
            public const string SLICE = "Slice";
            public const string MONSTER_ATTACK = "MonsterAttack";
            public const string EXPENDABLE = "Expendable";
            public const string FRENZY = "Frenzy";
            public const string DEADEYE = "Deadeye";
            public const string CRUSH_THROUGH = "CrushThrough";
            public const string BRUTE = "Brute";
            public const string CLEAR_BLOOD_BURST = "ClearBloodBurst";
            public const string IMMORTALITY = "Immortality";
            public const string KILLING_BLOW = "KillingBlow";

            //Priests
            //These attributes may be applied to both player and npc. They are used to track prayer-using followers 
            public const string PRIEST_LADY = "PriestLady";//damsels
            public const string PRIEST = "Priest";//generic, unclear name, old attribute, specific to warrior priests as it was used to mark the player who had that career in the past
            public const string PRIEST_SIGMAR = "PriestSigmar";
            public const string PRIEST_ULRIC = "PriestUlric";

            //Careers
            //Seals and other troop-applied career modifiers are not present here.
            //Attributes specific to a single career perk are not currently here.

            public const string CAREER_TIER_1 = "CareerTier1";//granted by fulfilling the unlock conditions
            public const string CAREER_TIER_2 = "CareerTier2";//granted by fulfilling the unlock conditions
            public const string CAREER_TIER_3 = "CareerTier3";//granted by fulfilling the unlock conditions
            public const string WINDS_LINK = "WindsLink";//orc shaman, spellsinger
            public const string WINDS_DEATH_LINK = "WindsDeathLink";//orc shaman
            public const string ACCUSATION_MARK = "AccusationMark";//witchhunter
            public const string FELLFANG_MARK = "FellfangMark";//greylord
            public const string NECROMANCER_CHAMPION = "NecromancerChampion";//necromancer
            public const string IMPENETRABLE = "Impenetrable";//ironbreaker
            public const string DOOM_SEEKING = "DoomSeeking";//slayer
            public const string ARCANE_DMG = "Arcane_Dmg";//Magister, update for nomenclature
            public const string KNIGHTLY_STRIKE = "KnightlyStrike";
            public const string EXTORSION = "Extorsion";

            //Traits
            public const string THORNS = "Thorns";//damage reflection

            //Custom Events
            public const string DEFEATED_VITTORIO = "DefeatedVittorio";//granted upon winning a duel

            //Asrai
            public const string WE_WANDERER_SYMBOL = "WEWandererSymbol";
            public const string WE_ARIEL_SYMBOL = "WEArielSymbol";
            public const string WE_DURTHU_SYMBOL = "WEDurthuSymbol";
            public const string WE_KITHBAND_SYMBOL = "WEKithbandSymbol";
            public const string WE_ORION_SYMBOL = "WEOrionSymbol";
            public const string WE_TREEKIN_SYMBOL = "WETreekinSymbol";
            public const string WE_WARDANCER_SYMBOL = "WEWardancerSymbol";

            //Dawi player
            //grudges are from character creation options
            public const string ELF_GRUDGE = "ElfGrudge";
            public const string GREENSKIN_GRUDGE = "GreenskinGrudge";
            public const string HUMAN_GRUDGE = "HumanGrudge";
            public const string SKAVEN_GRUDGE = "SkavenGrudge";
            public const string UNDEAD_GRUDGE = "UndeadGrudge";

            //Dawi guild tiers
            public const string GUILD_BREWERS_1 = "GuildBrewersI";
            public const string GUILD_BREWERS_2 = "GuildBrewersII";
            public const string GUILD_BREWERS_3 = "GuildBrewersIII";
            public const string GUILD_ENGINEERS_1 = "GuildEngineersI";
            public const string GUILD_ENGINEERS_2 = "GuildEngineersII";
            public const string GUILD_ENGINEERS_3 = "GuildEngineersIII";
            public const string GUILD_MINERS_1 = "GuildMinersI";
            public const string GUILD_MINERS_2 = "GuildMinersII";
            public const string GUILD_MINERS_3 = "GuildMinersIII";
            public const string GUILD_WARRIORS_1 = "GuildWarriorsI";
            public const string GUILD_WARRIORS_2 = "GuildWarriorsII";
            public const string GUILD_WARRIORS_3 = "GuildWarriorsIII";
            public const string GUILD_RUNESMITH_1 = "GuildRunesmithsI";
            public const string GUILD_RUNESMITH_2 = "GuildRunesmithsII";
            public const string GUILD_RUNESMITH_3 = "GuildRunesmithsIII";

            //Empire
            public const string PRESTIGE_NOBLE = "PrestigeNoble";//trades related to Prestige
            
            //Eonir
            public const string DRUCHII_ENVOY = "DruchiiEnvoy";//druchii npc
            public const string ASUR_ENVOY = "AsurEnvoy";//high elf npc
            public const string EMPIRE_ENVOY = "EmpireEnvoy";//empire npc
            public const string SPELLSINGER_ENVOY = "SpellsingerEnvoy";//spellsinger npc

            //Greenskin
            public const string WAAAAGH_0 = "Waaagh0";
            public const string WAAAAGH_1 = "Waaagh1";
            public const string WAAAAGH_2 = "Waaagh2";
            public const string WAAAAGH_3 = "Waaagh3";
            
            //Characters? generic?
            public const string DWARF_MINER = "DwarfMiner";
            public const string DWARF_GUN = "DwarfGun";
            public const string DWARF_WARRIOR = "DwarfWarrior";
            public const string IRONBREAKER = "Ironbreaker";
            public const string ARTILLERY_CREW = "ArtilleryCrew";
            public const string CREW_2 = "CrewII";
            public const string CREW_3 = "CrewIII";
            public const string KNIGHTLY = "Knightly";
            public const string MONSTROUS = "Monstrous";
            public const string TREE_SPIRIT = "TreeSpirit";//this detection should instead move to the race and LHM gets cleaned up to be specific to a single type, but dryads would be unaccounted for which is why it remains for the moment. I don't think keeping all of the giant races on a single race entry is saving us enough memory to be worth other headaches.
            public const string UNDEAD = "Undead";//is race detection faster than attribute lookup? It should be, particularly if we change to caching the race mapping and just performing integer comparisons. This is used for so many other things that there are hidden risks with a straight swap.

            //Other?
            public const string HAS_ANIMATION_TRIGGERED_EFFECTS = "HasAnimationTriggeredEffects";//treemen and trolls
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