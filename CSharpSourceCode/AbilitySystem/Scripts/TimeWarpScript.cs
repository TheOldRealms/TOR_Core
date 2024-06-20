using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.TriggeredEffect.Scripts;

namespace TOR_Core.AbilitySystem.Scripts;

public class TimeWarpScript : AbilityScript
{
    private int _timeRequestID = 1444; 
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