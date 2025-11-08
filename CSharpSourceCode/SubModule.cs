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
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.CustomBattle;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TOR_Core.AbilitySystem;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TOR_Core.BattleMechanics;
using TOR_Core.BattleMechanics.AI.TeamAI;
using TOR_Core.BattleMechanics.Atmosphere;
using TOR_Core.BattleMechanics.Banners;
using TOR_Core.BattleMechanics.Dismemberment;
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
using TOR_Core.CampaignSupport.TownBehaviours;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
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
        public static Harmony HarmonyInstance { get; private set; }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            TORCommon.Say("TOR Core loaded.");
        }

        protected override void OnSubModuleLoad()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(ResolveDllPath);

            ConfigureLogging();
            HarmonyInstance = new Harmony("mod.harmony.theoldrealms");
            HarmonyInstance.PatchAll();
            UIConfig.DoNotUseGeneratedPrefabs = true;

            TORConfig.ReadConfig();
            TORKeyInputManager.Initialize();
            StatusEffectManager.LoadStatusEffects();
            TriggeredEffectManager.LoadTemplates();
            AbilityFactory.LoadTemplates();
            ExtendedItemObjectManager.LoadXML();
            CustomBannerManager.LoadXML();
            RORManager.LoadTemplates();
            //InkStoryManager.Initialize(); TODO: need to solve early binding of external functions before enabling
            AnimationTriggerManager.LoadAnimationTriggers();
            ItemTraitManager.LoadItemTraits();
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

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            CustomResourceManager.Initialize();
            if (Game.Current.GameType is Campaign && starterObject is CampaignGameStarter)
            {
                var starter = starterObject as CampaignGameStarter;
                TORGameStarterHelper.CleanCampaignStarter(starter);
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
                gameStarterObject.AddModel(new TORBuildingEffectModel());
                gameStarterObject.AddModel(new TOREncounterGameMenuModel());
                gameStarterObject.AddModel(new TORAgentStatCalculateModel());
                gameStarterObject.AddModel(new TORCompanionHiringPriceCalculationModel());
                gameStarterObject.AddModel(new TORBanditDensityModel());
                gameStarterObject.AddModel(new TORCharacterStatsModel());
                gameStarterObject.AddModel(new TORClanFinanceModel());
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
                gameStarterObject.AddModel(new TORPersuasionModel());
                gameStarterObject.AddModel(new TORVoiceOverModel());
                gameStarterObject.AddModel(new TORFaithModel());
                gameStarterObject.AddModel(new TORCustomResourceModel());
                gameStarterObject.AddModel(new TORClanPoliticsModel());
                gameStarterObject.AddModel(new TORMapVisibilityModel());
                gameStarterObject.AddModel(new TORTournamentModel());
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
                gameStarterObject.AddModel(new TORVolunteerModel());
                gameStarterObject.AddModel(new TORSmithingModel());
                gameStarterObject.AddModel(new TOREnchantmentIngredientsModel());
                gameStarterObject.AddModel(new TORCompanionTrainingModel());
                gameStarterObject.AddModel(new TORVillageProductionCalculatorModel());
                gameStarterObject.AddModel(new TOREnchantmentCraftingModel());

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
            /*
            var missionCombatantsLogic = mission.GetMissionBehavior<MissionCombatantsLogic>();

            if (missionCombatantsLogic == null)
                return;

            mission.AddMissionLogicAtIndexOf(missionCombatantsLogic, TORMissionCombatantsLogic.CreateFromInstance(missionCombatantsLogic));
            */
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            var toRemove = mission.GetMissionBehavior<MissionGauntletCrosshair>();
            if (toRemove != null) mission.RemoveMissionBehavior(toRemove);

            mission.AddMissionBehavior(new StatusEffectMissionLogic());
            mission.AddMissionBehavior(new ExtendedInfoMissionLogic());
            mission.AddMissionBehavior(new AbilityManagerMissionLogic());
            mission.AddMissionBehavior(new AbilityHUDMissionView());
            mission.AddMissionBehavior(new CustomCrosshairMissionBehavior());
            mission.AddMissionBehavior(new WeaponHitScriptsMissionLogic());
            mission.AddMissionBehavior(new CustomBannerMissionLogic());
            mission.AddMissionBehavior(new DismembermentMissionLogic());
            mission.AddMissionBehavior(new MoraleMissionLogic());
            mission.AddMissionBehavior(new FirearmsMissionLogic());
            mission.AddMissionBehavior(new ForceAtmosphereMissionLogic());
            mission.AddMissionBehavior(new AnimationTriggerMissionLogic());
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

            if (Debugger.IsAttached)
            {
                mission.AddMissionBehavior(new TORAnimationLogger());
            }
        }
    }
}
