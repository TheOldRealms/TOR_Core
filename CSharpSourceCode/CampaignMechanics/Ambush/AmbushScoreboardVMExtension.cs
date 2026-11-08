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
    /// Terminal outcomes (Done + Victory/Defeat visible):
    ///  - Phase D TotalVictory — ambusher cleared the field
    ///  - Phase D Defeated     — ambusher party wiped (rare)
    ///  - Phase F AmbushRepelled — ambushed repelled the attack
    ///  - Phase F AmbushedWiped  — ambushed party wiped
    ///
    /// Non-terminal outcomes exit via dedicated menus, not the
    /// scoreboard:
    ///  - Extracted → scoreboard is skipped entirely via Mission.EndMission
    ///  - HeroWounded / Stalemate / AmbushedFled → ambush menu on
    ///    campaign side; mid-mission scoreboard flash was the
    ///    original #82 bug.
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
        /// this is true only when the outcome is terminal
        /// (TotalVictory, Defeated, AmbushRepelled, AmbushedWiped) —
        /// the overridden <c>SPScoreboard.xml</c> prefab binds the
        /// title group and the Quit button's <c>IsVisible</c> to
        /// this so the mid-mission scoreboard flash and the
        /// mid-fight Done button both disappear.</summary>
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

            return behavior.LastAmbushOutcome == AmbushOutcome.TotalVictory
                || behavior.LastAmbushOutcome == AmbushOutcome.Defeated
                || behavior.LastAmbushedOutcome == AmbushedOutcome.AmbushRepelled
                || behavior.LastAmbushedOutcome == AmbushedOutcome.AmbushedWiped;
        }
    }
}
