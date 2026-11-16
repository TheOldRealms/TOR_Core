using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Shared
{
    /// <summary>
    /// Shared primitive for "player picks a subset of eligible items from their
    /// inventory to hand over for a reward" flows. Extracted from OathGold's
    /// <c>DeliverSteel</c> / <c>AddOathGoldForSteel</c> pattern so the Inquisition
    /// materials turn-in and OathGold's steel + grain donations share one
    /// implementation.
    ///
    /// <para>Caller provides two delegates: <see cref="OptionSupplier"/> yields
    /// the currently-eligible <see cref="TurnInOption"/>s from the player's roster,
    /// and <see cref="RewardCallback"/> receives the picked options after the
    /// helper deducts their <c>BatchSize</c> from the roster. The reward callback
    /// is where per-item rewards + aggregate perk modifiers (e.g. OathGold's
    /// Legacy of Grungni bonus) are applied — matches OathGold's existing
    /// callback shape.</para>
    /// </summary>
    public readonly struct TurnInOption
    {
        public TurnInOption(ItemObject item, int batchSize, string displayTitle,
                            string tooltip = null, object tag = null)
        {
            Item = item;
            BatchSize = batchSize;
            DisplayTitle = displayTitle;
            Tooltip = tooltip;
            Tag = tag;
        }

        public readonly ItemObject Item;
        public readonly int BatchSize;
        public readonly string DisplayTitle;
        public readonly string Tooltip;
        /// <summary>Caller discriminator — set to any value the reward callback uses to tell options apart.</summary>
        public readonly object Tag;
    }

    public delegate IEnumerable<TurnInOption> OptionSupplier(ItemRoster roster);
    public delegate void RewardCallback(IReadOnlyList<TurnInOption> picked);

    public static class MaterialTurnInHelper
    {
        public static void OpenInquiry(
            TextObject title, TextObject description,
            int minSelect, int maxSelect,
            OptionSupplier supplier,
            RewardCallback reward)
        {
            var roster = Hero.MainHero?.PartyBelongedTo?.ItemRoster;
            if (roster == null || supplier == null || reward == null) return;

            var options = supplier(roster)?.ToList() ?? new List<TurnInOption>();
            if (options.Count == 0) return;

            var elements = options.Select(o => new InquiryElement(
                o,
                o.DisplayTitle,
                new ItemImageIdentifier(o.Item),
                true,
                o.Tooltip)).ToList();

            var accept = TORTextHelper.GetText("tor_inquiry_accept_text", "Accept");
            var cancel = TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel");

            var data = new MultiSelectionInquiryData(
                title.ToString(), description.ToString(), elements, true, minSelect, maxSelect,
                accept, cancel,
                picked =>
                {
                    if (picked == null || picked.Count == 0) return;
                    var pickedOptions = picked.Select(p => (TurnInOption)p.Identifier).ToList();
                    foreach (var opt in pickedOptions)
                        roster.AddToCounts(opt.Item, -opt.BatchSize);
                    reward(pickedOptions);
                },
                null);

            MBInformationManager.ShowMultiSelectionInquiry(data, true);
        }
    }
}
