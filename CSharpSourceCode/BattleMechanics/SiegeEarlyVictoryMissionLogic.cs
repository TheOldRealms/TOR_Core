using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics
{
    /// <summary>
    /// allows the player to skip the last few stuck troops in sieges when it is clear 
    /// that the player has won the battle and the enemy has no more troops to spawn
    /// </summary>
    public class SiegeEarlyVictoryMissionLogic : MissionLogic
    {
        private DefaultBattleMissionAgentSpawnLogic _spawnLogic;
        private int _enemyDefendersNeededForPullBack;
        private bool _finishBattle;
        private bool _continueToLordsHall;

        public override void EarlyStart()
        {
            base.EarlyStart();
            _spawnLogic = Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();

            if (Mission.PlayerTeam.Side == BattleSideEnum.Attacker)
            {
                _enemyDefendersNeededForPullBack =
                    Campaign.Current.Models.SiegeLordsHallFightModel.DefenderTroopNumberForSuccessfulPullBack;
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            if (Mission.PlayerTeam.Side == BattleSideEnum.Attacker &&
                _enemyDefendersNeededForPullBack > 0 &&
                affectedAgent.IsHuman &&
                agentState == AgentState.Routed &&
                affectedAgent.Team != null &&
                affectedAgent.Team.Side == BattleSideEnum.Defender)
            {
                _enemyDefendersNeededForPullBack--;
            }
        }

        public override void OnMissionTick(float dt)
        {
            if (_finishBattle || !Mission.InputManager.IsHotKeyPressed("HoldShow"))
            {
                return;
            }

            var playerSide = Mission.PlayerTeam.Side;
            var enemySide = playerSide.GetOppositeSide();
            var playerActiveTroops = 0;
            var enemyActiveTroops = 0;

            foreach (var team in Mission.Teams)
            {
                foreach (var agent in team.ActiveAgents)
                {
                    if (agent.IsMount)
                    {
                        continue;
                    }

                    if (team.Side == playerSide)
                    {
                        playerActiveTroops++;
                    }
                    else if (team.Side == enemySide)
                    {
                        enemyActiveTroops++;
                    }
                }
            }

            // siege can only be skipped when less than 15 enemy troops remain
            if (enemyActiveTroops == 0 || enemyActiveTroops >= 15)
            {
                return;
            }

            var enemyRemainingTroops = enemySide == BattleSideEnum.Attacker
                ? _spawnLogic.NumberOfRemainingAttackerTroops
                : _spawnLogic.NumberOfRemainingDefenderTroops;

            // another reinforcement wave still has troops to spawn
            if (enemyRemainingTroops != 0)
            {
                return;
            }

            var playerRemainingTroops = playerSide == BattleSideEnum.Attacker
                ? _spawnLogic.NumberOfRemainingAttackerTroops
                : _spawnLogic.NumberOfRemainingDefenderTroops;

            if (playerActiveTroops + playerRemainingTroops < enemyActiveTroops * 4)
            {
                return;
            }

            // continue to keep battle going if the player is the attacker and the enemy has enough troops to pull back to the lords hall
            if (playerSide == BattleSideEnum.Attacker && _enemyDefendersNeededForPullBack <= 0)
            {
                _continueToLordsHall = true;
                _finishBattle = true;
                return;
            }

            // defender pullback threshold has not been reached and a keep fight won't be possible
            // surviving defenders are order retreated to preserve their campaign affiliations
            foreach (var team in Mission.Teams)
            {
                if (team.Side != enemySide)
                {
                    continue;
                }

                foreach (var agent in team.ActiveAgents)
                {
                    if (!agent.IsMount)
                    {
                        agent.Origin.SetRouted(isOrderRetreat: true);
                    }
                }
            }

            _finishBattle = true;
        }

        public override bool MissionEnded(ref MissionResult missionResult)
        {
            if (!_finishBattle)
            {
                return false;
            }

            _spawnLogic.StopSpawner(BattleSideEnum.Attacker);
            _spawnLogic.StopSpawner(BattleSideEnum.Defender);

            if (_continueToLordsHall)
            {
                missionResult = MissionResult.CreateDefenderPushedBack();
                return true;
            }

            missionResult = MissionResult.CreateSuccessful(Mission, enemyRetreated: true);
            return true;
        }
    }
}