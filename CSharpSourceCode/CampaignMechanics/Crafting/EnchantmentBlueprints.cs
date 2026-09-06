using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Crafting
{
    /// <summary>
    /// The single read and write point for enchantment blueprint knowledge.
    ///
    /// Blueprints are held **once, campaign-wide**, in
    /// <see cref="EnchantmentBlueprintBehavior"/>. There is no per-hero storage any more:
    /// learning a blueprint teaches it to the player's whole operation, and it is never
    /// lost, including when the hero who learned it leaves.
    ///
    /// <para><b>This deliberately retires the hired-Runesmith retention mechanic.</b> Until
    /// P2 the enchanting table unioned per-hero lists over the *current* party, so dismissing
    /// the Runesmith who read your rune manuscripts took those runes with them. That is gone.
    /// What survives is the *acquisition* gate: a manuscript still has to be read by someone
    /// who satisfies its lore/attribute restriction, so you still need a Runesmith to learn
    /// runes — you just no longer need to keep them. See
    /// <c>docs/enchantment-blueprint-storage-proposal.md</c>, whose "correction" section
    /// argued the opposite and is superseded by this.</para>
    ///
    /// <para>Crafting-time gating replaces it: knowing a blueprint and being able to execute
    /// it are now separate questions, answered by <see cref="IsKnown"/> and
    /// <see cref="EnchantmentHelper.GetUnmetRequirement"/> respectively.</para>
    /// </summary>
    public static class EnchantmentBlueprints
    {
        /// <summary>
        /// True if the player has ever learned <paramref name="blueprintId"/>. Knowing it
        /// says nothing about whether it can be crafted right now — see
        /// <see cref="EnchantmentHelper.GetUnmetRequirement"/> for that.
        /// </summary>
        public static bool IsKnown(string blueprintId)
        {
            if (string.IsNullOrEmpty(blueprintId)) return false;
            return EnchantmentBlueprintBehavior.Instance?.Contains(blueprintId) ?? false;
        }

        /// <summary>
        /// Every blueprint id the player has learned. A snapshot, so callers can hold it
        /// across a loop and test membership in constant time without touching the store's
        /// own set.
        /// </summary>
        public static HashSet<string> GetKnown()
        {
            var store = EnchantmentBlueprintBehavior.Instance;
            return store == null ? [] : new HashSet<string>(store.Known);
        }

        /// <summary>
        /// Records <paramref name="blueprintId"/> as learned and raises
        /// <c>TORCampaignEvents.EnchantmentLearned</c>. Returns false if it was already
        /// known, so callers can avoid charging for a no-op.
        /// </summary>
        /// <param name="learnedBy">
        /// Who did the learning. Storage is campaign-wide so this does not affect *where* the
        /// blueprint goes — it is carried on the event for quest/UI attribution, and names the
        /// hero in the notification. Defaults to the main hero.
        /// </param>
        public static bool Learn(string blueprintId, Hero learnedBy = null, bool showNotification = false)
        {
            if (string.IsNullOrEmpty(blueprintId)) return false;

            var store = EnchantmentBlueprintBehavior.Instance;
            if (store == null)
            {
                TORCommon.Log($"ENCHANTMENT ERROR: cannot learn {blueprintId}, EnchantmentBlueprintBehavior is not registered.", NLog.LogLevel.Error);
                return false;
            }

            if (!store.Record(blueprintId)) return false;

            var hero = learnedBy ?? Hero.MainHero;
            TORCampaignEvents.Instance.OnEnchantmentLearned(hero, blueprintId);

            if (showNotification) ShowLearnedNotification(blueprintId, hero);
            return true;
        }

        private static void ShowLearnedNotification(string blueprintId, Hero hero)
        {
            var itemTrait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == blueprintId);
            if (itemTrait == null)
            {
                TORCommon.Log("ENCHANTMENT ERROR: recipe " + blueprintId + " doesnt exist", NLog.LogLevel.Error);
                return;
            }

            var learnedEnchantmentText = TORTextHelper.GetTextObject("tor_learned_enchantment_text", "{HERO_NAME} learned the enchantment {ENCHANTMENT_NAME}");
            learnedEnchantmentText.SetTextVariable("HERO_NAME", hero.Name);
            learnedEnchantmentText.SetTextVariable("ENCHANTMENT_NAME", itemTrait.ItemTraitName);
            MBInformationManager.AddQuickInformation(learnedEnchantmentText, 0, hero.CharacterObject);
        }
    }
}
