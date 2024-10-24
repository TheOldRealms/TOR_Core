using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using LogLevel = NLog.LogLevel;

namespace TOR_Core.BattleMechanics.Dismemberment
{
    public class DismembermentMissionLogic : MissionLogic
    {
        private bool canTroopDismember = true;

        private Probability dismembermentFrequency = Probability.Always;

        private Probability slowMotionFrequency = Probability.Probably;

        private float slowMotionEndTime = -1;

        private float maxChance = 33;

        private float maxTroopChance = 10;

        private readonly string[] headMeshes = { "head", "hair", "beard", "eyebrow" };

        private const int poolSize=20; // the higher this number, the longer body parts stay, the more Objects are "kept" in memory.
        //14*20 = 280 Game Entities are spawned. keep the pool size as reasonable small as possible.
        private GameEntity[][] _pooledDismemberedLimbs;
        private int _index;
        private bool _fullyInstantiated;

        private int _timeSpeedRequestID=1111;

        public override void AfterStart()
        {
            _pooledDismemberedLimbs = new GameEntity[poolSize][];
            for (int i = 0; i < poolSize; i++)
            {
                _pooledDismemberedLimbs[i]= new GameEntity[14];
                _pooledDismemberedLimbs[i][0]= InstantiateObjectAtPoolIndex("exploded_head_001", "exploded_torso_001", i);
                _pooledDismemberedLimbs[i][1] = InstantiateObjectAtPoolIndex("exploded_arms_001");
                _pooledDismemberedLimbs[i][2] = InstantiateObjectAtPoolIndex("exploded_arms_002");
                _pooledDismemberedLimbs[i][3]= InstantiateObjectAtPoolIndex("exploded_legs_002");
                _pooledDismemberedLimbs[i][4] = InstantiateObjectAtPoolIndex("exploded_legs_003");
                _pooledDismemberedLimbs[i][5] = InstantiateObjectAtPoolIndex("exploded_flesh_pieces_001");
                _pooledDismemberedLimbs[i][6] = InstantiateObjectAtPoolIndex("exploded_flesh_pieces_002");
                _pooledDismemberedLimbs[i][7] = InstantiateObjectAtPoolIndex("exploded_flesh_pieces_003");
                _pooledDismemberedLimbs[i][8] = InstantiateObjectAtPoolIndex("exploded_limb_pieces_001");
                _pooledDismemberedLimbs[i][9] = InstantiateObjectAtPoolIndex("exploded_limb_pieces_002");
                _pooledDismemberedLimbs[i][10]= InstantiateObjectAtPoolIndex("exploded_limb_pieces_003");
                _pooledDismemberedLimbs[i][11]= InstantiateObjectAtPoolIndex("exploded_limb_pieces_001");
                _pooledDismemberedLimbs[i][12] = InstantiateObjectAtPoolIndex("exploded_limb_pieces_002");
                _pooledDismemberedLimbs[i][13] = InstantiateObjectAtPoolIndex("exploded_limb_pieces_003");
            }
        }

        public override void OnClearScene()
        {
            Clear();
        }

        private void Clear()
        {
            // just to make sure that all references are cleared
            foreach( var container in _pooledDismemberedLimbs)
            {
                for (int i = 0; i < container.Length; i++)
                {
                    container[i].Remove(0);
                    container[i] = null;
                }
            }

            _pooledDismemberedLimbs = null;
            
        }
        
