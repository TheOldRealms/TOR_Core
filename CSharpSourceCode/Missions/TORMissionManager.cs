using SandBox;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.View;
using SandBox.View.Missions;
using SandBox.View.Missions.Sound.Components;
using SandBox.ViewModelCollection;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TOR_Core.BattleMechanics.CustomArenaModes;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Missions.AmbushMission;
using static TaleWorlds.MountAndBlade.Mission;

namespace TOR_Core.Missions
{
    [MissionManager]
    public static class TorMissionManager
    {
        /// <summary>
        /// Launch a POI "visit scene" via MissionState.OpenNew, bypassing LocationEncounter.
        /// Used by <see cref="TOR_Core.CampaignMechanics.SettlementPOIs.SettlementPOI"/>s on
        /// TOR custom settlements (shrines, Oak of Ages, raiding sites, …) where vanilla's
        /// LocationEncounter is not populated because the engine's encounter machinery only
        /// recognises Town/Castle/Village/Hideout components. Scene loads cleanly; any NPC
        /// placement is the caller's responsibility (follow-up: pluggable trainer-spawn
        /// behavior per POI).
        /// </summary>
        [MissionMethod]
        public static Mission OpenPoiVisitMission(string scene, string returnMenuId, Hero trainerHero = null)
        {
            // "level_3 civilian" matches the atmosphere config BrawlFightMission uses —
            // tells SandBoxMissions to configure the scene for civilian traffic.
            // PoiVisitMissionController owns player-spawn + leave routing.
            // PoiCivilianAgentSpawner (optional) adds NPCs like trainers — composed in
            // only when a trainer is supplied; future POIs can add more spawners here.
            return MissionState.OpenNew(
                "TorPoiVisit",
                SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "level_3 civilian", false, DecalAtlasGroup.All),
                (Mission mission) =>
                {
                    var behaviors = new List<MissionBehavior>
                    {
                        new MissionOptionsComponent(),
                        new CampaignMissionComponent(),
                        new MissionBasicTeamLogic(),
                        new BasicLeaveMissionLogic(false),
                        new LeaveMissionLogic(),
                        new MissionAgentLookHandler(),
                        new AgentHumanAILogic(),
                        new MissionConversationLogic(),
                        new TORMissionAgentHandler(),
                        new MissionFacialAnimationHandler(),
                        new PoiVisitMissionController(returnMenuId),
                    };
                    if (trainerHero != null)
                    {
                        behaviors.Add(new PoiCivilianAgentSpawner(trainerHero));
                    }
                    return behaviors.ToArray();
                });
        }

