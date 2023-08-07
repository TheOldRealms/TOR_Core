using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TOR_Core.BattleMechanics.TriggeredEffect;

namespace TOR_Core.BattleMechanics.Artillery
{
    public class ArtilleryMissionBehavior : MissionLogic
    {
        public override void OnMissionTick(float dt)
        {
            if (Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle && Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.Siege)
                return;
            if (Mission.Current.MainAgent == null || Agent.Main.Controller == Agent.ControllerType.AI)
            {
                
                var missionScreen = (ScreenManager.TopScreen as MissionScreen);
                if (missionScreen.IsDeploymentActive)
                {
                    // ScreenManager.TopScreen.AddComponent();
                }
                
                
                if (!Input.IsKeyReleased(InputKey.B))
                    return;
                
              
                var orderFlag = missionScreen.OrderFlag;
                if (orderFlag.IsVisible)
                {
                    MatrixFrame orderFlagFrame = missionScreen.GetOrderFlagFrame();
                    ArtilleryCreate(orderFlagFrame, Mission.Current.MainAgent);
                }
            }
        }

        private void ArtilleryCreate(MatrixFrame userFrame, Agent triggerer)
        {
            var foo = TriggeredEffectManager.CreateNew("place_greatcannon");

            foo.Trigger(userFrame.origin, Vec3.One, triggerer);
        }
    }
}
