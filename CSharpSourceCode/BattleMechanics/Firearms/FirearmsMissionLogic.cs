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
        private bool _soundEventsValidated;
        private readonly Dictionary<int, ContinousFiringData> _continousFiringAgents = [];
        private readonly float _continousFiringInterval = 100f;
        private readonly float _continousFiringBurstLength = 1.2f;
        private readonly Dictionary<int, SoundEvent> _activeSounds = [];
        private readonly List<int> _soundsToRemove = [];
        private readonly List<int> _firingAgentsToRemove = [];

        private const int _explosionDamage = 125;
        private const float _explosionRadius = 6;
        private const float __explosionDamageVariance = 0.25f;

        // Trollhammer torpedo explosion - 30% smaller than cannonball
        private const int _torpedoExplosionDamage = 125;
        private const float _torpedoExplosionRadius = 4.2f; // 6 * 0.7 = 4.2
        private const float _torpedoExplosionDamageVariance = 0.25f;

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
        private void ValidateSoundEvents()
        {
            for (int i = 0; i < _soundNames.Length; i++)
            {
                string soundName = _soundNames[i];
                int soundIndex = SoundEvent.GetEventIdFromString(soundName);

                if (soundIndex < 0)
                {
                    throw new InvalidOperationException(
                        $"[TOR] Missing firearm sound event '{soundName}' in FirearmsMissionLogic.");
                }
            }

            for (int i = 0; i < _grenadeSoundNames.Length; i++)
            {
                string soundName = _grenadeSoundNames[i];
                int soundIndex = SoundEvent.GetEventIdFromString(soundName);

                if (soundIndex < 0)
                {
                    throw new InvalidOperationException(
                        $"[TOR] Missing grenade launcher sound event '{soundName}' in FirearmsMissionLogic.");
                }
            }
        }

        public override void OnMissionTick(float dt)
        {
            if (!_soundEventsValidated)
            {
                ValidateSoundEvents();
                _soundEventsValidated = true;
            }
            _firingAgentsToRemove.Clear();
            foreach (int index in _continousFiringAgents.Keys.ToList())
            //naber: Keys.ToList() so we dont blow up if the dict gets touched midtick
            {
                //Sly : agents that have died are kept in the AllAgents cache until they are nulled out by mission end (or possibly during reinforcement waves that need to make new space). When the tick iterates through the dictionary, an agent may be nulled during processing leading to future NREs depending on timing.
                //While putting additional null checks into BurstFireShot as well will protect against this, dead agents should instead be cleared from the dictionary to avoid the risk completely.
                var firingData = _continousFiringAgents[index];
                var agent = firingData.OwnerAgent;
                //naber note: intentionally firingData.OwnerAgent here instead of Mission.FindAgentWithIndex
                //to avoid index reuse causing tick to latch a wrong agent after the inital spawns

                if (agent?.IsActive() != true)
                {
                    firingData.IsParticleEnabled = false; //Sly : there's a bug with flame particles that persist in the battle, seemingly unattached to a source. I wonder if these are the locations where an agent died and the particles persist. 
                    //OnAgentShootMissile is responsible for removing the previous projectile, but if the agent has died and become null, they will never fire another missile and so their previous one will keep persisting at its final location until we remove it (which isn't done atm), or the game despawns it (which i'm unsure if it's doing because i've seen the flame puff effect persist for multiple minutes).
                    //this does make sense because the RemainingTime is only decreased if the agent is not null and still active and therefore the time is static and will persist until mission end
                    RemoveLastProjectileByShooterIndex(index);
                    _firingAgentsToRemove.Add(index);
                    continue;
                }

                firingData.RemainingTime -= dt;
                if (firingData.RemainingTime <= 0f)
                {
                    firingData.IsParticleEnabled = false;
                    RemoveLastProjectileByShooterIndex(index);
                    _firingAgentsToRemove.Add(index);
                    continue;
                }
                //naber: why the previous approach early continued here? it could kill flame vfx / struck entires but it also kills the last 0.5s of firing. lmk if that was intentional
                if (firingData.RemainingTime <= 0.5f)
                {
                    firingData.IsParticleEnabled = false;
                }

                // switch/reload
                if (agent.WieldedWeapon.IsEmpty || agent.WieldedWeapon.CurrentUsageItem == null)
                {
                    firingData.IsParticleEnabled = false;
                    _firingAgentsToRemove.Add(index);
                    continue;
                }

                if (MissionTime.Now.ToMilliseconds - _continousFiringInterval > firingData.LastFiredTime)
                {
                    firingData.LastFiredTime = MissionTime.Now.ToMilliseconds;
                    BurstFireShot(agent, 0.2f, firingData.FireAmmoId);
                }
            }

            foreach (int index in _firingAgentsToRemove)
            {
                _continousFiringAgents.Remove(index);
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

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            var affectedIndex = affectedAgent.Index;

            if (_continousFiringAgents.TryGetValue(affectedIndex, out var firingData))
            {
                firingData.IsParticleEnabled = false;
                RemoveLastProjectileByShooterIndex(affectedIndex);
                _continousFiringAgents.Remove(affectedIndex);
            }
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

            // Trollhammer Torpedo - Single rocket shot weapon
            if (shooterAgent.WieldedWeapon.Item.StringId.Contains("_gun_trollhammer"))
            {
                // Torpedo mode: Single rocket shot with short release animation
                // Use act_hw_release instead of act_hw_release_long for quick firing
                shooterAgent.SetActionChannel(1, ActionIndexCache.Create("act_hw_release"));

                CreateMuzzleFireSound(position, MuzzleFireSoundType.Grenadelauncher);

                // Let the default missile spawn, the explosion is handled in OnMissileCollisionReaction
                return;
            }

            // Drakegun - Continuous flamethrower weapon
            if (shooterAgent.WieldedWeapon.Item.StringId.Contains("_gun_drakegun"))
            {
                var ammoId = shooterAgent.WieldedWeapon.AmmoWeapon.Item.StringId;

                RemoveLastProjectile(shooterAgent);

                // Canister mode: Continuous flamethrower
                _continousFiringAgents[shooterAgent.Index] = new ContinousFiringData
                {
                    OwnerAgent = shooterAgent,
                    FireAmmoId = ammoId,
                    RemainingTime = _continousFiringBurstLength,
                    LastFiredTime = MissionTime.Now.ToMilliseconds,
                    IsParticleEnabled = true,
                };
                BurstFireShot(shooterAgent, 0.1f, ammoId);
                CreateMuzzleFireSound(position, MuzzleFireSoundType.DrakeGun);
                return;
            }

            // Handle scatter shot and buckshot ammo
            var ammoStringId = shooterAgent.WieldedWeapon.AmmoWeapon.Item.StringId;
            if (ammoStringId.Contains("scatter") || ammoStringId.Contains("buckshot"))
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
                    CreateMuzzleFireSound(position, MuzzleFireSoundType.DrakeGun);
                    return;
                }
            }

            // play sound of shot and create shot effects
            // Hand-thrown grenades and blasting charges don't make firing sounds, only explosion on impact
            if (!shooterAgent.WieldedWeapon.AmmoWeapon.Item.StringId.Contains("grenade") && !shooterAgent.WieldedWeapon.AmmoWeapon.Item.StringId.Contains("blasting_charges"))
            {
                // run particles of smoke
                Mission.AddParticleSystemBurstByName("handgun_shoot_2", frame, false);
                CreateMuzzleFireSound(position);
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
                case MuzzleFireSoundType.DrakeGun:
                    {
                        var selectedName = "drakefire";
                        var soundIndex = SoundEvent.GetEventIdFromString(selectedName);
                        var soundEvent = SoundEvent.CreateEvent(soundIndex, Mission.Scene);
                        _activeSounds.Add(soundEvent.GetSoundId(), soundEvent);
                        soundEvent.PlayInPosition(position);
                        break;
                    }
                case MuzzleFireSoundType.Pistol: //no sounds for pistol?
                    break;
            }
        }


        private void RemoveLastProjectile(Agent shooterAgent)
        {
            var falseMissle = Mission.MissilesList.FirstOrDefault(missle => missle.ShooterAgent == shooterAgent);
            if (falseMissle != null) Mission.RemoveMissileAsClient(falseMissle.Index);
        }

        private void RemoveLastProjectileByShooterIndex(int shooterAgentIndex)
        {
            var missile = Mission.MissilesList.FirstOrDefault(m => m.ShooterAgent?.Index == shooterAgentIndex);
            if (missile != null)
            {
                Mission.RemoveMissileAsClient(missile.Index);
            }
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
            if (shooterAgent == null || 
                shooterAgent.State != AgentState.Active || 
                shooterAgent.IsFadingOut() || 
                shooterAgent.AgentVisuals == null || 
                shooterAgent.WieldedWeapon.IsEmpty || 
                shooterAgent.WieldedWeapon.CurrentUsageItem == null)
            {
                return;
            }
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

            if (missileObj.Weapon.Item.StringId.Contains("grenade") || missileObj.Weapon.Item.StringId.Contains("blasting_charges"))
            {
                RunExplosionSoundEffects(pos, "mortar_explosion_1");
                RunExplosionVisualEffects(pos, "cannonball_explosion_8");
            }

            if (missileObj.Weapon.Item.StringId.Contains("trollhammer_torpedo"))
            {
                RunExplosionSoundEffects(pos, "mortar_explosion_1");
                RunExplosionVisualEffects(pos, "psys_fireball_explosion_1"); // Test with fireball explosion
                ApplySplashDamage(attackerAgent, pos, _torpedoExplosionRadius, _torpedoExplosionDamage, _torpedoExplosionDamageVariance);
            }

            if (missileObj.Weapon.Item.StringId.Contains("cannonball"))
            {
                RunExplosionSoundEffects(pos, "mortar_explosion_1");
                RunExplosionVisualEffects(pos, "cannonball_explosion_7");
                ApplySplashDamage(attackerAgent, pos, _explosionRadius, _explosionDamage, __explosionDamageVariance);
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
                if (attackerWeapon.ItemUsage == "dwarf_hand_grenade")
                {
                    if (affectorAgent.IsHero)
                    {
                        affectorAgent.GetHero().AddSkillXp(TORSkills.GunPowder, damagedHp);
                    }

                }
            }
        }
        private static bool CanUseAffectorForScriptedExplosion(Agent affector)
        {
            return affector != null &&
                   Mission.Current != null &&
                   Mission.Current.FindAgentWithIndex(affector.Index) == affector &&
                   affector.IsActive() &&
                   !affector.IsFadingOut() &&
                   affector.Team != null &&
                   affector.Team != Team.Invalid &&
                   affector.Origin != null &&
                   affector.Character != null &&
                   affector.Monster != null;
        }
        private void ApplySplashDamage(Agent affector, Vec3 position, float explosionRadius, int explosionDamage, float damageVariance)
        {
            var nearbyAgents = Mission.Current.GetNearbyAgents(position.AsVec2, explosionRadius, new MBList<Agent>()).ToArray();
            for (int i = 0; i < nearbyAgents.Length; i++)
            {
                if (!CanUseAffectorForScriptedExplosion(affector))
                {
                    break;
                }

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
        Grenadelauncher,
        DrakeGun,
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