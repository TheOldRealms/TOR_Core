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
    /// Softens villager resistance to raids and demands for parties holding Shakedown.
    /// <para><b>Problem:</b> <c>WillVillageResistHostileAction</c> is static
    /// private and not routed through a <c>GameModel</c>.</para>
    /// <para><b>Fix:</b> postfix the method. Flip <c>true</c> to <c>false</c>
    /// with probability equal to the perk bonus magnitude.</para>
    /// </summary>
    [HarmonyPatch(typeof(VillageHostileActionCampaignBehavior))]
    public static class VillageHostileActionCampaignBehavior_WillVillageResistHostileAction_Patch
    {
        /// <summary>Rolls to turn resistance off when the perk is held.</summary>
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
    /// Adds a Fencer loot top-up when villagers comply with a force demand.
    /// <para><b>Problem:</b> the loot amounts live inside two private
    /// consequence methods on <c>VillageHostileActionCampaignBehavior</c>,
    /// not a <c>GameModel</c>.</para>
    /// <para><b>Fix:</b> postfix each method, mirror vanilla's base formula,
    /// and grant the 20% top-up directly.</para>
    /// </summary>
    [HarmonyPatch(typeof(VillageHostileActionCampaignBehavior))]
    public static class VillageHostileActionCampaignBehavior_Fencer_Patches
    {
        /// <summary>Grants extra gold and production goods on a force-supplies success.</summary>
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
                TORTextHelper.GetText("tor_perk_fencer_supplies_msg",
                    "Fencer: extracted an extra share from the compliant villagers.")));
        }

        /// <summary>Grants extra basic-troop recruits on a force-volunteers success.</summary>
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

            int baseNum = (int)Math.Ceiling(settlement.Village.Hearth / 30f);
            int extra = (int)(baseNum * bonus);
            if (extra <= 0) return;

            var basicTroop = settlement.Culture?.BasicTroop;
            if (basicTroop == null) return;

            main.MemberRoster.AddToCounts(basicTroop, extra);
            InformationManager.DisplayMessage(new InformationMessage(
                TORTextHelper.GetText("tor_perk_fencer_volunteers_msg",
                    "Fencer: an extra group of villagers signs on under duress.")));
        }
    }
}
