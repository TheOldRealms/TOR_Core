using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
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

    /// <summary>
    /// <para><b>Problem:</b> Fencer primary (Roguery 200, PartyLeader +0.2
    /// AddFactor): "+20% loot when villagers comply with your hostile
    /// action." The loot amount is computed inside two private methods on
    /// <c>VillageHostileActionCampaignBehavior</c>
    /// (<c>village_force_supplies_ended_successfully_on_consequence</c>
    /// and <c>village_force_volunteers_ended_successfully_on_consequence</c>);
    /// the behavior isn't a <c>GameModel</c>, so no subclass path.</para>
    /// <para><b>Fix:</b> postfix each method. Recompute vanilla's base
    /// <c>num</c> from the hearth formula, grant the 20% top-up directly
    /// (extra gold + extra production goods on the supplies side, extra
    /// basic-troop recruits on the volunteers side). Vanilla's own
    /// <c>InBestLight</c> check is inline in the same private method —
    /// our patch follows the same inline-perk-check pattern, just from
    /// outside the type.</para>
    /// </summary>
    [HarmonyPatch(typeof(VillageHostileActionCampaignBehavior))]
    public static class VillageHostileActionCampaignBehavior_Fencer_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch("village_force_supplies_ended_successfully_on_consequence")]
        public static void ForceSuppliesSuccess_Postfix()
        {
            var main = MobileParty.MainParty;
            var settlement = Settlement.CurrentSettlement;
            if (main == null || settlement?.Village == null) return;
            if (!main.HasPerk(TORPerks.Roguery.Fencer)) return;

            float bonus = TORPerks.Roguery.Fencer.PrimaryBonus;
            if (bonus <= 0f) return;

            // Mirror vanilla's base-amount formula.
            int baseNum = MathF.Max((int)(settlement.Village.Hearth * 0.15f), 20);

            int extraGold = (int)(baseNum
                * Campaign.Current.Models.RaidModel.GoldRewardForEachLostHearth
                * bonus);
            if (extraGold > 0)
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, extraGold);

            for (int i = 0; i < settlement.Village.VillageType.Productions.Count; i++)
            {
                var (item, factor) = settlement.Village.VillageType.Productions[i];
                int extra = (int)(factor / 60f * baseNum * bonus);
                if (extra > 0)
                    PartyBase.MainParty.ItemRoster.AddToCounts(item, extra);
            }

            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=!}Fencer: extracted an extra share from the compliant villagers.").ToString()));
        }

        [HarmonyPostfix]
        [HarmonyPatch("village_force_volunteers_ended_successfully_on_consequence")]
        public static void ForceVolunteersSuccess_Postfix()
        {
            var main = MobileParty.MainParty;
            var settlement = Settlement.CurrentSettlement;
            if (main == null || settlement?.Village == null) return;
            if (!main.HasPerk(TORPerks.Roguery.Fencer)) return;

            float bonus = TORPerks.Roguery.Fencer.PrimaryBonus;
            if (bonus <= 0f) return;

            // Mirror vanilla's base-count formula (hearth / 30, rounded up).
            int baseNum = (int)Math.Ceiling(settlement.Village.Hearth / 30f);
            int extra = (int)(baseNum * bonus);
            if (extra <= 0) return;

            var basicTroop = settlement.Culture?.BasicTroop;
            if (basicTroop == null) return;

            main.MemberRoster.AddToCounts(basicTroop, extra);
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=!}Fencer: an extra group of villagers signs on under duress.").ToString()));
        }
    }
}
