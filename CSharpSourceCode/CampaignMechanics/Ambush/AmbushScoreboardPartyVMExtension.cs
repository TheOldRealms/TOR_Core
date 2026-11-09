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
    /// <summary>
    /// Scoreboard per-row mask for Phase F ambushed. Hooks the Stats
    /// VM directly — the per-column widgets in
    /// <c>SPScoreboardUnit.xml</c> live in <c>{Score}</c> (Stats)
    /// scope, so the mask bools have to live on Stats itself; Gauntlet
    /// attribute bindings don't walk up to parent scopes.
    ///
    /// Two exposed flags:
    ///  - <see cref="IsNameMasked"/> = mask applies AND the type
    ///    hasn't been killed yet (<c>Dead == 0</c>). Row name reads
    ///    "?"; the Dead column already shows 0.
    ///  - <see cref="IsCountsMasked"/> = mask applies AND the type
    ///    still has alive troops (<c>Remaining &gt; 0</c>). Remaining
    ///    / Kill / Wounded / Routed read "?". Once the type is fully
    ///    wiped the mask lifts so the final tally shows.
    ///
    /// Side resolution: scan <see cref="Mission.Teams"/> for an
    /// active agent whose <c>Character.Name</c> matches the stats
    /// row's <see cref="NameText"/>. Fragile vs. multi-language / same
    /// troop on both sides, but the ambush scope guarantees distinct
    /// factions so collisions in practice are nil. If no match (unit
    /// wiped), returns false — treated as enemy by default, but the
    /// Wiped branch unmasks anyway.
    ///
    /// Yes/no delegates to
    /// <see cref="Models.TORAmbushModel.ShouldMaskScoreboardRow"/>.
    /// </summary>
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

            // Party-aggregate row: vanilla flips Score.IsMainParty to
            // true on the main party's SPScoreboardPartyVM (set inside
            // SPScoreboardPartyVM.GetUnitAddIfNotExists when the
            // PlayerTroop is added as a member). Reliable signal for
            // the player's own party row.
            if (vm.IsMainParty) return true;

            var nameText = vm.NameText;
            if (string.IsNullOrEmpty(nameText)) return false;

            // Side-aggregate row: vanilla builds SPScoreboardSideVM with
            // NameText = GameTexts.FindText("str_battle_result_side",
            // "attacker"/"defender"). Match against both localized
            // strings and compare to PlayerTeam.Side.
            var attackerSideName = GameTexts.FindText("str_battle_result_side", "attacker")?.ToString();
            if (!string.IsNullOrEmpty(attackerSideName) && nameText == attackerSideName)
                return mission.PlayerTeam.Side == BattleSideEnum.Attacker;
            var defenderSideName = GameTexts.FindText("str_battle_result_side", "defender")?.ToString();
            if (!string.IsNullOrEmpty(defenderSideName) && nameText == defenderSideName)
                return mission.PlayerTeam.Side == BattleSideEnum.Defender;

            // Unit row: scan mission for an active agent whose
            // Character.Name matches the stats row's NameText.
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

            // No match (e.g. enemy party aggregate row like "Ungor
            // Warband" — no character has that name, IsMainParty is
            // false because PlayerTroop isn't in Members). Treat as
            // enemy side so the mask applies.
            return false;
        }
    }
}
