using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.AI.AgentBehavior.Components;
using TOR_Core.BattleMechanics.AI.FormationBehavior;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.AI.TeamBehavior
{
    public class TacticArtilleryBombardment : TacticDefensiveLine
    {
        private bool _usingMachines = true;
        private readonly Formation _artilleryFormation;
        private readonly Formation _guardFormation;

        public TacticArtilleryBombardment(Team team) : base(team)
        {
            _artilleryFormation = new Formation(this.team, this.team.FormationsIncludingSpecialAndEmpty.Count);
            this.team.FormationsIncludingSpecialAndEmpty.Add(_artilleryFormation);
            _guardFormation = new Formation(this.team, this.team.FormationsIncludingSpecialAndEmpty.Count);
            this.team.FormationsIncludingSpecialAndEmpty.Add(_guardFormation);
            var method = Traverse.Create(this.team).Method("FormationAI_OnActiveBehaviorChanged").GetValue();
            //_artilleryFormation.AI.OnActiveBehaviorChanged += new Action<Formation>(this.team.FormationAI_OnActiveBehaviorChanged);
            //_guardFormation.AI.OnActiveBehaviorChanged += new Action<Formation>(this.team.FormationAI_OnActiveBehaviorChanged);
        }

        protected override void ManageFormationCounts()
        {
            base.ManageFormationCounts();
            var formationAI = _guardFormation.AI;
            var allFormations = team.Formations.ToList();
            var infantryFormations = team.Formations.ToList().FindAll(formation => formation.QuerySystem.IsInfantryFormation);
            var updatedFormations = new List<Formation>();
            foreach (var agent in allFormations.SelectMany(form => form.Arrangement.GetAllUnits()).ToList().Select(unit => (Agent) unit))
            {
                if (agent.Name.Contains("Engineer"))
                {
                    if (!updatedFormations.Contains(agent.Formation))
                        updatedFormations.Add(agent.Formation);
                    agent.Formation = _artilleryFormation;
                }
            }

            if (infantryFormations.Count > 0 && _guardFormation.Arrangement.UnitCount <= 0)
            {
                var count = 50;

                foreach (var agent in infantryFormations.SelectMany(form => form.Arrangement.GetAllUnits()).ToList().Select(unit => (Agent) unit))
                {
                    count += -1;
                    if (count >= 0)
                    {
                        if (!updatedFormations.Contains(agent.Formation))
                            updatedFormations.Add(agent.Formation);
                        agent.Formation = _guardFormation;
                    }
                }
            }

            team.TriggerOnFormationsChanged(_artilleryFormation);
            team.TriggerOnFormationsChanged(_guardFormation);
            updatedFormations.ForEach(formation => team.TriggerOnFormationsChanged(formation));

            if (formationAI.GetBehavior<BehaviorProtectArtillery>() == null)
            {
                formationAI.AddAiBehavior(new BehaviorProtectArtillery(_guardFormation, _artilleryFormation, this));
                _guardFormation.AI.SetBehaviorWeight<BehaviorProtectArtillery>(15.0f);
            }
        }

        protected override void TickOccasionally()
        {
            base.TickOccasionally();
            //    ManageFormationCounts();
        }

        protected override void OnCancel()
        {
            _usingMachines = false;
            StopUsingAllMachines();
        }

        protected override void StopUsingAllMachines()
        {
            if (_usingMachines) return; // A way to cancel out a call in the tick() method that we dont otherwise want to modify.
            base.StopUsingAllMachines();
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
                ? 10000f
                : 0f;
        }
    }
}