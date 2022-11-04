using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.AI.AgentBehavior.Components;
using TOR_Core.BattleMechanics.AI.Decision;
using TOR_Core.BattleMechanics.AI.FormationBehavior;
using TOR_Core.BattleMechanics.Artillery;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.AI.TeamBehavior
{
    public class TacticPositionalArtillery : TacticDefensiveLine
    {
        private readonly Formation _artilleryFormation;
        private readonly Formation _guardFormation;

        private List<Axis> _positionScoring; //Do not access this directly. Use the generator function public method below.
        public List<Axis> PositionScoring => _positionScoring ?? (_positionScoring = CreateArtilleryPositionAssessment());
        private List<Target> _latestScoredPositions;
        private readonly List<WizardAIComponent> _artilleryPlacerComponents;

        private Target _chosenArtilleryPosition;
        private TacticalPosition _mainDefensiveLinePosition;
        private TacticalPosition _linkedRangedDefensivePosition;

        private bool _usingMachines = true;
        private bool _hasBattleBeenJoined;


        public TacticPositionalArtillery(Team team) : base(team)
        {
            _artilleryFormation = new Formation(this.team, (int) TORFormationClass.Artillery);
            this.team.FormationsIncludingSpecialAndEmpty.Add(_artilleryFormation);
            _guardFormation = new Formation(this.team, (int) TORFormationClass.ArtilleryGuard);
            this.team.FormationsIncludingSpecialAndEmpty.Add(_guardFormation);

            _artilleryPlacerComponents = new List<WizardAIComponent>();

            //TODO: Reminder, might need this if certain updates dont work.
            // var method = Traverse.Create(this.team).Method("FormationAI_OnActiveBehaviorChanged").GetValue();
            // _artilleryFormation.AI.OnActiveBehaviorChanged += new Action<Formation>(this.team.FormationAI_OnActiveBehaviorChanged);
            // _guardFormation.AI.OnActiveBehaviorChanged += new Action<Formation>(this.team.FormationAI_OnActiveBehaviorChanged);
        }

        protected override float GetTacticWeight()
        {
            if (team.GeneralAgent.GetComponent<AbilityComponent>().GetKnownAbilityTemplates().Exists(item => item.AbilityEffectType == AbilityEffectType.ArtilleryPlacement)
                && team.GeneralAgent.Controller != Agent.ControllerType.Player)
                return 10000f; //TODO Improve

            if (!team.TeamAI.IsDefenseApplicable || !CheckAndDetermineFormation(ref _mainInfantry, f => f.QuerySystem.IsInfantryFormation))
                return 0.0f;
            // if (!team.TeamAI.IsCurrentTactic(this) || _mainDefensiveLinePosition == null || !IsTacticalPositionEligible(_mainDefensiveLinePosition))
            //     DeterminePositions();

            return 0.0f;
        }

        protected override void ManageFormationCounts()
        {
            AssignTacticFormations1121();

            var allFormations = team.FormationsIncludingSpecialAndEmpty.ToList();
            var infantryFormations = team.Formations.ToList().FindAll(formation => formation.QuerySystem.IsInfantryFormation);
            var updatedFormations = new List<Formation>();

            allFormations.SelectMany(form => form.Arrangement.GetAllUnits()).ToList().Select(unit => (Agent) unit).ToList().ForEach(agent =>
            {
                if (agent.HasAttribute("ArtilleryCrew"))
                {
                    if (!updatedFormations.Contains(agent.Formation))
                        updatedFormations.Add(agent.Formation);
                    if (!updatedFormations.Contains(_artilleryFormation))
                        updatedFormations.Add(_artilleryFormation);
                    agent.Formation = _artilleryFormation;
                }

                var wizardAIComponent = agent.GetComponent<WizardAIComponent>();
                if (wizardAIComponent != null)
                {
                    _artilleryPlacerComponents.Add(wizardAIComponent);
                }
            });

            if (infantryFormations.Count > 0)
            {
                var count = infantryFormations.Sum(form => form.Arrangement.UnitCount) * 0.1;
                {
                    count += count < _artilleryFormation.Arrangement.UnitCount ? 10 : 0;
                }
                count -= _guardFormation.Arrangement.UnitCount;


                infantryFormations.SelectMany(form => form.Arrangement.GetAllUnits()).ToList().Select(unit => (Agent) unit).ToList().ForEach(agent =>
                {
                    count += -1;
                    if (count >= 0)
                    {
                        if (!updatedFormations.Contains(agent.Formation))
                            updatedFormations.Add(agent.Formation);
                        if (!updatedFormations.Contains(_artilleryFormation))
                            updatedFormations.Add(_guardFormation);
                        agent.Formation = _guardFormation;
                    }
                });
            }

            updatedFormations.ForEach(formation => team.TriggerOnFormationsChanged(formation));
            team.TeamAI.OnUnitAddedToFormationForTheFirstTime(_artilleryFormation);
            team.TeamAI.OnUnitAddedToFormationForTheFirstTime(_guardFormation);
        }

        protected override void TickOccasionally()
        {
            if (!AreFormationsCreated)
                return;

            bool battleJoinedNew = HasBattleBeenJoined();
            var checkAndSetAvailableFormationsChanged = CheckAndSetAvailableFormationsChanged();
            if (_chosenArtilleryPosition == null || checkAndSetAvailableFormationsChanged || battleJoinedNew != _hasBattleBeenJoined || IsTacticReapplyNeeded)
            {
                if (checkAndSetAvailableFormationsChanged) ManageFormationCounts();
                DeterminePositions();

                _hasBattleBeenJoined = battleJoinedNew;
                if (_hasBattleBeenJoined)
                {
                    Engage();
                }
                else
                {
                    Defend();
                    if (_chosenArtilleryPosition != null)
                    {
                        if (!_usingMachines)
                            ResumeUsingMachines();
                    }
                }

                IsTacticReapplyNeeded = false;
            }
        }

        public bool IsArtilleryAtPosition(TacticalPosition position)
        {
            return Mission.Current.GetActiveEntitiesWithScriptComponentOfType<ArtilleryRangedSiegeWeapon>()
                .Any(entity => entity.GlobalPosition.Distance(position.Position.GetGroundVec3()) < 30);
        }

        public void DeterminePositions()
        {
            if (_chosenArtilleryPosition != null && IsArtilleryAtPosition(_chosenArtilleryPosition.TacticalPosition)) return;
            
            MBDebug.ClearRenderObjects();
            _latestScoredPositions = GatherCandidatePositions()
                .Select(pos => new Target {TacticalPosition = pos})
                .Select(target =>
                {
                    target.UtilityValue = PositionScoring.GeometricMean(target);
                    MBDebug.RenderDebugText3D(target.TacticalPosition.Position.GetGroundVec3(), target.UtilityValue.ToString(), 4294967295U, 0, 0, 30);
                    MBDebug.RenderDebugSphere(target.TacticalPosition.Position.GetGroundVec3(), 2, 4294967295, false, 30);
                    return target;
                }).ToList();
            var candidate = _latestScoredPositions.MaxBy(target => target.UtilityValue);
            if (float.IsNaN(candidate.UtilityValue)) _positionScoring = null;

            if (candidate != null && candidate.UtilityValue != 0.0 && !float.IsNaN(candidate.UtilityValue))
            {
                _chosenArtilleryPosition = candidate;
                var tp = _chosenArtilleryPosition.TacticalPosition;
                var direction = (team.QuerySystem.AverageEnemyPosition - tp.Position.AsVec2).Normalized();
                TacticalPosition primaryDefensivePosition = new TacticalPosition(
                    new WorldPosition(Mission.Current.Scene, tp.Position.GetGroundVec3() + direction.ToVec3() * 50),
                    direction, tp.Width, tp.Slope, tp.IsInsurmountable, tp.TacticalPositionType, tp.TacticalRegionMembership);

                if (primaryDefensivePosition != _mainDefensiveLinePosition)
                {
                    _mainDefensiveLinePosition = primaryDefensivePosition;
                    IsTacticReapplyNeeded = true;
                }

                if (_mainDefensiveLinePosition.LinkedTacticalPositions.Count > 0)
                {
                    TacticalPosition tacticalPosition2 = _mainDefensiveLinePosition.LinkedTacticalPositions.FirstOrDefault();
                    if (tacticalPosition2 == _linkedRangedDefensivePosition)
                        return;
                    _linkedRangedDefensivePosition = tacticalPosition2;
                    IsTacticReapplyNeeded = true;
                }
                else
                    _linkedRangedDefensivePosition = null;


                UpdateArtilleryPlacementTargets();
            }
            else
            {
                _mainDefensiveLinePosition = null;
                _linkedRangedDefensivePosition = null;
            }
        }

        private List<TacticalPosition> GatherCandidatePositions()
        {
            var teamAiAPositions = team.TeamAI.TacticalPositions;
         

            var extractedPositions = team.TeamAI.TacticalRegions
                .SelectMany(region => ExtractPossibleTacticalPositionsFromTacticalRegion(region));

            return teamAiAPositions.Concat(extractedPositions).ToList();
        }

        private List<Axis> CreateArtilleryPositionAssessment()
        {
            var function = new List<Axis>();
            var distance = team.QuerySystem.AveragePosition.Distance(team.QuerySystem.AverageEnemyPosition);

            function.Add(new Axis(0, distance, x => x, CommonAIDecisionFunctions.TargetDistanceToHostiles(team)));
            function.Add(new Axis(0, distance, x => 1 - x, CommonAIDecisionFunctions.TargetDistanceToOwnArmy(team)));
            function.Add(new Axis(0, 1, x => x, CommonAIDecisionFunctions.AssessPositionForArtillery()));
            return function;
        }

        private bool IsTacticalPositionEligible(TacticalPosition tacticalPosition)
        {
            if (tacticalPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.SpecialMissionPosition)
                return true;

            Vec2 avgPosition = _mainInfantry != null ? _mainInfantry.QuerySystem.AveragePosition : team.QuerySystem.AveragePosition;

            float distanceToPosition = avgPosition.Distance(tacticalPosition.Position.AsVec2);
            float enemyDistanceToPosition = team.QuerySystem.AverageEnemyPosition.Distance(avgPosition);

            if (distanceToPosition > 20.0 && distanceToPosition > enemyDistanceToPosition * 0.5) //TODO: Remove this check? Maybe?
                return false;

            if (tacticalPosition.IsInsurmountable)
                return true;

            Vec2 vec2 = (team.QuerySystem.AverageEnemyPosition - tacticalPosition.Position.AsVec2).Normalized();
            return vec2.DotProduct(tacticalPosition.Direction) > 0.5;
        }

        private List<TacticalPosition> ExtractPossibleTacticalPositionsFromTacticalRegion(
            TacticalRegion tacticalRegion)
        {
            List<TacticalPosition> fromTacticalRegion = new List<TacticalPosition>();
            fromTacticalRegion.AddRange(tacticalRegion.LinkedTacticalPositions); //.Where(ltp => ltp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.HighGround);
            if (tacticalRegion.tacticalRegionType == TacticalRegion.TacticalRegionTypeEnum.Forest)
            {
                Vec2 direction = (team.QuerySystem.AverageEnemyPosition - tacticalRegion.Position.AsVec2).Normalized();
                TacticalPosition tacticalPosition1 = new TacticalPosition(tacticalRegion.Position, direction, tacticalRegion.radius, tacticalRegionMembership: TacticalRegion.TacticalRegionTypeEnum.Forest);
                fromTacticalRegion.Add(tacticalPosition1);
                float num = tacticalRegion.radius * 0.87f;
                TacticalPosition tacticalPosition2 = new TacticalPosition(new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, tacticalRegion.Position.GetNavMeshVec3() + (num * direction).ToVec3(), false), direction, tacticalRegion.radius,
                    tacticalRegionMembership: TacticalRegion.TacticalRegionTypeEnum.Forest);
                fromTacticalRegion.Add(tacticalPosition2);
                TacticalPosition tacticalPosition3 = new TacticalPosition(new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, tacticalRegion.Position.GetNavMeshVec3() - (num * direction).ToVec3(), false), direction, tacticalRegion.radius,
                    tacticalRegionMembership: TacticalRegion.TacticalRegionTypeEnum.Forest);
                fromTacticalRegion.Add(tacticalPosition3);
            }

            return fromTacticalRegion;
        }


        private void UpdateArtilleryPlacementTargets()
        {
            _artilleryPlacerComponents.ForEach(component => component.UpdateArtilleryTargetPosition(_chosenArtilleryPosition));
        }


        private bool HasBattleBeenJoined() => _mainInfantry?.QuerySystem.ClosestEnemyFormation == null || _mainInfantry.AI.ActiveBehavior is BehaviorCharge || _mainInfantry.AI.ActiveBehavior is BehaviorTacticalCharge ||
                                              _mainInfantry.QuerySystem.MedianPosition.AsVec2.Distance(_mainInfantry.QuerySystem.ClosestEnemyFormation.MedianPosition.AsVec2) / (double) _mainInfantry.QuerySystem.ClosestEnemyFormation.MovementSpeedMaximum <=
                                              5.0 + (_hasBattleBeenJoined ? 5.0 : 0.0); //TODO: Need to improve logic for detecting that battle has started.

        protected override bool CheckAndSetAvailableFormationsChanged()
        {
            var aiControlledFormationCount = Formations.Count(f => f.IsAIControlled);
            if (aiControlledFormationCount != _AIControlledFormationCount)
            {
                _AIControlledFormationCount = aiControlledFormationCount;
                IsTacticReapplyNeeded = true;
                return true;
            }

            if (_mainInfantry != null && (_mainInfantry.CountOfUnits == 0 || !_mainInfantry.QuerySystem.IsInfantryFormation) ||
                _archers != null && (_archers.CountOfUnits == 0 || !_archers.QuerySystem.IsRangedFormation) ||
                _leftCavalry != null && (_leftCavalry.CountOfUnits == 0 || !_leftCavalry.QuerySystem.IsCavalryFormation) ||
                _rightCavalry != null && (_rightCavalry.CountOfUnits == 0 || !_rightCavalry.QuerySystem.IsCavalryFormation))
                return true;

            return _rangedCavalry != null && (_rangedCavalry.CountOfUnits == 0 || !_rangedCavalry.QuerySystem.IsRangedCavalryFormation);
        }

        protected override bool ResetTacticalPositions()
        {
            DeterminePositions();
            return true;
        }


        private float GetCavalryFactor(TacticalPosition tacticalPosition)
        {
            if (tacticalPosition.TacticalRegionMembership != TacticalRegion.TacticalRegionTypeEnum.Forest)
                return 1f;
            double remainingPowerRatio = team.QuerySystem.RemainingPowerRatio;
            double teamPower = team.QuerySystem.TeamPower;
            float num1 = team.QuerySystem.EnemyTeams.Sum(et => et.TeamPower);
            double num2 = teamPower - teamPower * (team.QuerySystem.CavalryRatio + (double) team.QuerySystem.RangedCavalryRatio) * 0.5;
            float num3 = num1 - (float) (num1 * (team.QuerySystem.EnemyCavalryRatio + (double) team.QuerySystem.EnemyRangedCavalryRatio) * 0.5);
            if (num3 == 0.0)
                num3 = 0.01f;
            double num4 = num3;
            return (float) (num2 / num4) / team.QuerySystem.RemainingPowerRatio;
        }

        private float GetRangedFactor(TacticalPosition tacticalPosition)
        {
            bool isOuterEdge = tacticalPosition.IsOuterEdge;
            if (tacticalPosition.TacticalRegionMembership != TacticalRegion.TacticalRegionTypeEnum.Forest)
                return 1f;
            double remainingPowerRatio = team.QuerySystem.RemainingPowerRatio;
            float teamPower = team.QuerySystem.TeamPower;
            float enemyTotalPower = team.QuerySystem.EnemyTeams.Sum(et => et.TeamPower);
            float enemyRangedPower = enemyTotalPower - (float) (enemyTotalPower * (team.QuerySystem.EnemyRangedRatio + (double) team.QuerySystem.EnemyRangedCavalryRatio) * 0.5);
            if (enemyRangedPower == 0.0)
                enemyRangedPower = 0.01f;
            if (!isOuterEdge)
                teamPower -= (float) (teamPower * (team.QuerySystem.RangedRatio + (double) team.QuerySystem.RangedCavalryRatio) * 0.5);
            return teamPower / enemyRangedPower / team.QuerySystem.RemainingPowerRatio;
        }

        private void Defend()
        {
            if (team.IsPlayerTeam && !team.IsPlayerGeneral && team.IsPlayerSergeant)
                SoundTacticalHorn(MoveHornSoundIndex);
            if (_mainInfantry != null)
            {
                _mainInfantry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_mainInfantry);
                _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(5f).TacticalDefendPosition = _mainDefensiveLinePosition;
                _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            }

            if (_artilleryFormation != null && _chosenArtilleryPosition != null)
            {
                _artilleryFormation.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_artilleryFormation);
                var enemyDirection = (_chosenArtilleryPosition.TacticalPosition.Position.AsVec2 - team.QuerySystem.AverageEnemyPosition).Normalized();
                _artilleryFormation.AI.SetBehaviorWeight<BehaviorDefend>(15f).DefensePosition = new WorldPosition(Mission.Current.Scene, _chosenArtilleryPosition.TacticalPosition.Position.GetGroundVec3() + enemyDirection.ToVec3() * 12);
                _artilleryFormation.AI.SetBehaviorWeight<BehaviorSkirmishLine>(1f);
                _artilleryFormation.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
            }

            if (_guardFormation != null && _chosenArtilleryPosition != null)
            {
                _guardFormation.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_guardFormation);
                _guardFormation.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
                _guardFormation.AI.SetBehaviorWeight<BehaviorProtectArtilleryCrew>(15.0f);
                _guardFormation.AI.SetBehaviorWeight<BehaviorDefend>(10).TacticalDefendPosition = _chosenArtilleryPosition.TacticalPosition;
            }

            if (_archers != null)
            {
                _archers.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_archers);
                _archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(1f);
                _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
                if (_linkedRangedDefensivePosition != null)
                    _archers.AI.SetBehaviorWeight<BehaviorDefend>(10f).TacticalDefendPosition = _linkedRangedDefensivePosition;
            }

            if (_leftCavalry != null)
            {
                _leftCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_leftCavalry);
                _leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Left;
                _leftCavalry.AI.SetBehaviorWeight<BehaviorCavalryScreen>(1f);
            }

            if (_rightCavalry != null)
            {
                _rightCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_rightCavalry);
                _rightCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Right;
                _rightCavalry.AI.SetBehaviorWeight<BehaviorCavalryScreen>(1f);
            }

            if (_rangedCavalry == null)
                return;
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
        }

        private void Engage()
        {
            if (team.IsPlayerTeam && !team.IsPlayerGeneral && team.IsPlayerSergeant)
                SoundTacticalHorn(AttackHornSoundIndex);
            if (_mainInfantry != null)
            {
                _mainInfantry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_mainInfantry);
                _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1f).TacticalDefendPosition = _mainDefensiveLinePosition;
                _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            }


            if (_archers != null)
            {
                _archers.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_archers);
                _archers.AI.SetBehaviorWeight<BehaviorSkirmish>(1f);
                _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
                if (_linkedRangedDefensivePosition != null)
                    _archers.AI.SetBehaviorWeight<BehaviorDefend>(1f).TacticalDefendPosition = _linkedRangedDefensivePosition;
            }

            if (_leftCavalry != null)
            {
                _leftCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_leftCavalry);
                _leftCavalry.AI.SetBehaviorWeight<BehaviorFlank>(1f);
                _leftCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            }

            if (_rightCavalry != null)
            {
                _rightCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_rightCavalry);
                _rightCavalry.AI.SetBehaviorWeight<BehaviorFlank>(1f);
                _rightCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            }

            if (_rangedCavalry == null)
                return;
            _rangedCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rangedCavalry);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            _rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
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
            foreach (UsableMachine usable in _artilleryFormation.GetUsedMachines().ToList())
            {
                _artilleryFormation.StartUsingMachine(usable);
            }

            _usingMachines = true;
        }
    }
}