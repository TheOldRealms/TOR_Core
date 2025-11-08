using TaleWorlds.MountAndBlade;
using TOR_Core.Items;

namespace TOR_Core.BattleMechanics.TriggeredEffect.Scripts;

public static class TraitHelper
{
    public static void ApplyEffectToRangedWeapons(ItemTraitAgentComponent traitComponent,ItemTrait trait, Agent agent,float duration)
    {
        if (agent.Equipment.ContainsNonConsumableRangedWeaponWithAmmo())
        {
            for (int i = 0; i < 3; i++)
            {
                if (agent.Equipment[i].CurrentUsageItem.IsRangedWeapon && !agent.Equipment[i].CurrentUsageItem.IsAmmo)
                {
                    traitComponent.AddTraitToWeapon(agent.Equipment[i],trait, duration);
                }
            }
        }
    }
}