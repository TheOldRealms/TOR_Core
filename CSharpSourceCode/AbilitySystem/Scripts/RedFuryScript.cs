using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class RedFuryScript : CareerAbilityScript
    {
        private int _timeRequestID = 1337; 
        public override void Initialize(Ability ability)
        {
            base.Initialize (ability);
            var timeRequest = new Mission.TimeSpeedRequest (0.60f,_timeRequestID);
            _timeRequestID = timeRequest.RequestID;
            Mission.Current.AddTimeSpeedRequest (timeRequest);
            
        }

        protected override void OnBeforeRemoved(int removeReason)
        {
            if (Mission.Current.GetRequestedTimeSpeed(_timeRequestID, out float _))
            {
                Mission.Current.RemoveTimeSpeedRequest(_timeRequestID);
            }
        }
    }
}