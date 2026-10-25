using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;

namespace TOR_Core.CampaignMechanics.Ambush
{
    public class AmbushMapView : MapView
    {
        protected override void OnMapScreenUpdate(float dt)
        {
            base.OnMapScreenUpdate(dt);
            // OnMapScreenUpdate fires unconditionally from MapScreen.OnFrameTick (SandBox.View.dll:1368),
            // so this one hook covers both free-map and in-menu states. Don't also hook OnMenuModeTick —
            // both fire on the same frame when a menu is open, which would read the key twice.
            HandleInput();
        }

        private static void HandleInput()
        {
            if (!Input.IsKeyReleased(AmbushCampaignBehavior.StealthToggleKey)) return;
            Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>()?.OnStealthHotkeyPressed();
        }
    }
}
