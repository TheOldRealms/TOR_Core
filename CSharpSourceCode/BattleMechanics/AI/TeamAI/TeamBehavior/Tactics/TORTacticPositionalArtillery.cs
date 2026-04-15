using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.AI.CastingAI.Components;
using TOR_Core.BattleMechanics.AI.CommonAIFunctions;
using TOR_Core.BattleMechanics.AI.TeamAI.FormationBehavior;
using TOR_Core.BattleMechanics.Artillery;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.AI.TeamAI.TeamBehavior.Tactics
{
    public class TORTacticPositionalArtillery : TacticDefensiveLine
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


        public TORTacticPositionalArtillery(Team Team) : base(Team)
        {
            _artilleryFormation = new Formation(this.Team, (int)TORFormationClass.Artillery);
            this.Team.FormationsIncludingSpecialAndEmpty.Add(_artilleryFormation);
            _guardFormation = new Formation(this.Team, (int)TORFormationClass.ArtilleryGuard);
            this.Team.FormationsIncludingSpecialAndEmpty.Add(_guardFormation);

            _artilleryPlacerComponents = new List<WizardAIComponent>();

            //TODO: Reminder, might need this if certain updates dont work.
            //var method = Traverse.Create(this.Team).Method("FormationAI_OnActiveBehaviorChanged").GetValue();
            //_artilleryFormation.AI.OnActiveBehaviorChanged += new Action<Formation>(this.Team.FormationAI_OnActiveBehaviorChanged);
            // _guardFormation.AI.OnActiveBehaviorChanged += new Action<Formation>(this.Team.FormationAI_OnActiveBehaviorChanged);
        }


        protected override float GetTacticWeight()
        {
            // Break down conditions for easier debugging
            bool hasGeneralAgent = Team.GeneralAgent != null;
            bool generalIsAbilityUser = hasGeneralAgent && Team.GeneralAgent.IsAbilityUser();
            bool generalHasArtilleryAbility = generalIsAbilityUser && Team.GeneralAgent.GetComponent<AbilityComponent>().GetKnownAbilityTemplates().Exists(item => item.AbilityEffectType == AbilityEffectType.ArtilleryPlacement);
            int artilleryCrewCount = Team.ActiveAgents.Count(agent => agent.HasAttribute("ArtilleryCrew")); // FIXED: Use Count() with predicate, not Select().Count()
            bool hasEnoughArtilleryCrew = artilleryCrewCount >= 2;

            // CRITICAL: Check if general can still place artillery
            int artillerySlotsLeft = Mission.Current.GetArtillerySlotsLeftForTeam(Team);
            bool hasArtillerySlots = artillerySlotsLeft > 0;

            bool hasAbilityCharges = false;
            if (generalHasArtilleryAbility)
            {
                var artilleryAbility = Team.GeneralAgent.GetComponent<AbilityComponent>()
                    .GetKnownAbilityTemplates()
                    .FirstOrDefault(item => item.AbilityEffectType == AbilityEffectType.ArtilleryPlacement);
                if (artilleryAbility != null)
                {
                    var abilities = Team.GeneralAgent?.GetComponent<AbilityComponent>().KnownAbilitySystem;

                    if (abilities == null)
                        return 0.0f;

                    foreach (var ability in abilities)
                    {
                        if (ability is not ItemBoundAbility boundAbility)
                        {
                            continue;
                        }

                        if (boundAbility.GetRemainingCharges() <= 0)
                        {
                            continue;
                        }

                        hasAbilityCharges = true;
                        break;
                    }

                }
            }

            // If general can't place artillery anymore, this tactic has NO weight
            // This allows the army to switch to offensive tactics after artillery is placed
            if (!hasGeneralAgent || !generalIsAbilityUser || !generalHasArtilleryAbility ||
                !hasEnoughArtilleryCrew || !hasArtillerySlots || !hasAbilityCharges)
            {
                
                return 0.0f;
            }
       

            // if (!Team.TeamAI.IsDefenseApplicable || !CheckAndDetermineFormation(ref _mainInfantry, f => f.QuerySystem.IsInfantryFormation))
            //     return 0.0f;

            if (!Team.TeamAI.IsCurrentTactic(this) || _mainDefensiveLinePosition == null)
                DeterminePositions();

            if (_chosenArtilleryPosition != null && !float.IsNaN(_chosenArtilleryPosition.UtilityValue))
            {
                var utility = (Team.QuerySystem.InfantryRatio + Team.QuerySystem.RangedRatio * 10) * 1.2f * _chosenArtilleryPosition.UtilityValue * 2.5f // * CalculateNotEngagingTacticalAdvantage(Team.QuerySystem) 
                              / MathF.Sqrt(Team.QuerySystem.RemainingPowerRatio);
                if (IsArtilleryAtPosition(_chosenArtilleryPosition.TacticalPosition))
                    utility += 5;

                return utility;
            }

            return 0.0f;
        }
        

        protected override void ManageFormationCounts()
        {
            AssignTacticFormations1121();

            var allFormations = Team.FormationsIncludingSpecialAndEmpty.ToList();
            var infantryFormations = Team.GetFormationsIncludingSpecial().ToList().FindAll(formation => formation.QuerySystem.IsInfantryFormation);
            var updatedFormations = new List<Formation>();

            allFormations.SelectMany(form => form.Arrangement.GetAllUnits()).ToList().Select(unit => (Agent)unit).ToList().ForEach(agent =>
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


                infantryFormations.SelectMany(form => form.Arrangement.GetAllUnits()).ToList().Select(unit => (Agent)unit).ToList().ForEach(agent =>
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

            updatedFormations.ForEach(formation => Team.TriggerOnFormationsChanged(formation));
            if (_artilleryFormation.CountOfUnits > 0) Team.TeamAI.OnUnitAddedToFormationForTheFirstTime(_artilleryFormation);
            if (_guardFormation.CountOfUnits > 0) Team.TeamAI.OnUnitAddedToFormationForTheFirstTime(_guardFormation);
        }

        public override void TickOccasionally()
        {
            if (!AreFormationsCreated)
                return;

            bool battleJoinedNew = HasBattleBeenJoined();
            var checkAndSetAvailableFormationsChanged = CheckAndSetAvailableFormationsChanged();
            DeterminePositions();
            
            if (_chosenArtilleryPosition == null || checkAndSetAvailableFormationsChanged || battleJoinedNew != _hasBattleBeenJoined || IsTacticReapplyNeeded)
            {
                if (checkAndSetAvailableFormationsChanged) ManageFormationCounts();

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
            return Mission.Current.GetActiveEntitiesWithScriptComponentOfType<BaseFieldSiegeWeapon>()
                .Any(entity => entity.GlobalPosition.Distance(position.Position.GetGroundVec3MT()) < 30);
        }

        public void DeterminePositions()
        {
            // CRITICAL FIX: Recalculate scoring if armies are now deployed but weren't when scoring was first created
            // This handles the case where scoring was initialized before deployment (distance = 0)
            if (_positionScoring != null)
            {
                var currentDistance = Team.QuerySystem.AveragePosition.Distance(Team.QuerySystem.AverageEnemyPosition);
                if (currentDistance > 10f) // Armies have deployed and moved apart
                {
                    // Invalidate old scoring that was calculated when distance was 0
                    _positionScoring = null;
                }
            }

            if (_chosenArtilleryPosition == null || !IsArtilleryAtPosition(_chosenArtilleryPosition.TacticalPosition))
            {
                // DEBUG: Break down for easier debugging
                var candidatePositions = GatherCandidatePositions().ToList();
                int candidateCount = candidatePositions.Count;

                _latestScoredPositions = candidatePositions
                    .Select(pos => new Target { TacticalPosition = pos })
                    .Select(target =>
                    {
                        // DEBUG: Evaluate each axis individually to find which is returning 0
                        float axis1_DistanceToHostiles = PositionScoring[0].Evaluate(target);
                        float axis2_DistanceToOwnArmy = PositionScoring[1].Evaluate(target);
                        float axis3_AssessPositionForArtillery = PositionScoring[2].Evaluate(target);
                        float axis4_PositionHeight = PositionScoring[3].Evaluate(target);

                        target.UtilityValue = PositionScoring.GeometricMean(target);
                        return target;
                    }).ToList();

                int scoredPositionsCount = _latestScoredPositions.Count;

                if (_latestScoredPositions.Count > 0)
                {
                    var candidate = _latestScoredPositions.MaxBy(target => target.UtilityValue);
                    float candidateUtility = candidate?.UtilityValue ?? float.NaN;
                    bool isUtilityNaN = float.IsNaN(candidateUtility);
                    bool isUtilityZero = candidateUtility == 0.0;

                    if (float.IsNaN(candidate.UtilityValue)) _positionScoring = null;
                    if (candidate != null && candidate.UtilityValue != 0.0 && !float.IsNaN(candidate.UtilityValue)) _chosenArtilleryPosition = candidate;
                }
                else _chosenArtilleryPosition = null;
            }

            if (_chosenArtilleryPosition != null)
            {
                var tp = _chosenArtilleryPosition.TacticalPosition;
                var direction = (Team.QuerySystem.AverageEnemyPosition - tp.Position.AsVec2).Normalized();
                TacticalPosition primaryDefensivePosition = new TacticalPosition(
                    new WorldPosition(Mission.Current.Scene, tp.Position.GetGroundVec3MT() + direction.ToVec3() * 50),
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
                .Where(position => LineOfSightAllowsArtillery(position, enemyPosition))
                .Where(position => IsPositionValidForArtillery(position))  // CRITICAL FIX: Filter out unreachable positions
                .ToList();
            return gatherCandidatePositions;
        }

        
        /// <summary>
        /// A bandaid attempt to fix AI placing Artillery at the very border of a map, leading to very weird placement
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool IsPositionValidForArtillery(TacticalPosition position)
        {
            var pos2D = position.Position.AsVec2;
            var pos3D = position.Position.GetGroundVec3MT();

            // Check if position is too close to map boundaries (30 meter buffer)
            var scene = Mission.Current.Scene;
            scene.GetBoundingBox(out Vec3 min, out Vec3 max);
            float boundaryBuffer = 30f;

            if (pos3D.x < min.x + boundaryBuffer || pos3D.x > max.x - boundaryBuffer ||
                pos3D.y < min.y + boundaryBuffer || pos3D.y > max.y - boundaryBuffer)
            {
                return false; // Too close to map edge
            }

            // Check if position is accessible (on valid navmesh)
            var navMeshVec3 = position.Position.GetNavMeshVec3();
            if (!navMeshVec3.IsValid || navMeshVec3.IsNonZero == false)
            {
                return false; // Not on valid navmesh
            }

            // Check if position is reachable from team's deployment area (not too far)
            float maxDistanceFromDeployment = 250f; // Maximum distance from team's median position
            float distanceFromTeam = pos2D.Distance(Team.QuerySystem.MedianPosition.AsVec2);
            if (distanceFromTeam > maxDistanceFromDeployment)
            {
                return false; // Too far from team deployment
            }

            return true;
        }

        private bool LineOfSightAllowsArtillery(TacticalPosition position, Vec3 enemyPosition)
        {
            return true; //TODO:Temp
            var posCorrected = position.Position.GetGroundVec3MT();
            posCorrected.z += 1.5f;
            var enemyCorrected = enemyPosition;
            enemyCorrected.z += 2.5f;
            if (position.TacticalRegionMembership == TacticalRegion.TacticalRegionTypeEnum.Forest || position.TacticalRegionMembership == TacticalRegion.TacticalRegionTypeEnum.DifficultTerrain)
            {
                return (CommonAIFunctions.CommonAIFunctions.HasLineOfSight(posCorrected, enemyCorrected, Team.TeamAI.IsDefenseApplicable ? 10 : 70) ||
                        CommonAIFunctions.CommonAIFunctions.HasLineOfSight(enemyCorrected, posCorrected, Team.TeamAI.IsDefenseApplicable ? 10 : 70));
                // && CommonAIFunctions.HasLineOfSight(posCorrected, posCorrected + position.Direction.Normalized().ToVec3()*15, 20);
            }

            return CommonAIFunctions.CommonAIFunctions.HasLineOfSight(posCorrected, enemyCorrected, Team.TeamAI.IsDefenseApplicable ? 70.0f : position.Position.GetGroundVec3MT().Distance(enemyCorrected) * 0.5f) ||
                   CommonAIFunctions.CommonAIFunctions.HasLineOfSight(enemyCorrected, posCorrected, Team.TeamAI.IsDefenseApplicable ? 70.0f : position.Position.GetGroundVec3MT().Distance(enemyCorrected) * 0.5f);
            //  && CommonAIFunctions.HasLineOfSight(posCorrected, posCorrected + position.Direction.Normalized().ToVec3()*15, 20);
        }

        private List<Axis> CreateArtilleryPositionAssessment()
        {
            var function = new List<Axis>();

            var distance = Team.QuerySystem.AveragePosition.Distance(Team.QuerySystem.AverageEnemyPosition);

            // CRITICAL FIX: At mission start, armies haven't deployed yet and are at the same position (distance ≈ 0)
            // If we create axes with range = 0, they will ALWAYS return 0 (see Axis.Evaluate line 36)
            // Use fallback distance until armies deploy
            if (distance < 10f)
            {
                distance = 150f; // Typical battle deployment distance
            }

            Mission.Current.Scene.GetTerrainMinMaxHeight(out float minHeight, out float maxHeight);
            float heightRange = maxHeight - minHeight;

            // CRITICAL FIX: On flat terrain, height range = 0, causing axes to always return 0
            if (heightRange < 1f)
            {
                minHeight = 0f;
                maxHeight = 100f;
            }

            function.Add(new Axis(0, distance, x => x, CommonAIDecisionFunctions.TargetDistanceToHostiles(Team)));
            function.Add(new Axis(0, distance, x => 1 - x, CommonAIDecisionFunctions.TargetDistanceToOwnArmy(Team)));
            function.Add(new Axis(0, 1, x => x, CommonAIDecisionFunctions.AssessPositionForArtillery()));
            function.Add(new Axis(minHeight, maxHeight, x => x, CommonAIDecisionFunctions.PositionHeight()));
            return function;
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


        private void UpdateArtilleryPlacementTargets()
        {
            // Ensure the general's WizardAIComponent is in the list

            _artilleryPlacerComponents.ForEach(component => component.UpdateArtilleryTargetPosition(_chosenArtilleryPosition));
        }
        
        
        private bool HasBattleBeenJoined() => _mainInfantry?.QuerySystem.ClosestSignificantlyLargeEnemyFormation == null || _mainInfantry.AI.ActiveBehavior is BehaviorCharge || _mainInfantry.AI.ActiveBehavior is BehaviorTacticalCharge ||
                                              _mainInfantry.CachedMedianPosition.AsVec2.Distance(_mainInfantry.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2) / (double)_mainInfantry.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MovementSpeedMaximum <=
                                              5.0 + (_hasBattleBeenJoined ? 5.0 : 0.0); //TODO: Need to improve logic for detecting that battle has started.

        protected override bool CheckAndSetAvailableFormationsChanged()
        {
            var aiControlledFormationCount = FormationsIncludingSpecialAndEmpty.ToList().FindAll(form => form.CountOfUnits > 0).Count(f => f.IsAIControlled);
            if (aiControlledFormationCount != _AIControlledFormationCount)
            {
                _AIControlledFormationCount = aiControlledFormationCount;
                IsTacticReapplyNeeded = true;
                return true;
            }

            if (_mainInfantry != null && (_mainInfantry.CountOfUnits == 0 || !_mainInfantry.QuerySystem.IsInfantryFormation) ||
                _archers != null && (_archers.CountOfUnits == 0 || !_archers.QuerySystem.IsRangedFormation) ||
                _leftCavalry != null && (_leftCavalry.CountOfUnits == 0 || !_leftCavalry.QuerySystem.IsCavalryFormation) ||
                _rightCavalry != null && (_rightCavalry.CountOfUnits == 0 || !_rightCavalry.QuerySystem.IsCavalryFormation) ||
                _artilleryFormation != null && _artilleryFormation.CountOfUnits == 0 ||
                _guardFormation != null && _guardFormation.CountOfUnits == 0)
                return true;

            return _rangedCavalry != null && (_rangedCavalry.CountOfUnits == 0 || !_rangedCavalry.QuerySystem.IsRangedCavalryFormation);
        }
        

        private void Defend()
        {
            if (Team.IsPlayerTeam && !Team.IsPlayerGeneral && Team.IsPlayerSergeant)
                SoundTacticalHorn(MoveHornSoundIndex);


            if (_mainInfantry != null)
            {
                _mainInfantry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_mainInfantry);
                _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(5f).TacticalDefendPosition = _mainDefensiveLinePosition;
                _mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            }

            if (_artilleryFormation != null && _artilleryFormation.CountOfUnits > 0 && _chosenArtilleryPosition != null)
            {
                _artilleryFormation.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_artilleryFormation);

                // CRITICAL FIX: Position formation AWAY from cannons (35m instead of 12m)
                // This prevents them from crowding on the cannons themselves
                var enemyDirection = (_chosenArtilleryPosition.TacticalPosition.Position.AsVec2 - Team.QuerySystem.AverageEnemyPosition).Normalized();
                var defendPosition = new WorldPosition(Mission.Current.Scene, _chosenArtilleryPosition.TacticalPosition.Position.GetGroundVec3MT() + enemyDirection.ToVec3() * 35f);

                // CRITICAL FIX: Lower Defend weight (5f instead of 15f) so formation spreads out naturally
                // Higher weight makes everyone crowd the exact defense point
                _artilleryFormation.AI.SetBehaviorWeight<BehaviorDefend>(5f).DefensePosition = defendPosition;
                _artilleryFormation.AI.SetBehaviorWeight<BehaviorSkirmishLine>(3f);  // Increased weight to encourage spreading
                _artilleryFormation.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
            }

            if (_guardFormation != null && _guardFormation.CountOfUnits > 0 && _chosenArtilleryPosition != null)
            {
                _guardFormation.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_guardFormation);
                _guardFormation.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
                _guardFormation.AI.SetBehaviorWeight<TORBehaviorProtectArtillery>(15.0f);
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
            if (Team.IsPlayerTeam && !Team.IsPlayerGeneral && Team.IsPlayerSergeant)
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
            _artilleryFormation.Arrangement.GetAllUnits()
                .Select(unit => (Agent)unit)
                .ToList()
                .ForEach(agent => agent.Formation = _archers);

            _guardFormation.Arrangement.GetAllUnits()
                .Select(unit => (Agent)unit)
                .ToList()
                .ForEach(agent => agent.Formation = _mainInfantry);
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