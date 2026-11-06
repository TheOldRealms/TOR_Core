using System.Collections.Generic;
using SandBox.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.CampaignMechanics.Formations;

namespace TOR_Core.Models
{
    public class TORBattleSpawnModel : SandboxBattleSpawnModel
    {
        // deployment troops and their formations use tor culture roles and saved player assignments 
        public override List<(IAgentOriginBase origin, int formationIndex)> GetInitialSpawnAssignments(BattleSideEnum battleSide, List<IAgentOriginBase> troopOrigins)
        {
            var assignments = base.GetInitialSpawnAssignments(battleSide, troopOrigins);
            if (!TORFormationAssignments.AppliesToPlayerTeam(battleSide))
            {
                return assignments;
            }

            for (var i = 0; i < assignments.Count; i++)
            {
                var (origin, _) = assignments[i];
                var team = Mission.GetAgentTeam(origin, true);
                if (TORFormationAssignments.TryGetFormation(origin, team, out var formation))
                {
                    assignments[i] = (origin, (int)formation);
                }
            }
            return assignments;
        }

        public override List<(IAgentOriginBase origin, int formationIndex)> GetReinforcementAssignments(BattleSideEnum battleSide, List<IAgentOriginBase> troopOrigins)
        {
            if (!TORFormationAssignments.AppliesToPlayerTeam(battleSide))
            {
                return base.GetReinforcementAssignments(battleSide, troopOrigins);
            }

            var vanillaTroops = new List<IAgentOriginBase>();
            var assignments = new List<(IAgentOriginBase origin, int formationIndex)>();
            foreach (var origin in troopOrigins)
            {
                var team = Mission.GetAgentTeam(origin, true);
                if (TORFormationAssignments.TryGetFormation(origin, team, out var formation))
                {
                    assignments.Add((origin, (int)formation));
                }
                else
                {
                    vanillaTroops.Add(origin);
                }
            }

            if (vanillaTroops.Count > 0)
            {
                assignments.AddRange(base.GetReinforcementAssignments(battleSide, vanillaTroops));
            }
            return assignments;
        }
    }
}