using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Server;
using SandBox.View.Missions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.Trinket
{
    public class TrinketMissionLogic : MissionLogic
    {
        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
        {
            base.OnAgentShootMissile(shooterAgent, weaponIndex, position, velocity, orientation, hasRigidBody, forcedMissileIndex);
            
            var weaponData = shooterAgent.WieldedWeapon.CurrentUsageItem;
            if(weaponData.WeaponClass != WeaponClass.Stone) return;

            if (!shooterAgent.WieldedWeapon.Item.IsMagicalItem()) return;
            
            if (shooterAgent.WieldedWeapon.Item.StringId.Contains("holy"))
            {
                TORCommon.Say("its an holy trinket!");
                
               
            }
            
            if (shooterAgent.WieldedWeapon.Item.StringId.Contains("magic"))
            {
                TORCommon.Say("its a magic trinket!");
            }

            var traits = shooterAgent.WieldedWeapon.Item.GetTraits();

            foreach (var trait in traits)
            {
                if (trait.ImbuedStatusEffectId == "none") continue;
                
                shooterAgent.ApplyStatusEffect(trait.ImbuedStatusEffectId,shooterAgent,1f);
            }
            
            //shooterAgent.WieldedWeapon.Consume(1);
            short newAmount = (short)( shooterAgent.WieldedWeapon.HitPoints - 1);
            shooterAgent.SetWeaponAmountInSlot(weaponIndex, newAmount, false);
            
            RemoveLastProjectile(shooterAgent);
            
        }
        
        
        private void RemoveLastProjectile(Agent shooterAgent)
        {
            var falseMissle = Mission.Missiles.FirstOrDefault(missle => missle.ShooterAgent == shooterAgent);
            if (falseMissle == null) return;
            
            Mission.RemoveMissileAsClient(falseMissle.Index);
            falseMissle.Entity.Remove(0);

        }
    }
}