using System.Collections.Generic;
using System.Data;
using System.Linq;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.AI.AgentBehavior.Components;
using TOR_Core.BattleMechanics.AI.Decision;
using TOR_Core.BattleMechanics.AI.FormationBehavior;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.AI.TeamBehavior
{
    public class TacticArtilleryBombardment : TacticDefensiveLine
    {
        private bool _usingMachines = true;
        private readonly Formation _artilleryFormation;
        private readonly Formation _guardFormation;

        private List<Axis> _positionScoring;
        private List<Target> _latestScoredPositions;
        private Target _chosenPosition;
        private readonly List<WizardAIComponent> _artilleryPlacerComponents;

        public TacticArtilleryBombardment(Team team) : base(team)
        {
            _artilleryFormation = new Formation(this.team, (int) TORFormationClass.Artillery);
            this.team.FormationsIncludingSpecialAndEmpty.Add(_artilleryFormation);
            _guardFormation = new Formation(this.team, (int) TORFormationClass.ArtilleryGuard);
            this.team.FormationsIncludingSpecialAndEmpty.Add(_guardFormation);

            _positionScoring = CreateArtilleryPositionAssessment();
            _artilleryPlacerComponents = new List<WizardAIComponent>();

            //TODO: Reminder, might need this if certain updates dont work.
            // var method = Traverse.Create(this.team).Method("FormationAI_OnActiveBehaviorChanged").GetValue();
            // _artilleryFormation.AI.OnActiveBehaviorChanged += new Action<Formation>(this.team.FormationAI_OnActiveBehaviorChanged);
            // _guardFormation.AI.OnActiveBehaviorChanged += new Action<Formation>(this.team.FormationAI_OnActiveBehaviorChanged);
        }


        protected override void ManageFormationCounts()
        {
            base.ManageFormationCounts();
            var formationAI = _guardFormation.AI;
            var allFormations = team.FormationsIncludingSpecialAndEmpty.ToList();
            var infantryFormations = team.Formations.ToList().FindAll(formation => formation.QuerySystem.IsInfantryFormation);
            var updatedFormations = new List<Formation>();
            foreach (var agent in allFormations.SelectMany(form => form.Arrangement.GetAllUnits()).ToList().Select(unit => (Agent) unit))
            {
                if (agent.HasAttribute("ArtilleryCrew"))
                {
                    if (!updatedFormations.Contains(agent.Formation))
                        updatedFormations.Add(agent.Formation);
                    agent.Formation = _artilleryFormation;
                }

                var wizardAIComponent = agent.GetComponent<WizardAIComponent>();
                if (wizardAIComponent != null)
                {
                    _artilleryPlacerComponents.Add(wizardAIComponent);
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

            if (formationAI.GetBehavior<BehaviorProtectArtilleryCrew>() == null)
            {
                formationAI.AddAiBehavior(new BehaviorProtectArtilleryCrew(_guardFormation, _artilleryFormation, this));
            }
        }

        protected override void TickOccasionally()
        {
            base.TickOccasionally();

            if (team.FormationsIncludingSpecial.Any())
            {
                AssessArtilleryPositions();
                UpdatePlacerTargets();
                if (!_usingMachines)
                    ResumeUsingMachines();
            }


            SetGuardFormationBehaviorWeights();
            SetArtilleryFormationBehaviorWeights();
        }


        private void AssessArtilleryPositions()
        {
            _latestScoredPositions = team.TeamAI.TacticalPositions
                .Select(pos => new Target {TacticalPosition = pos})
                .Select(target =>
                {
                    target.UtilityValue = _positionScoring.GeometricMean(target);
                    return target;
                }).ToList();
            var candidate = _latestScoredPositions.MaxBy(target => target.UtilityValue);
            _chosenPosition = candidate;
        }


        private void UpdatePlacerTargets()
        {
            _artilleryPlacerComponents.ForEach(component => component.UpdateArtilleryTargetPosition(_chosenPosition));
        }

        private void SetGuardFormationBehaviorWeights()
        {
            _guardFormation.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_guardFormation);
            _guardFormation.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            _guardFormation.AI.SetBehaviorWeight<BehaviorProtectArtilleryCrew>(15.0f);
            _guardFormation.AI.SetBehaviorWeight<BehaviorDefend>(10).TacticalDefendPosition = _chosenPosition.TacticalPosition;
        }

        private void SetArtilleryFormationBehaviorWeights()
        {
            _artilleryFormation.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_artilleryFormation);
            var enemyDirection = (_chosenPosition.TacticalPosition.Position.AsVec2 - team.QuerySystem.AverageEnemyPosition).Normalized();
            _artilleryFormation.AI.SetBehaviorWeight<BehaviorDefend>(15f).DefensePosition = new WorldPosition(Mission.Current.Scene, _chosenPosition.TacticalPosition.Position.GetGroundVec3() + enemyDirection.ToVec3(0) * 12);
            _artilleryFormation.AI.SetBehaviorWeight<BehaviorSkirmishLine>(1f);
            _artilleryFormation.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
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
            foreach (UsableMachine usable in _artilleryFormation.GetUsedMachines().ToList<UsableMachine>())
            {
                _artilleryFormation.StartUsingMachine(usable);
            }

            _usingMachines = true;
        }

        protected override float GetTacticWeight()
        {
            return team.GeneralAgent.GetComponent<AbilityComponent>().GetKnownAbilityTemplates().Exists(item => item.AbilityEffectType == AbilityEffectType.ArtilleryPlacement)
                   && team.GeneralAgent.Controller != Agent.ControllerType.Player
                ? 10000f
                : 0f;
        }

        private List<Axis> CreateArtilleryPositionAssessment()
        {
            var function = new List<Axis>();
            var distance = team.QuerySystem.AveragePosition.Distance(team.QuerySystem.AverageEnemyPosition);
            function.Add(new Axis(0, distance / 5, x => x, CommonAIDecisionFunctions.TargetDistanceToHostiles(team)));
            function.Add(new Axis(0, distance / 4, x => 1 - x, CommonAIDecisionFunctions.TargetDistanceToOwnArmy(team)));
            function.Add(new Axis(0, 1, x => x, CommonAIDecisionFunctions.AssessPositionForArtillery()));
            return function;
        }
    }
}