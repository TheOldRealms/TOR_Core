using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics;
using TOR_Core.Extensions.ExtendedInfoSystem;

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
        public static bool IsSpellBlow(Blow b)
        {
            return b.StrikeType == StrikeType.Thrust && b.AttackType == AgentAttackType.Kick && b.DamageCalculated && b.BlowFlag.HasFlag(BlowFlags.NoSound) && b.VictimBodyPart == BoneBodyPartType.Chest;
        }

        /// <summary>
        /// Determines if a KillingBlow originated from a spell based on its properties.
        /// </summary>
        public static bool IsSpellBlow(KillingBlow b)
        {
            return b.AttackType == AgentAttackType.Kick && b.WeaponItemKind == -1 && b.VictimBodyPart == BoneBodyPartType.Chest;
        }

        /// <summary>
        /// Determines the attack type mask from a Blow.
        /// </summary>
        public static AttackTypeMask DetermineMask(Blow blow)
        {
            if (IsSpellBlow(blow)) return AttackTypeMask.Spell;
            if (blow.IsMissile)
            {
                return AttackTypeMask.Ranged;
            }

            return AttackTypeMask.Melee;
        }

        /// <summary>
        /// Determines the attack type mask from a KillingBlow.
        /// </summary>
        public static AttackTypeMask DetermineMask(KillingBlow blow)
        {
            if (IsSpellBlow(blow)) return AttackTypeMask.Spell;
            if (blow.IsMissile)
            {
                return AttackTypeMask.Ranged;
            }

            return AttackTypeMask.Melee;
        }
    }
}
