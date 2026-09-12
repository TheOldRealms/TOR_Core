using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Framework
{
    /// <summary>
    /// Registration class for module. We do this so every module is self-contained.
    /// </summary>
    public interface ITORModule
    {
        /// <summary>Template/XML loading, Harmony setup specific to this module, etc. Called from SubModule.OnSubModuleLoad.</summary>
        void OnSubModuleLoad();

        /// <summary>starter.AddBehavior(...) for every CampaignBehaviorBase this module owns. Called from SubModule.InitializeGameStarter.</summary>
        void RegisterCampaignBehaviors(CampaignGameStarter starter);

        /// <summary>gameStarterObject.AddModel(...) for every GameModel this module owns. Called from SubModule.OnGameStart.</summary>
        void RegisterModels(IGameStarter gameStarterObject);

        /// <summary>mission.AddMissionBehavior(...) for every mission behavior this module owns. Called from SubModule.OnMissionBehaviorInitialize.</summary>
        void RegisterMissionBehaviors(Mission mission);

        /// <summary>game.ObjectManager.RegisterType<Blah>(...) for every custom object type this module owns. Called from SubModule.BeginGameStart.</summary>
        void RegisterGameObjectTypes(Game game);
    }
}
