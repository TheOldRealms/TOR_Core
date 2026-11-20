using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// <para><b>Problem:</b> vanilla's
    /// <c>VillageHostileActionCampaignBehavior.WillVillageResistHostileAction</c>
    /// decides whether villagers resist a raid / force-supplies / force-volunteers
    /// demand. It's a <c>static private</c> method, not virtual and not
    /// routed through a <c>GameModel</c>, so <c>AddModel</c> can't reach it.
    /// It's called from three menu-consequence sites inside the same behavior,
    /// each of which is also static-private, so patching the three callers is
    /// no cleaner than patching the single source.</para>
    /// <para><b>Fix:</b> postfix the one source. Shakedown primary
    /// (<c>TORPerks.Roguery.Shakedown</c>, Quartermaster role, -0.15 AddFactor)
    /// flips the result from <c>true</c> to <c>false</c> with probability
    /// equal to <c>|PrimaryBonus|</c> (15%) when the main party's
    /// quartermaster holds the perk — matches the perk description
    /// "15% less likely to resist" verbatim without touching vanilla's
    /// strength-comparison formula. Village-menu-adjacent single-method
    /// patch, tolerable per the Harmony policy.</para>
    /// </summary>
    [HarmonyPatch(typeof(VillageHostileActionCampaignBehavior))]
    public static class VillageHostileActionCampaignBehavior_WillVillageResistHostileAction_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch("WillVillageResistHostileAction")]
        public static void WillVillageResistHostileAction_Postfix(ref bool __result)
        {
            if (!__result) return;
            var main = MobileParty.MainParty;
            if (main == null) return;
            if (!main.HasPerk(TORPerks.Roguery.Shakedown, checkSecondaryRole: true)) return;

            float reduceChance = Math.Abs(TORPerks.Roguery.Shakedown.PrimaryBonus);
            if (MBRandom.RandomFloat < reduceChance)
            {
                __result = false;
            }
        }
    }
}
