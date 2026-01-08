using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.Firearms
{
    public class FirearmsMissionLogic : MissionLogic
    {
        private readonly string[] _grenadeSoundNames = new string[5];
        private readonly string[] _soundNames = new string[5];
        private readonly Random _random;
        private readonly Dictionary<int, ContinousFiringData> _continousFiringAgents = [];
        private readonly float _continousFiringInterval = 100f;
        private readonly float _continousFiringBurstLength = 1.5f;
        private readonly Dictionary<int, SoundEvent> _activeSounds = [];
        private readonly List<int> _soundsToRemove = [];

        private const int _explosionDamage = 125;
        private const float _explosionRadius = 6;
        private const float __explosionDamageVariance = 0.25f;

        public FirearmsMissionLogic()
        {
            for (int i = 0; i < _grenadeSoundNames.Length; i++)
            {
                _grenadeSoundNames[i] = "grenadelauncher_muzzle_" + (i + 1);
            }

            for (int i = 0; i < _soundNames.Length; i++)
            {
                _soundNames[i] = "musket_fire_sound_" + (i + 1);
            }

            _random = new Random();
        }

        public override void OnMissionTick(float dt)
        {
            foreach (int index in _continousFiringAgents.Keys)
            {
                //Sly : agents that have died are kept in the AllAgents cache until they are nulled out by mission end (or possibly during reinforcement waves that need to make new space). When the tick iterates through the dictionary, an agent may be nulled during processing leading to future NREs depending on timing.
                //While putting additional null checks into BurstFireShot as well will protect against this, dead agents should instead be cleared from the dictionary to avoid the risk completely.
                var firingData = _continousFiringAgents[index];
                if (firingData.RemainingTime <= 0.5f)
                {
                    if (firingData.IsParticleEnabled) firingData.IsParticleEnabled = false; //Sly : there's a bug with flame particles that persist in the battle, seemingly unattached to a source. I wonder if these are the locations where an agent died and the particles persist.
                    //OnAgentShootMissile is responsible for removing the previous projectile, but if the agent has died and become null, they will never fire another missile and so their previous one will keep persisting at its final location until we remove it (which isn't done atm), or the game despawns it (which i'm unsure if it's doing because i've seen the flame puff effect persist for multiple minutes).
                    //this does make sense because the RemainingTime is only decreased if the agent is not null and still active and therefore the time is static and will persist until mission end
                    continue;
                }
                var agent = Mission.FindAgentWithIndex(index);
                if (agent?.IsActive() != true) continue;

                firingData.RemainingTime -= dt;
                firingData.RemainingTime = Math.Max(0, firingData.RemainingTime);
                if (MissionTime.Now.ToMilliseconds - _continousFiringInterval > firingData.LastFiredTime)
                {
                    firingData.LastFiredTime = MissionTime.Now.ToMilliseconds;
                    BurstFireShot(agent, 0.2f, firingData.FireAmmoId);
                }
            }

            _soundsToRemove.Clear();
            foreach (var sound in _activeSounds) 
            { 
                if(!sound.Value.IsValid || !sound.Value.IsPlaying())
                {
                    _soundsToRemove.Add(sound.Key);
                }
            }
            foreach (int id in _soundsToRemove)
            {
                var sound = _activeSounds[id];
                sound?.Release();
                _activeSounds.Remove(id);
            }
        }

        protected override void OnEndMission()
        {
            //clean up any sounds if they are still playing
            _soundsToRemove.Clear();
            foreach (var item in _activeSounds)
            {
                item.Value?.Stop();
                item.Value?.Release();
            }
            _activeSounds.Clear();
        }

        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
        {
            var weaponData = shooterAgent.WieldedWeapon.CurrentUsageItem;

            // Check for any weapon with scatter trait
            var traits = shooterAgent.WieldedWeapon.Item?.GetTraits(shooterAgent);
            var scatterTrait = traits?.FirstOrDefaultQ(t => t.StatsTuple?.StatType == ItemTraitStatType.ScatterShot);
            if (scatterTrait != null)
            {
                RemoveLastProjectile(shooterAgent);
                float accuracy = 0.05f; // Higher value = more spread for shotgun-style shots
                short amount = (short)scatterTrait.StatsTuple.Value;
                ScatterShot(shooterAgent, accuracy, shooterAgent.WieldedWeapon.AmmoWeapon, position, orientation,
                    weaponData.MissileSpeed, amount);
                return;
            }

            if (weaponData.WeaponClass != WeaponClass.Musket && weaponData.WeaponClass != WeaponClass.Pistol) return;

            var frame = new MatrixFrame(orientation, position);
            var offset = (shooterAgent.WieldedWeapon.CurrentUsageItem.WeaponLength + 30) / 100;
            frame.Advance(offset);

            if (shooterAgent.WieldedWeapon.Item.StringId.Contains("_gun_drakegun"))
            {
                RemoveLastProjectile(shooterAgent);
                _continousFiringAgents[shooterAgent.Index] = new ContinousFiringData
                {
                    OwnerAgent = shooterAgent,
                    FireAmmoId = shooterAgent.WieldedWeapon.AmmoWeapon.Item.StringId,
                    RemainingTime = _continousFiringBurstLength,
                    LastFiredTime = MissionTime.Now.ToMilliseconds,
                    IsParticleEnabled = true,

                };
                BurstFireShot(shooterAgent, 0.1f, shooterAgent.WieldedWeapon.AmmoWeapon.Item.StringId);
                return;
            }

            if (shooterAgent.WieldedWeapon.AmmoWeapon.Item.StringId.Contains("scatter"))
            {
                RemoveLastProjectile(shooterAgent);
                float accuracy = 1 / (weaponData.Accuracy * 1.2f); //this is currently arbitrary
                short amount = 6; // hardcoded for now
                if (shooterAgent.Character is CharacterObject character && character.GetPerkValue(TORPerks.GunPowder.PackItIn))
                {
                    ExplainedNumber num = new(amount);
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.PackItIn, character, true, ref num);
                    amount = (short)num.ResultNumber;
                }
                ScatterShot(shooterAgent, accuracy, shooterAgent.WieldedWeapon.AmmoWeapon, position, orientation,
                    weaponData.MissileSpeed, amount);
            }

            // Add drakefire effect for drakefire pistol
            if (shooterAgent.WieldedWeapon.Item.StringId.Contains("drakefire"))
            {
                var missile = Mission.MissilesList.FirstOrDefaultQ(x => x.ShooterAgent == shooterAgent);
                if (missile != null)
                {
                    missile.Entity.AddParticleSystemComponent("drakefire_effect");
                    var light = Light.CreatePointLight(5f);
                    light.Intensity = 80f;
                    light.LightColor = new Vec3(1f, 0.5f, 0.1f); // Orange fire glow
                    light.SetLightFlicker(0.3f, 0.1f);
                    light.Frame = MatrixFrame.Identity;
                    light.SetVisibility(true);
                    missile.Entity.AddLight(light);
                    CreateMuzzleFireSound(position);
                    return;
                }
            }

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

        //TODO implement explicit SoundEvent creation, add them to a dictionary, keep track of lifetime and when sound playback is over, explicitly remove them, null out the memory pointer
        private void CreateMuzzleFireSound(Vec3 position, MuzzleFireSoundType soundTypetype = MuzzleFireSoundType.Musket)
        {
            int selected = 0;
            switch (soundTypetype)
            {
                case MuzzleFireSoundType.Musket:
                    if (_soundNames.Length > 0)
                    {
                        selected = _random.Next(0, _soundNames.Length - 1);
                        var soundIndex = SoundEvent.GetEventIdFromString(_soundNames[selected]);
                        var soundEvent = SoundEvent.CreateEvent(soundIndex, Mission.Scene);
                        _activeSounds.Add(soundEvent.GetSoundId(), soundEvent);
                        soundEvent.PlayInPosition(position);
                    }

                    break;
                case MuzzleFireSoundType.Grenadelauncher:
                    if (_grenadeSoundNames.Length > 0)
                    {
                        selected = _random.Next(0, _grenadeSoundNames.Length - 1);
                        var soundIndex = SoundEvent.GetEventIdFromString(_grenadeSoundNames[selected]);
                        var soundEvent = SoundEvent.CreateEvent(soundIndex, Mission.Scene);
                        _activeSounds.Add(soundEvent.GetSoundId(), soundEvent);
                        soundEvent.PlayInPosition(position);
                    }

                    break;
                case MuzzleFireSoundType.Pistol: //no sounds for pistol?
                    break;
            }
        }


        private void RemoveLastProjectile(Agent shooterAgent)
        {
            var falseMissle = Mission.MissilesList.FirstOrDefault(missle => missle.ShooterAgent == shooterAgent);
            if (falseMissle != null) Mission.RemoveMissileAsClient(falseMissle.Index);
        }

        public void ScatterShot(Agent shooterAgent, float accuracy, MissionWeapon projectileType, Vec3 shotPosition,
            Mat3 shotOrientation, float missileSpeed, short scatterShotAmount)
        {
            for (int i = 0; i < scatterShotAmount; i++)
            {
                var deviation = TORCommon.GetRandomOrientation(shotOrientation, accuracy);
                var missile = Mission.AddCustomMissileWithWeaponDamage(shooterAgent, projectileType, shotPosition, deviation.f, deviation,
                    missileSpeed, missileSpeed, false, null);
                ApplyWeaponTraitParticles(missile, shooterAgent);
            }
        }

        /// <summary>
        /// Applies weapon trait particle effects to a missile.
        /// </summary>
        public static void ApplyWeaponTraitParticles(Mission.Missile missile, Agent shooterAgent)
        {
            if (missile == null || shooterAgent == null || shooterAgent.WieldedWeapon.IsEmpty)
                return;

            var weapon = shooterAgent.WieldedWeapon;
            if (weapon.Item == null || !weapon.Item.HasAnyTrait(shooterAgent))
                return;

            var traits = weapon.Item.GetTraits(shooterAgent);
            foreach (var trait in traits)
            {
                if (trait.WeaponParticlePreset != null)
                {
                    missile.Entity.AddParticleSystemComponent(trait.WeaponParticlePreset.ParticlePrefab);
                }
            }
        }

        public void BurstFireShot(Agent shooterAgent, float accuracy, string ammoID)
        {
            //Sly : NREs can occur here due to dead agents that are nulled out by the mission at some moments.
            var itemBoneFrame = shooterAgent.AgentVisuals.GetBoneEntitialFrame(Game.Current.DefaultMonster.MainHandItemBoneIndex, false);
            var agentFrame = shooterAgent.AgentVisuals.GetGlobalFrame();
            itemBoneFrame = agentFrame.TransformToParent(itemBoneFrame);
            var offset = (shooterAgent.WieldedWeapon.CurrentUsageItem.WeaponLength + 40) / 100;
            float rotateSide = 85f;
            float rotateUp = 1f;
            itemBoneFrame.rotation.RotateAboutSide(rotateSide.ToRadians());
            itemBoneFrame.rotation.RotateAboutUp(rotateUp.ToRadians());
            var frame = itemBoneFrame.Advance(offset);
            var ammoItem = MBObjectManager.Instance.GetObject<ItemObject>(ammoID);
            var ammo = new MissionWeapon(ammoItem, null, null, 1);

            var baseSpeed = 15;
            var bonusSpeed = 18;

            var missile = Mission.AddCustomMissileWithWeaponDamage(shooterAgent, ammo, frame.origin, frame.rotation.f, frame.rotation,
                baseSpeed, bonusSpeed, true, null);
            
            missile.Entity.RemoveAllParticleSystems();
            
            
        }

        public override void OnMissileCollisionReaction(Mission.MissileCollisionReaction collisionReaction,
            Agent attackerAgent, Agent attachedAgent,
            sbyte attachedBoneIndex)
        {
            base.OnMissileCollisionReaction(collisionReaction, attackerAgent, attachedAgent, attachedBoneIndex);

            if (collisionReaction != Mission.MissileCollisionReaction.BecomeInvisible) return;
            var missileObj = Mission.MissilesList.FirstOrDefault(missile => missile.ShooterAgent == attackerAgent);

            if (missileObj == null) return;

            var pos = missileObj.Entity.GlobalPosition;

            if (missileObj.Weapon.Item.StringId.Contains("grenade"))
            {
                RunExplosionSoundEffects(pos, "mortar_explosion_1");
                RunExplosionVisualEffects(pos, "cannonball_explosion_8");
            }

            if (missileObj.Weapon.Item.StringId.Contains("cannonball"))
            {
                RunExplosionSoundEffects(pos, "mortar_explosion_1");
                RunExplosionVisualEffects(pos, "cannonball_explosion_7");
                //ApplySplashDamage(attackerAgent, pos, _explosionRadius, _explosionDamage, __explosionDamageVariance);
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

        private void RunExplosionSoundEffects(Vec3 position, string soundID, string farAwaySoundID = null)
        {
            farAwaySoundID ??= soundID;

            var distanceFromPlayer = position.Distance(Mission.Current.GetCameraFrame().origin);
            int soundIndex = distanceFromPlayer < 30 ? SoundEvent.GetEventIdFromString(soundID) : SoundEvent.GetEventIdFromString(farAwaySoundID);
            var sound = SoundEvent.CreateEvent(soundIndex, Mission.Current.Scene);
            sound?.PlayInPosition(position);
        }

        public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow,
            in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
        {
            base.OnScoreHit(affectedAgent, affectorAgent, attackerWeapon, isBlocked, isSiegeEngineHit, in blow, in collisionData, damagedHp, hitDistance, shotDifficulty);

            if (attackerWeapon != null && attackerWeapon.WeaponClass == WeaponClass.Stone)
            {
                if (attackerWeapon.ItemUsage == "tor_dw_weapon_grenade_hand_grenade")
                {
                    if (affectorAgent.IsHero)
                    {
                        affectorAgent.GetHero().AddSkillXp(TORSkills.GunPowder, damagedHp);
                    }

                }
            }
        }


        private void ApplySplashDamage(Agent affector, Vec3 position, float explosionRadius, int explosionDamage, float damageVariance)
        {
            /*
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
            */
        }
    }

    public enum MuzzleFireSoundType
    {
        Musket,
        Pistol,
        Grenadelauncher
    }

    public class ContinousFiringData
    {
        public float RemainingTime;
        public double LastFiredTime;
        public ParticleSystem FireStreamPS;
        public Agent OwnerAgent;
        public string FireAmmoId;
        private bool _isParticleEnabled;

        public bool IsParticleEnabled
        {
            get
            {
                return _isParticleEnabled;
            }
            set
            {
                if (_isParticleEnabled != value)
                {
                    _isParticleEnabled = value;
                    if (_isParticleEnabled && OwnerAgent != null)
                    {
                        if (FireStreamPS == null) FireStreamPS = TORParticleSystem.ApplyParticleToAgentBone(OwnerAgent, "drakegun_fire", Game.Current.DefaultMonster.MainHandItemBoneIndex, out _, 0, new Vec3(90, 0, 0));
                        FireStreamPS.SetEnable(true);
                    }
                    else
                    {
                        FireStreamPS?.SetEnable(false);
                    }
                }
            }
        }
    }
}