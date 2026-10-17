using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions
{
    /// <summary>
    /// MissionLogic that spawns a single <see cref="Hero"/> as a civilian agent in a
    /// POI visit mission. Decoupled from <see cref="PoiVisitMissionController"/> so
    /// that the visit controller stays focused on player-spawn + leave handling, and
    /// future POIs can mix in multiple spawners (e.g. a damsel + a seneschal) by just
    /// adding more of these to the mission's behavior list.
    /// </summary>
    public class PoiCivilianAgentSpawner : MissionLogic
    {
        private readonly Hero _hero;

        public PoiCivilianAgentSpawner(Hero hero)
        {
            _hero = hero;
        }

        public override void EarlyStart()
        {
            base.EarlyStart();
            if (_hero?.CharacterObject == null) return;

            var frame = GetSpawnFrame();
            var agentBuildData = new AgentBuildData(_hero.CharacterObject)
                .Team(Mission.PlayerTeam)
                .InitialPosition(frame.origin)
                .InitialDirection(frame.rotation.f.AsVec2.Normalized())
                .NoHorses(true)
                .CivilianEquipment(true);

            var agent = Mission.SpawnAgent(agentBuildData);
            agent.FadeIn();
        }

        /// <summary>
        /// Try a few common SandBox civilian spawn tags; fall back to a position a short
        /// distance in front of the player so the NPC is visible.
        /// </summary>
        private MatrixFrame GetSpawnFrame()
        {
            foreach (var tag in new[] { "sp_civilian_1", "sp_civilian_2", "sp_npc_common_1", "spawnpoint_npc_common" })
            {
                var entity = Mission.Scene.FindEntityWithTag(tag);
                if (entity != null) return entity.GetGlobalFrame();
            }
            var playerSpawn = Mission.Scene.FindEntityWithTag("spawnpoint_player");
            if (playerSpawn != null)
            {
                var frame = playerSpawn.GetGlobalFrame();
                frame.origin += frame.rotation.f * 3f;
                return frame;
            }
            return MatrixFrame.Identity;
        }
    }
}
