using SandBox;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.CharacterCreation;

namespace TOR_Core.GameManagers
{
    class TORCampaignGameManager : SandBoxGameManager
    {
        public override void OnLoadFinished()
        {
            LaunchCharacterCreation();
            IsLoaded = true;
        }

        private void LaunchCharacterCreation()
        {
            CharacterCreationState gameState = Game.Current.GameStateManager.CreateState<CharacterCreationState>(new object[]
            {
                new TORCharacterCreationContent()
            });

            Game.Current.GameStateManager.CleanAndPushState(gameState, 0);
        }
    }
}
