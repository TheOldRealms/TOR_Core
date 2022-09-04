using SandBox.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;


public class HideoutAlertMissionLogic : MissionLogic
{
    private bool _enemiesAreAlarmed;

    public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position,
        Vec3 velocity, Mat3 orientation,
        bool hasRigidBody, int forcedMissileIndex)
    {
        if (_enemiesAreAlarmed) return;
        var weaponClass = shooterAgent.WieldedWeapon.CurrentUsageItem.WeaponClass;
        if (weaponClass != WeaponClass.Musket && weaponClass != WeaponClass.Pistol) return;
        var hideoutMissionController = Mission.Current.GetMissionBehavior<HideoutMissionController>();
        if (hideoutMissionController == null) return;
        foreach (var agent in base.Mission.PlayerEnemyTeam.TeamAgents)
        {
            hideoutMissionController.OnAgentAlarmedStateChanged(agent, Agent.AIStateFlag.Alarmed);
            agent.SetWatchState(Agent.WatchState.Alarmed);
        }

        _enemiesAreAlarmed = true;
    }
}