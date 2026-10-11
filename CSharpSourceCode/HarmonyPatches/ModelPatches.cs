using HarmonyLib;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.HarmonyPatches;

[HarmonyPatch]
[HarmonyPatchCategory("LatePatches")]
public static class ModelPatches
{

    // this patch ensures that the randomly AI hired  mercenary costs , while being a vassal in a kingdom, are not getting crazy high. Especially in smaller kingdoms.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DefaultClanFinanceModel), "CalculateShareFactor")]
    public static bool Prefix(ref float __result, Clan clan)
    {
        if (clan == Clan.PlayerClan && clan.Kingdom != null && clan.Kingdom.RulingClan != Clan.PlayerClan)
        {
            __result = 0;
            return false;
        }

        return true;
    }

    // removing influence changes for the hirelings
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ChangeClanInfluenceAction), nameof(ChangeClanInfluenceAction.Apply))]
    private static void Prefix_FreezeHirelingInfluence(Clan clan, ref float amount)
    {
        if (clan != Clan.PlayerClan || !Hero.MainHero.IsEnlisted())
        {
            return;
        }
        amount = -clan.Influence;
    }

    // unit attributes overriding kill/ko
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SandboxAgentDecideKilledOrUnconsciousModel), nameof(SandboxAgentDecideKilledOrUnconsciousModel.GetAgentStateProbability))]
    private static bool Prefix_SandboxAgentStateProbability(
    Agent affectorAgent,
    Agent effectedAgent,
    DamageTypes damageType,
    WeaponFlags weaponFlags,
    ref float __result,
    ref float useSurgeryProbability)
    {
        return OverrideAgentKilledOrUnconsciousState(effectedAgent, ref __result, ref useSurgeryProbability);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DefaultAgentDecideKilledOrUnconsciousModel), nameof(DefaultAgentDecideKilledOrUnconsciousModel.GetAgentStateProbability))]
    private static bool Prefix_DefaultAgentStateProbability(
        Agent affectorAgent,
        Agent effectedAgent,
        DamageTypes damageType,
        WeaponFlags weaponFlags,
        ref float __result,
        ref float useSurgeryProbability)
    {
        return OverrideAgentKilledOrUnconsciousState(effectedAgent, ref __result, ref useSurgeryProbability);
    }

    private static bool OverrideAgentKilledOrUnconsciousState(
        Agent effectedAgent,
        ref float result,
        ref float useSurgeryProbability)
    {
        if (effectedAgent == null)
        {
            return true;
        }

        var killingBlowForcedKill = TORAgentApplyDamageModel.ConsumeKillingBlowVictim(effectedAgent);

        if (effectedAgent.HasImmortality())
        {
            useSurgeryProbability = 1f;
            result = 0f; // force knockout
            return false;
        }

        if (killingBlowForcedKill)
        {
            useSurgeryProbability = 0f;
            result = 1f; // force kill
            return false;
        }

        return true;
    }

}