using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// ViewModel for the §19 ambush-prep overlay. Mirrors the shape of
    /// vanilla's <c>MapSiegeVM</c> prep slider (PreparationProgress /
    /// IsPreparationsCompleted / PreparationTitleText) so the same
    /// prefab vocabulary carries over — our XML copies the vanilla
    /// MapSiegeOverlay.xml "Preparation Slider" block verbatim.
    ///
    /// <see cref="Update"/> is called from
    /// <see cref="AmbushPrepOverlayMapView.OnMapScreenUpdate"/> per
    /// frame; it pulls the live prep state from
    /// <see cref="AmbushCampaignBehavior"/>.
    /// </summary>
    public class AmbushPrepOverlayVM : ViewModel
    {
        private float _preparationProgress;
        private bool _isPreparationsCompleted = true;

        [DataSourceProperty]
        public float PreparationProgress
        {
            get => _preparationProgress;
            set
            {
                if (value == _preparationProgress) return;
                _preparationProgress = value;
                OnPropertyChangedWithValue(value, nameof(PreparationProgress));
            }
        }

        [DataSourceProperty]
        public bool IsPreparationsCompleted
        {
            get => _isPreparationsCompleted;
            set
            {
                if (value == _isPreparationsCompleted) return;
                _isPreparationsCompleted = value;
                OnPropertyChangedWithValue(value, nameof(IsPreparationsCompleted));
            }
        }

        public void Update()
        {
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior == null || !behavior.IsPlayerPreppingStealth)
            {
                // Hide the overlay — the prefab's ListPanel has
                // IsHidden="@IsPreparationsCompleted".
                IsPreparationsCompleted = true;
                return;
            }

            var model = Campaign.Current.Models?.GetAmbushModel();
            float totalMinutes = model?.CalculatePreparationMinutes(MobileParty.MainParty) ?? 15f;
            if (totalMinutes <= 0f) totalMinutes = 1f;

            float elapsed = (float)(CampaignTime.Now - behavior.PlayerStealthPrepStartedAt).ToMinutes;
            float frac = System.Math.Max(0f, System.Math.Min(1f, elapsed / totalMinutes));

            PreparationProgress = frac;
            IsPreparationsCompleted = false;
        }
    }
}
