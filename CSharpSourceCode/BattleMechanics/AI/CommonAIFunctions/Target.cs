using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.AI.CommonAIFunctions
{
    // This class exists primarily for two reasons.
    // Decoupling our implementation. If TaleWorlds changes their Threat class or removes it, we can rework all references to the logic in this class without having to change all of our classes.
    // Additionally, it did not make sense to refer to friendly units / formations as "Threats".
    public class Target : Threat
    {
        public Vec3 SelectedWorldPosition = Vec3.Zero;
        public TacticalPosition TacticalPosition;

        public float UtilityValue
        {
            get => ThreatValue;
            set => ThreatValue = value;
        }

        public Vec3 GetPosition()
        {
            try
            {
                if (WeaponEntity != null)
                    return (WeaponEntity.GetTargetEntity().GlobalBoxMax + WeaponEntity.GetTargetEntity().GlobalBoxMin) * 0.5f;
                if (Agent != null)
                    return Agent.CollisionCapsuleCenter;
                if (Formation != null)
                {
                    var formationAgent = Formation.GetMedianAgent(false, false, Formation.GetAveragePositionOfUnits(false, false));
                    // this can somehow be null... I'm guessing the formation gets wiped out?
                    if (formationAgent != null)
                        return formationAgent.Position;
                    // else just go on to the next few decisions
                }
                if (SelectedWorldPosition != Vec3.Zero)
                    return SelectedWorldPosition;
                if (TacticalPosition != null)
                    return TacticalPosition.Position.GetGroundVec3MT();
                return base.Position;
            }
            catch(NullReferenceException)
            {
                TORCommon.Log("Null error in TOR_Core.BattleMechanics.AI.Decision.Target.GetPosition(). Suppressed.", NLog.LogLevel.Error);
                return Vec3.Invalid;
            }
        }

        public new Vec3 GetVelocity()
        {
            if(Formation != null)
            {
                return Formation.QuerySystem.CurrentVelocity.ToVec3();
            }
            else return base.GetVelocity();
        }

        public Vec3 GetPositionPrioritizeCalculated()
        {
            if (SelectedWorldPosition != Vec3.Zero)
                return SelectedWorldPosition;
            if (TacticalPosition != null)
                return TacticalPosition.Position.GetGroundVec3MT();
            try
            {
                return Position;
            }
            catch (NullReferenceException)
            {
                TORCommon.Log("Null error in TOR_Core.BattleMechanics.AI.Decision.Target.GetPositionPrioritizeCalculated(). Suppressed.", NLog.LogLevel.Error);
                return Vec3.Invalid;
            }
        }

        public new Agent Agent
        {
            get
            {
                if (base.Agent == null && Formation != null)
                {
                    return Formation.GetMedianAgent(false, false, SelectedWorldPosition == Vec3.Zero ? Formation.CurrentPosition : SelectedWorldPosition.AsVec2);
                }

                return base.Agent;
            }
            set => base.Agent = value;
        }

        public new Vec3 Position => GetPosition();
    }
}