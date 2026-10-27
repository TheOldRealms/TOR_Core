using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Strike-range visualization — draws a ring on the campaign map while
    /// the player is in stealth, scaled to the current
    /// <c>TORAmbushModel.CalculateStrikeRange(MainParty)</c>.
    ///
    /// Uses the same decal material (<c>map_circle_decal</c>) vanilla uses
    /// for the yellow hover-highlight ring around a party. Pattern lifted
    /// from <c>SandBox.View.Map.MapCursor</c>: create one GameEntity with
    /// a Decal child at session start, then per frame reposition / rescale
    /// / toggle visibility. No per-frame allocation.
    /// </summary>
    public class AmbushRangeDebugMapView : MapView
    {
        // Vanilla hover-ring material. Used by MapCursor's
        // _partyHoverOutlineDecal for the same visual.
        private const string RingMaterial = "map_circle_decal";

        private GameEntity _ringEntity;
        private Decal _ringDecal;
        // The map_circle_decal mesh is natively party-icon-sized (vanilla
        // uses it at scale ~1-3 for hover rings around a single party /
        // battle wreckage; see SandBox.View BattleWreckagesVisualManager
        // and MapScreen._partyHoverOutlineDecal). We want the ring's
        // rendered radius to approximate the strike range in map units:
        //   scale = strikeRange / BaseMeshRadius
        // With BaseMeshRadius=1 the ring radius scales linearly with
        // strike range. Bump UP to shrink, DOWN to grow if the mesh's
        // natural radius turns out to be bigger / smaller than ~1u.
        private const float BaseMeshRadius = 1f;
        // Sky blue — stands out clearly against the yellow / green
        // terrain and distinguishes the stealth range from vanilla's
        // yellow hover ring.
        // Factor1Linear is an ARGB uint: 0xFF0088FF = alpha 255,
        // red 0, green 136, blue 255.
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
            // Vanilla idiom: start from Identity, then uniform-scale.
            // Matches BattleWreckagesVisualManager.OnVisualTick, which
            // places map_circle_decal rings on wreckages the same way.
            var frame = MatrixFrame.Identity;
            frame.origin = new Vec3(pos.x, pos.y, z);
            var scaleVec = Vec3.One * (range / BaseMeshRadius);
            frame.Scale(ref scaleVec);
            _ringEntity.SetGlobalFrame(frame);

            // Shader state has to be re-set each frame — the decal
            // pipeline resets its uniforms between passes. SetVectorArgument
            // (0.166, 1, 0.83, 0) draws a THIN ring; SetFactor1Linear
            // tints it. Vanilla's MapCursor does the same in its own
            // per-frame update.
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
