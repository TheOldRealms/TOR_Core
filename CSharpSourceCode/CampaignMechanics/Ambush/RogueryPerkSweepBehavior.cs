using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Clears vanilla Roguery perks off every hero on session launch. Vanilla's own
    /// <c>Hero.ClearChangedPerks</c> trash-filters IsTrash perks in <c>AfterLoad</c>,
    /// but new campaigns don't hit AfterLoad on template-baked heroes, so perks
    /// seeded through <c>characterData.UnlockedPerks</c> would otherwise stick.
    /// Running every session is idempotent — calling ClearPerksForSkill on a
    /// hero with no Roguery perks set is a no-op.
    /// </summary>
    public class RogueryPerkSweepBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

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
