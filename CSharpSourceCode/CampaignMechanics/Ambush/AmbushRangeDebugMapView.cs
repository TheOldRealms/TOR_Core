using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Debug visualization — draws the player's current ambush strike range
    /// as a ring on the campaign map while in stealth stance. Uses
    /// <see cref="DebugExtensions.RenderDebugCircleOnTerrain"/>, which is
    /// a direct native binding (not stripped like the manager-level
    /// RenderDebugSphere).
    ///
    /// Intended as a tuning aid while we calibrate the Scouting /
    /// strike-range curve. The full visual (proper mesh decal matching
    /// vanilla's settlement circles) is a planned follow-up — see
    /// campaign-stealth-design.md § 16.
    /// </summary>
    public class AmbushRangeDebugMapView : MapView
    {
        private const uint RingColor = 0xFFFFCC00u;    // amber

        protected override void OnMapScreenUpdate(float dt)
        {
            base.OnMapScreenUpdate(dt);

            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior == null || !behavior.IsPlayerInStealth) return;

            var main = MobileParty.MainParty;
            if (main == null || !main.IsActive) return;

            var model = Campaign.Current.Models.GetAmbushModel();
            if (model == null) return;

            var scene = (Campaign.Current.MapSceneWrapper as MapScene)?.Scene;
            if (scene == null) return;

            float range = model.CalculateStrikeRange(main);
            var pos = main.Position.ToVec2();
            var frame = new MatrixFrame(Mat3.Identity, new Vec3(pos.x, pos.y, 0f));
            DebugExtensions.RenderDebugCircleOnTerrain(scene, frame, range, RingColor,
                depthCheck: true, isDotted: false);
        }
    }
}
