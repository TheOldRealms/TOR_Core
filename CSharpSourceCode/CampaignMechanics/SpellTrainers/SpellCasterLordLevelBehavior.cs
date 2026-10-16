using TaleWorlds.CampaignSystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.SpellTrainers
{
    /// <summary>
    /// Keeps every spellcaster-Lord at <see cref="SpellCastingLevel.Master"/>.
    ///
    /// Two hooks for coverage:
    ///
    /// <list type="bullet">
    ///   <item><description><c>HeroCreated</c> catches lords generated at
    ///   runtime (new game, clan expansion, etc.) so they come in at Master
    ///   immediately — no need to wait for the next session launch.</description></item>
    ///   <item><description><c>OnSessionLaunched</c> runs an idempotent sweep
    ///   so pre-existing saves that predate this behavior still get upgraded on
    ///   the next load.</description></item>
    /// </list>
    ///
    /// <para><b>REVISIT:</b> this behavior may no longer be needed. It was extracted
    /// from the legacy <c>SpellTrainerInTownBehavior.ForceLevelsForAllHeroes</c>
    /// sweep without re-validating that AI-lord-spellcaster progression actually
    /// still needs forcing. If NPC lord generation now sets a sensible casting
    /// level elsewhere (character templates, hero-creator flow, campaign model),
    /// this whole file can be deleted. Audit before relying on it long-term.</para>
    /// </summary>
    public class SpellCasterLordLevelBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
        }

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            foreach (var hero in Hero.AllAliveHeroes)
            {
                EnsureMasterLevelIfApplicable(hero);
            }
        }

        private void OnHeroCreated(Hero hero, bool showNotification)
        {
            EnsureMasterLevelIfApplicable(hero);
        }

        private static void EnsureMasterLevelIfApplicable(Hero hero)
        {
            if (hero == null || hero == Hero.MainHero) return;
            if (hero.Occupation != Occupation.Lord) return;
            if (!hero.IsSpellCaster()) return;

            var info = hero.GetExtendedInfo();
            if (info == null || info.SpellCastingLevel == SpellCastingLevel.Master) return;

            hero.SetSpellCastingLevel(SpellCastingLevel.Master);
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
