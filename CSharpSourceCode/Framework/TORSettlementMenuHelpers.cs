using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.GameMenus;

namespace TOR_Core.Framework
{
    public static class TORSettlementMenuHelpers
    {
        /// <summary>
        /// Repositions <paramref name="entryId"/> directly after (default) or before
        /// (<paramref name="above"/>=true) <paramref name="targetEntryId"/> in the given
        /// <paramref name="menu"/>'s options list.
        ///
        /// <para>Previous single-pass implementation had a latent bug: when the entry
        /// appeared in the list AFTER the target (which is the common case — new TOR
        /// options are typically appended to vanilla menus, and anchor against earlier
        /// vanilla entries), the one-past-target iteration would attempt
        /// <c>options[-1]</c> because the entry hadn't been encountered yet.
        /// Replaced with a straightforward two-pass: strip entry, locate target, insert.</para>
        /// </summary>
        public static void RearrangeTownMenus(GameMenu menu, string entryId, string targetEntryId, bool above = false)
        {
            if (menu == null) return;

            var optionsField = AccessTools.Field(typeof(GameMenu), "_menuItems");
            if (optionsField == null) return;

            var options = optionsField.GetValue(menu) as List<GameMenuOption>;
            if (options == null) return;

            var entry = options.FirstOrDefault(x => x.IdString == entryId);
            var targetEntry = options.FirstOrDefault(x => x.IdString == targetEntryId);
            if (entry == null || targetEntry == null) return;

            // Pass 1: drop the entry from the list (reference-identity, matches above lookup).
            var reordered = options.Where(o => o != entry).ToList();

            // Pass 2: find the target in the entry-less list and insert at the correct side.
            int targetIndex = reordered.IndexOf(targetEntry);
            if (targetIndex < 0) return;

            int insertIndex = above ? targetIndex : targetIndex + 1;
            reordered.Insert(insertIndex, entry);

            optionsField.SetValue(menu, reordered);
        }
    }
}