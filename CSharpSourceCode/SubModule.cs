using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using HarmonyLib;
using NLog;
using NLog.Config;
using NLog.Targets;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.CustomBattle;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TOR_Core.BattleMechanics;
using TOR_Core.BattleMechanics.AI.TeamAI;
using TOR_Core.BattleMechanics.Atmosphere;
using TOR_Core.BattleMechanics.Banners;
using TOR_Core.BattleMechanics.Dismemberment;
using TOR_Core.BattleMechanics.DualWield;
using TOR_Core.BattleMechanics.Firearms;
using TOR_Core.BattleMechanics.Morale;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics;
using TOR_Core.CampaignMechanics.AICompanions;
using TOR_Core.CampaignMechanics.Assimilation;
using TOR_Core.CampaignMechanics.BountyMaster;
using TOR_Core.CampaignMechanics.Careers;
using TOR_Core.CampaignMechanics.Chaos;
using TOR_Core.CampaignMechanics.CustomDialogs;
using TOR_Core.CampaignMechanics.CustomEncounterDialogs;
using TOR_Core.CampaignMechanics.CustomEvents;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Diplomacy;
using TOR_Core.CampaignMechanics.Menagery;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.RaiseDead;
using TOR_Core.CampaignMechanics.RegimentsOfRenown;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CampaignMechanics.ServeAsAHireling;
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
using TOR_Core.Ink;
using TOR_Core.Items;
using TOR_Core.Models;
using TOR_Core.Models.CustomBattleModels;
using TOR_Core.Quests;
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
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(ResolveDllPath);

            CampaignTime startTime = CampaignTime.Years(2502) + CampaignTime.Weeks(4) + CampaignTime.Days(5) + CampaignTime.Hours(12);
            
            typeof(CampaignData).GetField("CampaignStartTime",BindingFlags.Static|BindingFlags.Public)?.SetValue(null,startTime);
            
            ViewModelExtensionManager.Initialize(); //has to happen before harmony PatchAll
            HarmonyInstance = new Harmony("mod.harmony.theoldrealms");
            HarmonyInstance.PatchAll();
            ConfigureLogging();
            UIConfig.DoNotUseGeneratedPrefabs = true;

            TORConfig.ReadConfig();
            TORKeyInputManager.Initialize();
            StatusEffectManager.LoadStatusEffects();
            TriggeredEffectManager.LoadTemplates();
            AbilityFactory.LoadTemplates();
            ExtendedItemObjectManager.LoadXML();
            CustomBannerManager.LoadXML();
            RORManager.LoadTemplates();
            InkStoryManager.Initialize();
            AnimationTriggerManager.LoadAnimationTriggers();
            CustomResourceManager.Initialize();
        }

        private Assembly ResolveDllPath(object sender, ResolveEventArgs args)
        {
            var dllPath = TORPaths.TORCoreModuleRootPath + "bin/Win64_Shipping_Client/" + new AssemblyName(args.Name).Name + ".dll";
            if (File.Exists(dllPath))
            {
                return Assembly.LoadFrom(dllPath);
            }
            else return null;
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
                //starter.AddBehavior(new InvasionCampaignBehavior());
                starter.AddBehavior(new CustomDialogCampaignBehavior());
                starter.AddBehavior(new PostBattleCampaignBehavior());
                starter.AddBehavior(new RaiseDeadInTownBehavior());
                starter.AddBehavior(new RORCampaignBehavior());
                starter.AddBehavior(new TORCaptivityCampaignBehavior());
                starter.AddBehavior(new AssimilationCampaignBehavior());
                starter.AddBehavior(new TORWanderersCampaignBehavior());
                starter.AddBehavior(new SpellTrainerInTownBehavior());
                starter.AddBehavior(new MasterEngineerTownBehaviour());
                starter.AddBehavior(new PrestigeNobleTownBehavior());
                starter.AddBehavior(new EonirFavorEnvoyTownBehavior());
                starter.AddBehavior(new TORPerkHandlerCampaignBehavior());
                starter.AddBehavior(new TORAICompanionCampaignBehavior());
                starter.AddBehavior(new CareerSwitchCampaignBehavior());
                starter.AddBehavior(new TORPartyUpgraderCampaignBehavior());
                starter.AddBehavior(new InkStoryCampaignBehavior());
                starter.AddBehavior(new ReligionCampaignBehavior());
                starter.AddBehavior(new BountyMasterCampaignBehavior());
                starter.AddBehavior(new HuntCultistsQuestCampaignBehavior());
                starter.AddBehavior(new TORCareerPerkCampaignBehavior());
                starter.AddBehavior(new RaceFixCampaignBehavior());
                starter.AddBehavior(new TORAIRecruitmentCampaignBehavior());
                starter.AddBehavior(new TORSpecialSettlementBehavior());
                starter.AddBehavior(new CustomEventsCampaignBehavior());
                starter.AddBehavior(new PlaguedVillageQuestCampaignBehavior());
                starter.AddBehavior(new CareerDialogOptionsCampaignBehavior());
                starter.AddBehavior(new TORFactionDiscontinuationCampaignBehavior());
                starter.AddBehavior(new TORKingdomDecisionsCampaignBehavior());
                starter.AddBehavior(new ServeAsAHirelingCampaignBehavior());
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
                gameStarterObject.Models.RemoveAllOfType(typeof(DefaultMapDistanceModel));
                gameStarterObject.Models.RemoveAllOfType(typeof(DefaultAlleyModel));
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
                //gameStarterObject.AddModel(new TORMapWeatherModel());
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
                gameStarterObject.AddModel(new TORFaithModel());
                gameStarterObject.AddModel(new TORCustomResourceModel());
                gameStarterObject.AddModel(new TORClanPoliticsModel());
                gameStarterObject.AddModel(new TORMapVisibilityModel());
                gameStarterObject.AddModel(new TORTournamentModel());
                gameStarterObject.AddModel(new TORDiplomacyModel());
                gameStarterObject.AddModel(new TORAlleyModel());
                gameStarterObject.AddModel(new TORRaidModel());
                gameStarterObject.AddModel(new TORBattleBannerBearersModel());
                gameStarterObject.AddModel(new TORKingdomDecisionPermissionModel());
                gameStarterObject.AddModel(new TORSettlementLoyaltyModel());
                gameStarterObject.AddModel(new TORBattleRewardModel());
                gameStarterObject.AddModel(new TORTroopSupplierModel());
                gameStarterObject.AddModel(new TORSettlementFoodModel());
                gameStarterObject.AddModel(new TOREquipmentSelectionModel());
                gameStarterObject.AddModel(new TOREncounterModel());
                gameStarterObject.AddModel(new TORSettlementDistanceModel());
                gameStarterObject.AddModel(new TORVolunteerModel());

                CampaignOptions.IsLifeDeathCycleDisabled = true;
            }
            else if (Game.Current.GameType is CustomGame && gameStarterObject is BasicGameStarter)
            {
                gameStarterObject.AddModel(new TORDamageParticleModel());
                gameStarterObject.AddModel(new TORCustomBattleMoraleModel());
                gameStarterObject.AddModel(new TORCustomBattleAgentStatCalculateModel());
            }
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            var missionCombatantsLogic = mission.GetMissionBehavior<MissionCombatantsLogic>();
            
            if (missionCombatantsLogic == null)
                return;
            
            mission.AddMissionLogicAtIndexOf(missionCombatantsLogic, TORMissionCombatantsLogic.CreateFromInstance(missionCombatantsLogic));
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            var toRemove = mission.GetMissionBehavior<MissionGauntletCrosshair>();
            if(toRemove != null) mission.RemoveMissionBehavior(toRemove);

            mission.AddMissionBehavior(new StatusEffectMissionLogic());
            mission.AddMissionBehavior(new ExtendedInfoMissionLogic());
            mission.AddMissionBehavior(new AbilityManagerMissionLogic());
            mission.AddMissionBehavior(new AbilityHUDMissionView());
            mission.AddMissionBehavior(new CustomCrosshairMissionBehavior());
            mission.AddMissionBehavior(new WeaponEffectMissionLogic());
            mission.AddMissionBehavior(new CustomBannerMissionLogic());
            mission.AddMissionBehavior(new DismembermentMissionLogic());
            mission.AddMissionBehavior(new MoraleMissionLogic());
            mission.AddMissionBehavior(new FirearmsMissionLogic());
            mission.AddMissionBehavior(new ForceAtmosphereMissionLogic());
            mission.AddMissionBehavior(new AnimationTriggerMissionLogic());
            mission.AddMissionBehavior(new DualWieldMissionLogic());
            mission.AddMissionBehavior(new BattleShoutsMissionLogic());


            if (Game.Current.GameType is Campaign)
            {
                mission.AddMissionBehavior(new CareerPerkMissionBehavior());
                if (mission.GetMissionBehavior<BattleAgentLogic>() != null)
                {
                    mission.RemoveMissionBehavior(mission.GetMissionBehavior<BattleAgentLogic>());
                    mission.AddMissionBehavior(new TORBattleAgentLogic());
                }
            }

            if(Debugger.IsAttached)
            {
                mission.AddMissionBehavior(new TORAnimationLogger());
            }
        }


        public override void BeginGameStart(Game game)
        {
            if(game.GameType is Campaign)
            {
                game.ObjectManager.RegisterType<ShrineComponent>("Shrine", "Components", 99U, true);
                game.ObjectManager.RegisterType<ChaosPortalComponent>("ChaosPortal", "Components", 100U, true);
                game.ObjectManager.RegisterType<HerdStoneComponent>("HerdStone", "Components", 101U, true);
                game.ObjectManager.RegisterType<CursedSiteComponent>("CursedSite", "Components", 102U, true);
                game.ObjectManager.RegisterType<CareerObject>("Career", "Careers", 103U, true);
                game.ObjectManager.RegisterType<CareerChoiceObject>("CareerChoice", "CareerChoices", 104U, true);
                game.ObjectManager.RegisterType<CareerChoiceGroupObject>("CareerChoiceGroup", "CareerChoiceGroups", 105U, true);
                game.ObjectManager.RegisterType<ReligionObject>("Religion", "Religions", 106U, true);
                game.ObjectManager.RegisterType<SlaverCampComponent>("SlaverCamp", "Components", 107U, true);
                game.ObjectManager.RegisterType<OakOfAgesComponent>("OakOfAges", "Components", 108U, true);
                game.ObjectManager.RegisterType<WorldRootsComponent>("WorldRoots", "Components", 109U, true);
                _ = new TORCareers();
                _ = new TORCareerChoiceGroups();
                _ = new TORCareerChoices();
                _ = new TORCampaignEvents();
                
                MBObjectManager.Instance.LoadXML("Religions", false);
                ReligionObject.FillAll();
            }
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
