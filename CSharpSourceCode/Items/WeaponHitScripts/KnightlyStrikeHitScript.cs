using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Utilities;

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
            weaponComponent.RemoveTraitFromWieldedWeapon("KnightlyStrike");
        }
    }
}