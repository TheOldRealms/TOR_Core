using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using NLog;
using NLog.Config;
using NLog.Targets;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.InputSystem;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.CustomBattle;
using TaleWorlds.MountAndBlade.GameKeyCategory;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.ScreenSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.BaseGameDebug;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TOR_Core.BattleMechanics;
using TOR_Core.BattleMechanics.AI;
using TOR_Core.BattleMechanics.AI.TeamBehavior;
using TOR_Core.BattleMechanics.Atmosphere;
using TOR_Core.BattleMechanics.Banners;
using TOR_Core.BattleMechanics.Dismemberment;
using TOR_Core.BattleMechanics.Firearms;
using TOR_Core.BattleMechanics.Morale;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics;
using TOR_Core.CampaignMechanics.Assimilation;
using TOR_Core.CampaignMechanics.Chaos;
using TOR_Core.CampaignMechanics.CustomDialogs;
using TOR_Core.CampaignMechanics.CustomEncounterDialogs;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.RaiseDead;
using TOR_Core.CampaignMechanics.RegimentsOfRenown;
using TOR_Core.CampaignMechanics.SkillBooks;
using TOR_Core.CampaignMechanics.SpellTrainers;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CampaignSupport.TownBehaviours;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Extensions.UI;
using TOR_Core.GameManagers;
using TOR_Core.Items;
using TOR_Core.Models;
using TOR_Core.Models.CustomBattleModels;
using TOR_Core.Utilities;

namespace TOR_Core
{
    public class SubModule : MBSubModuleBase
    {
        private static float _tick = 0f;
        private static int _num = -1;
        public static Harmony HarmonyInstance { get; private set; }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            TORCommon.Say("TOR Core loaded.");
        }

        protected override void OnSubModuleLoad()
        {
            ViewModelExtensionManager.Initialize(); //has to happen before harmony PatchAll
            HarmonyInstance = new Harmony("mod.harmony.theoldrealms");
            HarmonyInstance.PatchAll();
            ConfigureLogging();
            UIConfig.DoNotUseGeneratedPrefabs = true;
            
            TORKeyInputManager.Initialize();
            StatusEffectManager.LoadStatusEffects();
            TriggeredEffectManager.LoadTemplates();
            AbilityFactory.LoadTemplates();
            ExtendedItemObjectManager.LoadXML();
            CustomBannerManager.LoadXML();
            RORManager.LoadTemplates();
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
                starter.AddBehavior(new RaiseDeadInTownBehavior());
                
                starter.AddBehavior(new RORCampaignBehavior());
                starter.AddBehavior(new TORCaptivityCampaignBehavior());
                starter.AddBehavior(new TORPartyHealCampaignBehavior());
                starter.AddBehavior(new AssimilationCampaignBehavior());
                starter.AddBehavior(new TORWanderersCampaignBehavior());
                starter.AddBehavior(new SpellTrainerInTownBehavior());
                starter.AddBehavior(new MasterEngineerTownBehaviour());
                starter.AddBehavior(new TORPerkHandlerCampaignBehavior());
                starter.AddBehavior(new BaseGameDebugCampaignBehavior());
                starter.AddBehavior(new BloodKissCampaignBehavior());
                starter.AddBehavior(new TORPartyUpgraderCampaignBehavior());
                TORGameStarterHelper.AddVerifiedIssueBehaviors(starter);

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
                gameStarterObject.AddModel(new TORCompanionHiringPriceCalculationModel());
                gameStarterObject.AddModel(new TORBanditDensityModel());
                gameStarterObject.AddModel(new TORCharacterStatsModel());
                gameStarterObject.AddModel(new TORClanFinanceModel());
                gameStarterObject.AddModel(new TORClanTierModel());
                gameStarterObject.AddModel(new TORCombatXpModel());
                gameStarterObject.AddModel(new TORDamageParticleModel());
                gameStarterObject.AddModel(new TORMapWeatherModel());
                gameStarterObject.AddModel(new TORMarriageModel());
                gameStarterObject.AddModel(new TORMobilePartyFoodConsumptionModel());
                gameStarterObject.AddModel(new TORPartyHealingModel());
                gameStarterObject.AddModel(new TORPartySizeModel());
                gameStarterObject.AddModel(new TORPartySpeedCalculatingModel());
                gameStarterObject.AddModel(new TORPartyTroopUpgradeModel());
                gameStarterObject.AddModel(new TORPartyWageModel());
                gameStarterObject.AddModel(new TORPrisonerRecruitmentCalculationModel());
                gameStarterObject.AddModel(new TORSettlementMilitiaModel());
                gameStarterObject.AddModel(new TORAbilityModel());
                gameStarterObject.AddModel(new TORCharacterDevelopmentModel());
                gameStarterObject.AddModel(new TORPartyTrainingModel());
                gameStarterObject.AddModel(new TORInventoryCapacityModel());
                gameStarterObject.AddModel(new TORAgentApplyDamageModel());
                gameStarterObject.AddModel(new TORStrikeMagnitudeModel());
                gameStarterObject.AddModel(new TORCombatSimulationModel());
                gameStarterObject.AddModel(new TORPartyMoraleModel());
                gameStarterObject.AddModel(new TORPersuasionModel());
                gameStarterObject.AddModel(new TORVoiceOverModel());

                CampaignOptions.IsLifeDeathCycleDisabled = true;
            }
            else if (Game.Current.GameType is CustomGame && gameStarterObject is BasicGameStarter)
            {
                gameStarterObject.AddModel(new TORCustomBattleMoraleModel());
                gameStarterObject.AddModel(new TORCustomBattleAgentStatCalculateModel());
            }
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            var missionCombatantsLogic = mission.GetMissionBehavior<MissionCombatantsLogic>();
            
            if (missionCombatantsLogic == null)
                return;
            
            mission.AddMissionLogicAtIndexOf(missionCombatantsLogic, TorMissionCombatantsLogic.CreateFromInstanace(missionCombatantsLogic));
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {

            mission.RemoveMissionBehavior(mission.GetMissionBehavior<MissionGauntletCrosshair>());
            //mission.AddMissionBehavior(new LanceRemovalMissionLogic());
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
            
            mission.AddMissionBehavior(new ForceAtmosphereMissionLogic());
            //mission.AddMissionBehavior(new BaseGameDebugMissionLogic());

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
            game.ObjectManager.RegisterType<CareerObject>("Career", "Careers", 100U, true);
            game.ObjectManager.RegisterType<CareerChoiceObject>("CareerChoice", "CareerChoices", 100U, true);
            _ = new TORCareers();
            _ = new TORCareerChoices();
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
