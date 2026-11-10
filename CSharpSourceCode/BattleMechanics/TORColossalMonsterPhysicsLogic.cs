using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics
{
    /// <summary>
    /// Ensures Colossal Monsters (Cygor, Treeman) cannot be pushed around by surrounding infantry,
    /// and actively shove/displace smaller units out of their path when moving.
    /// NOTE: We ONLY displace the surrounding smaller units. We NEVER call TeleportToPosition
    /// on the colossal monster itself, ensuring 100% smooth turning, rotation, and pathfinding.
    /// </summary>
    public class TORColossalMonsterPhysicsLogic : MissionLogic
    {
        private readonly List<Agent> _trackedColossalAgents = new List<Agent>();
        private readonly MBList<Agent> _nearbyAgentsCache = new MBList<Agent>();

        // Cygor personal perimeter ring
        private const float CygorPersonalRadius = 2.2f;
        private const float TreemanPersonalRadius = 1.65f;

        private static bool IsColossalMonster(Agent agent)
        {
            if (agent == null || !agent.IsHuman) return false;
            string monsterId = agent.Monster?.StringId;
            return monsterId == "cygor"
                || monsterId == "cygor_settlement"
                || monsterId == "large_humanoid_monster"
                || monsterId == "large_humanoid_monster_settlement";
        }

        private static float GetPersonalRadius(Agent colossalAgent)
        {
            string monsterId = colossalAgent.Monster?.StringId;
            if (monsterId == "cygor" || monsterId == "cygor_settlement")
            {
                return CygorPersonalRadius;
            }
            return TreemanPersonalRadius;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            if (IsColossalMonster(agent) && !_trackedColossalAgents.Contains(agent))
            {
                _trackedColossalAgents.Add(agent);
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
            if (affectedAgent != null)
            {
                _trackedColossalAgents.Remove(affectedAgent);
            }
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Mission == null || dt <= 0f || _trackedColossalAgents.Count == 0) return;

            for (int i = _trackedColossalAgents.Count - 1; i >= 0; i--)
            {
                Agent monster = _trackedColossalAgents[i];
                if (monster == null || !monster.IsActive())
                {
                    _trackedColossalAgents.RemoveAt(i);
                    continue;
                }

                ProcessColossalMonsterDisplacement(monster, dt);
            }
        }

        private void ProcessColossalMonsterDisplacement(Agent monster, float dt)
        {
            Vec3 currentPos = monster.Position;
            Vec2 movementInput = monster.MovementInputVector;
            Vec3 currentVelocity = monster.Velocity;
            string currentActionName = monster.GetCurrentAction(0).GetName();

            bool isKnockedBack = currentActionName != null &&
                (currentActionName.Contains("knock_back") || currentActionName.Contains("strike"));

            bool isMoving = !isKnockedBack &&
                (movementInput.LengthSquared > 0.01f ||
                 currentVelocity.AsVec2.LengthSquared > 0.05f ||
                 (currentActionName != null && (currentActionName.Contains("walk") || currentActionName.Contains("run"))));

            float personalRadius = GetPersonalRadius(monster);

            // Displace surrounding smaller units:
            // 1. When stationary, keeps infantry clamped outside his personal radius so they cannot crowd/push him.
            // 2. When moving, actively shoves smaller units radially and forward along his movement path ("Bulldozer").
            PushSmallerUnitsOutOfPerimeter(
                monster,
                currentPos,
                isMoving ? currentVelocity.AsVec2 : Vec2.Zero,
                isMoving ? personalRadius + 0.15f : personalRadius,
                isMoving,
                dt);
        }

        private void PushSmallerUnitsOutOfPerimeter(
            Agent monster,
            Vec3 monsterPos,
            Vec2 monsterVelocityXY,
            float minRadius,
            bool isMoving,
            float dt)
        {
            _nearbyAgentsCache.Clear();
            Mission.GetNearbyAgents(monsterPos.AsVec2, minRadius + 0.5f, _nearbyAgentsCache);

            Vec2 monsterXY = monsterPos.AsVec2;
            Vec2 monsterForward = monster.Frame.rotation.f.AsVec2;
            if (monsterForward.LengthSquared > 0.001f) monsterForward.Normalize();

            for (int i = 0; i < _nearbyAgentsCache.Count; i++)
            {
                Agent other = _nearbyAgentsCache[i];
                if (other == null || other == monster || !other.IsActive()) continue;

                // Rival Colossal Monsters (e.g. Treeman vs Cygor) collide mutually and do not get teleport-shoved
                if (IsColossalMonster(other)) continue;

                // If the other agent is a rider on a mount, displace the mount entity rather than detaching the rider
                Agent targetToMove = other.MountAgent != null ? other.MountAgent : other;
                if (other.MountAgent != null && other != targetToMove) continue;

                Vec3 otherPos = targetToMove.Position;
                Vec2 toOther = otherPos.AsVec2 - monsterXY;
                float dist = toOther.Length;

                if (dist < minRadius)
                {
                    Vec2 pushDir;
                    if (dist > 0.001f)
                    {
                        pushDir = toOther * (1.0f / dist);
                    }
                    else
                    {
                        pushDir = monsterForward;
                    }

                    // When the Cygor is moving forward and a unit is in his front hemisphere,
                    // push the unit both radially outward and laterally/forward along the Cygor's movement!
                    if (isMoving && monsterVelocityXY.LengthSquared > 0.05f)
                    {
                        Vec2 moveDir = monsterVelocityXY.Normalized();
                        float inFront = Vec2.DotProduct(pushDir, moveDir);
                        if (inFront > -0.2f)
                        {
                            // Blend radial push with the Cygor's movement vector so they are carried/shoved where he moves
                            pushDir = (pushDir * 0.65f + moveDir * 0.35f).Normalized();
                        }
                    }

                    Vec2 targetXY = monsterXY + pushDir * (minRadius + 0.04f);
                    if (Mission.Scene != null)
                    {
                        float groundHeight = 0f;
                        if (Mission.Scene.GetHeightAtPoint(targetXY, BodyFlags.CommonCollisionExcludeFlagsForCombat, ref groundHeight))
                        {
                            // Only move to targetXY if there is no steep wall/cliff blocking the shove
                            if (MathF.Abs(groundHeight - otherPos.z) <= 1.2f)
                            {
                                targetToMove.TeleportToPosition(new Vec3(targetXY.x, targetXY.y, groundHeight));
                            }
                        }
                    }
                }
            }
        }
    }
}
