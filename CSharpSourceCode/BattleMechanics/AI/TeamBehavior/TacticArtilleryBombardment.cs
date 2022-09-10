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
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.AI.TeamBehavior
{
    public class TacticArtilleryBombardment : TacticDefensiveLine
    {
        private bool _usingMachines = true;
        private readonly Formation _artilleryFormation;
        private readonly Formation _guardFormation;

        private List<Axis> _positionScoring; //Do not access this directly. Use the generator function public method below.
        public List<Axis> PositionScoring => _positionScoring ?? (_positionScoring = CreateArtilleryPositionAssessment());

        private List<Target> _latestScoredPositions;
        private readonly List<WizardAIComponent> _artilleryPlacerComponents;

        private Target _chosenArtilleryPosition;
        private TacticalPosition _mainDefensiveLinePosition;
        private TacticalPosition _linkedRangedDefensivePosition;

        private bool _hasBattleBeenJoined;

        public TacticArtilleryBombardment(Team team) : base(team)
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
            if (!team.TeamAI.IsCurrentTactic(this) || _mainDefensiveLinePosition == null || !IsTacticalPositionEligible(_mainDefensiveLinePosition))
                DeterminePositions();

            return _mainDefensiveLinePosition == null
                ? 0.0f
                : (float) ((team.QuerySystem.InfantryRatio + (double) team.QuerySystem.RangedRatio) * 1.20000004768372) * GetTacticalPositionScore(_mainDefensiveLinePosition) * CalculateNotEngagingTacticalAdvantage(team.QuerySystem) /
                  MathF.Sqrt(team.QuerySystem.RemainingPowerRatio);
        }

        protected override void ManageFormationCounts()
        {
            ManageFormationCounts(1, 1, 2, 1);
            _mainInfantry = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsInfantryFormation, f => f.IsAIControlled, f => f.QuerySystem.FormationPower).FirstOrDefault();
            if (_mainInfantry != null)
            {
                _mainInfantry.AI.IsMainFormation = true;
                _mainInfantry.AI.Side = FormationAI.BehaviorSide.Middle;
            }

            _archers = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsRangedFormation, f => f.IsAIControlled, f => f.QuerySystem.FormationPower).FirstOrDefault();
            List<Formation> formationList = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsCavalryFormation, f => f.IsAIControlled, f => f.QuerySystem.FormationPower);
            if (formationList.Count > 0)
            {
                _leftCavalry = formationList[0];
                _leftCavalry.AI.Side = FormationAI.BehaviorSide.Left;
                if (formationList.Count > 1)
                {
                    _rightCavalry = formationList[1];
                    _rightCavalry.AI.Side = FormationAI.BehaviorSide.Right;
                }
                else
                    _rightCavalry = null;
            }
            else
            {
                _leftCavalry = null;
                _rightCavalry = null;
            }

            _rangedCavalry = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsRangedCavalryFormation, f => f.IsAIControlled, f => f.QuerySystem.FormationPower).FirstOrDefault();

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
            if (!AreFormationsCreated)
                return;

            bool flag = HasBattleBeenJoined();
            var checkAndSetAvailableFormationsChanged = CheckAndSetAvailableFormationsChanged();
            if (checkAndSetAvailableFormationsChanged || flag != _hasBattleBeenJoined || IsTacticReapplyNeeded)
            {
                DeterminePositions();
                _hasBattleBeenJoined = flag;
                if (checkAndSetAvailableFormationsChanged) ManageFormationCounts();
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

        public void DeterminePositions()
        {
            DetermineMainDefensiveLine();

            _latestScoredPositions = team.TeamAI.TacticalPositions
                .FindAll(IsTacticalPositionEligible)
                .Select(pos => new Target {TacticalPosition = pos})
                .Select(target =>
                {
                    target.UtilityValue = PositionScoring.GeometricMean(target);
                    return target;
                }).ToList();
            var candidate = _latestScoredPositions.MaxBy(target => target.UtilityValue);
            _chosenArtilleryPosition = candidate;

            UpdatePlacerTargets();
        }

        private void UpdatePlacerTargets()
        {
            _artilleryPlacerComponents.ForEach(component => component.UpdateArtilleryTargetPosition(_chosenArtilleryPosition));
        }

        private List<Axis> CreateArtilleryPositionAssessment()
        {
            var function = new List<Axis>();
            var distance = team.QuerySystem.AveragePosition.Distance(team.QuerySystem.AverageEnemyPosition);
            function.Add(new Axis(0, distance, x => x, CommonAIDecisionFunctions.TargetDistanceToHostiles(team)));
            function.Add(new Axis(0, distance, x => 1 - x, CommonAIDecisionFunctions.TargetDistanceToOwnArmy(team)));
            //     function.Add(new Axis(0, 1, x => x, CommonAIDecisionFunctions.AssessPositionForArtillery()));
            return function;
        }

        private bool HasBattleBeenJoined() => _mainInfantry?.QuerySystem.ClosestEnemyFormation == null || _mainInfantry.AI.ActiveBehavior is BehaviorCharge || _mainInfantry.AI.ActiveBehavior is BehaviorTacticalCharge ||
                                              _mainInfantry.QuerySystem.MedianPosition.AsVec2.Distance(_mainInfantry.QuerySystem.ClosestEnemyFormation.MedianPosition.AsVec2) / (double) _mainInfantry.QuerySystem.ClosestEnemyFormation.MovementSpeedMaximum <=
                                              5.0 + (_hasBattleBeenJoined ? 5.0 : 0.0); //TODO: Need to improve logic for detecting that battle has started.

        protected override bool CheckAndSetAvailableFormationsChanged()
        {
            int num1 = Formations.Count(f => f.IsAIControlled);
            int num2 = num1 != _AIControlledFormationCount ? 1 : 0;
            if (num2 != 0)
            {
                _AIControlledFormationCount = num1;
                IsTacticReapplyNeeded = true;
            }

            if (num2 != 0 || _mainInfantry != null && (_mainInfantry.CountOfUnits == 0 || !_mainInfantry.QuerySystem.IsInfantryFormation) || _archers != null && (_archers.CountOfUnits == 0 || !_archers.QuerySystem.IsRangedFormation) ||
                _leftCavalry != null && (_leftCavalry.CountOfUnits == 0 || !_leftCavalry.QuerySystem.IsCavalryFormation) || _rightCavalry != null && (_rightCavalry.CountOfUnits == 0 || !_rightCavalry.QuerySystem.IsCavalryFormation))
                return true;
            if (_rangedCavalry == null)
                return false;
            return _rangedCavalry.CountOfUnits == 0 || !_rangedCavalry.QuerySystem.IsRangedCavalryFormation;
        }

        protected override bool ResetTacticalPositions()
        {
            DeterminePositions();
            return true;
        }

        private void DetermineMainDefensiveLine()
        {
            List<(TacticalPosition, float)> list = team.TeamAI.TacticalPositions
                .Where(tp => (tp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.SpecialMissionPosition || tp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.HighGround) && IsTacticalPositionEligible(tp))
                .Select((Func<TacticalPosition, (TacticalPosition, float)>) (tp => (tp, GetTacticalPositionScore(tp)))).Concat(team.TeamAI.TacticalRegions
                    .SelectMany(r => ExtractPossibleTacticalPositionsFromTacticalRegion(r))
                    .Where(tpftr => (tpftr.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.Regional || tpftr.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.HighGround) && IsTacticalPositionEligible(tpftr))
                    .Select((Func<TacticalPosition, (TacticalPosition, float)>) (tp => (tp, GetTacticalPositionScore(tp))))).ToList();
            if (list.Count > 0)
            {
                TacticalPosition tacticalPosition1 = list.MaxBy(pst => pst.Item2).Item1;
                if (tacticalPosition1 != _mainDefensiveLinePosition)
                {
                    _mainDefensiveLinePosition = tacticalPosition1;
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
            }
            else
            {
                _mainDefensiveLinePosition = null;
                _linkedRangedDefensivePosition = null;
            }
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

        private float GetTacticalPositionScore(TacticalPosition tacticalPosition)
        {
            if (tacticalPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.SpecialMissionPosition)
                return 100f;
            if (!CheckAndDetermineFormation(ref _mainInfantry, f => f.QuerySystem.IsInfantryFormation))
                return 0.0f;
            double num1 = MBMath.Lerp(1f, 1.5f, MBMath.ClampFloat(tacticalPosition.Slope, 0.0f, 60f) / 60f);
            int countOfUnits = _mainInfantry.CountOfUnits;
            float num2 = MBMath.Lerp(0.67f, 1f, (float) ((6.0 - MBMath.ClampFloat((float) (_mainInfantry.MaximumInterval * (double) (countOfUnits - 1) + _mainInfantry.UnitDiameter * (double) countOfUnits) / tacticalPosition.Width, 3f, 6f)) / 3.0));
            float num3 = tacticalPosition.IsInsurmountable ? 1.3f : 1f;
            float num4 = 1f;
            if (_archers != null && tacticalPosition.LinkedTacticalPositions.Where(lcp => lcp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.Cliff).ToList().Any())
                num4 = MBMath.Lerp(1f, 1.5f, (float) ((MBMath.ClampFloat(team.QuerySystem.RangedRatio, 0.05f, 0.25f) - 0.0500000007450581) * 5.0));
            float rangedFactor = GetRangedFactor(tacticalPosition);
            float cavalryFactor = GetCavalryFactor(tacticalPosition);
            float num5 = MBMath.Lerp(0.7f, 1f, (float) ((150.0 - MBMath.ClampFloat(_mainInfantry.QuerySystem.AveragePosition.Distance(tacticalPosition.Position.AsVec2), 50f, 150f)) / 100.0));
            double num6 = num2;
            return (float) (num1 * num6) * num4 * rangedFactor * cavalryFactor * num5 * num3;
        }

        private List<TacticalPosition> ExtractPossibleTacticalPositionsFromTacticalRegion(
            TacticalRegion tacticalRegion)
        {
            List<TacticalPosition> fromTacticalRegion = new List<TacticalPosition>();
            tacticalRegion.LinkedTacticalPositions.Where(ltp => ltp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.HighGround);
            if (tacticalRegion.tacticalRegionType == TacticalRegion.TacticalRegionTypeEnum.Forest)
            {
                Vec2 direction = (team.QuerySystem.AverageEnemyPosition - tacticalRegion.Position.AsVec2).Normalized();
                TacticalPosition tacticalPosition1 = new TacticalPosition(tacticalRegion.Position, direction, tacticalRegion.radius, tacticalRegionMembership: TacticalRegion.TacticalRegionTypeEnum.Forest);
                fromTacticalRegion.Add(tacticalPosition1);
                float num = tacticalRegion.radius * 0.87f;
                TacticalPosition tacticalPosition2 = new TacticalPosition(new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, tacticalRegion.Position.GetNavMeshVec3() + new Vec3(num * direction), false), direction, tacticalRegion.radius,
                    tacticalRegionMembership: TacticalRegion.TacticalRegionTypeEnum.Forest);
                fromTacticalRegion.Add(tacticalPosition2);
                TacticalPosition tacticalPosition3 = new TacticalPosition(new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, tacticalRegion.Position.GetNavMeshVec3() - new Vec3(num * direction), false), direction, tacticalRegion.radius,
                    tacticalRegionMembership: TacticalRegion.TacticalRegionTypeEnum.Forest);
                fromTacticalRegion.Add(tacticalPosition3);
            }

            return fromTacticalRegion;
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
                _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1f).TacticalDefendPosition = _mainDefensiveLinePosition;
                _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            }

            if (_artilleryFormation != null)
            {
                _artilleryFormation.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_artilleryFormation);
                var enemyDirection = (_chosenArtilleryPosition.TacticalPosition.Position.AsVec2 - team.QuerySystem.AverageEnemyPosition).Normalized();
                _artilleryFormation.AI.SetBehaviorWeight<BehaviorDefend>(15f).DefensePosition = new WorldPosition(Mission.Current.Scene, _chosenArtilleryPosition.TacticalPosition.Position.GetGroundVec3() + enemyDirection.ToVec3() * 12);
                _artilleryFormation.AI.SetBehaviorWeight<BehaviorSkirmishLine>(1f);
                _artilleryFormation.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
            }

            if (_guardFormation != null)
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