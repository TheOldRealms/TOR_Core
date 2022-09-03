using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.Firearms
{
    public class FirearmsMissionLogic : MissionLogic
    {
        private int[] _soundIndex = new int[5];
        private Random _random;

        public FirearmsMissionLogic()
        {
            for (int i = 0; i < 5; i++)
            {
                _soundIndex[i] = SoundEvent.GetEventIdFromString("musket_fire_sound_" + (i + 1));
            }
            _random = new Random();
        }

        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
        {
            var weaponData = shooterAgent.WieldedWeapon.CurrentUsageItem;
            if (weaponData.ItemUsage.Contains("handgun") || weaponData.ItemUsage.Contains("pistol"))
            {
                var frame = new MatrixFrame(orientation, position);
                // run particles of smoke


                TORCommon.Say(shooterAgent.WieldedWeapon.AmmoWeapon.Item.StringId);
                if (shooterAgent.WieldedWeapon.AmmoWeapon.Item.StringId.Contains("scatter"))
                {
                    RemoveLastProjectile(shooterAgent);
                    float accuracy = 1/ (weaponData.Accuracy * 1.2f); //this is currently arbitrary
                    short amount = 6; // hardcoded for now
                    ScatterShot(shooterAgent, accuracy,shooterAgent.WieldedWeapon.AmmoWeapon, position, orientation, weaponData.MissileSpeed, amount);
                }
                // play sound of shot and create shot effects
                var offset = (shooterAgent.WieldedWeapon.CurrentUsageItem.WeaponLength + 30) / 100;
                frame.Advance(offset);
                Mission.AddParticleSystemBurstByName("handgun_shoot_2", frame, false);
                CreateMuzzleFireSound(position);
            }
            
            
        }
        
        private void CreateMuzzleFireSound(Vec3 position)
        {
            if (this._soundIndex.Length > 0)
            {
                int selected = this._random.Next(0, this._soundIndex.Length - 1);
                Mission.MakeSound(this._soundIndex[selected], position, false, true, -1, -1);
            }
        }
        
        
        private void RemoveLastProjectile(Agent shooterAgent)
        {
            var falseMissle = Mission.Missiles.FirstOrDefault(missle => missle.ShooterAgent == shooterAgent);
            if (falseMissle != null) Mission.RemoveMissileAsClient(falseMissle.Index);
        }
        
        private void RestoreAmmo(Agent agent, WeaponClass ammoType)
        {
            MissionEquipment equipment = agent.Equipment;
            EquipmentIndex equipmentIndex = (EquipmentIndex)equipment.GetAmmoSlotIndexOfWeapon(ammoType);
            short restoredAmmo = (short)(equipment.GetAmmoAmount(ammoType) + 1);
            agent.SetWeaponAmountInSlot(equipmentIndex, restoredAmmo, false);
        }
        
        private bool TryShotgunShot(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 shotPosition,
            Mat3 shotOrientation, short scatterShots, short requiredAmmoAmount)
        {
            MissionWeapon weaponAtIndex = shooterAgent.Equipment[weaponIndex];
            var weaponData = weaponAtIndex.CurrentUsageItem;
            if (weaponData != null && weaponAtIndex.CurrentUsageItem.IsRangedWeapon)
            {
                RemoveLastProjectile(shooterAgent);
                RestoreAmmo(shooterAgent, weaponData.AmmoClass);
                if (!weaponAtIndex.AmmoWeapon.IsEmpty)
                {
                    var accuracy = 1f / (weaponData.Accuracy * 1.2f); //this should be definable via XML or other data format.
                    if (CanConsumeAmmoOfAgent(requiredAmmoAmount, shooterAgent, weaponData.AmmoClass))
                    {
                        ScatterShot(shooterAgent, accuracy, weaponAtIndex.AmmoWeapon, shotPosition, shotOrientation, weaponData.MissileSpeed, scatterShots);
                        return true;
                    }
                }
            }
            return false;
        }
        
        public void ScatterShot(Agent shooterAgent, float accuracy, MissionWeapon projectileType, Vec3 shotPosition,
            Mat3 shotOrientation, float missleSpeed,short scatterShotAmount)
        {
            for (int i = 0; i < scatterShotAmount; i++)
            {
                var deviation = GetRandomOrientation(shotOrientation, accuracy);
                Mission.AddCustomMissile(shooterAgent, projectileType, shotPosition, deviation.f, deviation,
                    missleSpeed, missleSpeed, false, null);
            }
        }
        
        private Mat3 GetRandomOrientation(Mat3 orientation, float scattering)
        {
            float rand1 = MBRandom.RandomFloatRanged(-scattering, scattering);
            orientation.f.RotateAboutX(rand1);
            float rand2 = MBRandom.RandomFloatRanged(-scattering, scattering);
            orientation.f.RotateAboutY(rand2);
            float rand3 = MBRandom.RandomFloatRanged(-scattering, scattering);
            orientation.f.RotateAboutZ(rand3);
            return orientation;
        }
        
        private bool CanConsumeAmmoOfAgent(int amount, Agent agent, WeaponClass ammoType)
        {
            MissionEquipment equipment = agent.Equipment;
            var d = agent.WieldedWeapon.CurrentUsageIndex;
            EquipmentIndex equipmentIndex = (EquipmentIndex)equipment.GetAmmoSlotIndexOfWeapon(ammoType);
            var currentAmmo = equipment.GetAmmoAmount(ammoType);
            if (currentAmmo >= amount)
            {
                short newAmount = (short)(currentAmmo - amount);
                agent.SetWeaponAmountInSlot(equipmentIndex, newAmount, false);
                return true;
            }

            return false;
        }
        
        public override void OnMissileCollisionReaction(Mission.MissileCollisionReaction collisionReaction, Agent attackerAgent, Agent attachedAgent,
            sbyte attachedBoneIndex)
        {
            base.OnMissileCollisionReaction(collisionReaction, attackerAgent, attachedAgent, attachedBoneIndex);

            if (collisionReaction != Mission.MissileCollisionReaction.BecomeInvisible) return;
            var missileObj = Mission.Missiles.FirstOrDefault(missile => missile.ShooterAgent == attackerAgent);
                
            if (missileObj != null&&missileObj.Weapon.Item.StringId.Contains("grenade"))
            {
                var frame = missileObj.Entity.GetFrame();
                Mission.AddParticleSystemBurstByName("handgun_shoot_2", frame, false);
            }
        }
        

    }
}
