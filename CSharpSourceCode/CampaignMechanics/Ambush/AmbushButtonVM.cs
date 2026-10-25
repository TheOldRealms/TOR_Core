using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Ambush
{
    public class AmbushButtonVM : ViewModel
    {
        private const string EyeSpritePrefix = "ambush_eye_";
        private const int EyeFullyExposedIndex = 9;    // wide open: no stealth / fully visible
        private const int EyeFullyCoveredIndex = 0;    // closed: perfectly hidden

        private bool _isVisible = true;
        private bool _isStealthActive;
        private bool _isOnCooldown;
        private string _eyeSprite = EyeSpritePrefix + EyeFullyExposedIndex;
        private BasicTooltipViewModel _hint;

        public AmbushButtonVM()
        {
            _hint = new BasicTooltipViewModel(ComputeHintText);
            Refresh();
        }

        private string ComputeHintText()
        {
            if (_isStealthActive) return "Break cover (X)";
            if (_isOnCooldown) return "Cooling down";
            return "Enter stealth stance (X)";
        }

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value) return;
                _isVisible = value;
                OnPropertyChangedWithValue(value);
            }
        }

        [DataSourceProperty]
        public bool IsStealthActive
        {
            get => _isStealthActive;
            set
            {
                if (_isStealthActive == value) return;
                _isStealthActive = value;
                OnPropertyChangedWithValue(value);
            }
        }

        [DataSourceProperty]
        public bool IsOnCooldown
        {
            get => _isOnCooldown;
            set
            {
                if (_isOnCooldown == value) return;
                _isOnCooldown = value;
                OnPropertyChangedWithValue(value);
            }
        }

        [DataSourceProperty]
        public string EyeSprite
        {
            get => _eyeSprite;
            set
            {
                if (_eyeSprite == value) return;
                _eyeSprite = value;
                OnPropertyChangedWithValue(value);
            }
        }

        [DataSourceProperty]
        public BasicTooltipViewModel Hint
        {
            get => _hint;
            set
            {
                if (_hint == value) return;
                _hint = value;
                OnPropertyChangedWithValue(value);
            }
        }

        public void ExecuteToggle()
        {
            Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>()?.OnStealthHotkeyPressed();
        }

        public void Refresh()
        {
            var ambush = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (ambush == null)
            {
                IsVisible = false;
                return;
            }

            IsVisible = CanShow();
            IsStealthActive = ambush.IsPlayerInStealth;
            IsOnCooldown = ambush.IsOnAmbushCooldown;
            EyeSprite = ComputeEyeSprite(ambush);

            // BasicTooltipViewModel reads its text from the delegate on hover; no imperative set.
        }

        private static string ComputeEyeSprite(AmbushCampaignBehavior ambush)
        {
            // Not stealthed → wide-open eye (player is exposed by default).
            if (!ambush.IsPlayerInStealth) return EyeSpritePrefix + EyeFullyExposedIndex;

            var mainParty = MobileParty.MainParty;
            var model = Campaign.Current?.Models?.GetAmbushModel();

            // No model registered or no observers nearby → treat as fully covered.
            if (mainParty == null) return EyeSpritePrefix + EyeFullyExposedIndex;
            var targets = ambush.GetAmbushTargets(mainParty);
            if (model == null || targets.Count == 0) return EyeSpritePrefix + EyeFullyCoveredIndex;

            // Pick the player's best ambush opportunity across all nearby targets — higher chance
            // = more covered = more closed eye.
            float maxChance = 0f;
            foreach (var target in targets)
            {
                float chance = model.CalculateAmbushSuccessChance(mainParty, target, ambush.StealthEnteredAt);
                if (chance > maxChance) maxChance = chance;
            }

            // Chance clamped to [MinChance, MaxChance] in model (~0.1 .. 0.95).
            // Map (1 - chance) to [0, 9]: chance=0.95 → idx 0 (closed), chance=0.10 → idx 9 (open).
            int idx = (int)((1f - maxChance) * 10f);
            if (idx < EyeFullyCoveredIndex) idx = EyeFullyCoveredIndex;
            if (idx > EyeFullyExposedIndex) idx = EyeFullyExposedIndex;
            return EyeSpritePrefix + idx;
        }

        private static bool CanShow()
        {
            if (Hero.MainHero == null || Hero.MainHero.IsPrisoner) return false;
            if (TaleWorlds.MountAndBlade.Mission.Current != null) return false;
            if (MobileParty.MainParty == null || !MobileParty.MainParty.IsActive) return false;
            if (MobileParty.MainParty.CurrentSettlement != null) return false;
            if (MapEvent.PlayerMapEvent != null) return false;
            return true;
        }
    }
}
