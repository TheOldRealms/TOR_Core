using HarmonyLib;
using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Hideout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class MissionPatches
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CampaignAgentComponent), "OwnerParty", MethodType.Getter)]
        public static bool PatchOwnerPartyForSummons(Agent ___Agent, ref PartyBase __result)
        {
            if (___Agent.Origin is SummonedAgentOrigin)
            {
                var origin = ___Agent.Origin as SummonedAgentOrigin;
                __result = origin.OwnerParty;
                return false;
            }
            else return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Mission), "FallDamageCallback")]
        public static bool FallDamageCallbackPrefix(Agent victim)
        {
            if (victim.IsVampire())
            {
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mission), "MeleeHitCallback")]
        public static void MeleeHitCallbackPostfix(
            ref AttackCollisionData collisionData,
            Agent attacker,
            Agent victim,
            GameEntity realHitEntity,
            ref float inOutMomentumRemaining,
            ref MeleeCollisionReaction colReaction,
            CrushThroughState crushThroughState,
            Vec3 blowDir,
            Vec3 swingDir,
            ref HitParticleResultData hitParticleResultData,
            bool crushedThroughWithoutAgentCollision)
        {
            if (Campaign.Current == null) return;

            int inflictedDamage = collisionData.InflictedDamage + collisionData.AbsorbedByArmor;
            if (inflictedDamage < 1)
            {
                return;
            }

            var model = MissionGameModels.Current.AgentApplyDamageModel as TORAgentApplyDamageModel;
            if (model == null)
            {
                return;
            }

            if (!model.ShouldCutThrough(collisionData, attacker, victim))
                return;

            float num2 = (float)collisionData.InflictedDamage / (float)inflictedDamage;
            inOutMomentumRemaining = num2 * 0.25f;
        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(HideoutCinematicController), "StartCinematic")]
        public static bool PostOnInitialFadeOutOver()
        {
            var abilityManagerMissionLogic = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
            abilityManagerMissionLogic?.InitHideOutBossFight();

            return true;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(MissionAgentSpawnLogic), "IsSideDepleted")]
        public static void IsSideDepletedPostfix(BattleSideEnum side, ref bool __result)
        {
            if (__result == true)
            {
                var teams = Mission.Current.Teams.Where(x => x.Side == side).ToList();
                foreach (var team in teams)
                {
                    if (team.ActiveAgents.Any(x => x.Origin is SummonedAgentOrigin))
                    {
                        __result = false;
                        return;
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("ScriptingInterfaceOfIMBMission", "InitializeMission")]
        public static void SetSceneAtmosphere(ref MissionInitializerRecord rec)
        {
            if (rec.SceneName.Contains("atmo_w_night"))
            {
                rec.PlayingInCampaignMode = false;
                rec.AtmosphereOnCampaign = default;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SandBoxMissions), "OpenSiegeMissionWithDeployment")]
        public static bool CreateExceptionForAsrai(ref Mission __result, string scene, float[] wallHitPointPercentages, bool hasAnySiegeTower, List<MissionSiegeWeapon> siegeWeaponsOfAttackers, List<MissionSiegeWeapon> siegeWeaponsOfDefenders, bool isPlayerAttacker, int sceneUpgradeLevel = 0, bool isSallyOut = false, bool isReliefForceAttack = false)
        {
            if (PlayerSiege.BesiegedSettlement != null && PlayerSiege.BesiegedSettlement.OriginalCulture().StringId == TORConstants.Cultures.ASRAI)
            {
                //__result = SandBoxMissions.OpenBattleMission(GetBattleSceneForAsraiSiege(), true);
                __result = SandBoxMissions.OpenBattleMission("TOR_wood_elf_city_001", true);
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SandBoxMissions), "OpenSiegeMissionNoDeployment")]
        public static bool CreateExceptionForAsrai2(ref Mission __result, string scene, bool isSallyOut = false, bool isReliefForceAttack = false)
        {
            if (PlayerSiege.BesiegedSettlement != null && PlayerSiege.BesiegedSettlement.OriginalCulture().StringId == TORConstants.Cultures.ASRAI)
            {
                //__result = SandBoxMissions.OpenBattleMission(GetBattleSceneForAsraiSiege(), true);
                __result = SandBoxMissions.OpenBattleMission("TOR_wood_elf_city_001", true);
                return false;
            }

            return true;
        }

        private static string GetBattleSceneForAsraiSiege()
        {
            MBList<SingleplayerBattleSceneData> sceneList = (from scene in GameSceneDataManager.Instance.SingleplayerBattleScenes
                                                             where scene.MapIndices.Any(i => i >= 90 && i <= 93)
                                                             select scene).ToMBList();
            string sceneID;
            if (sceneList.IsEmpty())
            {
                Debug.ShowWarning("Failed to get a battle scene for asrai siege map replacement. Reverting to random map.");
                sceneID = GameSceneDataManager.Instance.SingleplayerBattleScenes.GetRandomElement().SceneID;
            }
            else if (sceneList.Count > 1)
            {
                sceneID = sceneList.GetRandomElement().SceneID;
            }
            else
            {
                sceneID = sceneList[0].SceneID;
            }
            return sceneID;
        }

        // Flag to enable weapon damage bonus for AddCustomMissile
        // Set this to true before calling AddCustomMissile to include bow damage, then reset to false
        public static bool UseWeaponDamageForCustomMissile;

        // Prefix patch for AddMissileSingleUsageAux - modifies damageBonus when our flag is set
        [HarmonyPatch(typeof(Mission), "AddMissileSingleUsageAux")]
        [HarmonyPrefix]
        public static void AddMissileSingleUsageAux_Prefix(Agent shooterAgent, ref float damageBonus)
        {
            if (UseWeaponDamageForCustomMissile && damageBonus == 0.0f)
            {
                damageBonus = TORAgentApplyDamageModel.CalculateCustomMissileDamage(shooterAgent);
            }
        }

        // Prefix patch for AddMissileAux - modifies damageBonus when our flag is set
        [HarmonyPatch(typeof(Mission), "AddMissileAux")]
        [HarmonyPrefix]
        public static void AddMissileAux_Prefix(Agent shooterAgent, ref float damageBonus)
        {
            if (UseWeaponDamageForCustomMissile && damageBonus == 0.0f)
            {
                damageBonus = TORAgentApplyDamageModel.CalculateCustomMissileDamage(shooterAgent);
            }
        }
    }
}