        private GameEntity InstantiateObjectAtPoolIndex(string prefabName, string secondPrefabVariantName="", int index=0)
        {
            string resultString = prefabName;
            if (index % 2!=0&&secondPrefabVariantName!="")
            {
                resultString = secondPrefabVariantName;
            }

            GameEntity item = GameEntity.Instantiate(Mission.Current.Scene, resultString, false);
            if (item == null)
            {
                throw new Exception("Pooled object was not found return");
            }

            Vec3 upPos = Mission.Current.GetBattleSideInitialSpawnPathFrame(BattleSideEnum.Defender).Origin.Normal;
            upPos += Vec3.Up * 50;
            var frame = new MatrixFrame(Mat3.Identity, upPos); 
            item.SetGlobalFrameMT(frame);
            using (new TWSharedMutexWriteLock(Scene.PhysicsAndRayCastLock))
            {
                item.AddPhysics(item.Mass, item.CenterOfMass, item.GetBodyShape(), Vec3.Zero, Vec3.Zero, PhysicsMaterial.GetFromName("flesh"), false, -1);
            }
            item.SetAlpha(0);
            
            return item;
        }
        
        public override void OnMissionTick(float dt)
        {
            if (slowMotionEndTime > 0 && Mission.CurrentTime >= slowMotionEndTime)
            {
               Mission.Current.RemoveTimeSpeedRequest (_timeSpeedRequestID);
               slowMotionEndTime = -1;
            }
        }

        public override void OnRegisterBlow(Agent attacker, Agent victim, GameEntity realHitEntity, Blow blow, ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon)
        {
            if (victim == null || attacker == null) return;
            if(!victim.IsHuman) return;
            if(victim.IsMainAgent) return;
            if(victim.Health >= 0&& victim.State!=AgentState.Killed) return;

            try
            {
                if (!blow.IsMissile)
                {
                    bool blowCanDecapitate = (collisionData.VictimHitBodyPart == BoneBodyPartType.Neck ||
                                              collisionData.VictimHitBodyPart == BoneBodyPartType.Head) &&
                                             blow.DamageType == DamageTypes.Cut &&
                                             (blow.WeaponRecord.WeaponClass == WeaponClass.OneHandedAxe ||
                                              blow.WeaponRecord.WeaponClass == WeaponClass.OneHandedSword ||
                                              blow.WeaponRecord.WeaponClass == WeaponClass.TwoHandedAxe ||
                                              blow.WeaponRecord.WeaponClass == WeaponClass.TwoHandedSword) &&
                                             (attacker.AttackDirection == Agent.UsageDirection.AttackLeft ||
                                              attacker.AttackDirection == Agent.UsageDirection.AttackRight);
                    if (!blowCanDecapitate) return;
                
                    if (attacker == Agent.Main)
                    {
                        if (!ShouldBeDismembered(attacker, victim, blow)) return;
                        DismemberHead(victim, collisionData);
                        if (slowMotionFrequency == Probability.Always)
                        {
                            EnableSlowMotion();
                        }
                        else if (slowMotionFrequency == Probability.Probably && MBRandom.RandomFloatRanged(0, 1) > 0.75f)
                        {
                            EnableSlowMotion();
                        }
                    }
                    else if (canTroopDismember)
                    {
                        DismemberHead(victim, collisionData);
                    }
                    return;
                }

                if (victim.IsUndead()) return;

                if (blow.InflictedDamage>80&&(attackerWeapon.Item != null&&attackerWeapon.Item.IsExplosiveAmmunition()))
                { 
                    InitializeBodyExplosion(victim,blow.GlobalPosition);
                }
            }
            catch(Exception exception)
            {
                TORCommon.Log("crashed inside Dismemberment interaction "+exception.Message,LogLevel.Error);
                throw;
            }
        }
        
        private void InitializeBodyExplosion(Agent agent, Vec3 position)
        {
            if (agent == null) return;
            
            var distance = agent.Position.Distance(position);
            
            if (distance > 2) return;

            RunParticleEffect(position, "blood_explosion");
            agent.Disappear();
            var frame = agent.Frame.Elevate(1);
            
            MoveCorpseParts(frame);
        }
        