        [MissionMethod]
        public static Mission OpenArcheryContestMission(string scene, ArcheryContestTournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
        {
            return MissionState.OpenNew("ArcheryContestFight", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.Town), delegate (Mission missionController)
            {
                ArcheryContestMissionController archeryContestMissionController = new ArcheryContestMissionController(culture);
                return
                [
                    new CampaignMissionComponent(),
                    new EquipmentControllerLeaveLogic(),
                    archeryContestMissionController,
                    new MissionExperienceBehavior(),
                    new ArcheryContestTournamentBehavior(tournamentGame, settlement, archeryContestMissionController, isPlayerParticipating),
                    new AgentVictoryLogic(),
                    new MissionAgentPanicHandler(),
                    new AgentHumanAILogic(),
                    new ArenaAgentStateDeciderLogic(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new MissionOptionsComponent(),
                    new HighlightsController(),
                    new SandboxHighlightsController()
                ];
            }, true, true);
        }

        [MissionMethod]
        public static Mission OpenJoustingFightMission(string scene, JoustTournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
        {
            return MissionState.OpenNew("JoustFight", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.Town), delegate (Mission missionController)
            {
                JoustFightMissionController joustFightMissionController = new JoustFightMissionController(culture);
                return
                [
                    new CampaignMissionComponent(),
                    new EquipmentControllerLeaveLogic(),
                    joustFightMissionController,
                    new MissionExperienceBehavior(),
                    new JoustTournamentBehavior(tournamentGame, settlement, joustFightMissionController, isPlayerParticipating),
                    new AgentVictoryLogic(),
                    new MissionAgentPanicHandler(),
                    new AgentHumanAILogic(),
                    new ArenaAgentStateDeciderLogic(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new MissionOptionsComponent(),
                    new HighlightsController(),
                    new SandboxHighlightsController()
                ];
            }, true, true);
        }

        [MissionMethod]
        public static Mission OpenDuelMission(Action<bool> onMissionEnd, Hero duelHero = null, string scene = "TOR_duel_001", bool useRapier = true)
        {
            return MissionState.OpenNew("InkDuelFight", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.All), (Mission mission) => new MissionBehavior[]
            {
                new MissionOptionsComponent(),
                new CampaignMissionComponent(),
                new MissionBasicTeamLogic(),
                new MissionAgentLookHandler(),
                new BasicLeaveMissionLogic(false),
                new LeaveMissionLogic(),
                new AgentHumanAILogic(),
                new MissionConversationLogic(),
                new DuelFightMissionController(onMissionEnd, duelHero, useRapier),
                new MissionExperienceBehavior(),
                new TORMissionAgentHandler(),
                new HeroSkillHandler(),
                new MissionFightHandler(),
                new MissionFacialAnimationHandler(),
                new MissionHardBorderPlacer(),
                new MissionBoundaryPlacer(),
                new MissionBoundaryCrossingHandler(),
                new EquipmentControllerLeaveLogic()
            }, true, true);
        }

        [MissionMethod]
        public static Mission OpenQuestMission(string scene, PartyTemplateObject enemyPartyTemplate, int enemyCount = 8, Action<bool> onMissionEnd = null, bool forceUsableMachineActivation = false)
        {
            return MissionState.OpenNew("QuestFight", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.All), (Mission mission) =>
            [
                new MissionOptionsComponent(),
                new CampaignMissionComponent(),
                new MissionBasicTeamLogic(),
                new MissionAgentLookHandler(),
                new BasicLeaveMissionLogic(false),
                new LeaveMissionLogic(),
                new AgentHumanAILogic(),
                new MissionConversationLogic(),
                new QuestFightMissionController(enemyPartyTemplate, enemyCount, onMissionEnd, forceUsableMachineActivation),
                new MissionExperienceBehavior(),
                new TORMissionAgentHandler(),
                new HeroSkillHandler(),
                new MissionFightHandler(),
                new MissionFacialAnimationHandler(),
                new MissionHardBorderPlacer(),
                new MissionBoundaryPlacer(),
                new MissionBoundaryCrossingHandler(),
                new EquipmentControllerLeaveLogic()
            ], true, true);
        }


