using HarmonyLib;
using NLog;
using NLog.Config;
using NLog.Targets;
using SandBox.Missions.MissionLogics;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.CustomBattle;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TOR_Core.BattleMechanics;
using TOR_Core.BattleMechanics.AI.TeamAI;
using TOR_Core.BattleMechanics.Banners;
using TOR_Core.BattleMechanics.Dismemberment;
using TOR_Core.BattleMechanics.Firearms;
using TOR_Core.BattleMechanics.Morale;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.BattleMechanics.Voice;
using TOR_Core.CampaignMechanics;
using TOR_Core.CampaignMechanics.Assimilation;
using TOR_Core.CampaignMechanics.BountyMaster;
using TOR_Core.CampaignMechanics.Careers;
using TOR_Core.CampaignMechanics.Chaos;
using TOR_Core.CampaignMechanics.Companions;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CampaignMechanics.CustomDialogs;
using TOR_Core.CampaignMechanics.CustomEncounterDialogs;
using TOR_Core.CampaignMechanics.CustomEvents;
using TOR_Core.CampaignMechanics.CustomResourceBehavior;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Diplomacy;
using TOR_Core.CampaignMechanics.Menagery;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.RaiseDead;
using TOR_Core.CampaignMechanics.RegimentsOfRenown;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CampaignMechanics.ServeAsAHireling;
using TOR_Core.CampaignMechanics.SpellTrainers;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CampaignMechanics.TORCustomSettlement.Component;
using TOR_Core.CampaignMechanics.UniqueSpawns;
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
        private static bool ENABLECOPYSHADERS = true;
        public static Harmony HarmonyInstance { get; private set; }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            TORCommon.Say("TOR Core loaded.");
            MBDebug.Print("TOR Core version loaded : " + ModuleHelper.GetModuleInfo("TOR_Core").Version.ToString());
        }

        protected override void OnSubModuleLoad()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(ResolveDllPath);

            ConfigureLogging();

            // Copy TOR_Armory shader sources to game folder (must happen before shaders are compiled)
            if (ENABLECOPYSHADERS)
            {
                ShaderSourceManager.CopyShaderSourcesToGame();
            }
  

            ViewModelExtensionManager.Initialize(); //has to happen before harmony PatchAll

            HarmonyInstance = new Harmony("mod.harmony.theoldrealms");
            HarmonyInstance.PatchAllUncategorized();
            UIConfig.DoNotUseGeneratedPrefabs = true;

            TORConfig.ReadConfig();
            TORKeyInputManager.Initialize();
            StatusEffectManager.LoadStatusEffects();
            TriggeredEffectManager.LoadTemplates();
            AbilityFactory.LoadTemplates();
            ExtendedItemObjectManager.LoadXML("", true);
            CustomBannerManager.LoadXML();
            RORManager.LoadTemplates();
            InkStoryManager.Initialize();
            AnimationTriggerManager.LoadAnimationTriggers();
            ItemTraitManager.LoadItemTraits();
            TORVoiceManager.Initialize();
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

        /// <remarks>
        /// Sly : Game.Initialize -> GameType.OnInitialize -> Campaign.OnInitialize override and InitializeGameStarter is within that last one.
        /// Game.Initialize is when the game text manager is initialized, so any harmony patches that cause issues with static strings being initialized too soon needs to be at this point or later - this is the earliest override that occurs post text manager initialization.
        /// Consequently, anything we have that makes use of GameText.FindText should be initialized inside of or after this so that all our strings in tor_strings.xml are available.
        /// </remarks>
        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            CustomResourceManager.Initialize();
            if (Game.Current.GameType is Campaign && starterObject is CampaignGameStarter)
            {
                var starter = starterObject as CampaignGameStarter;
                TORGameStarterHelper.CleanCampaignStarter(starter);
                starter.AddBehavior(new ExtendedInfoManager());
                starter.AddBehavior(new ChaosCampaignBehavior());
                starter.AddBehavior(new InventoryUseScriptsCampaignBehavior());
                starter.AddBehavior(new TORCustomSettlementCampaignBehavior());
                starter.AddBehavior(new TrollCaveCampaignBehavior());
                starter.AddBehavior(new RaidingPartyCampaignBehavior());
                starter.AddBehavior(new UniqueSpawnCampaignBehavior());
                starter.AddBehavior(new OrionCampaignBehavior());
                starter.AddBehavior(new CustomDialogCampaignBehavior());
                starter.AddBehavior(new TORCompanionDialogBehavior());
                starter.AddBehavior(new PostBattleCampaignBehavior());
                starter.AddBehavior(new RaiseDeadInTownBehavior());
                starter.AddBehavior(new RORCampaignBehavior());
                starter.AddBehavior(new TORCaptivityCampaignBehavior());
                starter.AddBehavior(new AssimilationCampaignBehavior());
                starter.AddBehavior(new SpellTrainerInTownBehavior());
                starter.AddBehavior(new EnchanterTownBehavior());
                starter.AddBehavior(new MasterEngineerTownBehaviour());
                starter.AddBehavior(new PrestigeNobleTownBehavior());
                starter.AddBehavior(new EonirFavorEnvoyTownBehavior());
                starter.AddBehavior(new TORPerkHandlerCampaignBehavior());
                starter.AddBehavior(new TORAICompanionCampaignBehavior());
                starter.AddBehavior(new TORCompanionsCampaignBehavior());
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
                starter.AddBehavior(new GreenskinAICampaignBehavior());
                starter.AddBehavior(new CustomEventsCampaignBehavior());
                starter.AddBehavior(new SimpleCareerQuestBehavior());
                starter.AddBehavior(new PlaguedVillageQuestCampaignBehavior());
                starter.AddBehavior(new CareerDialogOptionsCampaignBehavior());
                starter.AddBehavior(new TORFactionDiscontinuationCampaignBehavior());
                starter.AddBehavior(new ServeAsAHirelingCampaignBehavior());
                starter.AddBehavior(new TORStartupBehavior());
                starter.AddBehavior(new TORKingdomDecisionsCampaignBehavior());
                starter.AddBehavior(new TORAllianceWarBehavior());
                starter.AddBehavior(new TORArtisanDistrictCampaignBehavior());
                starter.AddBehavior(new PriestBehavior());
                starter.AddBehavior(new SkillTrainerBehavior());
                starter.AddBehavior(new EnchantmentIngredientLootCampaignBehavior());
                starter.AddBehavior(new LootCampaignBehavior());
                starter.AddBehavior(new OathGoldBehavior());
                starter.AddBehavior(new TeefBehavior());
                starter.AddBehavior(new WaaaghBehavior());
                starter.AddBehavior(new GreenskinBrawlBehavior());
                starter.AddBehavior(new GoblinRecruitmentBehavior());
                starter.AddBehavior(new DuelBehavior());
                TORGameStarterHelper.AddVerifiedIssueBehaviors(starter);

            }
            else if (Game.Current.GameType is CustomGame && starterObject is BasicGameStarter)
            {
                ExtendedInfoManager.CreateDefaultInstanceAndLoad();
            }
        }

        public override void OnGameInitializationFinished(Game game)
        {
            HarmonyInstance.PatchCategory("LatePatches");//Sly : Campaign.OnInitialize starts the cascade that reaches this method; if each time a campaign is started/loaded this is patched, will we end up patching a given method multiple times?
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (Game.Current.GameType is Campaign && gameStarterObject is CampaignGameStarter)
            {
                gameStarterObject.AddModel(new TORBattleMoraleModel());
                gameStarterObject.AddModel(new TORBuildingEffectModel());
                gameStarterObject.AddModel(new TOREncounterGameMenuModel());
                gameStarterObject.AddModel(new TORAgentStatCalculateModel());
                gameStarterObject.AddModel(new TORCompanionHiringPriceCalculationModel());
                gameStarterObject.AddModel(new TORBanditDensityModel());
                gameStarterObject.AddModel(new TORCharacterStatsModel());
                gameStarterObject.AddModel(new TORClanFinanceModel());
                gameStarterObject.AddModel(new TORMinorFactionsModel());
                gameStarterObject.AddModel(new TORClanTierModel());
                gameStarterObject.AddModel(new TORCombatXpModel());
                gameStarterObject.AddModel(new TORDamageParticleModel());
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
                gameStarterObject.AddModel(new TORPartyDesertionModel());
                gameStarterObject.AddModel(new TORPersuasionModel());
                gameStarterObject.AddModel(new TORVoiceOverModel());
                gameStarterObject.AddModel(new TORFaithModel());
                gameStarterObject.AddModel(new TORCustomResourceModel());
                gameStarterObject.AddModel(new TORClanPoliticsModel());
                gameStarterObject.AddModel(new TORMapVisibilityModel());
                gameStarterObject.AddModel(new TORMobilePartyAIModel());
                gameStarterObject.AddModel(new TORTournamentModel());
                gameStarterObject.AddModel(new TORAlleyModel());
                gameStarterObject.AddModel(new TORRaidModel());
                gameStarterObject.AddModel(new TORBattleBannerBearersModel());
                gameStarterObject.AddModel(new TORKingdomDecisionPermissionModel());
                gameStarterObject.AddModel(new TORDiplomacyModel());
                gameStarterObject.AddModel(new TORAllianceModel());
                gameStarterObject.AddModel(new TORTradeAgreementModel());
                gameStarterObject.AddModel(new TORTradeItemPriceFactorModel());
                gameStarterObject.AddModel(new TORSettlementLoyaltyModel());
                gameStarterObject.AddModel(new TORSettlementProsperityModel());
                gameStarterObject.AddModel(new TORBattleRewardModel());
                gameStarterObject.AddModel(new TORTroopSupplierModel());
                gameStarterObject.AddModel(new TORSettlementFoodModel());
                gameStarterObject.AddModel(new TOREquipmentSelectionModel());
                gameStarterObject.AddModel(new TOREncounterModel());
                gameStarterObject.AddModel(new TORVolunteerModel());
                gameStarterObject.AddModel(new TORSmithingModel());
                gameStarterObject.AddModel(new TOREnchantmentIngredientsModel());
                gameStarterObject.AddModel(new TORCompanionTrainingModel());
                gameStarterObject.AddModel(new TORVillageProductionCalculatorModel());
                gameStarterObject.AddModel(new TOREnchantmentCraftingModel());
                gameStarterObject.AddModel(new TORCampaignTimeModel());
                gameStarterObject.AddModel(new TORSiegeEngineCalculationModel());
                gameStarterObject.AddModel(new TORHiringCompatibilityModel());
                gameStarterObject.AddModel(new TORReinforcementRestrictionModel());

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
            if (toRemove != null) mission.RemoveMissionBehavior(toRemove);

            mission.AddMissionBehavior(new StatusEffectMissionLogic());
            mission.AddMissionBehavior(new AbilityManagerMissionLogic());
            mission.AddMissionBehavior(new AbilityHUDMissionView());
            mission.AddMissionBehavior(new CustomCrosshairMissionBehavior());
            mission.AddMissionBehavior(new WeaponHitScriptsMissionLogic());
            mission.AddMissionBehavior(new CustomBannerMissionLogic());
            mission.AddMissionBehavior(new DismembermentMissionLogic());
            mission.AddMissionBehavior(new AddAgentComponentsMissionLogic());
            mission.AddMissionBehavior(new FirearmsMissionLogic());
            mission.AddMissionBehavior(new AnimationTriggerMissionLogic());
            mission.AddMissionBehavior(new BattleShoutsMissionLogic());
            mission.AddMissionBehavior(new CinematicCameraMissionView());
            mission.AddMissionBehavior(new TORMonsterSiegeLogic());

            if (Game.Current.GameType is Campaign)
            {
                mission.AddMissionBehavior(new CareerPerkMissionBehavior());
                if (mission.GetMissionBehavior<BattleAgentLogic>() != null)
                {
                    mission.RemoveMissionBehavior(mission.GetMissionBehavior<BattleAgentLogic>());
                    mission.AddMissionBehavior(new TORBattleAgentLogic());
                }
            }

            if (Debugger.IsAttached)
            {
                mission.AddMissionBehavior(new TORAnimationLogger());
            }
        }

        public override void BeginGameStart(Game game)
        {
            if (game.GameType is Campaign)
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
                game.ObjectManager.RegisterType<TrollCaveComponent>("TrollCave", "Components", 110U, true);
                _ = new TORCareers();
                _ = new TORCareerChoiceGroups();
                _ = new TORCareerChoices();
                _ = new TORCampaignEvents();

                MBObjectManager.Instance.LoadXML("Religions", false);
                ReligionObject.FillAll();
            }
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