using SandBox.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;

namespace TOR_Core.BattleMechanics
{
    public class TORBattleAgentLogic : BattleAgentLogic
    {
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (agent.Origin is SummonedAgentOrigin) return;
            base.OnAgentBuild(agent, banner);
        }

        public override void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
        {
            if (agent.Origin is SummonedAgentOrigin) return;
            base.OnAgentTeamChanged(prevTeam, newTeam, agent);
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            if (affectedAgent.Origin is SummonedAgentOrigin) return;
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
        }

        public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
        {
            if (affectorAgent.Origin is SummonedAgentOrigin) return;
            base.OnScoreHit(affectedAgent, affectorAgent, attackerWeapon, isBlocked, isSiegeEngineHit, blow, collisionData, damagedHp, hitDistance, shotDifficulty);
        }
    }
}
