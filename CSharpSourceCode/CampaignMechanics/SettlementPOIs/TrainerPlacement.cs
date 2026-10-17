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

            var current = s.LocationComplex.GetLocationOfCharacter(hero);
            if (current != null && current != target)
            {
                var locChar = s.LocationComplex.GetLocationCharacterOfHero(hero);
                s.LocationComplex.ChangeLocation(locChar, current, target);
                return;
            }

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
