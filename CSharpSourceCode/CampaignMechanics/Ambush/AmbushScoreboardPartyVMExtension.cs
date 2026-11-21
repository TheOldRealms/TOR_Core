using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>Scoreboard row mask for ambushed missions. Masks name and counts on enemy rows until the troop type is wiped.</summary>
    [ViewModelExtension(typeof(SPScoreboardStatsVM), "UpdateScores")]
    public class AmbushScoreboardPartyVMExtension : BaseViewModelExtension
    {
        private bool _isNameMasked;
        private bool _isCountsMasked;

        [DataSourceProperty]
        public bool IsNameMasked
        {
            get => _isNameMasked;
            set
            {
                if (value == _isNameMasked) return;
                _isNameMasked = value;
                _vm?.OnPropertyChangedWithValue(value, nameof(IsNameMasked));
            }
        }

        [DataSourceProperty]
        public bool IsCountsMasked
        {
            get => _isCountsMasked;
            set
            {
                if (value == _isCountsMasked) return;
                _isCountsMasked = value;
                _vm?.OnPropertyChangedWithValue(value, nameof(IsCountsMasked));
            }
        }

        public AmbushScoreboardPartyVMExtension(ViewModel vm) : base(vm) { }

        public override void RefreshValues()
        {
            base.RefreshValues();
            var vm = _vm as SPScoreboardStatsVM;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (vm == null || model == null)
            {
                IsNameMasked = false;
                IsCountsMasked = false;
                return;
            }

            bool isOnPlayerSide = ResolveSideIsPlayer(vm);
            if (!model.ShouldMaskScoreboardRow(isOnPlayerSide))
            {
                IsNameMasked = false;
                IsCountsMasked = false;
                return;
            }

            IsNameMasked = vm.Dead == 0;
            IsCountsMasked = vm.Remaining > 0;
        }

        private static bool ResolveSideIsPlayer(SPScoreboardStatsVM vm)
        {
            if (vm == null) return false;
            var mission = Mission.Current;
            if (mission?.PlayerTeam == null) return false;

            // Party-aggregate row uses vanilla's IsMainParty flag.
            if (vm.IsMainParty) return true;

            var nameText = vm.NameText;
            if (string.IsNullOrEmpty(nameText)) return false;

            // Side-aggregate row: match NameText against the localised attacker/defender strings.
            var attackerSideName = GameTexts.FindText("str_battle_result_side", "attacker")?.ToString();
            if (!string.IsNullOrEmpty(attackerSideName) && nameText == attackerSideName)
                return mission.PlayerTeam.Side == BattleSideEnum.Attacker;
            var defenderSideName = GameTexts.FindText("str_battle_result_side", "defender")?.ToString();
            if (!string.IsNullOrEmpty(defenderSideName) && nameText == defenderSideName)
                return mission.PlayerTeam.Side == BattleSideEnum.Defender;

            // Unit row: match by Character.Name across mission teams.
            foreach (var team in mission.Teams)
            {
                if (team == null) continue;
                foreach (var agent in team.ActiveAgents)
                {
                    var name = agent?.Character?.Name?.ToString();
                    if (name != null && name == nameText)
                        return team == mission.PlayerTeam;
                }
            }

            // Fallback: unmatched rows are treated as enemy so the mask applies.
            return false;
        }
    }
}
