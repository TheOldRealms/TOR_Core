using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

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

        private const int poolSize=50;


        private  GameEntity[] headObjects = new GameEntity[poolSize];
        private  GameEntity[] armObjects1 = new GameEntity[poolSize];
        private  GameEntity[] armObjects2 = new GameEntity[poolSize];
        private  GameEntity[] legObjects1 = new GameEntity[poolSize];
        private  GameEntity[] legObjects2 = new GameEntity[poolSize];
        private GameEntity[] fleshpieces1 = new GameEntity[poolSize];
        private GameEntity[] fleshpieces2 = new GameEntity[poolSize];
        private GameEntity[] fleshpieces3 = new GameEntity[poolSize];
        private GameEntity[] limbpieces1 = new GameEntity[poolSize];
        private GameEntity[] limbpieces2 = new GameEntity[poolSize];
        private GameEntity[] limbpieces3 = new GameEntity[poolSize];
        private List<GameEntity[]> _pooledItemList;
        private int _index;


        public override void AfterStart()
        {

            base.AfterStart();

            for (int i = 0; i < poolSize; i++)
            {
                headObjects[i] =  GameEntity.Instantiate(Mission.Current.Scene, "exploded_head_001", false);
                headObjects[i].AddPhysics(headObjects[i].Mass, headObjects[i].CenterOfMass, headObjects[i].GetBodyShape(), Vec3.Zero, Vec3.Zero, PhysicsMaterial.GetFromName("flesh"), false, -1);
                headObjects[i].SetPhysicsState(false,true);
                headObjects[i].SetAlpha(0);
                
                armObjects1[i] =  GameEntity.Instantiate(Mission.Current.Scene, "exploded_arms_001", false);
                armObjects1[i].AddPhysics(armObjects1[i].Mass, armObjects1[i].CenterOfMass, armObjects1[i].GetBodyShape(), Vec3.Zero, Vec3.Zero, PhysicsMaterial.GetFromName("flesh"), false, -1);
                armObjects1[i].SetPhysicsState(false,true);
                armObjects1[i].SetAlpha(0);
                
                armObjects2[i] = GameEntity.Instantiate(Mission.Current.Scene, "exploded_arms_002", false);
                armObjects2[i].AddPhysics(armObjects2[i].Mass, armObjects2[i].CenterOfMass, armObjects2[i].GetBodyShape(), Vec3.Zero, Vec3.Zero, PhysicsMaterial.GetFromName("flesh"), false, -1);
                armObjects2[i].SetPhysicsState(false,true);
                armObjects2[i].SetAlpha(0);
                
                legObjects1[i] = GameEntity.Instantiate(Mission.Current.Scene, "exploded_legs_002", false);
                legObjects1[i].AddPhysics(legObjects1[i].Mass, legObjects1[i].CenterOfMass, legObjects1[i].GetBodyShape(), Vec3.Zero, Vec3.Zero, PhysicsMaterial.GetFromName("flesh"), false, -1);
                legObjects1[i].SetPhysicsState(false,true);
                legObjects1[i].SetAlpha(0);
                
                legObjects2[i] = GameEntity.Instantiate(Mission.Current.Scene, "exploded_legs_003", false);
                legObjects2[i].AddPhysics(legObjects2[i].Mass, legObjects2[i].CenterOfMass, legObjects2[i].GetBodyShape(), Vec3.Zero, Vec3.Zero, PhysicsMaterial.GetFromName("flesh"), false, -1);
                legObjects2[i].SetPhysicsState(false,true);
                legObjects2[i].SetAlpha(0);
                
                fleshpieces1[i] = GameEntity.Instantiate(Mission.Current.Scene, "exploded_flesh_pieces_001", false);
                fleshpieces1[i].AddPhysics(fleshpieces1[i].Mass, fleshpieces1[i].CenterOfMass, fleshpieces1[i].GetBodyShape(), Vec3.Zero, Vec3.Zero, PhysicsMaterial.GetFromName("flesh"), false, -1);
                fleshpieces1[i].SetPhysicsState(false,true);
                fleshpieces1[i].SetAlpha(0);
                
                fleshpieces2[i] = GameEntity.Instantiate(Mission.Current.Scene, "exploded_flesh_pieces_002", false);
                fleshpieces2[i].AddPhysics(fleshpieces2[i].Mass, fleshpieces2[i].CenterOfMass, fleshpieces2[i].GetBodyShape(), Vec3.Zero, Vec3.Zero, PhysicsMaterial.GetFromName("flesh"), false, -1);
                fleshpieces2[i].SetPhysicsState(false,true);
                fleshpieces2[i].SetAlpha(0);
                
                fleshpieces3[i] = GameEntity.Instantiate(Mission.Current.Scene, "exploded_flesh_pieces_003", false);
                fleshpieces3[i].AddPhysics(fleshpieces3[i].Mass, fleshpieces3[i].CenterOfMass, fleshpieces3[i].GetBodyShape(), Vec3.Zero, Vec3.Zero, PhysicsMaterial.GetFromName("flesh"), false, -1);
                fleshpieces3[i].SetPhysicsState(false,true);
                fleshpieces3[i].SetAlpha(0);
                
                limbpieces1[i] = GameEntity.Instantiate(Mission.Current.Scene, "exploded_limb_pieces_001", false);
                limbpieces1[i].AddPhysics(limbpieces1[i].Mass, limbpieces1[i].CenterOfMass, limbpieces1[i].GetBodyShape(), Vec3.Zero, Vec3.Zero, PhysicsMaterial.GetFromName("flesh"), false, -1);
                limbpieces1[i].SetPhysicsState(false,true);
                limbpieces1[i].SetAlpha(0);
                
                limbpieces2[i] = GameEntity.Instantiate(Mission.Current.Scene, "exploded_limb_pieces_002", false);
                limbpieces2[i].AddPhysics(limbpieces2[i].Mass, limbpieces2[i].CenterOfMass, limbpieces2[i].GetBodyShape(), Vec3.Zero, Vec3.Zero, PhysicsMaterial.GetFromName("flesh"), false, -1);
                limbpieces2[i].SetPhysicsState(false,true);
                limbpieces2[i].SetAlpha(0);
                limbpieces3[i] = GameEntity.Instantiate(Mission.Current.Scene, "exploded_limb_pieces_003", false);
                limbpieces3[i].AddPhysics(limbpieces3[i].Mass, limbpieces3[i].CenterOfMass, limbpieces3[i].GetBodyShape(), Vec3.Zero, Vec3.Zero, PhysicsMaterial.GetFromName("flesh"), false, -1);
                limbpieces3[i].SetPhysicsState(false,true);
                limbpieces3[i].SetAlpha(0);
            }

            _pooledItemList = new List<GameEntity[]>();
            
            _pooledItemList.Add(headObjects);
            _pooledItemList.Add(armObjects1);
            _pooledItemList.Add(legObjects1);
            _pooledItemList.Add(legObjects2);

            _pooledItemList.Add(fleshpieces3);

            _pooledItemList.Add(fleshpieces1);
            _pooledItemList.Add(fleshpieces2);
            _pooledItemList.Add(fleshpieces3);
            _pooledItemList.Add(limbpieces1);
            _pooledItemList.Add(limbpieces2);
            _pooledItemList.Add(limbpieces3);




        }


        public override void OnMissionTick(float dt)
        {
            if (slowMotionEndTime > 0 && Mission.CurrentTime >= slowMotionEndTime)
            {
                Mission.Current.Scene.TimeSpeed *= 2;
            }
        }

        public override void OnRegisterBlow(Agent attacker, Agent victim, GameEntity realHitEntity, Blow blow, ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon)
        {
            if (victim == null || attacker == null) return;
            if(!victim.IsHuman) return;
            if(victim.IsMainAgent) return;
            if(victim.Health >= 0&& victim.State!=AgentState.Killed) return;


            /*bool blowCanDecapitate = (collisionData.VictimHitBodyPart == BoneBodyPartType.Neck ||
                                     collisionData.VictimHitBodyPart == BoneBodyPartType.Head) &&
                                    blow.DamageType == DamageTypes.Cut &&
                                    (blow.WeaponRecord.WeaponClass == WeaponClass.OneHandedAxe ||
                                     blow.WeaponRecord.WeaponClass == WeaponClass.OneHandedSword ||
                                     blow.WeaponRecord.WeaponClass == WeaponClass.TwoHandedAxe ||
                                     blow.WeaponRecord.WeaponClass == WeaponClass.TwoHandedSword) &&
                                    (attacker.AttackDirection == Agent.UsageDirection.AttackLeft ||
                                     attacker.AttackDirection == Agent.UsageDirection.AttackRight);

            if (blowCanDecapitate)
            {
                if (attacker == Agent.Main)
                {
                    if (ShouldBeDismembered(attacker, victim, blow))
                    {
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
                }
                else if (attacker.IsHuman && canTroopDismember)
                {
                    DismemberHead(victim, collisionData);
                }
            }*/
            
            
            if(victim.IsUndead())
            
            if (attackerWeapon.Item != null&&attackerWeapon.Item.IsExplosiveAmmunition())
            {
                InitializeBodyExplosion(victim,blow.Position);
            }
        }
        

        private void InitializeBodyExplosion(Agent agent, Vec3 position)
        {
            var distance = agent.Position.Distance(position);

            if (distance > 2) return;
            agent.Disappear();
            var frame = agent.Frame.Elevate(1);
            if (distance <= 1.5f)
            {
                
            }
            ExplosionNearVictim(frame);
            // ExplosionFarVictim(frame);


        }
        
        private void ExplosionNearVictim(MatrixFrame frame)
        {
            if (_index >= poolSize)
                _index = 0;



            foreach (var go in _pooledItemList.Select(item => item[_index]))
            {
                go.SetGlobalFrame(frame);
                go.SetAlpha(1);
                go.SetPhysicsState(true,true);
                var dir = TORCommon.GetRandomDirection(3);
                var multiplier = 50/ go.Mass;

                go.ApplyLocalImpulseToDynamicBody(Vec3.Up*-1, dir * multiplier*5);
            }
            /*
            ObjectPooling(headObjects[_index], frame);
            ObjectPooling(armObjects1[_index], frame);
            ObjectPooling(armObjects2[_index], frame);
            
            ObjectPooling(legObjects1[_index], frame);
            ObjectPooling(legObjects2[_index], frame);
            ObjectPooling(fleshpieces1[_index], frame);
            ObjectPooling(fleshpieces2[_index], frame);
            ObjectPooling(fleshpieces3[_index], frame);
            
            ObjectPooling(limbpieces1[_index], frame);
            ObjectPooling(limbpieces2[_index], frame);
            ObjectPooling(limbpieces3[_index], frame);
            */
            

            _index++;
        }
        
        private void ExplosionFarVictim(MatrixFrame frame)
        {
            LaunchLimb(frame, "exploded_torso_001"); 
            LaunchLimb(frame, "exploded_legs_001");

            LaunchLimb(frame, "exploded_flesh_pieces_001");
            LaunchLimb(frame, "exploded_flesh_pieces_002");
            LaunchLimb(frame, "exploded_limb_pieces_001");
            LaunchLimb(frame, "exploded_limb_pieces_002");
        }

        private void ObjectPooling(GameEntity go, MatrixFrame frame)
        {
            go.SetGlobalFrame(frame);
            go.SetAlpha(1);
            go.SetPhysicsState(true,true);
            var dir = TORCommon.GetRandomDirection(3);
            var multiplier = 50/ go.Mass;

            go.ApplyLocalImpulseToDynamicBody(Vec3.Zero, dir * multiplier*5);
            
            //limbpieces1[i].
           // limb.AddPhysics(limb.Mass, limb.CenterOfMass, limb.GetBodyShape(), dir * multiplier, dir * 2, PhysicsMaterial.GetFromName("flesh"), false, -1);
        }
        
        
        private void LaunchLimb(MatrixFrame frame, string name)
        {
            var limb = GameEntity.Instantiate(Mission.Current.Scene, name, false);
            limb.SetGlobalFrame(frame);
            var dir = TORCommon.GetRandomDirection(3);
            var multiplier = 50/ limb.Mass;
            limb.ApplyLocalImpulseToDynamicBody(Vec3.Up, dir * multiplier*2);
        }
                       

        private void EnableSlowMotion()
        {
            slowMotionEndTime = Mission.CurrentTime + 0.5f;
            Mission.Current.Scene.TimeSpeed *= 0.5f;
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
            head.SetGlobalFrame(headFrame);

            float weight = 0;
            var headEquipment = victim.SpawnEquipment[EquipmentIndex.Head];
            if (!headEquipment.IsEmpty)
            {
                MeshTag tag;
                weight = headEquipment.Weight;
                var headArmor = CopyHeadArmor(victim, headEquipment, out tag);
                if (tag == MeshTag.SHA)
                {
                    headArmor.SetGlobalFrame(headFrame);
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
            entity.AddSphereAsBody(Vec3.Zero, 0.15f, BodyFlags.BodyOwnerEntity);
            entity.EnableDynamicBody();
            Vec3 blowDir = collisionData.WeaponBlowDir;
            entity.AddPhysics(weight, entity.CenterOfMass, entity.GetBodyShape(), blowDir * 2, blowDir * 10, PhysicsMaterial.GetFromName("flesh"), false, -1);
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

        private void CreateBloodBurst(Agent victim)
        {
            MatrixFrame boneEntitialFrameWithIndex = victim.AgentVisuals.GetSkeleton().GetBoneEntitialFrameWithIndex(victim.BoneMappingArray[HumanBone.Head]);
            Vec3 vec = victim.AgentVisuals.GetGlobalFrame().TransformToParent(boneEntitialFrameWithIndex.origin);
            victim.CreateBloodBurstAtLimb(13, ref vec, 0.5f + MBRandom.RandomFloat * 0.5f);
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
