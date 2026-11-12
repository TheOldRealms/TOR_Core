using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    public static class TrainerPlacement
    {
        public static bool IsAt(Settlement s, Hero hero, Location target)
        {
            if (s?.LocationComplex == null || hero == null || target == null) return false;
            return target.GetLocationCharacter(hero) != null;
        }

        public static void EnsureAt(Settlement s, Hero hero, Location target)
        {
            if (s?.LocationComplex == null || hero == null || target == null) return;
            if (target.GetLocationCharacter(hero) != null) return;

            // Hero drifted to another Location (vanilla's notable placement
            // likely put them at the keep on first visit): move the existing
            // LocationCharacter to our target, don't spawn a duplicate.
            var current = s.LocationComplex.GetLocationOfCharacter(hero);
            if (current != null && current != target)
            {
                var locChar = s.LocationComplex.GetLocationCharacterOfHero(hero);
                if (locChar != null)
                {
                    s.LocationComplex.ChangeLocation(locChar, current, target);
                    return;
                }
            }

            // Hero has no LocationCharacter yet anywhere in this complex —
            // first-time placement in this settlement visit.
            var fresh = new LocationCharacter(
                new AgentData(new SimpleAgentOrigin(hero.CharacterObject, -1, null, default))
                    .Monster(FaceGen.GetBaseMonsterFromRace(hero.CharacterObject.Race)),
                SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors,
                "npc_common", true,
                LocationCharacter.CharacterRelations.Neutral,
                null, true, false, null, false, false, true);
            target.AddCharacter(fresh);
        }
    }
}