        private  Vec3 GetRandomDirection(float deviation, bool fixZ=true)
        {
            float x = MBRandom.RandomFloatRanged(-deviation, deviation);
            var y = MBRandom.RandomFloatRanged(-deviation, deviation);
            var z = fixZ? 1: MBRandom.RandomFloatRanged(-deviation, deviation);
            return new Vec3(x, y, z);
        }
        private void MoveCorpseParts(MatrixFrame frame)
        {
            if (_index >= poolSize)
            {
                _index = 0;
                _fullyInstantiated = true;
            }

            for (var i = 0; i < _pooledDismemberedLimbs[_index].Length;i++)
            {
                if(_pooledDismemberedLimbs[_index][i]==null) continue; //that shouldn't be... but maybe?
                
                if (!_fullyInstantiated)    
                {
                    _pooledDismemberedLimbs[_index][i].SetAlpha(1);
                }
                _pooledDismemberedLimbs[_index][i].SetGlobalFrameMT(frame);
                var dir = GetRandomDirection(3);
                using (new TWSharedMutexWriteLock(Scene.PhysicsAndRayCastLock))
                {
                    _pooledDismemberedLimbs[_index][i].ApplyLocalImpulseToDynamicBody(Vec3.Up * -1, dir * 25);
                }
            }

            _index++;
        }


        private void EnableSlowMotion()
        {
            slowMotionEndTime = Mission.CurrentTime + 0.5f;
            var timeRequest = new Mission.TimeSpeedRequest (0.50f,_timeSpeedRequestID);
            Mission.Current.AddTimeSpeedRequest (timeRequest);
        }

