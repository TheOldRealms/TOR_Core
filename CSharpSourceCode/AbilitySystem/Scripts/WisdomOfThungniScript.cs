using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Items;

namespace TOR_Core.AbilitySystem.Scripts;

/// <summary>
/// Enchants the wielded weapon and refunds cooldown of the next 'Rune' ability on cooldown. The empowerment itself
/// (the WisdomThungni attribute) is applied by the ability's triggered effect, apply_wisdom_thungni.
/// </summary>
public class WisdomOfThungniScript : CareerAbilityScript
{
    private const float RuneCooldownRefund = 15;

    public override void Initialize(Ability ability, ref GameEntity entity)
    {
        base.Initialize(ability, ref entity);

        var agent = Agent.Main;
        var abilityComponent = agent?.GetComponent<AbilityComponent>();
        if (abilityComponent == null) return;

        var weaponEnchantment = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "magical_weapon_15");
        if (weaponEnchantment != null) agent.GetComponent<ItemTraitAgentComponent>()?.AddTraitToWieldedWeapon(weaponEnchantment, ability.Template.Duration);

        // Forgefire Burning keystone: the next rune in the sequence is refunded as well, at half efficiency.
        var runesToRefund = Hero.MainHero.HasCareerChoice("ForgefireBurningKeystone") ? 2 : 1;
        var refund = RuneCooldownRefund;

        foreach (var rune in abilityComponent.KnownAbilitySystem.Where(IsRuneOnCooldown).Take(runesToRefund))
        {
            rune.RefundCooldown(refund);
            refund /= 2;
        }
    }

    private static bool IsRuneOnCooldown(Ability ability) => ability.Template.BelongsToLoreID == "RuneMagic" && ability.IsOnCooldown();
}
