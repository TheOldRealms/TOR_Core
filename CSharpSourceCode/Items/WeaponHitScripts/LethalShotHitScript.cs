using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.Extensions;
using TOR_Core.Items;

namespace TOR_Core.Items.WeaponHitScripts;

public class LethalShotHitScript : BaseWeaponHitScript
{
    public static int id;
    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        // Only consume charge on ranged hits#
        
        if (! blow.IsMissile)
            return;

        if (id == -1)
        {
            id = blow.WeaponRecord.AffectorWeaponSlotOrMissileIndex;
        }
        else
        {
            if (id == blow.WeaponRecord.AffectorWeaponSlotOrMissileIndex)
            {
                return;
            }
        }


        // Minimum damage threshold to consume a charge
        if (blow.InflictedDamage <= 5)
            return;

        var statusEffectComponent = attackingAgent.GetComponent<StatusEffectComponent>();

        var lethalShots = new List<string>();

        if (statusEffectComponent != null)
        {
            // Remove one stack
            statusEffectComponent.RemoveStatusEffect("lethal_shot");

            // Check remaining stacks
            var list = statusEffectComponent.GetTemporaryAttributes(true).Where(x => x == "LethalShot").ToList();
            lethalShots.AddRange(list);
        }

        // If stacks remain, don't remove traits yet
        if (lethalShots.Count > 0)
        {
            return;
        }
        id = -1;

        // No stacks left - remove all Lethal Shot traits
        var weaponComponent = attackingAgent.GetComponent<ItemTraitAgentComponent>();

        if (weaponComponent != null)
        {
            // Remove all lethal shot trait ids
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_hagbane");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_loec");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_scatter");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_reload");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_pierce");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_swiftshiver");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_speed");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_starfire");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_starfire_shield");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_lethal_shot_moonfire");
        }
    }
}
