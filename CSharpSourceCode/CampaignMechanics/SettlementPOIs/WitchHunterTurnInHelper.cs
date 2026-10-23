using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.Shared;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Dialog consequences for the Witch Hunter Lodge's Turn-ins branch. Two
    /// sub-flows per the Chapterhouse design doc:
    ///
    /// <list type="bullet">
    /// <item><b>Materials</b> — Warpstone Dust and Arcane Scrolls, delivered via
    /// the shared <see cref="MaterialTurnInHelper"/>. Enchanted gear deferred to
    /// Phase E (needs a trait-based filter).</item>
    /// <item><b>Prisoners</b> — the Druchii-envoy pattern: open vanilla's donate-
    /// to-town screen as a UI vehicle, catch <c>OnPrisonerDonatedToSettlementEvent</c>
    /// in <see cref="WitchHunter.WitchHunterInquisitionBehavior"/>, apply ransom-
    /// broker-style rewards with a heretical-category multiplier, suppress the
    /// vanilla kingdom-influence gain via <see cref="DonationSideEffectSuppression"/>.</item>
    /// </list>
    ///
    /// Reward constants are placeholders anchored to the Empire Quests Mechanics
    /// spreadsheet; expect to tune after playtest.
    /// </summary>
    public static class WitchHunterTurnInHelper
    {
        // ------- Materials turn-in

        public const int WarpstoneBatchSize = 1;
        public const int ScrollBatchSize    = 1;

        public const int WarpstonePrestige  = 50;
        public const int WarpstoneGold      = 300;
        public const int ScrollPrestige     = 10;
        public const int ScrollGold         = 100;

        public static void OpenMaterialTurnIn()
        {
            MaterialTurnInHelper.OpenInquiry(
                title: new TextObject(TORTextHelper.GetText(
                    "tor_poi_witchhunter_lodge.materials_title",
                    "For the Pyre")),
                description: new TextObject(TORTextHelper.GetText(
                    "tor_poi_witchhunter_lodge.materials_description",
                    "What have you brought the Order?")),
                minSelect: 1, maxSelect: 32,
                supplier: BuildMaterialOptions,
                reward: AwardMaterialRewards);
        }

        private static IEnumerable<TurnInOption> BuildMaterialOptions(ItemRoster roster)
        {
            var warpstone = MBObjectManager.Instance.GetObject<ItemObject>("tor_tradegood_warpstone_dust");
            if (warpstone != null)
            {
                var count = roster.GetItemNumber(warpstone);
                for (int i = 0; i < count; i += WarpstoneBatchSize)
                {
                    yield return new TurnInOption(
                        warpstone, WarpstoneBatchSize,
                        warpstone.Name.ToString(),
                        tooltip: $"{WarpstonePrestige} Prestige + {WarpstoneGold} gold",
                        tag: "warpstone");
                    // One option per unit shown — player can pick as many as they
                    // like via the multi-select. Keeps per-item visibility simple.
                    if (i >= 99) break;   // sanity ceiling
                }
            }

            var scroll = MBObjectManager.Instance.GetObject<ItemObject>("tor_tradegood_arcane_scroll");
            if (scroll != null)
            {
                var count = roster.GetItemNumber(scroll);
                for (int i = 0; i < count; i += ScrollBatchSize)
                {
                    yield return new TurnInOption(
                        scroll, ScrollBatchSize,
                        scroll.Name.ToString(),
                        tooltip: $"{ScrollPrestige} Prestige + {ScrollGold} gold",
                        tag: "scroll");
                    if (i >= 99) break;
                }
            }
        }

        private static void AwardMaterialRewards(IReadOnlyList<TurnInOption> picked)
        {
            int prestige = 0, gold = 0;
            foreach (var opt in picked)
            {
                switch (opt.Tag as string)
                {
                    case "warpstone": prestige += WarpstonePrestige; gold += WarpstoneGold; break;
                    case "scroll":    prestige += ScrollPrestige;    gold += ScrollGold;    break;
                }
            }
            if (gold > 0) GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold);
            if (prestige > 0) Hero.MainHero.AddCustomResource("Prestige", prestige);
        }

        // ------- Prisoner turn-in (Druchii envoy pattern)

        public const float HereticalGoldMultiplier = 2.5f;
        public const int HereticalPrestigePerHead = 1;
        public const int HeroPrestige = 25;
        public const int HeroGold = 1000;

        /// <summary>
        /// True while a prisoner-donation flow originated from the lodge is in
        /// flight. The Inquisition behavior's <c>OnPrisonersSold</c> handler
        /// checks this before processing — the same event also fires for other
        /// TOR flows (Eonir envoy) and vanilla donations.
        /// </summary>
        public static bool IsLodgeDonationActive { get; private set; }

        public static void OpenPrisonerTurnIn()
        {
            IsLodgeDonationActive = true;
            DonationSideEffectSuppression.Begin();
            Helpers.PartyScreenHelper.OpenScreenAsDonatePrisoners();
            // Flag + suppression are cleared in the behavior's OnPrisonersSold
            // handler. If the player cancels, the stale flag is harmless until
            // the player enters another donation flow in a non-lodge town, where
            // the behavior's defensive settlement check will skip and clear it.
        }

        public static void EndLodgeDonation()
        {
            if (IsLodgeDonationActive)
            {
                IsLodgeDonationActive = false;
                DonationSideEffectSuppression.End();
            }
        }

        public static int CalculateHereticalGoldFor(CharacterObject troop)
        {
            if (troop == null) return 0;
            if (troop.IsHero) return HeroGold;
            var baseGold = 20 + troop.Tier * 20;  // placeholder tier-scaling rate
            return IsHeretical(troop) ? (int)(baseGold * HereticalGoldMultiplier) : baseGold;
        }

        public static int CalculateHereticalPrestigeFor(CharacterObject troop)
        {
            if (troop == null) return 0;
            if (troop.IsHero) return HeroPrestige;
            return IsHeretical(troop) ? HereticalPrestigePerHead : 0;
        }

        private static bool IsHeretical(CharacterObject troop)
        {
            if (troop == null) return false;
            // IsChaos covers CHAOS + CHAOS_CULTIST cultures (includes cultists).
            if (troop.IsChaos()) return true;
            if (troop.IsUndead()) return true;
            if (troop.IsVampire()) return true;
            return false;
        }
    }
}
