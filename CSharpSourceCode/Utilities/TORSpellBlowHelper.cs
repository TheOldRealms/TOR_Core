using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Utilities
{
    public static class TORSpellBlowHelper
    {
        /// <summary>
        /// Sentinel value used in AttackCollisionData.AffectorWeaponSlotOrMissileIndex to identify spell blows.
        /// This value is impossible in normal gameplay (weapon slots are 0-3, missile indices are positive).
        /// </summary>
        public const int SpellBlowSentinel = -999;

        /// <summary>
        /// Flag set during OnScoreHit processing for spell hits.
        /// Used by TORCombatXpModel to skip weapon skill XP while still allowing kill tracking.
        /// </summary>
        public static bool IsProcessingSpellHit;

        /// <summary>
        /// Detects if the current attack is a spell based on the sentinel value.
        /// Use this in damage model methods where Blow is not yet available.
        /// </summary>
        public static bool IsSpellAttack(in AttackCollisionData collisionData)
        {
            return collisionData.AffectorWeaponSlotOrMissileIndex == SpellBlowSentinel;
        }

        /// <summary>
        /// Determines if a Blow originated from a spell based on its properties.
        /// </summary>
        /// <remarks>
        /// Amber Spear and Green Eye are the only weapons in the game with an undefined weapon class and so they are detected by that as well as their missile status.
        /// </remarks>
        public static bool IsSpellBlow(Blow b)
        {
            return (b.StrikeType == StrikeType.Thrust && b.AttackType == AgentAttackType.Kick && b.DamageCalculated && b.BlowFlag.HasFlag(BlowFlags.NoSound) && b.VictimBodyPart == BoneBodyPartType.Chest) || (b.IsMissile && b.WeaponRecord.WeaponClass == WeaponClass.Undefined);
        }

        /// <summary>
        /// Determines if a KillingBlow originated from a spell based on its properties.
        /// </summary>
        public static bool IsSpellBlow(KillingBlow b)
        {
            return b.AttackType == AgentAttackType.Kick && b.WeaponItemKind == -1 && b.VictimBodyPart == BoneBodyPartType.Chest;
        }
    }
}
