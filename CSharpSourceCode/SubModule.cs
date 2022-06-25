﻿using HarmonyLib;
using NLog;
using NLog.Config;
using NLog.Targets;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.CustomBattle;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TOR_Core.BattleMechanics.Banners;
using TOR_Core.BattleMechanics.Dismemberment;
using TOR_Core.BattleMechanics.Firearms;
using TOR_Core.BattleMechanics.Morale;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics;
using TOR_Core.CampaignMechanics.Chaos;
using TOR_Core.CampaignMechanics.CustomEncounterDialogs;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.RaiseDead;
using TOR_Core.CampaignMechanics.SkillBooks;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core
{
    public class SubModule : MBSubModuleBase
    {
        private static float _tick = 0f;
        private static int _num = -1;

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            TORCommon.Say("TOR Core loaded.");
        }

        protected override void OnSubModuleLoad()
        {
            Harmony harmony = new Harmony("mod.harmony.theoldrealms");
            harmony.PatchAll();
            ConfigureLogging();
            UIConfig.DoNotUseGeneratedPrefabs = true;

            StatusEffectManager.LoadStatusEffects();
            TriggeredEffectManager.LoadTemplates();
            AbilityFactory.LoadTemplates();
            ExtendedItemObjectManager.LoadXML();
            CustomBannerManager.LoadXML();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            if(Game.Current.GameType is Campaign && starterObject is CampaignGameStarter)
            {
                var starter = starterObject as CampaignGameStarter;
                TORGameStarterHelper.CleanCampaignStarter(starter);
                starter.AddBehavior(new ExtendedInfoManager());
                starter.AddBehavior(new ChaosCampaignBehavior());
                starter.AddBehavior(new TORSkillBookCampaignBehavior());
                starter.AddBehavior(new TORCustomSettlementCampaignBehavior());
                starter.AddBehavior(new RaidingPartyCampaignBehavior());
                starter.AddBehavior(new CustomDialogCampaignBehavior());
                starter.AddBehavior(new SpellBookMapIconCampaignBehavior());
                starter.AddBehavior(new PostBattleCampaignBehavior());

            }
            else if (Game.Current.GameType is CustomGame && starterObject is BasicGameStarter)
            {
                ExtendedInfoManager.CreateDefaultInstanceAndLoad();
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (Game.Current.GameType is Campaign && gameStarterObject is CampaignGameStarter)
            {
                gameStarterObject.AddModel(new TORBattleMoraleModel());
                gameStarterObject.AddModel(new TOREncounterGameMenuModel());
                gameStarterObject.AddModel(new TORAgentStatCalculateModel());
            }
            else if (Game.Current.GameType is CustomGame && gameStarterObject is BasicGameStarter)
            {
                gameStarterObject.AddModel(new TORCustomBattleMoraleModel());
                gameStarterObject.AddModel(new TORCustomBattleAgentStatCalculateModel());
            }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {

            mission.RemoveMissionBehavior(mission.GetMissionBehavior<MissionGauntletCrosshair>());

            mission.AddMissionBehavior(new StatusEffectMissionLogic());
            mission.AddMissionBehavior(new ExtendedInfoMissionLogic());
            mission.AddMissionBehavior(new AbilityManagerMissionLogic());
            mission.AddMissionBehavior(new AbilityHUDMissionView());
            mission.AddMissionBehavior(new CustomCrosshairMissionBehavior());
            mission.AddMissionBehavior(new WeaponEffectMissionLogic());
            mission.AddMissionBehavior(new CustomBannerMissionLogic());
            mission.AddMissionBehavior(new DismembermentMissionLogic());
            mission.AddMissionBehavior(new UndeadMoraleMissionLogic());
            mission.AddMissionBehavior(new FirearmsMissionLogic());

            if (Game.Current.GameType is Campaign)
            {
                if (mission.GetMissionBehavior<BattleAgentLogic>() != null)
                {
                    mission.RemoveMissionBehavior(mission.GetMissionBehavior<BattleAgentLogic>());
                    mission.AddMissionBehavior(new TORBattleAgentLogic());
                }
            }
        }

        public override void BeginGameStart(Game game)
        {
            game.ObjectManager.RegisterType<TORCustomSettlementComponent>("TORCustomSettlementComponent", "TORCustomSettlementComponents", 99U, true);
        }

        private static void ConfigureLogging()
        {
            var config = new LoggingConfiguration();

            // Log debug/exception info to the log file
            var logfile = new FileTarget("logfile") { FileName = TORPaths.TORLogPath };
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            // Log info and higher to the VS debugger
            var logdebugger = new DebuggerTarget("logdebugger");
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logdebugger);

            LogManager.Configuration = config;
        }

        protected override void OnApplicationTick(float dt)
        {
            _tick += dt;
            if (_tick > 1)
            {
                _tick = 0;
                if (!LoadingWindow.IsLoadingWindowActive)
                {
                    var lastnum = _num;
                    _num = TaleWorlds.Engine.Utilities.GetNumberOfShaderCompilationsInProgress();
                    if (_num > 0 && _num != lastnum)
                    {
                        TORCommon.Say("Shader compilation in progress. Remaining shaders to compile: " + _num);
                    }
                }
            }
        }
    }
}
