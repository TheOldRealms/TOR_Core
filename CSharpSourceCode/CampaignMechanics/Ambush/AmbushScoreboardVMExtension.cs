using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions.UI;
using TOR_Core.Missions.AmbushMission;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Scoreboard overlay for ambush missions. Adds two
    /// DataSourceProperties the overridden <c>SPScoreboard.xml</c>
    /// prefab reads to hide the "Defeat" banner and the Done button
    /// during the mid-mission phases (empty battlefield between
    /// reinforcement waves, Phase 1 patrol clear before waves, etc.),
    /// and to only surface the vanilla result + Done when the ambush
    /// has actually terminated.
    ///
    /// Terminal resolutions (Done + Victory/Defeat visible):
    ///  - AmbusherTotalVictory — ambusher cleared the field
    ///  - AmbusherDefeated (non-hero-down) — ambusher party wiped,
    ///    or Phase F player repelled the AI ambush
    ///  - VictimDefeated (non-hero-down) — victim party wiped
    ///
    /// Non-terminal resolutions exit via dedicated menus, not the
    /// scoreboard:
    ///  - AmbusherPartialVictory → scoreboard skipped via
    ///    Mission.EndMission (extraction path)
    ///  - AmbusherDefeated with AmbusherLeaderDown, Stalemate,
    ///    VictimFled, VictimDefeated with VictimLeaderDown → ambush
    ///    menu on campaign side; mid-mission scoreboard flash was
    ///    the original #82 bug.
    ///
    /// Vanilla ambush-less missions are unaffected: IsAmbushMode is
    /// false, both gates degenerate to vanilla behaviour.
    /// </summary>
    [ViewModelExtension(typeof(SPScoreboardVM), "OnTick")]
    public class AmbushScoreboardVMExtension : BaseViewModelExtension
    {
        private bool _isAmbushResultVisible = true;

        /// <summary>True when the scoreboard's result title + Done
        /// button should render. For non-ambush missions this is
        /// always true (vanilla pass-through). For ambush missions
        /// this is true only when the resolution is terminal
        /// (AmbusherTotalVictory, non-hero-down AmbusherDefeated,
        /// non-hero-down VictimDefeated) — the overridden
        /// <c>SPScoreboard.xml</c> prefab binds the title group and
        /// the Quit button's <c>IsVisible</c> to this so the
        /// mid-mission scoreboard flash and the mid-fight Done
        /// button both disappear.</summary>
        [DataSourceProperty]
        public bool IsAmbushResultVisible
        {
            get => _isAmbushResultVisible;
            set
            {
                if (value == _isAmbushResultVisible) return;
                _isAmbushResultVisible = value;
                _vm?.OnPropertyChangedWithValue(value, nameof(IsAmbushResultVisible));
            }
        }

        public AmbushScoreboardVMExtension(ViewModel vm) : base(vm) { }

        public override void RefreshValues()
        {
            base.RefreshValues();
            IsAmbushResultVisible = ComputeVisibility();
        }

        private static bool ComputeVisibility()
        {
            var mission = Mission.Current;
            if (mission == null) return true;

            bool isAmbush = mission.GetMissionBehavior<AmbushEndLogic>() != null
                            || mission.GetMissionBehavior<AmbushedEndLogic>() != null;
            if (!isAmbush) return true;

            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior == null) return false;

            var resolution = behavior.LastResolution;
            if (resolution == AmbushResolution.AmbusherTotalVictory) return true;
            if (resolution == AmbushResolution.AmbusherDefeated
                && !behavior.LastAmbusherLeaderDown) return true;
            if (resolution == AmbushResolution.VictimDefeated
                && !behavior.LastVictimLeaderDown) return true;
            return false;
        }
    }
}
