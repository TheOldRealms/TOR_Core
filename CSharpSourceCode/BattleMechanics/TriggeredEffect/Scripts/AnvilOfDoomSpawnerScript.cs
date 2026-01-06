using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.TriggeredEffect.Scripts
{
    public class AnvilOfDoomSpawnerScript : ITriggeredScript
    {
        public string PrefabName { get; private set; }

        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            SpawnAnvil(position, triggeredByAgent);
        }

        private void SpawnAnvil(Vec3 position, Agent triggeredByAgent)
        {
            var team = Mission.Current.GetEnemyTeamsOf(triggeredByAgent.Team).FirstOrDefault();
            Vec3 target = Vec3.Invalid;
            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                target = team.GetMedianPosition(team.GetAveragePosition()).GetGroundVec3MT();
            }
            if (!target.IsValid) return;
            var direction = (target - position).NormalizedCopy();
            var rotation = Mat3.CreateMat3WithForward(-direction);
            var entity = GameEntity.Instantiate(Mission.Current.Scene, PrefabName, true); 
            entity.SetMobility(GameEntity.Mobility.Dynamic);
            entity.EntityFlags = (entity.EntityFlags | EntityFlags.DontSaveToScene);
            entity.SetPhysicsState(true, true);
            var frame = new MatrixFrame(rotation, position);
            entity.SetGlobalFrame(frame);

            // Store the anvil position for RuneMagic proximity check
            var component = triggeredByAgent.GetComponent<AbilityComponent>();
            if (component != null)
            {
                component.AnvilOfDoomPosition = position;
            }
        }

        internal void OnInit(string spawnPrefabName)
        {
            PrefabName = spawnPrefabName;
        }
    }
}
