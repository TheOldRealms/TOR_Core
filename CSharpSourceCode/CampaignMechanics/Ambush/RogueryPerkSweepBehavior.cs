using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Clears vanilla Roguery perks off every hero at session launch.
    /// Idempotent: running against a hero with no Roguery perks is a no-op.
    /// </summary>
    public class RogueryPerkSweepBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        /// <summary>Sweep hook. Clears vanilla Roguery perks from every alive hero.</summary>
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            foreach (var hero in Hero.AllAliveHeroes)
            {
                PerkHelper.ClearPerksForSkill(hero, DefaultSkills.Roguery);
            }
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
