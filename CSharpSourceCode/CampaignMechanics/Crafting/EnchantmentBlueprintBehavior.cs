using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Crafting
{
    /// <summary>
    /// Campaign-scoped store of the enchantment blueprints the player has acquired.
    ///
    /// Phase 1 of <c>docs/enchantment-blueprint-storage-proposal.md</c>. The store is
    /// populated and persisted here, but <b>nothing reads it yet</b> —
    /// <see cref="EnchantmentBlueprints"/> still answers from the per-hero
    /// <c>HeroExtendedInfo.KnownEnchantmentBlueprints</c> lists. Phase 2 flips that read
    /// over; until then this is deliberately write-only so a mistake in here cannot affect
    /// play.
    ///
    /// Persisted as a <see cref="List{T}"/> rather than a <see cref="HashSet{T}"/> because
    /// nothing in this codebase syncs a HashSet through <see cref="IDataStore"/> and the
    /// save system's container support for one is unverified. <see cref="_index"/> is the
    /// runtime lookup, rebuilt from the list on load.
    /// </summary>
    public class EnchantmentBlueprintBehavior : CampaignBehaviorBase
    {
        private List<string> _knownBlueprints = [];
        private readonly HashSet<string> _index = [];

        public static EnchantmentBlueprintBehavior Instance =>
            Campaign.Current?.GetCampaignBehavior<EnchantmentBlueprintBehavior>();

        /// <summary>
        /// The stored blueprint ids. Phase 1 exposes this for the
        /// <c>tor.check_enchantment_blueprint_store</c> diagnostic only — no gameplay path
        /// should read it until phase 2.
        /// </summary>
        public IReadOnlyCollection<string> Known => _index;

        public override void RegisterEvents()
        {
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
            TORCampaignEvents.Instance.EnchantmentLearned += OnEnchantmentLearned;
        }

        private void OnAfterSessionLaunched(CampaignGameStarter starter) => AbsorbPartyBlueprints();

        /// <summary>
        /// Unions the party's existing per-hero lists into the store. Runs on every session
        /// launch rather than once behind a flag: it is purely additive and idempotent, so
        /// re-running it costs nothing and self-heals any grant whose event was missed (for
        /// example one raised before <see cref="MobileParty.MainParty"/> existed).
        /// </summary>
        /// <remarks>
        /// Only walks the *current* party, so a companion who left before this first ran does
        /// not contribute. That matches today's behaviour, where the enchanting table already
        /// unions over current party members only — it is not a new loss.
        /// </remarks>
        private void AbsorbPartyBlueprints()
        {
            foreach (var blueprintId in EnchantmentBlueprints.GetKnown())
            {
                Add(blueprintId);
            }
        }

        private void OnEnchantmentLearned(object sender, EnchantmentLearnedEventArgs e)
        {
            if (!ShouldRecord(e?.Hero)) return;
            Add(e.EnchantmentTrait);
        }

        /// <summary>
        /// The store tracks what the *player* can craft, so a blueprint granted to an
        /// unrelated hero (a console command aimed at a lord, say) is ignored.
        /// </summary>
        private static bool ShouldRecord(Hero hero)
        {
            if (hero == null) return false;
            if (hero == Hero.MainHero) return true;
            return MobileParty.MainParty.GetMemberHeroes().Contains(hero);
        }

        private void Add(string blueprintId)
        {
            if (string.IsNullOrEmpty(blueprintId)) return;
            if (!_index.Add(blueprintId)) return;
            _knownBlueprints.Add(blueprintId);
        }

        private void RebuildIndex()
        {
            _index.Clear();
            foreach (var blueprintId in _knownBlueprints)
            {
                if (!string.IsNullOrEmpty(blueprintId)) _index.Add(blueprintId);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_knownBlueprints", ref _knownBlueprints);
            _knownBlueprints ??= [];
            RebuildIndex();
        }
    }
}
