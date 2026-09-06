using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Crafting
{
    /// <summary>
    /// The single read point for "can the player's party craft this enchantment blueprint?".
    /// </summary>
    public static class EnchantmentBlueprints
    {
        /// <summary>
        /// True if any hero in the player's party knows <paramref name="blueprintId"/>.
        /// </summary>
        public static bool IsKnown(string blueprintId)
        {
            if (string.IsNullOrEmpty(blueprintId)) return false;
            return MobileParty.MainParty.GetMemberHeroes().Any(hero => hero.HasKnownEnchantmentBlueprint(blueprintId));
        }

        /// <summary>
        /// Every blueprint id known to the player's party, unioned across its heroes.
        /// </summary>
        /// <remarks>
        /// Prefer this over calling <see cref="IsKnown"/> in a loop: each call walks the party's
        /// member roster, and <c>MobilePartyExtensions.GetMemberHeroes</c> revalidates that
        /// roster every time.
        /// </remarks>
        public static HashSet<string> GetKnown()
        {
            var known = new HashSet<string>();
            foreach (var hero in MobileParty.MainParty.GetMemberHeroes())
            {
                var info = hero.GetExtendedInfo();
                if (info == null) continue;
                known.UnionWith(info.KnownEnchantmentBlueprints);
            }
            return known;
        }
    }
}
