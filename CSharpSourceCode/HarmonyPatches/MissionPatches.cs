using HarmonyLib;
using Helpers;
using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;

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

        
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HideoutMissionController), "OnInitialFadeOutOver")]
        public static bool PostOnInitialFadeOutOver()
        {
            var abilityManagerMissionLogic = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
            if (abilityManagerMissionLogic != null)
            {
                abilityManagerMissionLogic.InitHideOutBossFight();
            }
            
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
        public static bool CreateExceptionForLeicheberg(ref Mission __result, string scene, float[] wallHitPointPercentages, bool hasAnySiegeTower, List<MissionSiegeWeapon> siegeWeaponsOfAttackers, List<MissionSiegeWeapon> siegeWeaponsOfDefenders, bool isPlayerAttacker, int sceneUpgradeLevel = 0, bool isSallyOut = false, bool isReliefForceAttack = false)
        {
            if(scene == "scn_leicheberg_doomed_city")
            {
                string text = "level_1";
                if (isSallyOut | isReliefForceAttack)
                {
                    text += " sally_out";
                }
                else
                {
                    text += " siege";
                }
                bool isPlayerSergeant = MobileParty.MainParty.MapEvent.IsPlayerSergeant();
                bool isPlayerInArmy = MobileParty.MainParty.Army != null;
                List<string> heroesOnPlayerSideByPriority = HeroHelper.OrderHeroesOnPlayerSideByPriority();
                MissionInitializerRecord rec = SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, text, false, DecalAtlasGroup.Town);

                if(Campaign.Current.IsNight) rec.AtmosphereOnCampaign.TimeInfo.TimeOfDay = 22;
                else rec.AtmosphereOnCampaign.TimeInfo.TimeOfDay = 12;

                __result = MissionState.OpenNew("SiegeMissionWithDeployment", rec, delegate (Mission mission)
                {
                    List<MissionBehavior> list = new List<MissionBehavior>();
                    list.Add(new BattleSpawnLogic(isSallyOut ? "sally_out_set" : (isReliefForceAttack ? "relief_force_attack_set" : "battle_set")));
                    list.Add(new MissionOptionsComponent());
                    list.Add(new CampaignMissionComponent());
                    BattleEndLogic battleEndLogic = new BattleEndLogic();
                    if (MobileParty.MainParty.MapEvent.PlayerSide == BattleSideEnum.Attacker)
                    {
                        battleEndLogic.EnableEnemyDefenderPullBack(Campaign.Current.Models.SiegeLordsHallFightModel.DefenderTroopNumberForSuccessfulPullBack);
                    }
                    list.Add(battleEndLogic);
                    list.Add(new BattleReinforcementsSpawnController());
                    list.Add(new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), isSallyOut ? Mission.MissionTeamAITypeEnum.SallyOut : Mission.MissionTeamAITypeEnum.Siege, isPlayerSergeant));
                    list.Add(new SiegeMissionPreparationHandler(isSallyOut, isReliefForceAttack, wallHitPointPercentages, hasAnySiegeTower));
                    list.Add(new CampaignSiegeStateHandler());
                    Settlement currentTown = GetCurrentTown();
                    if (currentTown != null)
                    {
                        list.Add(new WorkshopMissionHandler(currentTown));
                    }
                    Mission.BattleSizeType battleSizeType = Mission.BattleSizeType.Siege;
                    if (isSallyOut)
                    {
                        battleSizeType = Mission.BattleSizeType.SallyOut;
                        FlattenedTroopRoster priorityTroopsForSallyOutAmbush = Campaign.Current.Models.SiegeEventModel.GetPriorityTroopsForSallyOutAmbush();
                        list.Add(new SandBoxSallyOutMissionController());
                        list.Add(CreateCampaignMissionAgentSpawnLogic(battleSizeType, priorityTroopsForSallyOutAmbush, null));
                    }
                    else
                    {
                        if (isReliefForceAttack)
                        {
                            list.Add(new SandBoxSallyOutMissionController());
                        }
                        else
                        {
                            list.Add(new SandBoxSiegeMissionSpawnHandler());
                        }
                        list.Add(CreateCampaignMissionAgentSpawnLogic(battleSizeType, null, null));
                    }
                    list.Add(new BattlePowerCalculationLogic());
                    list.Add(new BattleObserverMissionLogic());
                    list.Add(new BattleAgentLogic());
                    list.Add(new BattleSurgeonLogic());
                    list.Add(new MountAgentLogic());
                    list.Add(new BannerBearerLogic());
                    list.Add(new AgentHumanAILogic());
                    list.Add(new AmmoSupplyLogic(new List<BattleSideEnum>
                {
                    BattleSideEnum.Defender
                }));
                    list.Add(new AgentVictoryLogic());
                    list.Add(new AssignPlayerRoleInTeamMissionController(!isPlayerSergeant, isPlayerSergeant, isPlayerInArmy, heroesOnPlayerSideByPriority, FormationClass.NumberOfRegularFormations));
                    List<MissionBehavior> list2 = list;
                    Hero leaderHero = MapEvent.PlayerMapEvent.AttackerSide.LeaderParty.LeaderHero;
                    TextObject attackerGeneralName = (leaderHero != null) ? leaderHero.Name : null;
                    Hero leaderHero2 = MapEvent.PlayerMapEvent.DefenderSide.LeaderParty.LeaderHero;
                    list2.Add(new SandboxGeneralsAndCaptainsAssignmentLogic(attackerGeneralName, (leaderHero2 != null) ? leaderHero2.Name : null, null, null, false));
                    list.Add(new MissionAgentPanicHandler());
                    list.Add(new MissionBoundaryPlacer());
                    list.Add(new MissionBoundaryCrossingHandler());
                    list.Add(new AgentMoraleInteractionLogic());
                    list.Add(new HighlightsController());
                    list.Add(new BattleHighlightsController());
                    list.Add(new EquipmentControllerLeaveLogic());
                    if (isSallyOut)
                    {
                        list.Add(new MissionSiegeEnginesLogic(new List<MissionSiegeWeapon>(), siegeWeaponsOfAttackers));
                    }
                    else
                    {
                        list.Add(new MissionSiegeEnginesLogic(siegeWeaponsOfDefenders, siegeWeaponsOfAttackers));
                    }
                    list.Add(new SiegeDeploymentHandler(isPlayerAttacker));
                    list.Add(new SiegeDeploymentMissionController(isPlayerAttacker));
                    return list.ToArray();
                }, true, true);

                return false;
            }
            return true;
        }

        private static MissionAgentSpawnLogic CreateCampaignMissionAgentSpawnLogic(Mission.BattleSizeType battleSizeType, FlattenedTroopRoster priorTroopsForDefenders = null, FlattenedTroopRoster priorTroopsForAttackers = null)
        {
            return new MissionAgentSpawnLogic(new IMissionTroopSupplier[]
            {
                new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender, priorTroopsForDefenders, null),
                new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker, priorTroopsForAttackers, null)
            }, PartyBase.MainParty.Side, battleSizeType);
        }

        private static Settlement GetCurrentTown()
        {
            if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown)
            {
                return Settlement.CurrentSettlement;
            }
            if (MapEvent.PlayerMapEvent != null && MapEvent.PlayerMapEvent.MapEventSettlement != null && MapEvent.PlayerMapEvent.MapEventSettlement.IsTown)
            {
                return MapEvent.PlayerMapEvent.MapEventSettlement;
            }
            return null;
        }
    }
}
