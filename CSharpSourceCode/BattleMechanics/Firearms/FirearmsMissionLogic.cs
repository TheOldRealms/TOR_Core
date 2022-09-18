using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.Firearms
{
    public class FirearmsMissionLogic : MissionLogic
    {
        private int[] _grenadeSoundIndex = new int[5];
        private int[] _soundIndex = new int[5];
        private Random _random;
        
        
        private const int _explosionDamage = 125;
        private const float _explosionRadius = 6;
        private const float __explosionDamageVariance = 0.25f;

        public FirearmsMissionLogic()
        {
            for (int i = 0; i < _grenadeSoundIndex.Length; i++)
            {
                _grenadeSoundIndex[i] = SoundEvent.GetEventIdFromString("grenadelauncher_muzzle_" + (i + 1));
            }


            for (int i = 0; i < _soundIndex.Length; i++)
            {
                _soundIndex[i] = SoundEvent.GetEventIdFromString("musket_fire_sound_" + (i + 1));
            }

            _random = new Random();
        }

        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
        {
            var weaponData = shooterAgent.WieldedWeapon.CurrentUsageItem;
            if (weaponData.WeaponClass != WeaponClass.Musket && weaponData.WeaponClass != WeaponClass.Pistol) return;

            var frame = new MatrixFrame(orientation, position);

            if (shooterAgent.WieldedWeapon.AmmoWeapon.Item.StringId.Contains("scatter"))
            {
                RemoveLastProjectile(shooterAgent);
                float accuracy = 1 / (weaponData.Accuracy * 1.2f); //this is currently arbitrary
                short amount = 6; // hardcoded for now
                ScatterShot(shooterAgent, accuracy, shooterAgent.WieldedWeapon.AmmoWeapon, position, orientation,
                    weaponData.MissileSpeed, amount);
            }

            var offset = (shooterAgent.WieldedWeapon.CurrentUsageItem.WeaponLength + 30) / 100;
            frame.Advance(offset);

            // play sound of shot and create shot effects
            if (!shooterAgent.WieldedWeapon.AmmoWeapon.Item.StringId.Contains("grenade"))
            {
                // run particles of smoke
                Mission.AddParticleSystemBurstByName("handgun_shoot_2", frame, false);
                CreateMuzzleFireSound(position);
            }
            else
            {
                CreateMuzzleFireSound(position, MuzzleFireSoundType.Grenadelauncher);
            }
        }

        private void CreateMuzzleFireSound(Vec3 position, MuzzleFireSoundType soundTypetype = MuzzleFireSoundType.Musket)
        {
            int selected = 0;
            switch (soundTypetype)
            {
                case MuzzleFireSoundType.Musket:
                    if (this._soundIndex.Length > 0)
                    {
                        selected = this._random.Next(0, this._soundIndex.Length - 1);
                        Mission.MakeSound(this._soundIndex[selected], position, false, true, -1, -1);
                    }

                    break;
                case MuzzleFireSoundType.Grenadelauncher:
                    if (this._grenadeSoundIndex.Length > 0)
                    {
                        selected = this._random.Next(0, this._grenadeSoundIndex.Length - 1);
                        Mission.MakeSound(_grenadeSoundIndex[selected], position, false, true, -1, -1);
                    }

                    break;
                case MuzzleFireSoundType.Pistol:
                    break;
            }
        }


        private void RemoveLastProjectile(Agent shooterAgent)
        {
            var falseMissle = Mission.Missiles.FirstOrDefault(missle => missle.ShooterAgent == shooterAgent);
            if (falseMissle != null) Mission.RemoveMissileAsClient(falseMissle.Index);
        }

        public void ScatterShot(Agent shooterAgent, float accuracy, MissionWeapon projectileType, Vec3 shotPosition,
            Mat3 shotOrientation, float missleSpeed, short scatterShotAmount)
        {
            for (int i = 0; i < scatterShotAmount; i++)
            {
                var deviation = TORCommon.GetRandomOrientation(shotOrientation, accuracy);
                Mission.AddCustomMissile(shooterAgent, projectileType, shotPosition, deviation.f, deviation,
                    missleSpeed, missleSpeed, false, null);
            }
        }
        

        public override void OnMissileCollisionReaction(Mission.MissileCollisionReaction collisionReaction,
            Agent attackerAgent, Agent attachedAgent,
            sbyte attachedBoneIndex)
        {
            base.OnMissileCollisionReaction(collisionReaction, attackerAgent, attachedAgent, attachedBoneIndex);

            if (collisionReaction != Mission.MissileCollisionReaction.BecomeInvisible) return;
            var missileObj = Mission.Missiles.FirstOrDefault(missile => missile.ShooterAgent == attackerAgent);
            
            if(missileObj==null)return;
            
            var pos = missileObj.Entity.GlobalPosition;
            
            if (missileObj.Weapon.Item.StringId.Contains("grenade"))
            {
                RunExplosionSoundEffects(pos,"mortar_explosion_1");
                RunExplosionVisualEffects(pos,"cannonball_explosion_8");
            }
            
            if (missileObj.Weapon.Item.StringId.Contains("cannonball"))
            {
                RunExplosionSoundEffects(pos,"mortar_explosion_1");
                RunExplosionVisualEffects(pos,"cannonball_explosion_7");
                ApplySplashDamage(attackerAgent, pos,_explosionRadius,_explosionDamage,__explosionDamageVariance);
            }
        }
        
        private void RunExplosionVisualEffects(Vec3 position, string particleEffectID)
        {
            var effect = GameEntity.CreateEmpty(Mission.Current.Scene);
            MatrixFrame frame = MatrixFrame.Identity;
            ParticleSystem.CreateParticleSystemAttachedToEntity(particleEffectID, effect, ref frame);
            var globalFrame = new MatrixFrame(Mat3.CreateMat3WithForward(in Vec3.Zero), position);
            effect.SetGlobalFrame(globalFrame);
        }
        
        private void RunExplosionSoundEffects(Vec3 position, string soundID, string farAwaySoundID=null)
        {
            if (farAwaySoundID == null)
            {
                farAwaySoundID = soundID;
            }
            
            var distanceFromPlayer = position.Distance(Mission.Current.GetCameraFrame().origin);
            int soundIndex = distanceFromPlayer < 30 ? SoundEvent.GetEventIdFromString(soundID) : SoundEvent.GetEventIdFromString(farAwaySoundID);
            var sound = SoundEvent.CreateEvent(soundIndex, Mission.Current.Scene);
            if (sound != null)
            {
                sound.PlayInPosition(position);
            }
        }


        private void ApplySplashDamage(Agent affector, Vec3 position, float explosionRadius, int explosionDamage, float damageVariance)
        {
            var nearbyAgents = Mission.Current.GetNearbyAgents(position.AsVec2, explosionRadius).ToArray();
            for (int i = 0; i < nearbyAgents.Length; i++)
            {
                var agent = nearbyAgents[i];
                var distance = agent.Position.Distance(position);
                if (distance <= explosionRadius)
                {
                    var baseDamage = explosionDamage * MBRandom.RandomFloatRanged(1 - damageVariance, 1 + damageVariance);
                    var damage = (explosionRadius - distance) / explosionRadius * baseDamage;
                    agent.ApplyDamage((int)damage, position, affector, doBlow: true, hasShockWave: true);
                }
            }
        }

    }
    
    
    


    public enum MuzzleFireSoundType
    {
        Musket,
        Pistol,
        Grenadelauncher
    }
}