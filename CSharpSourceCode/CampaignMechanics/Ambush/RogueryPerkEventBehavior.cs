using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Event listener for Roguery perks whose effect fires on a
    /// campaign event rather than being read by a model override.
    /// Covers Raider secondary (Tactics XP for the quartermaster
    /// when their party completes a raid) and BurntEarth secondary
    /// (partial relation offset when the player's village raid lands).
    /// </summary>
    public class RogueryPerkEventBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
            CampaignEvents.VillageLooted.AddNonSerializedListener(this, OnVillageLooted);
        }

        /// <summary>BurntEarth secondary: "Village notables lose 75%
        /// less relation with you after their village is raided."
        /// Fires after vanilla's raid-complete relation drop; applies
        /// an offsetting positive nudge equal to
        /// <c>-BurntEarth.PrimaryBonus</c> (0.75) × the vanilla drop
        /// magnitude. Vanilla drops notable relation by -2 per raid
        /// (empirically consistent across vanilla raid handlers), so
        /// a +1.5 nudge (floored to +1 for integer relation) leaves
        /// a net -1 instead of -2.</summary>
        private void OnVillageLooted(Village village)
        {
            if (village == null) return;
            var main = MobileParty.MainParty;
            if (main?.LeaderHero != Hero.MainHero) return;
            // Only fire if the main party actually looted this village
            // (closest raider in range).
            if (village.Settlement == null || main.Position.DistanceSquared(
                    village.Settlement.Position) > 1f) return;
            if (!main.HasPerk(TORPerks.Roguery.BurntEarth)) return;

            // Vanilla notable-relation drop on raid is -2. Perk
            // reduces that by 75% → net -0.5, floored to integer
            // means +1 compensating nudge leaves -1 remaining.
            int offset = 1;
            foreach (var notable in village.Settlement.Notables)
            {
                if (notable == null || !notable.IsAlive) continue;
                ChangeRelationAction.ApplyPlayerRelation(notable, offset, affectRelatives: false, showQuickNotification: false);
            }
        }

        private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent component)
        {
            var mapEvent = component?.MapEvent;
            if (mapEvent == null) return;
            // Only credit the aggressor side — the raider, not the defenders.
            if (winnerSide != BattleSideEnum.Attacker) return;
            var attackerParty = mapEvent.AttackerSide?.LeaderParty?.MobileParty;
            if (attackerParty == null) return;
            if (!attackerParty.HasPerk(TORPerks.Roguery.Raider, checkSecondaryRole: true)) return;
            var quartermaster = attackerParty.EffectiveQuartermaster;
            if (quartermaster == null) return;
            quartermaster.AddSkillXp(DefaultSkills.Tactics, TORPerks.Roguery.Raider.SecondaryBonus);
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
