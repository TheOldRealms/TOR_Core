using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.BattleMechanics.Firearms;

public class MagicArrowMissionLogic :  MissionLogic
{
    private Queue<Mission.Missile> _unprocessedMissleIndex = new Queue<Mission.Missile>();
    public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
    {
        var weaponData = shooterAgent.WieldedWeapon.CurrentUsageItem;
        if (!weaponData.IsRangedWeapon) return;

        var missle = Mission.Current.Missiles.First(x => x.ShooterAgent == shooterAgent);

        _unprocessedMissleIndex.Enqueue(missle);
    }


    public override void OnMissionTick(float dt)
    {

        if (_unprocessedMissleIndex.Count != 0)
        {
            var missile = _unprocessedMissleIndex.Dequeue();
            var light = Light.CreatePointLight(4);

            var entity = GameEntity.CreateEmpty(Mission.Scene);
            
            missile.Entity.AddParticleSystemComponent("psys_flaming_weapon");
            
            
            light.Intensity = 500f;
            light.LightColor = Vec3.Up;
            missile.Entity.AddLight(light);
            
            
        }
        
        
    }
}