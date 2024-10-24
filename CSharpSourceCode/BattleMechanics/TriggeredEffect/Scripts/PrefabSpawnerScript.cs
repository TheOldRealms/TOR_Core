using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.Artillery;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.TriggeredEffect.Scripts
{
    public class PrefabSpawnerScript : ITriggeredScript
    {
        public string PrefabName { get; private set; }

        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            SpawnPrefab(position, triggeredByAgent);
        }

        private void SpawnPrefab(Vec3 position, Agent triggeredByAgent)
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
            entity.SetMobility(GameEntity.Mobility.dynamic);
            entity.EntityFlags = (entity.EntityFlags | EntityFlags.DontSaveToScene);
            var frame = new MatrixFrame(rotation, position);
            entity.SetGlobalFrameMT(frame);
            var artillery = entity.GetFirstScriptInFamilyDescending<BaseFieldSiegeWeapon>();
            if (artillery != null)
            {
                artillery.SetSide(triggeredByAgent.Team.Side);
                artillery.Team = triggeredByAgent.Team;
                artillery.SetForcedUse(!triggeredByAgent.Team.IsPlayerTeam);
            }
        }

        internal void OnInit(string spawnPrefabName)
        {
            PrefabName = spawnPrefabName;
        }
    }
}