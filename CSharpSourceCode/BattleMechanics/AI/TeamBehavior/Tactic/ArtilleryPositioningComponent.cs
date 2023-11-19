using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.AgentBehavior.Components;
using TOR_Core.BattleMechanics.AI.Decision;
using TOR_Core.BattleMechanics.Artillery;

namespace TOR_Core.BattleMechanics.AI.TeamBehavior.Tactic
{
    public class ArtilleryPositioningComponent
    {

        private Target _chosenArtilleryPosition;

        private List<Axis> _positionScoring; //Do not access this directly. Use the generator function public method below.
        public List<Axis> PositionScoring => _positionScoring ?? (_positionScoring = CreateArtilleryPositionAssessment());
        private List<Target> _latestScoredPositions;
        public Team Team;
        private List<WizardAIComponent> _artilleryPlacerComponents;

        public ArtilleryPositioningComponent(Team team)
        {
            Team = team;
            _artilleryPlacerComponents = new List<WizardAIComponent>();
        }



        public Target DeterminePositions()
        {
            if (_chosenArtilleryPosition == null || !IsArtilleryAtPosition(_chosenArtilleryPosition.TacticalPosition))
            {
                _latestScoredPositions = GatherCandidatePositions()
                    .Select(pos => new Target {TacticalPosition = pos})
                    .Select(target =>
                    {
                        target.UtilityValue = PositionScoring.GeometricMean(target);
                        return target;
                    }).ToList();
                if (_latestScoredPositions.Count > 0)
                {
                    var candidate = _latestScoredPositions.MaxBy(target => target.UtilityValue);
                    if (float.IsNaN(candidate.UtilityValue)) _positionScoring = null;
                    if (candidate != null && candidate.UtilityValue != 0.0 && !float.IsNaN(candidate.UtilityValue)) _chosenArtilleryPosition = candidate;
                }
                else _chosenArtilleryPosition = null;
            }
            UpdateArtilleryPlacementTargets(_chosenArtilleryPosition);
            return _chosenArtilleryPosition;
        }

        private List<TacticalPosition> GatherCandidatePositions()
        {
            var TeamAiAPositions = Team.TeamAI.TacticalPositions;

            var extractedPositions = Team.TeamAI.TacticalRegions
                .SelectMany(region => ExtractPossibleTacticalPositionsFromTacticalRegion(region));

            TacticalPosition tacticalPosition1 = new TacticalPosition(Team.QuerySystem.MedianPosition, (Team.QuerySystem.AverageEnemyPosition - Team.QuerySystem.MedianPosition.AsVec2).Normalized(), 50);
            var averageEnemyPosition = Team.QuerySystem.AverageEnemyPosition;

            float height = 0.0f;
            Mission.Current.Scene.GetHeightAtPoint(averageEnemyPosition, BodyFlags.CommonCollisionExcludeFlagsForCombat, ref height);
            var enemyPosition = averageEnemyPosition.ToVec3(height);
            var gatherCandidatePositions = TeamAiAPositions
                .Concat(extractedPositions)
                .AddItem(tacticalPosition1)
                .Where(position => LineOfSightAllowsArtillery(position, enemyPosition)).ToList();
            return gatherCandidatePositions;
        }

        private bool LineOfSightAllowsArtillery(TacticalPosition position, Vec3 enemyPosition)
        {
            return true; //TODO:Temp
            var posCorrected = position.Position.GetGroundVec3();
            posCorrected.z += 1.5f;
            var enemyCorrected = enemyPosition;
            enemyCorrected.z += 2.5f;
            if (position.TacticalRegionMembership == TacticalRegion.TacticalRegionTypeEnum.Forest || position.TacticalRegionMembership == TacticalRegion.TacticalRegionTypeEnum.DifficultTerrain)
            {
                return (CommonAIFunctions.HasLineOfSight(posCorrected, enemyCorrected, Team.TeamAI.IsDefenseApplicable ? 10 : 70) ||
                        CommonAIFunctions.HasLineOfSight(enemyCorrected, posCorrected, Team.TeamAI.IsDefenseApplicable ? 10 : 70));
                // && CommonAIFunctions.HasLineOfSight(posCorrected, posCorrected + position.Direction.Normalized().ToVec3()*15, 20);
            }

            return CommonAIFunctions.HasLineOfSight(posCorrected, enemyCorrected, Team.TeamAI.IsDefenseApplicable ? 70.0f : position.Position.GetGroundVec3().Distance(enemyCorrected) * 0.5f) ||
                   CommonAIFunctions.HasLineOfSight(enemyCorrected, posCorrected, Team.TeamAI.IsDefenseApplicable ? 70.0f : position.Position.GetGroundVec3().Distance(enemyCorrected) * 0.5f);
            //  && CommonAIFunctions.HasLineOfSight(posCorrected, posCorrected + position.Direction.Normalized().ToVec3()*15, 20);
        }


        private List<TacticalPosition> ExtractPossibleTacticalPositionsFromTacticalRegion(
            TacticalRegion tacticalRegion)
        {
            List<TacticalPosition> fromTacticalRegion = new List<TacticalPosition>();
            fromTacticalRegion.AddRange(tacticalRegion.LinkedTacticalPositions); //.Where(ltp => ltp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.HighGround);
            if (tacticalRegion.tacticalRegionType == TacticalRegion.TacticalRegionTypeEnum.Forest)
            {
                Vec2 direction = (Team.QuerySystem.AverageEnemyPosition - tacticalRegion.Position.AsVec2).Normalized();
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

        public bool IsArtilleryAtPosition(TacticalPosition position)
        {
            return Mission.Current.GetActiveEntitiesWithScriptComponentOfType<BaseFieldSiegeWeapon>()
                .Any(entity => entity.GlobalPosition.Distance(position.Position.GetGroundVec3()) < 30);
        }

        public Target GetLatestChosenArtilleryPosition()
        {
            return _chosenArtilleryPosition;
        }
        public void AddWizardPlacerComponent(WizardAIComponent wizardAIComponent)
        {
            _artilleryPlacerComponents.Add(wizardAIComponent);
        }

        private void UpdateArtilleryPlacementTargets(Target artilleryPosition)
        {
            _artilleryPlacerComponents.ForEach(component => component.UpdateArtilleryTargetPosition(artilleryPosition));
        }

        private List<Axis> CreateArtilleryPositionAssessment()
        {
            var function = new List<Axis>();
            var distance = Team.QuerySystem.AveragePosition.Distance(Team.QuerySystem.AverageEnemyPosition);
            Mission.Current.Scene.GetTerrainMinMaxHeight(out float minHeight, out float maxHeight);
            function.Add(new Axis(0, distance, x => x, CommonAIDecisionFunctions.TargetDistanceToHostiles(Team)));
            function.Add(new Axis(0, distance, x => 1 - x, CommonAIDecisionFunctions.TargetDistanceToOwnArmy(Team)));
            function.Add(new Axis(0, 1, x => x, CommonAIDecisionFunctions.AssessPositionForArtillery()));
            function.Add(new Axis(minHeight, maxHeight, x => x, CommonAIDecisionFunctions.PositionHeight()));
            return function;
        }
    }
}
