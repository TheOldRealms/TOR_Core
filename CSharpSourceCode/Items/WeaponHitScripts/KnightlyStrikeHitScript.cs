using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.StatusEffect;

namespace TOR_Core.Items.WeaponHitScripts;

public class KnightlyStrikeHitScript : BaseWeaponHitScript
{

    public override void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
    {
        if (blow.InflictedDamage <= 15)
            return;

        var statusEffectComponent = attackingAgent.GetComponent<StatusEffectComponent>();

        var knightlyStrikes = new List<string>();

        if (statusEffectComponent != null)
        {
            statusEffectComponent.RemoveStatusEffect("knightly_strike");
            var list = statusEffectComponent.GetTemporaryAttributes(true).Where(x => x == "KnightlyStrike").ToList();
            knightlyStrikes.AddRange(list);
        }

        if (knightlyStrikes.Count > 0)
        {
            return;
        }

        var weaponComponent = attackingAgent.GetComponent<ItemTraitAgentComponent>();

        if (weaponComponent != null)
        {
            //remove all trait ids that could be added
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_knightlystrike");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_knightlystrike_cleave");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_knightlystrike_extra_damage");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_knightlystrike_extra_armorpen");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_knight_religion_default");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_knight_religion_sigmar");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_knight_religion_ulric");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_knight_religion_taal");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_knight_religion_manaan");
            weaponComponent.RemoveTraitFromWieldedWeapon("ca_knight_religion_shallya");
        }
    }
}