using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>Draws a strike-range ring on the campaign map while the player is in stealth.</summary>
    public class AmbushRangeMapView : MapView
    {
        // Vanilla hover-ring material.
        private const string RingMaterial = "map_circle_decal";

        private GameEntity _ringEntity;
        private Decal _ringDecal;
        // Ring mesh natural radius; scale = strikeRange / BaseMeshRadius.
        private const float BaseMeshRadius = 1f;
        // Sky blue, ARGB.
        private const uint RingColor = 0xFF0088FFu;

        protected override void OnFinalize()
        {
            if (_ringEntity != null)
            {
                _ringEntity.Remove(0);
                _ringEntity = null;
            }
            _ringDecal = null;
            base.OnFinalize();
        }

        protected override void OnMapScreenUpdate(float dt)
        {
            base.OnMapScreenUpdate(dt);

            var scene = (Campaign.Current?.MapSceneWrapper as MapScene)?.Scene;
            if (scene == null) return;

            EnsureRingCreated(scene);
            if (_ringEntity == null) return;

            var behavior = Campaign.Current.GetCampaignBehavior<AmbushCampaignBehavior>();
            var main = MobileParty.MainParty;
            bool shouldShow = behavior != null && behavior.IsPlayerInStealth
                              && main != null && main.IsActive;

            _ringEntity.SetVisibilityExcludeParents(shouldShow);
            if (!shouldShow) return;

            var model = Campaign.Current.Models.GetAmbushModel();
            if (model == null) return;
            float range = model.CalculateStrikeRange(main);

            var pos = main.Position.ToVec2();
            float z = scene.GetGroundHeightAtPosition(new Vec3(pos.x, pos.y, 0f));
            // Identity then uniform scale, same pattern as BattleWreckagesVisualManager.
            var frame = MatrixFrame.Identity;
            frame.origin = new Vec3(pos.x, pos.y, z);
            var scaleVec = Vec3.One * (range / BaseMeshRadius);
            frame.Scale(ref scaleVec);
            _ringEntity.SetGlobalFrame(frame);

            // Re-set shader state each frame; the decal pipeline resets uniforms between passes.
            _ringDecal.SetFactor1Linear(RingColor);
            _ringDecal.SetVectorArgument(0.166f, 1f, 0.83f, 0f);
        }

        private void EnsureRingCreated(Scene scene)
        {
            if (_ringEntity != null) return;
            _ringEntity = GameEntity.CreateEmpty(scene, true, true, true);
            _ringEntity.Name = "TOR_AmbushRangeRing";
            _ringDecal = Decal.CreateDecal(null);
            var material = Material.GetFromResource(RingMaterial);
            if (material != null) _ringDecal.SetMaterial(material);
            scene.AddDecalInstance(_ringDecal, "editor_set", false);
            _ringEntity.AddComponent(_ringDecal);
            _ringEntity.SetVisibilityExcludeParents(false);
        }
    }
}
