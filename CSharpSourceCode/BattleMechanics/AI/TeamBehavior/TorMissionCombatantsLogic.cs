using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.TeamBehavior.Tactic;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.AI.TeamBehavior
{
    public class TorMissionCombatantsLogic : MissionCombatantsLogic
    {
        private readonly IEnumerable<IBattleCombatant> _battleCombatants;
        private readonly Mission.MissionTeamAITypeEnum _teamAIType;
        
        public TorMissionCombatantsLogic(IEnumerable<IBattleCombatant> battleCombatants, IBattleCombatant playerBattleCombatant, IBattleCombatant defenderLeaderBattleCombatant, IBattleCombatant attackerLeaderBattleCombatant, Mission.MissionTeamAITypeEnum teamAIType, bool isPlayerSergeant) :
            base(battleCombatants, playerBattleCombatant, defenderLeaderBattleCombatant, attackerLeaderBattleCombatant, teamAIType, isPlayerSergeant)
        {
            if (battleCombatants == null)
                battleCombatants = new IBattleCombatant[2]
                {
                    defenderLeaderBattleCombatant,
                    attackerLeaderBattleCombatant
                };
            _battleCombatants = battleCombatants;
            _teamAIType = teamAIType;
        }

        //Copy-paste of parent to avoid complications.
        public override void EarlyStart()
        {
            Mission.Current.MissionTeamAIType = _teamAIType;
            switch (_teamAIType)
            {
                case Mission.MissionTeamAITypeEnum.FieldBattle:
                    using (IEnumerator<Team> enumerator = Mission.Current.Teams.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Team current = enumerator.Current;
                            current.AddTeamAI(new TORTeamAIGeneral(Mission, current));
                        }

                        break;
                    }
                case Mission.MissionTeamAITypeEnum.Siege:
                    using (IEnumerator<Team> enumerator = Mission.Current.Teams.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Team current = enumerator.Current;
                            if (current.Side == BattleSideEnum.Attacker)
                                current.AddTeamAI(new TeamAISiegeAttacker(Mission, current, 5f, 1f));
                            if (current.Side == BattleSideEnum.Defender)
                                current.AddTeamAI(new TeamAISiegeDefender(Mission, current, 5f, 1f));
                        }

                        break;
                    }
                case Mission.MissionTeamAITypeEnum.SallyOut:
                    using (IEnumerator<Team> enumerator = Mission.Current.Teams.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Team current = enumerator.Current;
                            if (current.Side == BattleSideEnum.Attacker)
                                current.AddTeamAI(new TeamAISallyOutDefender(Mission, current, 5f, 1f));
                            else
                                current.AddTeamAI(new TeamAISallyOutAttacker(Mission, current, 5f, 1f));
                        }

                        break;
                    }
            }

            if (!Mission.Current.Teams.Any())
                return;
            switch (Mission.Current.MissionTeamAIType)
            {
                case Mission.MissionTeamAITypeEnum.NoTeamAI:
                    using (IEnumerator<Team> enumerator = Mission.Current.Teams.Where(t => t.HasTeamAi).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Team current = enumerator.Current;
                            current.AddTacticOption(new TacticCharge(current));
                        }

                        break;
                    }
                case Mission.MissionTeamAITypeEnum.FieldBattle:
                    using (IEnumerator<Team> enumerator = Mission.Current.Teams.Where(t => t.HasTeamAi).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Team team = enumerator.Current;
                            int num = _battleCombatants.Where(bc => bc.Side == team.Side).Max(bcs => bcs.GetTacticsSkillAmount());
                            team.AddTacticOption(new TacticCharge(team));
                            if (num >= 20.0)
                            {
                                team.AddTacticOption(new TacticFullScaleAttack(team));
                                if (team.Side == BattleSideEnum.Defender)
                                {
                                    team.AddTacticOption(new TacticDefensiveEngagement(team));
                                    team.AddTacticOption(new TacticDefensiveLine(team));
                                    team.AddTacticOption(new TacticPositionalArtillery(team)); 
                                }

                                if (team.Side == BattleSideEnum.Attacker)
                                {
                                    team.AddTacticOption(new TacticRangedHarrassmentOffensive(team));
                                }
                               
                            }

                            if (num >= 50.0)
                            {
                                team.AddTacticOption(new TacticFrontalCavalryCharge(team));
                                if (team.Side == BattleSideEnum.Defender)
                                {
                                    team.AddTacticOption(new TacticDefensiveRing(team));
                                    team.AddTacticOption(new TacticHoldChokePoint(team));
                                }

                                if (team.Side == BattleSideEnum.Attacker)
                                    team.AddTacticOption(new TacticCoordinatedRetreat(team));
                            }
                        }

                        break;
                    }
                case Mission.MissionTeamAITypeEnum.Siege:
                    using (IEnumerator<Team> enumerator = Mission.Current.Teams.Where(t => t.HasTeamAi).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Team current = enumerator.Current;
                            if (current.Side == BattleSideEnum.Attacker)
                                current.AddTacticOption(new TacticBreachWalls(current));
                            if (current.Side == BattleSideEnum.Defender)
                                current.AddTacticOption(new TacticDefendCastle(current));
                        }

                        break;
                    }
                case Mission.MissionTeamAITypeEnum.SallyOut:
                    using (IEnumerator<Team> enumerator = Mission.Current.Teams.Where(t => t.HasTeamAi).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Team current = enumerator.Current;
                            if (current.Side == BattleSideEnum.Defender)
                                current.AddTacticOption(new TacticSallyOutHitAndRun(current));
                            if (current.Side == BattleSideEnum.Attacker)
                                current.AddTacticOption(new TacticSallyOutDefense(current));
                            current.AddTacticOption(new TacticCharge(current));
                        }

                        break;
                    }
            }

            foreach (Team team in Mission.Teams)
            {
                team.QuerySystem.Expire();
                team.ResetTactic();
            }
        }

        public static TorMissionCombatantsLogic CreateFromInstance(MissionCombatantsLogic missionCombatantsLogic)
        {
            return new TorMissionCombatantsLogic(
                Traverse.Create(missionCombatantsLogic).Field("_battleCombatants").GetValue() as IEnumerable<IBattleCombatant>,
                Traverse.Create(missionCombatantsLogic).Field("_playerBattleCombatant").GetValue() as IBattleCombatant,
                Traverse.Create(missionCombatantsLogic).Field("_defenderLeaderBattleCombatant").GetValue() as IBattleCombatant,
                Traverse.Create(missionCombatantsLogic).Field("_attackerLeaderBattleCombatant").GetValue() as IBattleCombatant,
                (Traverse.Create(missionCombatantsLogic).Field("_teamAIType").GetValue() as Mission.MissionTeamAITypeEnum?).Value,
                (Traverse.Create(missionCombatantsLogic).Field("_isPlayerSergeant").GetValue() as bool?).Value);
        }
    }
}