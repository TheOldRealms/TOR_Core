using System.Linq;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.AI
{
    /// <summary>
    /// Defines culture-specific battle AI personalities.
    /// Each culture has different aggression, defensive tendencies, and engagement behaviors.
    /// </summary>
    public static class TORCultureBattleSettings
    {
        public struct BattlePersonality
        {
            /// <summary>Multiplier for charge behavior weights. Higher = more aggressive charging.</summary>
            public float ChargeWeightMultiplier;

            /// <summary>Multiplier for defend behavior weights. Higher = more defensive.</summary>
            public float DefendWeightMultiplier;

            /// <summary>Multiplier for skirmish behavior weights. Higher = more kiting/retreating.</summary>
            public float SkirmishWeightMultiplier;

            /// <summary>Multiplier for engagement distance threshold. Lower = engage sooner.</summary>
            public float EngagementDistanceMultiplier;

            /// <summary>Resistance to retreat behavior. Higher = harder to make retreat.</summary>
            public float RetreatResistance;

            /// <summary>If true, formation prefers to hold position rather than chase (for slow infantry like Dwarfs).</summary>
            public bool PreferStandAndFight;

            /// <summary>Minimum floor for charge weight. Ensures aggressive cultures always have a base charge impulse.</summary>
            public float ChargeWeightMinimum;
        }

        /// <summary>
        /// Gets the battle personality for a given culture.
        /// </summary>
        public static BattlePersonality GetPersonality(string cultureId)
        {
            return cultureId switch
            {
                // Greenskins - WAAAGH! organized aggression
                TORConstants.Cultures.GREENSKIN or
                TORConstants.Cultures.GREENSKIN_BANDIT => new BattlePersonality
                {
                    ChargeWeightMultiplier = 3.0f,       // +200% charge weight - WAAAGH!
                    DefendWeightMultiplier = 0.3f,       // -70% defend - orcs don't wait
                    SkirmishWeightMultiplier = 0.2f,     // -80% skirmish - orcs charge in
                    EngagementDistanceMultiplier = 0.5f, // Engage 50% sooner
                    RetreatResistance = 1.5f,            // 50% harder to make retreat
                    PreferStandAndFight = false,
                    ChargeWeightMinimum = 0.5f           // Always have some charge impulse
                },

                // Bandits - desperate, undisciplined, YOLO charge
                "looters" or
                "forest_bandits" or
                "mountain_bandits" or
                "sea_raiders" => new BattlePersonality
                {
                    ChargeWeightMultiplier = 5.0f,       // +400% charge - desperate attack
                    DefendWeightMultiplier = 0.2f,       // -80% defend - bandits don't hold lines
                    SkirmishWeightMultiplier = 0.1f,     // -90% skirmish - no discipline to kite
                    EngagementDistanceMultiplier = 0.3f, // Engage very early
                    RetreatResistance = 1.2f,            // Will eventually flee but charge first
                    PreferStandAndFight = false,
                    ChargeWeightMinimum = 1.0f           // Strong base charge impulse
                },

                // Beastmen - feral aggression, like Chaos
                TORConstants.Cultures.BEASTMEN => new BattlePersonality
                {
                    ChargeWeightMultiplier = 3.0f,       // +200% charge - feral aggression
                    DefendWeightMultiplier = 0.3f,       // -70% defend - beasts don't hold lines
                    SkirmishWeightMultiplier = 0.2f,     // -80% skirmish - charge in
                    EngagementDistanceMultiplier = 0.4f, // Engage very early
                    RetreatResistance = 1.5f,            // Hard to make retreat
                    PreferStandAndFight = false,
                    ChargeWeightMinimum = 0.6f
                },

                TORConstants.Cultures.DAWI => new BattlePersonality
                {
                    ChargeWeightMultiplier = 0.8f,       // -20% charge - dwarfs are steady
                    DefendWeightMultiplier = 1.2f,       // +20% defend - hold the line!
                    SkirmishWeightMultiplier = 0.3f,     // -70% skirmish - dwarfs don't run
                    EngagementDistanceMultiplier = 1.2f, // Wait for enemy to come closer
                    RetreatResistance = 2.0f,            // Very hard to make retreat
                    PreferStandAndFight = true,          // Don't chase - let them come
                    ChargeWeightMinimum = 0f
                },

                TORConstants.Cultures.SYLVANIA => new BattlePersonality
                {
                    ChargeWeightMultiplier = 2.0f,       // +100% charge - undead are relentless
                    DefendWeightMultiplier = 0.5f,       // -50% defend
                    SkirmishWeightMultiplier = 0.2f,     // -80% skirmish - undead march forward
                    EngagementDistanceMultiplier = 0.6f,
                    RetreatResistance = 10.0f,           // Undead basically never retreat
                    PreferStandAndFight = false,
                    ChargeWeightMinimum = 0.4f           // Always shambling forward
                },

                TORConstants.Cultures.CHAOS => new BattlePersonality
                {
                    ChargeWeightMultiplier = 2.5f,       // +150% charge - blood for blood god
                    DefendWeightMultiplier = 0.3f,       // -70% defend - chaos attacks
                    SkirmishWeightMultiplier = 0.2f,
                    EngagementDistanceMultiplier = 0.4f, // Engage very early
                    RetreatResistance = 1.5f,
                    PreferStandAndFight = false,
                    ChargeWeightMinimum = 0.5f
                },

                TORConstants.Cultures.BRETONNIA => new BattlePersonality
                {
                    ChargeWeightMultiplier = 1.5f,       // +50% charge - FOR THE LADY!
                    DefendWeightMultiplier = 0.8f,
                    SkirmishWeightMultiplier = 0.6f,
                    EngagementDistanceMultiplier = 0.7f,
                    RetreatResistance = 1.2f,
                    PreferStandAndFight = false,
                    ChargeWeightMinimum = 0.2f
                },

                // Hit and run tactics - Wood Elves and Herrimaults
                TORConstants.Cultures.ASRAI or
                TORConstants.Cultures.HERRIMAULT => new BattlePersonality
                {
                    ChargeWeightMultiplier = 0.7f,       // -30% charge - guerrilla/ambush fighters
                    DefendWeightMultiplier = 0.8f,
                    SkirmishWeightMultiplier = 1.5f,     // +50% skirmish - hit and run
                    EngagementDistanceMultiplier = 1.3f,
                    RetreatResistance = 0.8f,            // Will tactically retreat
                    PreferStandAndFight = false,
                    ChargeWeightMinimum = 0f
                },

                TORConstants.Cultures.EONIR => new BattlePersonality // High Elves - disciplined, prefer ranged
                {
                    ChargeWeightMultiplier = 0.6f,       // -40% charge - elves are cautious, prefer ranged
                    DefendWeightMultiplier = 1.1f,       // Slightly prefer holding formation
                    SkirmishWeightMultiplier = 1.4f,     // Strong skirmishers
                    EngagementDistanceMultiplier = 1.3f, // Wait for enemy to close
                    RetreatResistance = 0.85f,           // Will tactically withdraw
                    PreferStandAndFight = false,
                    ChargeWeightMinimum = 0f
                },

                TORConstants.Cultures.EMPIRE => new BattlePersonality // Empire - balanced but disciplined
                {
                    ChargeWeightMultiplier = 1.0f,
                    DefendWeightMultiplier = 1.0f,
                    SkirmishWeightMultiplier = 1.0f,
                    EngagementDistanceMultiplier = 1.0f,
                    RetreatResistance = 1.0f,
                    PreferStandAndFight = false,
                    ChargeWeightMinimum = 0f
                },

                _ => new BattlePersonality // Default fallback
                {
                    ChargeWeightMultiplier = 1.0f,
                    DefendWeightMultiplier = 1.0f,
                    SkirmishWeightMultiplier = 1.0f,
                    EngagementDistanceMultiplier = 1.0f,
                    RetreatResistance = 1.0f,
                    PreferStandAndFight = false,
                    ChargeWeightMinimum = 0f
                }
            };
        }

        /// <summary>
        /// Gets the culture ID for a team based on its general or leader.
        /// </summary>
        public static string GetTeamCulture(Team team)
        {
          
            
            
            var culture=  team?.GeneralAgent?.Character?.Culture?.StringId ?? team?.Leader?.GetHero()?.Culture?.StringId;

            if (culture == null)
            {
                culture = team.ActiveAgents.FirstOrDefault()?.Character.Culture.StringId;
            }


            return culture;
        }
    }
}
