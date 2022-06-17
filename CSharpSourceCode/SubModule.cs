using HarmonyLib;
using NLog;
using NLog.Config;
using NLog.Targets;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.SkillBooks;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
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
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            //this needs to be loaded early
            if(starterObject is CampaignGameStarter)
            {
                var starter = starterObject as CampaignGameStarter;
                starter.AddBehavior(new ExtendedInfoManager());
                starter.AddBehavior(new TORSkillBookCampaignBehavior());
            }
            else if (starterObject is BasicGameStarter)
            {
                ExtendedInfoManager.CreateDefaultInstanceAndLoad();
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (gameStarterObject is CampaignGameStarter)
            {
                var starter = gameStarterObject as CampaignGameStarter;
                //starter.AddBehavior(new TORSkillBookCampaignBehavior());
            }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            /*
            mission.RemoveMissionBehavior(mission.GetMissionBehavior<MissionGauntletCrosshair>());

            mission.AddMissionBehavior(new StatusEffectMissionLogic());
            mission.AddMissionBehavior(new ExtendedInfoMissionLogic());
            mission.AddMissionBehavior(new AbilityManagerMissionLogic());
            mission.AddMissionBehavior(new AbilityHUDMissionView());
            mission.AddMissionBehavior(new CustomCrosshairMissionBehavior());
            mission.AddMissionBehavior(new WeaponEffectMissionLogic());
            */
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
