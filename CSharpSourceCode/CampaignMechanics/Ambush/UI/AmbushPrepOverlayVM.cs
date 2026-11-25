using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Ambush.UI
{
    /// <summary>
    /// VM for the ambush prep overlay. Drives a PreparationProgress bar shaped like the siege prep slider.
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

        /// <summary>
        /// Recompute the prep bar from live stealth state. Called per-frame.
        /// </summary>
        public void Update()
        {
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior == null || !behavior.IsPlayerPreppingStealth)
            {
                // Hide via the prefab's IsHidden binding.
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
