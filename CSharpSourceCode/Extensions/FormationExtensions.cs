using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.Core.ItemObject;

namespace TOR_Core.Extensions
{
    public static class FormationExtensions
    {
        public static bool IsFormationDispersed(this Formation formation, out WorldPosition reformPosition, float speedModifier = 0.5f)
        {
            reformPosition = formation.CachedMedianPosition;
            if (formation.GetReadonlyMovementOrderReference().MovementState == MovementOrder.MovementStateEnum.Charge) return false;
            var formationIntegrityData = formation.CachedFormationIntegrityData;
            if(formationIntegrityData.DeviationOfPositionsExcludeFarAgents * formation.CachedMovementSpeed * speedModifier > formation.QuerySystem.IdealAverageDisplacement)
            {
                var targetFormationDirection = (formation.QuerySystem.Team.MedianTargetFormationPosition.AsVec2 - formation.CachedAveragePosition).Normalized();
                var reformPositionVec2 = formation.CachedAveragePosition + targetFormationDirection * 5f;
                reformPosition.SetVec2(reformPositionVec2);
                return true;
            }
            return false;
        }

        public static bool IsFormationBeingChargedByCavalry(this Formation formation, float distanceToCheck = 30f, float minSpeedToRegisterCharge = 2f)
        {
            bool result = false;
            if(formation.CachedClosestEnemyFormation != null && formation.CachedClosestEnemyFormation.IsCavalryFormation)
            {
                var formationDirection = (formation.CachedAveragePosition - formation.CachedClosestEnemyFormation.Formation.CachedAveragePosition);
                var distance = formationDirection.Normalize();
                var enemyVelocity = formation.CachedClosestEnemyFormation.Formation.CachedCurrentVelocity;
                var enemySpeed = enemyVelocity.Normalize();
                if(distance < distanceToCheck && enemySpeed > minSpeedToRegisterCharge && formationDirection.DotProduct(enemyVelocity) > 0.5f)
                {
                    result = true;
                }
            }
            return result;
        }

        public static bool IsMountedSkirmishFormation(this Formation formation, float desiredRatio = 0.6f)
        {
            if (formation != null && formation.QuerySystem.IsCavalryFormation)
            {
                float ratio = 0f;
                int mountedSkirmishersCount = 0;
                int countedUnits = 0;
                formation.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent)
                {
                    bool ismountedSkrimisher = false;
                    if (ratio <= desiredRatio && ((float)countedUnits / (float)formation.CountOfUnits) <= desiredRatio)
                    {
                        for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
                        {
                            if (agent.Equipment != null && !agent.Equipment[equipmentIndex].IsEmpty)
                            {
                                if (agent.MountAgent != null && agent.Equipment[equipmentIndex].Item.Type == ItemTypeEnum.Thrown && agent.Equipment[equipmentIndex].Amount > 0)
                                {
                                    ismountedSkrimisher = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (ismountedSkrimisher)
                    {
                        mountedSkirmishersCount++;
                    }
                    countedUnits++;
                    ratio = (float)mountedSkirmishersCount / (float)formation.CountOfUnits;
                });

                if (ratio > desiredRatio)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsFightingInMelee(this Formation formation, float desiredRatio)
        {
            float currentTime = MBCommon.GetTotalMissionTime();
            float countedUnits = 0;
            float ratio = 0f;
            float countOfUnitsFightingInMelee = 0;
            formation.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent)
            {
                if (agent != null && ratio <= desiredRatio && ((float)countedUnits / (float)formation.CountOfUnits) <= desiredRatio)
                {
                    float lastMeleeAttackTime = agent.LastMeleeAttackTime;
                    float lastMeleeHitTime = agent.LastMeleeHitTime;
                    if ((currentTime - lastMeleeAttackTime < 6f) || (currentTime - lastMeleeHitTime < 6f))
                    {
                        countOfUnitsFightingInMelee++;
                    }
                    countedUnits++;
                }
            });
            if (countOfUnitsFightingInMelee / formation.CountOfUnits >= desiredRatio)
            {
                return true;
            }
            return false;
        }
    }
}