        [MissionMethod]
        public static Mission OpenGraveyardMission()
        {
            var rec = SandBoxMissions.CreateSandBoxMissionInitializerRecord("TOR_graveyard_01_atmo_w_night", "", false, DecalAtlasGroup.All);
            return MissionState.OpenNew("Battle", rec, delegate (Mission mission)
            {
                IMissionTroopSupplier[] suppliers =
                [
                    new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender),
                    new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker)
                ];
                List<MissionBehavior> list =
                [
                    new DefaultBattleMissionAgentSpawnLogic(suppliers, BattleSideEnum.Defender, BattleSizeType.Battle), //OK
                    new BattlePowerCalculationLogic(), //OK
                    new BattleSpawnLogic("battle_set"), //OK
                    new GraveyardFightMissionController(), //OK
                    new MissionExperienceBehavior(),
                    new CampaignMissionComponent(), //OK
                    new BattleAgentLogic(), //OK
                    new MountAgentLogic(), //OK
                    new MissionOptionsComponent(), //OK
                    new BattleEndLogic(), //OK
                    new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.FieldBattle, false),
                    new BattleObserverMissionLogic(), //OK
                    new AgentHumanAILogic(),
                    new AgentVictoryLogic(),
                    new MissionAgentPanicHandler(),
                    new BattleMissionAgentInteractionLogic(),
                    new AgentMoraleInteractionLogic(),
                    new AssignPlayerRoleInTeamMissionController(true, false, false),
                    new BannerBearerLogic(),
                ];
                Hero attackerHero = MapEvent.PlayerMapEvent.AttackerSide.LeaderParty.LeaderHero;
                TextObject attackerGeneralName = attackerHero?.Name;
                Hero defenderHero = MapEvent.PlayerMapEvent.DefenderSide.LeaderParty.LeaderHero;
                list.Add(new SandboxGeneralsAndCaptainsAssignmentLogic(attackerGeneralName, defenderHero?.Name, null, null, false));
                list.Add(new EquipmentControllerLeaveLogic());
                list.Add(new MissionHardBorderPlacer());
                list.Add(new MissionBoundaryPlacer());
                list.Add(new MissionBoundaryCrossingHandler());
                list.Add(new HighlightsController());
                list.Add(new BattleHighlightsController());
                list.Add(new BattleDeploymentMissionController(false));
                list.Add(new BattleDeploymentHandler(false));
                return list.ToArray();
            }, true, true);
        }
        /// <summary>
        /// Open an ambush mission against the player's current MapEvent target.
        /// Caller must have run EncounterManager.StartPartyEncounter +
        /// PlayerEncounter.StartBattle so MapEvent.PlayerMapEvent is set.
        ///
        /// Phase D first-pass: scene is passed in by the caller. Terrain-driven
        /// scene selection (plus a bank of scener-authored ambush scenes) is a
        /// later improvement — the hybrid procedural / authored RetreatZone
        /// handles missing authored content for Phase D ship.
        /// </summary>
        [MissionMethod]
        public static Mission OpenAmbushMission(string scene)
        {
            TaleWorlds.Library.Debug.Print("[AmbushMission] OpenAmbushMission scene=" + scene);
            var rec = SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.All);
            TaleWorlds.Library.Debug.Print("[AmbushMission] rec built, calling MissionState.OpenNew");
            return MissionState.OpenNew("Battle", rec, delegate (Mission mission)
            {
                TaleWorlds.Library.Debug.Print("[AmbushMission] behaviors lambda enter");
                IMissionTroopSupplier[] suppliers =
                [
                    new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender),
                    new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker),
                ];
                int playerTroopCount = MapEvent.PlayerMapEvent.AttackerSide.TroopCount;
                int targetTroopCount = MapEvent.PlayerMapEvent.DefenderSide.TroopCount;
                List<MissionBehavior> list =
                [
                    // Player side = Attacker (user-confirmed; previous read of EncounterManager
                    // internals was misleading).
                    new DefaultBattleMissionAgentSpawnLogic(suppliers, BattleSideEnum.Attacker, BattleSizeType.Battle),
                    new BattlePowerCalculationLogic(),
                    new BattleSpawnLogic("battle_set"),
                    new AmbushMission.AmbushMissionController(playerTroopCount, targetTroopCount),
                    new AmbushMission.AmbushEndLogic(),
                    new MissionExperienceBehavior(),
                    new CampaignMissionComponent(),
                    new BattleAgentLogic(),
                    new MountAgentLogic(),
                    new MissionOptionsComponent(),
                    new BattleEndLogic(),
                    new MissionCombatantsLogic(
                        MobileParty.MainParty.MapEvent.InvolvedParties,
                        PartyBase.MainParty,
                        MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender),
                        MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker),
                        Mission.MissionTeamAITypeEnum.FieldBattle, false),
                    new BattleObserverMissionLogic(),
                    new AgentHumanAILogic(),
                    new AgentVictoryLogic(),
                    new MissionAgentPanicHandler(),
                    new BattleMissionAgentInteractionLogic(),
                    new AgentMoraleInteractionLogic(),
                    new AssignPlayerRoleInTeamMissionController(true, false, false),
                    new BannerBearerLogic(),
                ];
                Hero attackerHero = MapEvent.PlayerMapEvent.AttackerSide.LeaderParty.LeaderHero;
                Hero defenderHero = MapEvent.PlayerMapEvent.DefenderSide.LeaderParty.LeaderHero;
                list.Add(new SandboxGeneralsAndCaptainsAssignmentLogic(attackerHero?.Name, defenderHero?.Name, null, null, false));
                list.Add(new EquipmentControllerLeaveLogic());
                list.Add(new MissionHardBorderPlacer());
                list.Add(new MissionBoundaryPlacer());
                list.Add(new MissionBoundaryCrossingHandler());
                list.Add(new HighlightsController());
                list.Add(new BattleHighlightsController());
                list.Add(new BattleDeploymentMissionController(false));
                list.Add(new BattleDeploymentHandler(false));
                return list.ToArray();
            }, true, true);
        }

        [MissionMethod]
        public static Mission OpenBrawlFightMission(Location location, TroopRoster playerSideTroops,
            TroopRoster enemyPartyRoster, int enemyPartySize, Action<BrawlMissionResult> onMissionEnd = null)
        {
            //Sly : scene levels specify "civilian" so that the siege layer is not also loaded leading to conflicting states for objects like gates.
            return MissionState.OpenNew("BrawlFight", SandBoxMissions.CreateSandBoxMissionInitializerRecord(location.GetSceneName(3), "level_3 civilian", false, DecalAtlasGroup.All),
            (Mission mission) =>
                [
                    new MissionOptionsComponent(),
                    new CampaignMissionComponent(),
                    new MissionAgentLookHandler(),
                    new BattleSpawnLogic("battle_set"),
                    new AgentHumanAILogic(),
                    new BrawlMissionController(playerSideTroops, enemyPartyRoster, enemyPartySize, onMissionEnd),
                    new MissionExperienceBehavior(),
                    new HeroSkillHandler(),
                    new MissionFightHandler(),
                    new MissionFacialAnimationHandler(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new MissionBoundaryCrossingHandler(),
                    new EquipmentControllerLeaveLogic()
            ], true, true);
        }

        [MissionMethod]
        public static Mission OpenTrollCaveMission(TroopRoster selectedTroops, int trollCount, MobileParty defenderParty, bool stealthMode = false, string scene = "TOR_troll_hideout_01")
        {
            // Follow vanilla hideout pattern: NoTeamAI, custom spawning, direct PlayerOwner setup
            var rec = SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.All);
            return MissionState.OpenNew("TrollCaveFight", rec, delegate (Mission mission)
            {
                List<MissionBehavior> list =
                [
                    new MissionOptionsComponent(),
                    new CampaignMissionComponent(),
                    new BattleEndLogic(),
                    // NoTeamAI like vanilla hideout - player controls formations directly
                    new MissionCombatantsLogic(
                        MobileParty.MainParty.MapEvent.InvolvedParties,
                        PartyBase.MainParty,
                        MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender),
                        MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker),
                        Mission.MissionTeamAITypeEnum.NoTeamAI,
                        false),
                    new BattleObserverMissionLogic(),
                    new BattleAgentLogic(),
                    new AgentVictoryLogic(),
                    new MissionAgentLookHandler(),
                    new AgentHumanAILogic(),
                    new MissionAgentPanicHandler(),
                    new AgentMoraleInteractionLogic(),
                    // Custom spawning controller - sets PlayerOwner directly like HideoutMissionController
                    new TrollCaveMissionController(selectedTroops, trollCount, defenderParty, stealthMode),
                    new MissionExperienceBehavior(),
                    new HeroSkillHandler(),
                    new MissionFightHandler(),
                    new MissionFacialAnimationHandler(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new MissionBoundaryCrossingHandler(),
                    new EquipmentControllerLeaveLogic(),
                ];

                return list.ToArray();
            }, true, true);
        }
    }

    [ViewCreatorModule]
    public class BrawlFightCreatorModule
    {
        [ViewMethod("BrawlFight")]
        public static MissionView[] OpenBrawlFightMission(Mission mission)
        {
            return new List<MissionView>
                {
                    new MissionCampaignView(),
                    new MissionConversationCameraView(),
                    SandBoxViewCreator.CreateMissionConversationView(mission),
                    ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
                    ViewCreator.CreateOptionsUIHandler(),
                    ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                    new MissionSingleplayerViewHandler(),
                    ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                    ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                    ViewCreator.CreateMissionAgentLockVisualizerView(mission),
                    ViewCreator.CreateMissionBoundaryCrossingView(),
                    SandBoxViewCreator.CreateMissionBarterView(),
                    ViewCreator.CreateMissionLeaveView(),
                    new MissionBoundaryWallView(),
                    SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
                    new MissionItemContourControllerView(),
                    new MissionAgentContourControllerView(),
                    new MissionCampaignBattleSpectatorView(),
                    ViewCreator.CreatePhotoModeView(),
                    ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
                }.ToArray();
        }
    }



    /// <summary>
    /// Views for POI "visit scene" missions (shrines, Oak of Ages, etc.).
    /// The key addition vs. a bare mission is <c>ViewCreator.CreateMissionLeaveView()</c>
    /// which drives the "hold Tab to leave" prompt — without it the player is stuck
    /// until something ends the mission externally.
    /// </summary>
    [ViewCreatorModule]
    public class PoiVisitViewCreatorModule
    {
        [ViewMethod("TorPoiVisit")]
        public static MissionView[] OpenPoiVisitMission(Mission mission)
        {
            // Minimal view set — just what a walkable civilian scene needs:
            // camera, escape menu, agent status, conversation UI, and the hold-Tab leave prompt.
            // Boundary / boundary-crossing views are intentionally omitted because they NRE when
            // the mission has no MissionBoundaryPlacer / MissionHardBorderPlacer behaviors.
            // Nothing stops the player leaving — hold-Tab triggers the leave logic directly.
            return new MissionView[]
            {
                new MissionCampaignView(),
                new MissionConversationCameraView(),
                SandBoxViewCreator.CreateMissionConversationView(mission),
                ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
                ViewCreator.CreateOptionsUIHandler(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionLeaveView(),
            };
        }
    }

    [ViewCreatorModule]
    public class QuestFightViewCreatorModule
    {
        [ViewMethod("QuestFight")]
        public static MissionView[] OpenQuestFightMission(Mission mission)
        {
            return new List<MissionView>
                {
                    new MissionCampaignView(),
                    new MissionConversationCameraView(),
                    SandBoxViewCreator.CreateMissionConversationView(mission),
                    ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
                    ViewCreator.CreateOptionsUIHandler(),
                    ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                    new MissionSingleplayerViewHandler(),
                    ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                    ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                    ViewCreator.CreateMissionAgentLockVisualizerView(mission),
                    ViewCreator.CreateMissionBoundaryCrossingView(),
                    SandBoxViewCreator.CreateMissionBarterView(),
                    ViewCreator.CreateMissionLeaveView(),
                    new MissionBoundaryWallView(),
                    SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
                    new MissionItemContourControllerView(),
                    new MissionAgentContourControllerView(),
                    new MissionCampaignBattleSpectatorView(),
                    ViewCreator.CreatePhotoModeView(),
                    ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
                }.ToArray();
        }
    }

    [ViewCreatorModule]
    public class InkDuelFightViewCreatorModule
    {
        [ViewMethod("InkDuelFight")]
        public static MissionView[] OpenInkDuelFightMission(Mission mission)
        {
            return new List<MissionView>
                {
                    new MissionCampaignView(),
                    new MissionConversationCameraView(),
                    SandBoxViewCreator.CreateMissionConversationView(mission),
                    ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
                    ViewCreator.CreateOptionsUIHandler(),
                    ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                    new MissionSingleplayerViewHandler(),
                    ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                    ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                    ViewCreator.CreateMissionAgentLockVisualizerView(mission),
                    ViewCreator.CreateMissionBoundaryCrossingView(),
                    SandBoxViewCreator.CreateMissionBarterView(),
                    ViewCreator.CreateMissionLeaveView(),
                    new MissionBoundaryWallView(),
                    SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
                    new MissionItemContourControllerView(),
                    new MissionAgentContourControllerView(),
                    new MissionCampaignBattleSpectatorView(),
                    ViewCreator.CreatePhotoModeView(),
                    ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
                }.ToArray();
        }
    }

    [ViewCreatorModule]
    public class JoustFightViewCreatorModule
    {
        [ViewMethod("JoustFight")]
        public static MissionView[] OpenJoustFightMission(Mission mission)
        {
            return new List<MissionView>
                {
                    new MissionCampaignView(),
                    new MissionConversationCameraView(),
                    ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
                    ViewCreator.CreateOptionsUIHandler(),
                    ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                    SandBoxViewCreator.CreateMissionTournamentView(),
                    new MissionAudienceHandler(0.4f + MBRandom.RandomFloat * 0.6f),
                    ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                    ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                    ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                    ViewCreator.CreateMissionAgentLockVisualizerView(mission),
                    ViewCreator.CreateMissionSpectatorControlView(mission),
                    new MusicTournamentMissionView(),
                    new MissionSingleplayerViewHandler(),
                    ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
                    ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                    new MissionItemContourControllerView(),
                    new MissionCampaignBattleSpectatorView(),
                    ViewCreator.CreatePhotoModeView()
                }.ToArray();
        }
    }

    [ViewCreatorModule]
    public class ArcheryContestFightViewCreatorModule
    {
        [ViewMethod("ArcheryContestFight")]
        public static MissionView[] OpenArcheryFightMission(Mission mission)
        {
            return new List<MissionView>
                {
                    new MissionCampaignView(),
                    new MissionConversationCameraView(),
                    ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
                    ViewCreator.CreateOptionsUIHandler(),
                    ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                    SandBoxViewCreator.CreateMissionTournamentView(),
                    new MissionAudienceHandler(5f),
                    ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                    ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                    ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                    ViewCreator.CreateMissionAgentLockVisualizerView(mission),
                    ViewCreator.CreateMissionSpectatorControlView(mission),
                    new MusicTournamentMissionView(),
                    new MissionSingleplayerViewHandler(),
                    ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
                    ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                    new MissionItemContourControllerView(),
                    new MissionCampaignBattleSpectatorView(),
                    ViewCreator.CreatePhotoModeView()
                }.ToArray();
        }
    }

    [ViewCreatorModule]
    public class TrollCaveFightViewCreatorModule
    {
        [ViewMethod("TrollCaveFight")]
        public static MissionView[] OpenTrollCaveFightMission(Mission mission)
        {
            return new List<MissionView>
                {
                    new MissionCampaignView(),
                    new MissionConversationCameraView(),
                    ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
                    ViewCreator.CreateOptionsUIHandler(),
                    ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                    ViewCreator.CreateMissionBattleScoreUIHandler(mission, new SPScoreboardVM(null, null)),
                    new MissionSingleplayerViewHandler(),
                    ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                    ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                    ViewCreator.CreateMissionAgentLockVisualizerView(mission),
                    ViewCreator.CreateMissionBoundaryCrossingView(),
                    ViewCreator.CreateMissionLeaveView(),
                    new MissionBoundaryWallView(),
                    SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
                    new MissionItemContourControllerView(),
                    new MissionAgentContourControllerView(),
                    new MissionCampaignBattleSpectatorView(),
                    ViewCreator.CreatePhotoModeView(),
                    ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
                    ViewCreator.CreateMissionOrderUIHandler(),
                    new OrderTroopPlacer(null),
                    ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
                    new MissionFormationTargetSelectionHandler()
                }.ToArray();
        }
    }
}