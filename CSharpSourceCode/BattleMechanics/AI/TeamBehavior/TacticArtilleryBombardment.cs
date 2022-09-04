using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.AI.AgentBehavior.Components;

namespace TOR_Core.BattleMechanics.AI.TeamBehavior
{
    public class TacticArtilleryBombardment : TacticDefensiveLine
    {
        public TacticArtilleryBombardment(Team team) : base(team)
        {
            // TODO
        }

        protected override void ManageFormationCounts()
        {
            ManageFormationCounts(2, 2, 1, 1);
            var rangedFormations = team.Formations.ToList().FindAll(formation => formation.QuerySystem.IsRangedFormation);
            var infantryFormations = team.Formations.ToList().FindAll(formation => formation.QuerySystem.IsInfantryFormation);
            if (rangedFormations.Count == 2)
            {
                foreach (var agent in rangedFormations.SelectMany(form => form.Arrangement.GetAllUnits()).ToList().Select(unit => (Agent) unit))
                {
                    if (agent.Name.Contains("Engineer"))
                    {
                        agent.Formation = rangedFormations[rangedFormations.Count - 1];
                    }
                    else
                    {
                        agent.Formation = rangedFormations[0];
                    }
                }

                team.TriggerOnFormationsChanged(rangedFormations[0]);
                team.TriggerOnFormationsChanged(rangedFormations[rangedFormations.Count - 1]);
            }

            if (infantryFormations.Count == 2)
            {
                var count = 50;

                foreach (var agent in infantryFormations.SelectMany(form => form.Arrangement.GetAllUnits()).ToList().Select(unit => (Agent) unit))
                {
                    count += -1;
                    if (count >= 0)
                    {
                        agent.Formation = infantryFormations[infantryFormations.Count - 1];
                    }
                    else
                    {
                        agent.Formation = infantryFormations[0];
                    }
                }

                team.TriggerOnFormationsChanged(infantryFormations[0]);
                team.TriggerOnFormationsChanged(infantryFormations[infantryFormations.Count - 1]);
            }
        }

        protected override void TickOccasionally()
        {
            base.TickOccasionally();
        }

        protected override bool CheckAndSetAvailableFormationsChanged()
        {
            return base.CheckAndSetAvailableFormationsChanged();
        }

        protected override void StopUsingAllMachines()
        {
            if (false)
            {
                foreach (Formation formation in this.Formations)
                {
                    foreach (UsableMachine usable in formation.GetUsedMachines().ToList<UsableMachine>())
                    {
                        formation.StopUsingMachine(usable);
                        if (usable is SiegeWeapon siegeWeapon)
                            siegeWeapon.ForcedUse = false;
                    }
                }
            }
        }

        protected void ResumeUsingMachines()
        {
            foreach (Formation formation in this.Formations)
            {
                foreach (UsableMachine usable in formation.GetUsedMachines().ToList<UsableMachine>())
                {
                    formation.StartUsingMachine(usable);
                }
            }
        }

        protected override float GetTacticWeight()
        {
            return team.GeneralAgent.GetComponent<AbilityComponent>().GetKnownAbilityTemplates().Exists(item => item.AbilityEffectType == AbilityEffectType.ArtilleryPlacement)
                   && team.GeneralAgent.Controller != Agent.ControllerType.Player
                ? 100f
                : 0f;
        }
    }
}