        private bool ShouldBeDismembered(Agent attacker, Agent victim, Blow blow)
        {
            if (dismembermentFrequency == Probability.Probably)
            {
                float damageModifier = blow.InflictedDamage / victim.HealthLimit;
                if (damageModifier > 1) damageModifier = 1;

                if (attacker.IsMainAgent)
                    damageModifier = damageModifier * maxChance / 100;
                else
                    damageModifier = damageModifier * maxTroopChance / 100;

                return damageModifier > MBRandom.RandomFloatRanged(0, 1);
            }
            else if (dismembermentFrequency == Probability.Always)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void DismemberHead(Agent victim, AttackCollisionData attackCollision)
        {
            GameEntity head = CopyHead(victim);
            MatrixFrame headFrame = new MatrixFrame(victim.LookFrame.rotation, victim.GetEyeGlobalPosition());
            head.SetGlobalFrameMT(headFrame);

            float weight = 0;
            var headEquipment = victim.SpawnEquipment[EquipmentIndex.Head];
            if (!headEquipment.IsEmpty)
            {
                MeshTag tag;
                weight = headEquipment.Weight;
                var headArmor = CopyHeadArmor(victim, headEquipment, out tag);
                if (tag == MeshTag.SHA)
                {
                    headArmor.SetGlobalFrameMT(headFrame);
                    AddPhysics(headArmor, attackCollision, weight);
                }
                else
                {
                    head.AddChild(headArmor);
                }
            }
            AddPhysics(head, attackCollision, 1.5f + weight);
            if (!victim.IsUndead())
            {
                CoverCutWithFlesh(victim, head);
                CreateBloodBurst(victim);
            }
        }

        private GameEntity CopyHead(Agent victim)
        {
            GameEntity head = GameEntity.CreateEmptyDynamic(Mission.Current.Scene, true);
            MatrixFrame headLocalFrame = new MatrixFrame(Mat3.CreateMat3WithForward(in Vec3.Zero), new Vec3(0, 0, -1.6f));
            var meshes = victim.AgentVisuals.GetSkeleton().GetAllMeshes();
            foreach (Mesh mesh in meshes)
            {
                foreach (String name in headMeshes)
                {
                    if (mesh.Name.ToLower().Contains(name))
                    {
                        mesh.SetVisibilityMask((VisibilityMaskFlags)16U);
                        Mesh childMesh = mesh.GetBaseMesh().CreateCopy();
                        childMesh.SetLocalFrame(headLocalFrame);
                        head.AddMesh(childMesh, true);
                        break;
                    }
                }
            }
            return head;
        }

        private GameEntity CopyHeadArmor(Agent victim, EquipmentElement equipment, out MeshTag tag)
        {
            tag = MeshTag.NSHA;
            var headArmor = GameEntity.CreateEmptyDynamic(Mission.Current.Scene, true);
            MatrixFrame headMeshFrame = new MatrixFrame(Mat3.CreateMat3WithForward(in Vec3.Zero), new Vec3(0, 0, -1.6f));
            var multiMesh = equipment.GetMultiMesh(victim.IsFemale, false, true);
            var meshes = victim.AgentVisuals.GetSkeleton().GetAllMeshes();
            for (int i = 0; i < multiMesh.MeshCount; i++)
            {
                var equipMesh = multiMesh.GetMeshAtIndex(i);
                var agentMesh = meshes.FirstOrDefault(m => m.Name == equipMesh.Name);
                agentMesh.SetVisibilityMask(VisibilityMaskFlags.ShadowStatic);
            }
            if (multiMesh.GetFirstMeshWithTag("SHA") != null)
            {
                tag = MeshTag.SHA;
            }
            multiMesh = multiMesh.CreateCopy();
            headArmor.AddMultiMesh(multiMesh, true);
            multiMesh.Frame = headMeshFrame;
            return headArmor;
        }

        private void AddPhysics(GameEntity entity, AttackCollisionData collisionData, float weight = 1)
        {
            using (new TWSharedMutexWriteLock(Scene.PhysicsAndRayCastLock))
            {
                entity.AddSphereAsBody(Vec3.Zero, 0.15f, BodyFlags.BodyOwnerEntity);
                entity.EnableDynamicBody();
                Vec3 blowDir = collisionData.WeaponBlowDir;
                entity.AddPhysics(weight, entity.CenterOfMass, entity.GetBodyShape(), blowDir * 2, blowDir * 10, PhysicsMaterial.GetFromName("flesh"), false, -1);
            }
        }

        private void CoverCutWithFlesh(Agent victim, GameEntity head)
        {
            Mesh throatMesh = Mesh.GetFromResource("dismemberment_head_throat");
            MatrixFrame throatFrame = new MatrixFrame(Mat3.CreateMat3WithForward(in Vec3.Zero), new Vec3(0, 0, -1.6f));
            throatMesh.SetLocalFrame(throatFrame);
            var throatEntity = GameEntity.CreateEmpty(Mission.Current.Scene, true);
            throatEntity.AddMesh(throatMesh);
            head.AddChild(throatEntity);

            Mesh neckMesh = Mesh.GetFromResource("dismemberment_head_neck").CreateCopy();
            victim.AgentVisuals.GetSkeleton().AddMesh(neckMesh);
        }

        private void CreateBloodBurst(Agent victim, HumanBone bone = HumanBone.Head)
        {
            victim.CreateBloodBurstAtLimb(victim.AgentVisuals.GetRealBoneIndex(bone), 0.5f + MBRandom.RandomFloat * 0.5f);
        }
        
        private void RunParticleEffect(Vec3 position, string particleEffectID)
        {
            var effect = GameEntity.CreateEmpty(Mission.Current.Scene);
            MatrixFrame frame = MatrixFrame.Identity;
            ParticleSystem.CreateParticleSystemAttachedToEntity(particleEffectID, effect, ref frame);
            var globalFrame = new MatrixFrame(Mat3.CreateMat3WithForward(in Vec3.Zero), position);
            effect.SetGlobalFrame(globalFrame);
        }

        public enum Probability
        {
            Always,
            Probably,
            Never
        }

        public enum MeshTag
        {
            None,
            SHA, //Separate head armor
            NSHA //Non separate head armor (hat, cap, little helmet, etc)
        }
    }
}
