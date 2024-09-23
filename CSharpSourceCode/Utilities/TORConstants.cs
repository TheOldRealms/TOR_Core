using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOR_Core.Utilities
{
    public static class TORConstants
    {
        public const int TotalNumberOfUniqueLoadingScreenImages = 11;
        public const int MIRACLE_CHANCE = 5;
        public const int MAXIMUM_DEVOTION_LEVEL = 99;
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

        public readonly struct Cultures
        {
            public const string EMPIRE = "empire";
            public const string HERRIMAULT = "desert_bandits";
            public const string BRETONNIA = "vlandia";
            public const string SYLVANIA = "khuzait";
            public const string MOUSILLON = "mousillon";
            public const string ASRAI = "battania";
            public const string DRUCHII =  "druchii";
            public const string BEASTMEN =  "steppe_bandits";
            public const string CHAOS = "chaos_culture";
            public const string EONIR = "eonir";
        }
    }
}
