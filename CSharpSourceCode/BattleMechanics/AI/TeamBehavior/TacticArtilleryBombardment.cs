using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.AI.Decision;
using TOR_Core.BattleMechanics.AI.FormationBehavior;

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

        public TacticArtilleryBombardment(Team team) : base(team)
        {
            _artilleryFormation = new Formation(this.team, (int) TORFormationClass.Artillery);
            this.team.FormationsIncludingSpecialAndEmpty.Add(_artilleryFormation);
            _guardFormation = new Formation(this.team, (int) TORFormationClass.ArtilleryGuard);
            this.team.FormationsIncludingSpecialAndEmpty.Add(_guardFormation);

            _positionScoring = CreateArtilleryPositionAssessment();

            //TODO: Reminder, might need this if certain updates dont work.
            // var method = Traverse.Create(this.team).Method("FormationAI_OnActiveBehaviorChanged").GetValue();
            // _artilleryFormation.AI.OnActiveBehaviorChanged += new Action<Formation>(this.team.FormationAI_OnActiveBehaviorChanged);
            // _guardFormation.AI.OnActiveBehaviorChanged += new Action<Formation>(this.team.FormationAI_OnActiveBehaviorChanged);
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
            }
        }

        protected override void TickOccasionally()
        {
            base.TickOccasionally();

            AssessArtilleryPositions();

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
            _chosenPosition = _latestScoredPositions.MaxBy(target => target.UtilityValue);
        }

        private void SetGuardFormationBehaviorWeights()
        {
            _guardFormation.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_guardFormation);
            _guardFormation.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            _guardFormation.AI.SetBehaviorWeight<BehaviorProtectArtillery>(15.0f);
            _guardFormation.AI.SetBehaviorWeight<BehaviorDefend>(10).TacticalDefendPosition = _chosenPosition.TacticalPosition;
        }

        private void SetArtilleryFormationBehaviorWeights()
        {
            _artilleryFormation.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_artilleryFormation);
            _artilleryFormation.AI.SetBehaviorWeight<BehaviorDefend>(15f).TacticalDefendPosition = _chosenPosition.TacticalPosition;
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

        private List<Axis> CreateArtilleryPositionAssessment()
        {
            var function = new List<Axis>();
            function.Add(new Axis(0, 70f, x => x, CommonAIDecisionFunctions.TargetDistanceToHostiles(team)));
            function.Add(new Axis(0, 1, x => x, CommonAIDecisionFunctions.AssessPositionForArtillery()));
            return function;
        }
    }
}