using System;
using SandBox;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem.Load;
using TOR_Core.CampaignMechanics.CharacterCreation;
using TOR_Core.Utilities;

namespace TOR_Core.GameManagers
{
    
    // Native 1.3.1: Handler registration moved to SubModule.cs via event subscription
    class TorCampaignGameManager : SandBoxGameManager
    {
        public TorCampaignGameManager(CampaignCreatorDelegate campaignCreator) : base(campaignCreator)
        {

        }

        public TorCampaignGameManager(LoadResult loadedGameResult) : base(loadedGameResult)
        {
        }

        public override void OnLoadFinished()
        {
            LaunchCharacterCreation();
            IsLoaded = true;
        }

        private void LaunchCharacterCreation()
        {
            // NEW 1.3.1: Subscribe to CampaignEvents to register our handler
            // This happens just before CharacterCreationState creation, which broadcasts the event
            TaleWorlds.CampaignSystem.CampaignEvents.OnCharacterCreationInitializedEvent.AddNonSerializedListener(
                (object)this,
                new Action<CharacterCreationManager>(OnCharacterCreationInitialized));

            // NEW 1.3.1: Parameterless constructor - handler registration happens via event
            CharacterCreationState gameState = Game.Current.GameStateManager.CreateState<CharacterCreationState>();
            Game.Current.GameStateManager.CleanAndPushState(gameState, 0);
        }

        private void OnCharacterCreationInitialized(CharacterCreationManager manager)
        {
            // Register TOR's character creation handler with priority 0 (default)
            manager.RegisterCharacterCreationContentHandler(new TorCharacterCreationContentHandler(), 0);
            TORCommon.Log("[TOR Campaign] Registered TORCharacterCreationContentHandler", NLog.LogLevel.Info);
        }
    }
}
