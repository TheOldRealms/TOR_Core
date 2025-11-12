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
                if (TargetableObject != null)
                    return (TargetableObject.GetTargetEntity().GlobalBoxMax + TargetableObject.GetTargetEntity().GlobalBoxMin) * 0.5f;
                if (Agent != null)
                    return Agent.CollisionCapsuleCenter;
                if (Formation != null)
                {
                    var formationAgent = Formation.GetMedianAgent(false, false, Formation.GetAveragePositionOfUnits(false, false));
                    // this can somehow be null... I'm guessing the formation gets wiped out?
                    if (formationAgent != null)
                        return formationAgent.Position;
                    if (formationAgent == null)
                    {
                        //siege defence locations have formations assigned to them and reinforcements are spawned in and attributed to existing formations that may be temporarily empty awaiting potential new troops
                        //if the AI targets one of these formations near the end of a battle when the formation is empty but will never be reinforced, there is no median agent because there are none
                        //this isn't an issue during gameplay because of the catch below, but crash loggers or debug mode will constantly break on base.Position which will attempt to evaluate an equivalent to formationAgent which has just been determined to be null already when the case can instead be handled explictly here
                        TORCommon.Log("Null agent in TOR_Core.BattleMechanics.AI.Decision.Target.GetPosition(). ", NLog.LogLevel.Warn);
                        return Vec3.Invalid;
                    }
                    // else just go on to the next few decisions
                }
                if (SelectedWorldPosition != Vec3.Zero)
                    return SelectedWorldPosition;
                if (TacticalPosition != null)
                    return TacticalPosition.Position.GetGroundVec3MT();
                return base.Position;
            }
            catch (NullReferenceException)
            {
                TORCommon.Log("Null error in TOR_Core.BattleMechanics.AI.Decision.Target.GetPosition(). Suppressed.", NLog.LogLevel.Error);
                return Vec3.Invalid;
            }
        }

        public new Vec3 GetGlobalVelocity()
        {
            if (Formation != null)
            {
                return Formation.CachedCurrentVelocity.ToVec3();
            }
            else return base.GetGlobalVelocity();
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